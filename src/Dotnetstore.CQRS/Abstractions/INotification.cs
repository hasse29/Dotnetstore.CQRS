namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Marker for an event that is published to zero or more handlers.
/// </summary>
public interface INotification;

/// <summary>
/// Handles a published notification.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Handles the notification.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when handling is done.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
