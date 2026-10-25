# Dotnetstore.CQRS

[![CI](https://github.com/hasse29/Dotnetstore.CQRS/actions/workflows/ci.yml/badge.svg)](https://github.com/hasse29/Dotnetstore.CQRS/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Dotnetstore.CQRS.svg?logo=nuget)](https://www.nuget.org/packages/Dotnetstore.CQRS)
[![NuGet downloads](https://img.shields.io/nuget/dt/Dotnetstore.CQRS.svg?logo=nuget)](https://www.nuget.org/packages/Dotnetstore.CQRS)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/github/license/hasse29/Dotnetstore.CQRS.svg)](https://github.com/hasse29/Dotnetstore.CQRS/blob/main/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/hasse29/Dotnetstore.CQRS.svg?style=social)](https://github.com/hasse29/Dotnetstore.CQRS/stargazers)

A small, dependency-light CQRS library for .NET 10: commands, queries, notifications, streaming requests and a
pipeline for cross-cutting concerns. It is a free, MIT-licensed alternative to MediatR with a deliberately similar API,
so moving existing code over is mostly a namespace change.

The only dependency is `Microsoft.Extensions.DependencyInjection.Abstractions`. There is no code generation and no
licence key. Handlers are discovered once at startup by scanning the assemblies you choose.

## Contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Concepts](#concepts)
  - [Commands](#commands)
  - [Queries](#queries)
  - [Notifications](#notifications)
  - [Stream requests](#stream-requests)
- [The request pipeline](#the-request-pipeline)
  - [Pipeline behaviors](#pipeline-behaviors)
  - [Pre- and post-processors](#pre--and-post-processors)
  - [Exception handling](#exception-handling)
- [Publishing notifications in parallel](#publishing-notifications-in-parallel)
- [Registration and lifetimes](#registration-and-lifetimes)
- [Migrating from MediatR](#migrating-from-mediatr)
- [API reference](#api-reference)
- [Behavior and limitations](#behavior-and-limitations)
- [Project layout](#project-layout)
- [Building and testing](#building-and-testing)
- [License](#license)

## Features

| Feature | Types |
| --- | --- |
| Commands (with or without a result) | `ICommand`, `ICommand<TResponse>`, `ICommandHandler<,>` |
| Queries | `IQuery<TResponse>`, `IQueryHandler<,>` |
| Notifications (events) | `INotification`, `INotificationHandler<>`, `IPublisher` |
| Streaming responses | `IStreamRequest<T>`, `IStreamRequestHandler<,>` |
| Pipeline behaviors | `IPipelineBehavior<,>` |
| Pre- and post-processors | `IRequestPreProcessor<>`, `IRequestPostProcessor<,>` |
| Exception handlers and actions | `IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>` |
| Sequential or parallel notification publishing | `NotificationPublishStrategy` |
| Assembly scanning and open generic registration | `AddCqrs(...)`, `CqrsOptions` |

## Installation

The library targets `net10.0`. Reference the project directly, or pack it and add the resulting package to your feed:

```bash
dotnet pack src/Dotnetstore.CQRS -c Release -o ./nupkg
```

```xml
<PackageReference Include="Dotnetstore.CQRS" Version="0.1.0" />
```

## Quick start

Define a command and its handler:

```csharp
using Dotnetstore.CQRS.Abstractions;

public sealed record CreateOrder(string Customer, decimal Total) : ICommand<Guid>;

public sealed class CreateOrderHandler(OrdersDbContext db) : ICommandHandler<CreateOrder, Guid>
{
    public async Task<Guid> Handle(CreateOrder command, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), command.Customer, command.Total);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
```

Register the library and scan your assembly for handlers:

```csharp
using Dotnetstore.CQRS;

builder.Services.AddCqrs(options => options
    .AddHandlersFromAssemblyContaining<CreateOrderHandler>());
```

Send the command through `ISender`:

```csharp
app.MapPost("/orders", async (CreateOrder command, ISender sender, CancellationToken ct) =>
{
    var id = await sender.Send(command, ct);
    return Results.Created($"/orders/{id}", new { id });
});
```

## Concepts

All requests are handled by **exactly one** handler. Notifications are handled by **zero or more**.

### Commands

A command changes state. It is either `ICommand<TResponse>` (returns a value) or `ICommand` (returns nothing).
Commands without a result use the `Unit` type, so their handlers are still `ICommandHandler<TCommand, Unit>`:

```csharp
public sealed record DeleteOrder(Guid Id) : ICommand;

public sealed class DeleteOrderHandler(OrdersDbContext db) : ICommandHandler<DeleteOrder, Unit>
{
    public async Task<Unit> Handle(DeleteOrder command, CancellationToken cancellationToken)
    {
        await db.Orders.Where(o => o.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
        return Unit.Value;
    }
}
```

Use `Unit.Value` to return a `Unit`, or `Unit.Task` for a cached completed `Task<Unit>` in handlers that do no async work.

### Queries

A query reads state and should not change it. The only difference from a command is the intent, which makes pipelines
easy to target (for example, wrapping commands but not queries in a transaction).

```csharp
public sealed record GetOrder(Guid Id) : IQuery<OrderDto?>;

public sealed class GetOrderHandler(OrdersDbContext db) : IQueryHandler<GetOrder, OrderDto?>
{
    public Task<OrderDto?> Handle(GetOrder query, CancellationToken cancellationToken) =>
        db.Orders
            .Where(o => o.Id == query.Id)
            .Select(o => new OrderDto(o.Id, o.Customer, o.Total))
            .FirstOrDefaultAsync(cancellationToken);
}
```

`ICommand<T>` and `IQuery<T>` both derive from `IRequest<T>`, and both handler interfaces derive from
`IRequestHandler<TRequest, TResponse>`. You can implement `IRequest<T>` and `IRequestHandler<,>` directly if you do not
want the command/query distinction.

### Notifications

A notification announces that something happened. Every registered handler receives it.

```csharp
public sealed record OrderPlaced(Guid OrderId) : INotification;

public sealed class SendConfirmationEmail(IEmailSender email) : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
        email.SendOrderConfirmation(notification.OrderId, cancellationToken);
}

public sealed class UpdateStatistics(IStatistics stats) : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
        stats.RecordOrder(notification.OrderId, cancellationToken);
}
```

```csharp
await publisher.Publish(new OrderPlaced(orderId), ct);
```

Publishing with no handlers completes normally. By default handlers run one after another in registration order and the
first exception stops the rest; see [Publishing notifications in parallel](#publishing-notifications-in-parallel) for
the alternative.

### Stream requests

A stream request answers with `IAsyncEnumerable<T>` instead of a single value, so results can be consumed as they are
produced.

```csharp
public sealed record StreamOrders(string Customer) : IStreamRequest<OrderDto>;

public sealed class StreamOrdersHandler(OrdersDbContext db) : IStreamRequestHandler<StreamOrders, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> Handle(
        StreamOrders request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var order in db.Orders
            .Where(o => o.Customer == request.Customer)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return new OrderDto(order.Id, order.Customer, order.Total);
        }
    }
}
```

```csharp
await foreach (var order in sender.CreateStream(new StreamOrders("Ada"), ct))
{
    Console.WriteLine(order.Id);
}
```

A missing handler throws `InvalidOperationException` from `CreateStream` itself, not on the first iteration.
Behaviors, processors and exception handlers do **not** apply to streams.

## The request pipeline

For every `ISender.Send` call the following happens, outermost first:

```text
Send(request)
 └─ exception handling (actions, then handlers)        ← wraps everything below
     ├─ pre-processors                  (registration order)
     ├─ behaviors                       (first registered = outermost)
     │   └─ request handler
     └─ post-processors                 (registration order)
```

An exception thrown by a pre-processor, behavior, handler or post-processor is offered to the exception handling stage.
Post-processors only run if the pipeline produced a response.

### Pipeline behaviors

A behavior wraps handler execution. Call `next()` to continue; skip it to short-circuit.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

Register an open generic behavior so it applies to every request. Behaviors run in the order they are added, the first
being the outermost:

```csharp
builder.Services.AddCqrs(options => options
    .AddHandlersFromAssemblyContaining<CreateOrderHandler>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>))
    .AddOpenBehavior(typeof(ValidationBehavior<,>)));
```

To target only commands, constrain the behavior's type parameters, for example
`where TRequest : ICommand<TResponse>`. Requests that do not satisfy the constraint are skipped by the container.

### Pre- and post-processors

Processors are lighter than behaviors: they run before or after the pipeline and cannot short-circuit it.

```csharp
public sealed class AuditPreProcessor<TRequest>(IAuditLog audit) : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken) =>
        audit.Record($"Starting {typeof(TRequest).Name}", cancellationToken);
}

public sealed class CachePostProcessor<TRequest, TResponse>(ICache cache) : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken) =>
        cache.Store(request, response, cancellationToken);
}
```

```csharp
builder.Services.AddCqrs(options => options
    .AddOpenPreProcessor(typeof(AuditPreProcessor<>))
    .AddOpenPostProcessor(typeof(CachePostProcessor<,>)));
```

A processor for one specific request type is picked up by assembly scanning, like a handler:

```csharp
public sealed class NormalizeNamePreProcessor : IRequestPreProcessor<CreateOrder> { /* ... */ }
```

### Exception handling

Two interfaces react to exceptions thrown anywhere in the pipeline.

**`IRequestExceptionAction<TRequest, TException>`** performs side effects (logging, metrics). It cannot recover, and
the exception is still thrown afterwards.

```csharp
public sealed class LogOrderFailure(ILogger<LogOrderFailure> logger)
    : IRequestExceptionAction<CreateOrder, Exception>
{
    public Task Execute(CreateOrder request, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Creating an order for {Customer} failed", request.Customer);
        return Task.CompletedTask;
    }
}
```

**`IRequestExceptionHandler<TRequest, TResponse, TException>`** can turn the failure into a response with
`state.SetHandled(response)`:

```csharp
public sealed class DuplicateOrderHandler
    : IRequestExceptionHandler<CreateOrder, Guid, DuplicateOrderException>
{
    public Task Handle(
        CreateOrder request,
        DuplicateOrderException exception,
        RequestExceptionHandlerState<Guid> state,
        CancellationToken cancellationToken)
    {
        state.SetHandled(exception.ExistingOrderId);
        return Task.CompletedTask;
    }
}
```

How they are chosen:

1. The thrown exception's type and each of its base types are considered, **most derived first**, up to `Exception`.
2. All matching **actions** run first.
3. Then matching **handlers** run in that order. The first to call `SetHandled` wins and its response is returned.
4. If no handler sets a response, the **original exception is rethrown** with its stack trace intact.

Handlers are matched by walking the type hierarchy rather than through DI variance, so a handler registered for
`Exception` catches everything, and one registered for `ArgumentNullException` takes priority for that exception.

## Publishing notifications in parallel

`NotificationPublishStrategy` controls how handlers of one notification run:

| Strategy | Behavior |
| --- | --- |
| `Sequential` (default) | One after another in registration order. The first exception stops the remaining handlers. |
| `Parallel` | All handlers start together and are awaited together. Every handler runs even if another fails. One failure rethrows the original exception; several are thrown as an `AggregateException`. |

Set the default for the whole application:

```csharp
builder.Services.AddCqrs(options =>
{
    options.AddHandlersFromAssemblyContaining<CreateOrderHandler>();
    options.NotificationPublishStrategy = NotificationPublishStrategy.Parallel;
});
```

Or override it for a single publish:

```csharp
await publisher.Publish(new OrderPlaced(id), NotificationPublishStrategy.Parallel, ct);
```

> **Warning:** parallel handlers share the caller's DI scope. They must not use scoped services that are not
> thread-safe, such as an EF Core `DbContext`. Use `Sequential` for those, or give each handler its own scope.

## Registration and lifetimes

`AddCqrs` registers:

| Service | Lifetime |
| --- | --- |
| `ISender`, `IPublisher` (one shared dispatcher) | Scoped |
| Handlers, processors, exception handlers and actions found by scanning | Transient |
| Open generic behaviors and processors | Transient |

Because the dispatcher is scoped, resolve `ISender`/`IPublisher` from a request scope (ASP.NET Core does this for you),
or create a scope yourself in background services:

```csharp
await using var scope = serviceProvider.CreateAsyncScope();
var sender = scope.ServiceProvider.GetRequiredService<ISender>();
```

Scanning registers every non-abstract, non-generic class in the assembly that implements any of these interfaces:
`IRequestHandler<,>` (and so `ICommandHandler<,>`/`IQueryHandler<,>`), `IStreamRequestHandler<,>`,
`INotificationHandler<>`, `IRequestPreProcessor<>`, `IRequestPostProcessor<,>`, `IRequestExceptionHandler<,,>` and
`IRequestExceptionAction<,>`. Generic implementations are not scanned; register those explicitly with the `AddOpen*`
methods or the container.

You can call `AddHandlersFromAssembly` / `AddHandlersFromAssemblyContaining<T>` several times to scan multiple assemblies.

## Migrating from MediatR

| MediatR | Dotnetstore.CQRS |
| --- | --- |
| `services.AddMediatR(cfg => ...)` | `services.AddCqrs(options => ...)` |
| `cfg.RegisterServicesFromAssembly(asm)` | `options.AddHandlersFromAssembly(asm)` |
| `cfg.AddOpenBehavior(typeof(X<,>))` | `options.AddOpenBehavior(typeof(X<,>))` |
| `IMediator` / `ISender` / `IPublisher` | `ISender` / `IPublisher` |
| `IRequest<T>` | `IRequest<T>`, or the clearer `ICommand<T>` / `IQuery<T>` |
| `IRequest` (no result) | `ICommand` (result is `Unit`) |
| `IRequestHandler<TRequest, TResponse>` | `IRequestHandler<,>`, `ICommandHandler<,>` or `IQueryHandler<,>` |
| `INotification`, `INotificationHandler<T>` | same |
| `IPipelineBehavior<,>`, `RequestHandlerDelegate<T>` | same |
| `IRequestPreProcessor<T>`, `IRequestPostProcessor<,>` | same |
| `IRequestExceptionHandler<,,>`, `IRequestExceptionAction<,>` | same |
| `IStreamRequest<T>`, `IStreamRequestHandler<,>`, `CreateStream` | same |
| Custom `INotificationPublisher` | `NotificationPublishStrategy` (sequential or parallel) |

Differences to be aware of:

- `Send` accepts only `IRequest<TResponse>`; there is no `Send(object)` overload.
- Exception handlers are found by walking the exception's type hierarchy.
- Stream requests have no pipeline behaviors.
- There is no custom notification publisher abstraction, only the two built-in strategies.

## API reference

### Namespace `Dotnetstore.CQRS.Abstractions`

| Type | Purpose |
| --- | --- |
| `IRequest<out TResponse>` | Marker for a request with a response |
| `IRequestHandler<in TRequest, TResponse>` | `Task<TResponse> Handle(TRequest, CancellationToken)` |
| `ICommand` / `ICommand<out TResponse>` | Command markers; `ICommand` is `ICommand<Unit>` |
| `ICommandHandler<in TCommand, TResponse>` | Handler for a command |
| `IQuery<out TResponse>` | Query marker |
| `IQueryHandler<in TQuery, TResponse>` | Handler for a query |
| `Unit` | Void result: `Unit.Value`, `Unit.Task` |
| `INotification` | Notification marker |
| `INotificationHandler<in TNotification>` | `Task Handle(TNotification, CancellationToken)` |
| `IStreamRequest<out TResponse>` | Stream request marker |
| `IStreamRequestHandler<in TRequest, out TResponse>` | `IAsyncEnumerable<TResponse> Handle(TRequest, CancellationToken)` |
| `IPipelineBehavior<in TRequest, TResponse>` | Wraps handling; `Handle(request, next, ct)` |
| `RequestHandlerDelegate<TResponse>` | The `next` step of a behavior |
| `IRequestPreProcessor<in TRequest>` | `Task Process(request, ct)` before the pipeline |
| `IRequestPostProcessor<in TRequest, in TResponse>` | `Task Process(request, response, ct)` after the pipeline |
| `IRequestExceptionHandler<in TRequest, TResponse, in TException>` | Can recover via `state.SetHandled(response)` |
| `IRequestExceptionAction<in TRequest, in TException>` | Side effects only |
| `RequestExceptionHandlerState<TResponse>` | `Handled`, `Response`, `SetHandled(response)` |
| `ISender` | `Send<TResponse>(IRequest<TResponse>, ct)`, `CreateStream<TResponse>(IStreamRequest<TResponse>, ct)` |
| `IPublisher` | `Publish<T>(T, ct)`, `Publish<T>(T, NotificationPublishStrategy, ct)` |
| `NotificationPublishStrategy` | `Sequential`, `Parallel` |

### Namespace `Dotnetstore.CQRS`

| Member | Purpose |
| --- | --- |
| `IServiceCollection.AddCqrs(Action<CqrsOptions>)` | Registers everything |
| `CqrsOptions.AddHandlersFromAssembly(Assembly)` | Scan an assembly |
| `CqrsOptions.AddHandlersFromAssemblyContaining<T>()` | Scan the assembly containing `T` |
| `CqrsOptions.AddOpenBehavior(Type)` | Add an open generic pipeline behavior |
| `CqrsOptions.AddOpenPreProcessor(Type)` | Add an open generic pre-processor |
| `CqrsOptions.AddOpenPostProcessor(Type)` | Add an open generic post-processor |
| `CqrsOptions.NotificationPublishStrategy` | Default publish strategy (`Sequential`) |

The `AddOpen*` methods throw `ArgumentException` if the type is not an open generic type implementing the matching
interface.

## Behavior and limitations

- **Errors:** sending a request with no registered handler throws `InvalidOperationException` naming the request type.
  Null requests and notifications throw `ArgumentNullException`.
- **Dispatch by runtime type:** handlers are looked up using the request's runtime type, and the lookup is cached per type.
- **One handler per request:** if two classes implement a handler for the same request type, the one registered last is
  used. Register each request's handler once.
- **Cancellation:** the `CancellationToken` passed to `Send`, `Publish` or `CreateStream` is handed to every handler,
  behavior and processor. A cancelled token raises `OperationCanceledException`, which exception handlers also see.
- **Streams are lazy:** items are produced as the caller iterates; cancel by cancelling the token you passed in.
- **Not included:** behaviors for stream requests, custom notification publishers, and `Send(object)`.

## Project layout

```text
src/Dotnetstore.CQRS/
  Abstractions/   Public interfaces, Unit and the publish strategy enum
  Extensions/     AddCqrs and CqrsOptions
  Internal/       Dispatcher and request/stream/exception invokers
tests/Dotnetstore.CQRS.Tests/
  Abstractions/   Mirrors src; tests for public abstractions
  Extensions/     Tests for registration and options
  Internal/       Tests for the dispatcher and pipeline
  Fixtures/       Shared request, handler and behavior test types
Directory.Build.props      Shared build settings (net10.0, nullable, warnings as errors)
Directory.Packages.props   Central package versions
```

## Building and testing

Requires the .NET 10 SDK.

```bash
dotnet build Dotnetstore.CQRS.slnx
dotnet run --project tests/Dotnetstore.CQRS.Tests
```

The tests use xUnit v3 (test projects are executables, hence `dotnet run`) and Shouldly. The suite takes a few seconds
because one test waits for a timeout to prove that sequential publishing does not run handlers concurrently.

Create a NuGet package:

```bash
dotnet pack src/Dotnetstore.CQRS -c Release -o ./nupkg
```

Warnings are treated as errors, and public members require XML documentation.

## License

[MIT](https://github.com/hasse29/Dotnetstore.CQRS/blob/main/LICENSE)
