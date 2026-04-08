using ThemePark.Shared.Time;

namespace ThemePark.Tests.Shared.Time;

/// <summary>
/// Test implementation of <see cref="ITimeProvider"/> that advances time programmatically,
/// allowing workflow timeout tests to run in milliseconds rather than real wall-clock time.
/// </summary>
public sealed class FakeTimeProvider : ITimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly List<(DateTimeOffset Due, TaskCompletionSource Tcs)> _pending = [];

    /// <summary>Gets the current fake time.</summary>
    public DateTimeOffset UtcNow => _now;

    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var due = _now + duration;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        _pending.Add((due, tcs));
        return tcs.Task;
    }

    /// <summary>
    /// Advances the fake clock by <paramref name="amount"/> and completes
    /// all pending delays whose due time is now in the past.
    /// </summary>
    public void Advance(TimeSpan amount)
    {
        _now += amount;
        var toComplete = _pending.Where(p => p.Due <= _now).Select(p => p.Tcs).ToList();
        _pending.RemoveAll(p => p.Due <= _now);
        foreach (var tcs in toComplete)
            tcs.TrySetResult();
    }
}
