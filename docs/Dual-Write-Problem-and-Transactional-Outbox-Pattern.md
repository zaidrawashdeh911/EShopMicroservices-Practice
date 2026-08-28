# Dual-Write Problem and Transactional Outbox Pattern

> Beginner-friendly notes for the EShop Microservices course

## 1. The problem in one sentence

The **dual-write problem** happens when one business operation must write to two independent systems, but those two writes cannot be committed as one reliable transaction.

In a microservices application, the two systems are often:

1. The service's database.
2. A message broker such as RabbitMQ.

For example, imagine that the Ordering service creates an order and then publishes an `OrderCreatedIntegrationEvent`:

```text
Ordering service
      |
      +--> Write the new order to SQL Server
      |
      +--> Publish OrderCreatedIntegrationEvent to RabbitMQ
```

These are two separate writes. SQL Server and RabbitMQ do not normally share the same database transaction.

## 2. Why two normal writes are unsafe

Consider this straightforward implementation:

```csharp
await dbContext.SaveChangesAsync();
await publishEndpoint.Publish(new OrderCreatedIntegrationEvent(...));
```

Several outcomes are possible:

| Database write | RabbitMQ publish | Result |
|---|---|---|
| Succeeds | Succeeds | Everything is consistent |
| Succeeds | Fails | The order exists, but other services never receive the event |
| Fails | Is not attempted | No order and no event, which is consistent |
| Publish happens first | Database later fails | Other services receive an event for an order that does not exist |

The dangerous moment is the gap between the two operations:

```text
1. Save order to SQL Server       SUCCESS
2. Application crashes            FAILURE
3. Publish event to RabbitMQ       NEVER HAPPENS
```

The Ordering service now contains the order, but a service waiting for the event knows nothing about it.

Reversing the order does not fix the problem:

```text
1. Publish event to RabbitMQ       SUCCESS
2. Save order to SQL Server        FAILURE
```

Now another service may act on an order that was never saved. This is sometimes called a **phantom event**.

The important lesson is:

> Changing which write happens first only changes the kind of inconsistency that can occur.

## 3. Why a normal database transaction is not enough

A database transaction can make multiple writes inside one database atomic:

```text
BEGIN TRANSACTION
    INSERT INTO Orders ...
    INSERT INTO OrderItems ...
COMMIT
```

Either all of those database changes commit, or all of them roll back.

However, this does not automatically include RabbitMQ:

```text
SQL Server transaction  X  RabbitMQ publish
```

They are separate systems with separate failure boundaries. Calling `SaveChangesAsync()` and `Publish()` from the same C# method does not make them one atomic transaction.

## 4. The transactional outbox solution

The **Transactional Outbox Pattern** removes the RabbitMQ publish from the original business transaction.

Instead, the service writes two records to its own database:

1. The business data, such as the new `Order`.
2. An outbox message describing the integration event that must eventually be published.

Because both records are stored in the same database, they can be committed in the same local transaction:

```text
Ordering database transaction
      |
      +--> INSERT INTO Orders
      |
      +--> INSERT INTO OutboxMessages
```

The result is all-or-nothing:

```text
Both writes succeed  --> COMMIT
Either write fails   --> ROLLBACK both
```

This gives the service a durable record of its intention to publish the event.

## 5. What is stored in the outbox table?

The slides describe the outbox row as a second copy of the data. More precisely, it is normally a **message representation**, not a duplicate of the complete business record.

An outbox table may look conceptually like this:

| Column | Meaning |
|---|---|
| `Id` | Unique identifier for this message |
| `Type` | Message contract name, such as `OrderCreatedIntegrationEvent` |
| `Content` | Serialized event payload, often JSON |
| `OccurredOn` | When the business event happened |
| `ProcessedOn` | When the message was successfully published; initially `NULL` |
| `Error` | Optional publishing error information |

Example outbox row:

```json
{
  "id": "8bd6...",
  "type": "OrderCreatedIntegrationEvent",
  "content": {
    "orderId": "2b61...",
    "customerId": "7f42...",
    "totalPrice": 125.50
  },
  "occurredOn": "2026-08-28T10:30:00Z",
  "processedOn": null
}
```

The `Orders` table is the source of business state. The `OutboxMessages` table is a durable list of messages waiting to be delivered.

## 6. The two phases of the pattern

### Phase 1: Save the business data and message

```text
Create order command
        |
        v
Begin SQL transaction
        |
        +--> Save Order
        |
        +--> Save OutboxMessage
        |
        v
Commit SQL transaction
```

At the end of this phase, one of two things is true:

- Neither the order nor the outbox message exists.
- Both the order and the outbox message exist.

There is no state where the order commits but the service forgets that it must publish an event.

### Phase 2: Publish the saved message

A separate background process reads unprocessed outbox rows and publishes them:

```text
OutboxMessages table
        |
        | Read pending messages
        v
Background worker / MassTransit outbox delivery service
        |
        | Publish
        v
RabbitMQ
        |
        v
Consumer service
```

After RabbitMQ accepts the message, the outbox process marks it as processed or removes it according to the implementation's retention policy.

This publisher may use:

- Periodic polling.
- A background hosted service.
- Change-data capture.
- A library implementation such as MassTransit's Entity Framework transactional outbox.

## 7. What happens if RabbitMQ is unavailable?

Suppose the order and outbox row commit successfully, but RabbitMQ is down:

```text
Orders table:          Order #123 exists
OutboxMessages table:  OrderCreated message is pending
RabbitMQ:              Unavailable
```

