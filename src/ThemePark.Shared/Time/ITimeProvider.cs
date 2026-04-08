namespace ThemePark.Shared.Time;

/// <summary>
/// Abstraction over time-based delay operations, enabling deterministic testing
/// of workflow timeout scenarios without requiring real wall-clock time.
/// </summary>
public interface ITimeProvider
{
    Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}
