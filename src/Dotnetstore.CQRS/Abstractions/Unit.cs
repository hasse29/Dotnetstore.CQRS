namespace Dotnetstore.CQRS.Abstractions;

/// <summary>
/// Represents a void result, used by commands that return no value.
/// </summary>
public readonly record struct Unit
{
    /// <summary>Gets the single <see cref="Unit"/> value.</summary>
    public static Unit Value => default;

    /// <summary>Gets a completed task that returns <see cref="Value"/>.</summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(default(Unit));
}
