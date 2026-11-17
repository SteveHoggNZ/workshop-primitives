using System.Globalization;

namespace Tasks.App;

/// <summary>
/// Settings from environment variables: APP_TZ (the user's time zone, default Pacific/Auckland),
/// APP_NOW (pin the clock, ISO 8601), APP_SEED (false to start empty).
/// </summary>
public sealed record AppSettings(TimeZoneInfo Zone, DateTimeOffset? Now, bool Seed)
{
    public const string DefaultZone = "Pacific/Auckland";

    public static AppSettings From(IConfiguration config)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(config["APP_TZ"] ?? DefaultZone);
        DateTimeOffset? now = config["APP_NOW"] is { Length: > 0 } text
            ? DateTimeOffset.Parse(text, CultureInfo.InvariantCulture)
            : null;
        var seed = !string.Equals(config["APP_SEED"], "false", StringComparison.OrdinalIgnoreCase);
        return new AppSettings(zone, now, seed);
    }
}
