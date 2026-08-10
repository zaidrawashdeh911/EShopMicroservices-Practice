# Understanding the CQRS Interfaces (ICommand & IQuery)

> BuildingBlocks / CQRS — beginner-friendly notes
> EShop Microservices course

---

## 1. The core idea of CQRS

**CQRS = Command Query Responsibility Segregation.**

Split your operations into two groups:

1. **Commands** → things that *change* data (create, update, delete). "Do something."
2. **Queries** → things that *read* data. "Get me something."

Writes go one way, reads go another way. You don't mix them.

**Restaurant analogy:**
- **Command** = placing an order ("make me a pizza") → you change something, maybe get an order number back.
- **Query** = reading the menu ("what food do you have?") → you only look, you never change anything.

---

## 2. The foundation: MediatR's `IRequest<TResponse>`

MediatR has one main interface:

```csharp
IRequest<TResponse>
```

It means: *"a message that expects a response of type `TResponse` back."*

Everything you send through MediatR must eventually be an `IRequest<TResponse>`.
Your `ICommand` and `IQuery` are just **specialized versions** of this.

MediatR is the "waiter": it takes your Command or Query and delivers it to the
right **handler** (the "kitchen") that does the actual work.

```
You send a Command/Query  ->  MediatR finds the Handler  ->  Handler works  ->  returns a result
```

---

## 3. IQuery — the simpler one

```csharp
public interface IQuery<TResponse> : IRequest<TResponse>
    where TResponse : notnull
{
}
```

Decoded piece by piece:

**`IQuery<TResponse>`**
`<TResponse>` is a **generic placeholder** — fill-in-the-blank for a type you pick later:

```csharp
IQuery<List<Product>>   // TResponse becomes List<Product>
IQuery<Product>         // TResponse becomes Product
```

One interface works for *any* return type. No need for separate query interfaces
per entity (products, orders, customers...).

**`: IRequest<TResponse>`**
The colon `:` means "inherits from." So `IQuery<TResponse>` **is a** `IRequest<TResponse>`.
This is the line that lets MediatR recognize and handle your query.

**`where TResponse : notnull`**
A **constraint** — a rule on what `TResponse` can be. `notnull` = "the return type can't be null."
A query's whole job is to return data, so this guardrail blocks a null-returning query at compile time.

**Empty `{ }` body**
No methods inside — intentional. This interface adds **no new behavior**.
It's purely a **label** meaning "this is a query." All the machinery comes from `IRequest<TResponse>`.

---

## 4. ICommand — same idea, with a twist

```csharp
public interface ICommand : ICommand<Unit>
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
```

There are **two** interfaces. Read the **bottom one first**:

**`ICommand<out TResponse> : IRequest<TResponse>`**
Same spirit as `IQuery` — a generic command that returns some `TResponse`:

```csharp
ICommand<CreateProductResult>   // a command that returns a result
```

**`ICommand : ICommand<Unit>`** (the top one)
No `<TResponse>` — the plain version. It inherits from `ICommand<Unit>`.

`Unit` is MediatR's special word for **"nothing"** (like `void`, but usable as a
generic type — you can't write `ICommand<void>`, so MediatR invented `Unit`).

So this means: *"a command that returns nothing."*

---

## 5. Why two Commands but only one Query?

| | Returns nothing | Returns a value |
|---|---|---|
| **Command** | `ICommand` (delete a product -> nothing to return) | `ICommand<T>` (create a product -> return its ID) |
| **Query** | pointless | `IQuery<T>` (always returns data) |

- A command might just *do the job* and return nothing ("delete this"), OR return
  something ("here's the new ID"). So it needs **both** versions.
- A query **always** returns data, so it needs **only** the generic version.
  That's why the `IQuery : IQuery<Unit>` line is commented out.

---

## 6. The leftover: `out`

```csharp
public interface ICommand<out TResponse>
```

`out` makes the generic **covariant** — a small flexibility feature for how generic
types can be substituted. At beginner stage you can basically ignore it; it doesn't
change how you *use* the interfaces. Keep it for consistency with the course.

---

## 7. Mental summary

Both interfaces are **empty labels** that:

1. Inherit from MediatR's `IRequest<TResponse>` (so MediatR can route them), and
2. Give a **meaningful name** (`ICommand` / `IQuery`) so the code reads clearly.

The only real design decision:
- **Commands** come in two flavors: return-nothing and return-something.
- **Queries** come in one: always return-something.

---

## Final code reference

**ICommand.cs**
```csharp
using MediatR;

namespace BuildingBlocks.CQRS;

public interface ICommand : ICommand<Unit>
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
```

**IQuery.cs**
```csharp
using MediatR;

namespace BuildingBlocks.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull
{
}
```

> Note: add the **MediatR** NuGet package to the BuildingBlocks project so
> `using MediatR;` resolves.
