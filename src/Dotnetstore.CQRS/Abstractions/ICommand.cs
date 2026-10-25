namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// A request that changes state and returns a response.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
/// A request that changes state and returns no value.
/// </summary>
public interface ICommand : ICommand<Unit>;

/// <summary>
/// Handles a command.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type. Use <see cref="Unit"/> for commands without a result.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
