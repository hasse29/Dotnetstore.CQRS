namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// How notification handlers are run.
/// </summary>
public enum NotificationPublishStrategy
{
    /// <summary>Handlers run one after another in registration order; the first exception stops the rest.</summary>
    Sequential = 0,

    /// <summary>
    /// All handlers are started at once and awaited together. Every handler runs even if another fails;
    /// multiple failures are surfaced as an <see cref="AggregateException"/>.
    /// Handlers share the caller's DI scope, so they must not use non-thread-safe scoped services such as a DbContext.
    /// </summary>
    Parallel = 1,
}
