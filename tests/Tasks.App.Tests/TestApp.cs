using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tasks.App.Services;

namespace Tasks.App.Tests;

/// <summary>The app in-process, empty, with the clock pinned to Monday 16 Nov 2026 10:00 in Auckland.</summary>
public sealed class TestApp : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Start =
        DateTimeOffset.Parse("2026-11-16T10:00:00+13:00", CultureInfo.InvariantCulture);

    public FixedTimeProvider Clock { get; } =
        new(Start, TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland"));

    public TaskService Tasks => Services.GetRequiredService<TaskService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("APP_SEED", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }
}
