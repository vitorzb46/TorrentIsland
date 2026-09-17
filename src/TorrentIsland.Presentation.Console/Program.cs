using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Presentation.Console.EntryPoint;
using TorrentIsland.Presentation.Console.Helpers;
using TorrentIsland.Presentation.Console.Renderers;

var builder = Host.CreateApplicationBuilder(args);

Console.CursorVisible = false;

builder.Services.AddSingleton<ILogPainel, LogPainel>();
builder.Services.AddSingleton<IConsoleLogRenderer, ConsoleLogRenderer>();
var consoleLog = builder.Services.BuildServiceProvider().GetRequiredService<IConsoleLogRenderer>();

builder.Services.AddTorrentIsland(builder.Configuration, consoleLog, maxLogs: 10);

builder.Services.AddSingleton<IFormattingHelper, FormattingHelper>();
builder.Services.AddTransient<EntryPoint>();
builder.Services.AddHostedService<ConsoleLogRenderer>();
// builder.Services.AddHostedService<TorrentLoopRenderer>();
// builder.Services.AddSingleton<IPlayerMonitorRenderer, PlayerMonitorRenderer>();

var host = builder.Build();

var app = host.Services.GetRequiredService<EntryPoint>();

await host.StartAsync();
await app.Executar(args);
await host.StopAsync();