The message is not lost. It remains in the Ordering database. The delivery process retries it when RabbitMQ becomes available again:

```text
RabbitMQ returns
      |
      v
Outbox publisher retries pending message
      |
      v
Message reaches RabbitMQ
```

This is the reliability benefit of the pattern: the intent to publish survives application restarts and broker outages.

## 8. Does the outbox guarantee exactly-once delivery?

No. A common outbox implementation provides **at-least-once delivery**.

Consider this failure:

```text
1. Outbox publisher sends message to RabbitMQ       SUCCESS
2. Application crashes before marking it processed FAILURE
3. Application restarts
4. The same outbox row still appears pending
5. The message is published again
```

The consumer may therefore receive the same logical message more than once.

Consumers should be **idempotent**, meaning processing the same message again does not produce an incorrect duplicate result.

Typical techniques include:

- Give every message a unique `MessageId`.
- Store consumed message IDs in an inbox table.
- Ignore a message whose ID was already processed.
- Use business constraints, such as a unique order ID, to prevent duplicate records.

The practical model is:

```text
Outbox on producer  +  idempotent consumer  =  reliable eventual processing
```

## 9. How this relates to this EShop project

The course flow begins with the Basket service publishing `BasketCheckoutEvent` and the Ordering service consuming it:

```text
Customer checks out
        |
        v
Basket service
        |
        | BasketCheckoutEvent
        v
MassTransit
        |
        v
RabbitMQ
        |
        v
Ordering consumer
        |
        v
Create Order in SQL Server
```

There are two different reliability questions here.

### Basket as the producer

Basket changes checkout-related state and publishes `BasketCheckoutEvent`. If both actions must remain consistent, an outbox can ensure the event is not lost after the state change commits.

### Ordering as the consumer

Ordering consumes `BasketCheckoutEvent` and creates an order. The consumer should tolerate message redelivery so the same checkout does not create multiple orders.

If Ordering later publishes an `OrderCreatedIntegrationEvent`, it has another dual-write problem:

```text
Ordering must:
1. Save the Order to SQL Server.
2. Publish OrderCreatedIntegrationEvent to RabbitMQ.
```

An Ordering outbox would save the order and the outgoing message in the same EF Core transaction, then publish the message afterward.

## 10. Domain event, integration event, and outbox

These concepts have different jobs:

| Concept | Job |
|---|---|
| Domain event | Describes an important fact inside one domain, such as an order being created |
| Integration event | Message contract sent to another microservice |
| Outbox message | Durable database representation of an integration event waiting to be published |
| RabbitMQ | Broker that routes and stores published messages |
| MassTransit | .NET library that connects application code to RabbitMQ and can manage outbox behavior |

A possible flow is:

```text
Order aggregate creates OrderCreatedDomainEvent
                    |
                    v
Internal handler creates OrderCreatedIntegrationEvent
                    |
                    v
Integration event is saved as an OutboxMessage
                    |
                    v
Outbox delivery process publishes through MassTransit
                    |
                    v
RabbitMQ delivers it to interested services
```

The exact code can differ, but keeping these responsibilities separate makes the architecture easier to understand.

## 11. Simplified EF Core pseudocode

The following code demonstrates the idea; it is not necessarily the exact implementation currently used by this project:

```csharp
await using var transaction =
    await dbContext.Database.BeginTransactionAsync();

var order = Order.Create(...);
dbContext.Orders.Add(order);

var integrationEvent = new OrderCreatedIntegrationEvent(
    order.Id.Value,
    order.CustomerId.Value,
    order.TotalPrice);

dbContext.OutboxMessages.Add(
    OutboxMessage.From(integrationEvent));

await dbContext.SaveChangesAsync();
await transaction.CommitAsync();
```

Notice that this request does **not** need to publish directly to RabbitMQ. A separate process handles delivery:

```csharp
var pendingMessages = await dbContext.OutboxMessages
    .Where(message => message.ProcessedOn == null)
    .ToListAsync();

foreach (var message in pendingMessages)
{
    await publishEndpoint.Publish(message.Deserialize());
    message.MarkAsProcessed();
}

await dbContext.SaveChangesAsync();
```

Real implementations must also handle concurrency, retries, ordering, cleanup, serialization, and duplicate delivery. Libraries such as MassTransit can handle much of this infrastructure.

## 12. What the outbox pattern does and does not solve

### It does solve

- Losing an outgoing event after the business database transaction commits.
- Depending on RabbitMQ being available during the original request.
- The need to atomically save business state and the intention to publish.

### It does not solve by itself

- Duplicate message delivery.
- Idempotency in consumers.
- Incorrect event contracts.
- Message ordering across every service.
- A consumer's own database and processing failures.
- Immediate consistency between all microservices.

Microservices using an outbox are usually **eventually consistent**: the database commits first, and other services learn about the change shortly afterward.

## 13. Short mental model

Without an outbox:

```text
Save to database + publish to RabbitMQ
        = two independent writes
        = dangerous failure gap
```

With an outbox:

```text
Save business data + save outgoing message
        = one database transaction

Later:
Outbox worker publishes saved message to RabbitMQ
```

The simplest analogy is:

- The business table records what happened.
- The outbox table is a reliable **to-send list**.
- The background publisher is the delivery worker.
- RabbitMQ is the post office.
- Idempotency prevents the receiver from processing the same package twice.

The key sentence to remember is:

> The outbox pattern does not make SQL Server and RabbitMQ share one transaction; it replaces the unreliable broker write with a reliable database record that can be published later.
