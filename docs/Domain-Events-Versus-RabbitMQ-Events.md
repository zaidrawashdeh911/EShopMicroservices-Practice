# Domain Events, Integration Events, RabbitMQ, and MassTransit

> Beginner-friendly notes for the EShop Microservices course

## 1. The main distinction

Both domain events and integration events say that something happened, but they communicate across different boundaries.

- A **domain event** says: "Something important happened inside this service's domain."
- An **integration event** says: "Something happened in this service that another service may need to know about."

The important question is therefore not merely "Is this an event?" It is:

> Is the event staying inside one microservice, or crossing from one microservice to another?

## 2. Domain events

A domain event represents a business fact that occurred inside one domain or microservice.

Examples in the Ordering service:

- `OrderCreatedDomainEvent`
- `OrderItemAddedDomainEvent`
- `OrderCancelledDomainEvent`

Domain events are normally named in the past tense because they describe something that has already happened.

In this project, a domain event implements `IDomainEvent`, which inherits from MediatR's `INotification`:

```csharp
public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    DateTime OccurredOn => DateTime.Now;
    string EventType => GetType().AssemblyQualifiedName;
}
```

An aggregate such as `Order` records domain events:

```csharp
AddDomainEvent(new OrderCreatedDomainEvent(this));
```

MediatR can then publish the event to one or more handlers inside the Ordering microservice:

```text
Order aggregate
      |
      | OrderCreatedDomainEvent
      v
    MediatR
      |
      +--> Handler A: update something inside Ordering
      +--> Handler B: write an audit record
      +--> Handler C: prepare an integration event
```

Domain events are in-process messages. MediatR does not provide a durable external queue. If the application is not running, MediatR cannot hold events for later processing.

## 3. Integration events

An integration event communicates between separate microservices.

Examples:

- Basket publishes `BasketCheckoutEvent` for Ordering.
- Ordering could publish `OrderCreatedIntegrationEvent` for another service.

Integration events normally travel through a message broker:

```text
Basket service
      |
      | BasketCheckoutEvent
      v
   RabbitMQ
      |
      v
Ordering service
```

The publisher does not directly call the consumer. It sends a message to the broker, and the consumer processes it asynchronously.

## 4. Domain event versus integration event

| Domain event | Integration event |
|---|---|
| Communicates inside one microservice | Communicates between microservices |
| Usually published with MediatR | Usually published through MassTransit and RabbitMQ |
| In-process | Out-of-process/network communication |
| Not stored in an external durable queue by MediatR | Can remain in a RabbitMQ queue until consumed |
| Can contain domain-oriented details | Should contain a stable message contract for other services |
| Example: `OrderCreatedDomainEvent` | Example: `OrderCreatedIntegrationEvent` |

A domain-event handler can create an integration event, but the two events should remain conceptually separate because they have different audiences and reliability requirements.

## 5. What is a message broker?

A **message broker** is infrastructure that receives, routes, stores, and delivers messages between applications.

In this course, **RabbitMQ is the message broker**.

Instead of Basket calling Ordering directly:

```text
Basket ----------> Ordering
```

Basket sends a message through RabbitMQ:

```text
Basket ---> RabbitMQ ---> Ordering
```

This reduces direct coupling. Basket does not need to know Ordering's HTTP endpoint or implementation.

RabbitMQ can also hold messages while Ordering is unavailable:

```text
Basket ---> RabbitMQ queue
              [Checkout #123]
              [Checkout #124]
              [Checkout #125]

Ordering is temporarily down
```

When Ordering returns, it can continue consuming the queued messages.

Important RabbitMQ concepts:

- **Producer/publisher:** sends a message.
- **Exchange:** receives a published message and decides where to route it.
- **Queue:** stores messages until a consumer can process them.
- **Consumer:** receives and processes messages from a queue.
- **Acknowledgement:** tells RabbitMQ that processing succeeded so the message can be removed.

## 6. What is MassTransit?

**MassTransit is a .NET messaging library.** It makes it easier for C# applications to work with brokers such as RabbitMQ.

RabbitMQ and MassTransit are different layers:

```text
Your C# producer
       |
       v
   MassTransit
       |
       v
    RabbitMQ
       |
       v
   MassTransit
       |
       v
Your C# consumer
```

- **RabbitMQ** is the running server/infrastructure that transports and stores messages.
- **MassTransit** is the C# library that configures producers, consumers, queues, serialization, retries, and acknowledgements.

Without MassTransit, the application would use the lower-level RabbitMQ client and manually handle more infrastructure details. MassTransit provides a higher-level API.

Conceptual publishing code:

```csharp
await publishEndpoint.Publish(new BasketCheckoutEvent(...));
```

Conceptual consumer:

```csharp
public class BasketCheckoutEventConsumer
    : IConsumer<BasketCheckoutEvent>
{
    public async Task Consume(
        ConsumeContext<BasketCheckoutEvent> context)
    {
        // Create the order from context.Message
    }
}
```

MassTransit handles converting the C# message to data, sending it through RabbitMQ, receiving it, and invoking the matching consumer.

## 7. Synchronous versus asynchronous communication

The Basket service uses both styles for different purposes.

### Synchronous: Basket calls Discount with gRPC

```text
Basket ---> Discount
Basket <--- Discount response
```

Basket needs the discount result immediately to calculate the final basket price, so it waits for a response.

### Asynchronous: Basket publishes checkout through RabbitMQ

```text
Basket ---> RabbitMQ ---> Ordering
```

Basket publishes the checkout message without directly waiting for Ordering to finish creating the order.

Use synchronous communication when the caller needs an immediate answer. Use asynchronous messaging when the work can be processed separately and the services should be less directly coupled.

## 8. How the course flow fits together

```text
1. Customer checks out a basket
              |
              v
2. Basket publishes BasketCheckoutEvent using MassTransit
              |
              v
3. RabbitMQ routes and stores the message in Ordering's queue
              |
              v
4. Ordering's MassTransit consumer receives the message
              |
              v
5. Ordering creates an Order aggregate
              |
              v
6. Order records an OrderCreatedDomainEvent
              |
              v
7. MediatR runs internal Ordering handlers
```

This flow contains two different kinds of event:

- `BasketCheckoutEvent` crosses the Basket/Ordering boundary, so it is an integration event.
- `OrderCreatedDomainEvent` describes what occurred inside Ordering, so it is a domain event.

## 9. What each technology is responsible for

| Technology/concept | Responsibility |
|---|---|
| Domain event | Represents an important business fact inside a service |
| Integration event | Message contract used between services |
| MediatR | Dispatches commands, queries, and notifications inside a .NET process |
| Message broker | Routes, stores, and delivers messages between processes |
| RabbitMQ | The message broker used by this course |
| MassTransit | .NET abstraction for publishing and consuming broker messages |
| gRPC | Fast synchronous request/response communication between services |

## 10. Short mental model

```text
Inside one service:       Domain Event + MediatR
Between services:         Integration Event + MassTransit + RabbitMQ
Immediate answer needed:  gRPC or HTTP
```

The simplest analogy is:

- **MediatR** is an internal office messenger.
- **MassTransit** is the shipping API used to send and receive packages.
- **RabbitMQ** is the post office and storage facility.
- **Domain event** is an internal company notice.
- **Integration event** is a package sent to another company.

