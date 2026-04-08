namespace ThemePark.Shared.Time;

/// <summary>
/// Production implementation of <see cref="ITimeProvider"/> that delegates
/// to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.Delay(duration, cancellationToken);
}
