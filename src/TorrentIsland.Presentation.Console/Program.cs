using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Renderers;
using TorrentIsland.Presentation.Console.EntryPoint;

var builder = Host.CreateApplicationBuilder(args);

Console.CursorVisible = false;

var consoleLog = builder.Services.BuildServiceProvider().GetRequiredService<IConsoleLogRenderer>();

builder.Services.AddTorrentIsland(builder.Configuration, consoleLog, maxLogs: 10);

builder.Services.AddTransient<EntryPoint>();
builder.Services.AddHostedService<ConsoleLogRenderer>();

var host = builder.Build();

var app = host.Services.GetRequiredService<EntryPoint>();

await host.StartAsync();
await app.Executar(args);
await host.StopAsync();