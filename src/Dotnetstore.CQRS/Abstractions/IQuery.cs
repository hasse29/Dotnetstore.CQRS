namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// A request that reads state and must not change it.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Handles a query.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
