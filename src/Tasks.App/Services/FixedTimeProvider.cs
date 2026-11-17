namespace Tasks.App.Services;

/// <summary>A clock that only moves when told to. Tests and APP_NOW use it.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo zone) : TimeProvider
{
    private DateTimeOffset _now = now.ToUniversalTime();

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => zone;

    public void Advance(TimeSpan by) => _now = _now.Add(by);

    public void Set(DateTimeOffset now) => _now = now.ToUniversalTime();
}
