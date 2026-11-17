using Tasks.App;
using Tasks.App.Agent;
using Tasks.App.Data;
using Tasks.App.Services;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("APP_PORT") is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
}

builder.Services.AddSingleton(sp => AppSettings.From(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<TimeProvider>(sp =>
{
    var settings = sp.GetRequiredService<AppSettings>();
    return settings.Now is { } now ? new FixedTimeProvider(now, settings.Zone) : TimeProvider.System;
});
builder.Services.AddSingleton(sp =>
{
    var store = new TaskStore();
    if (sp.GetRequiredService<AppSettings>().Seed)
    {
        SeedData.Fill(store, sp.GetRequiredService<TimeProvider>());
    }

    return store;
});
builder.Services.AddSingleton<TaskService>();
builder.Services.AddSingleton<ITaskQueries>(sp => sp.GetRequiredService<TaskService>());
builder.Services.AddSingleton<ActivityFeed>().AddSingleton<SearchService>();
builder.Services.AddSingleton<ScriptedAgent>();
builder.Services.AddRazorPages();

var app = builder.Build();
app.MapRazorPages();
app.MapGet("/api/tasks/recent", (ITaskQueries queries) => queries.Recent());
TaskApi.Map(app);
app.Run();

/// <summary>Exposed so tests can start the app in-process.</summary>
public partial class Program;
