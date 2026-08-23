using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Presentation.Console.EntryPoint;
using TorrentIsland.Presentation.Console.Helpers;
using TorrentIsland.Presentation.Console.Renderers;

var builder = Host.CreateApplicationBuilder(args);

Console.CursorVisible = false;

builder.Services.AddTorrentIsland(builder.Configuration, maxLogs: 10);
builder.Services.AddSingleton<ConsoleLogRenderer>();
builder.Services.AddSingleton<IFormattingHelper, FormattingHelper>();
builder.Services.AddSingleton<ITorrentLoopRenderer, TorrentLoopRenderer>();
builder.Services.AddTransient<EntryPoint>();
builder.Services.AddHostedService<TorrentLoopRenderer>();
builder.Services.AddSingleton<IPlayerMonitorRenderer, PlayerMonitorRenderer>();

var host = builder.Build();

var app = host.Services.GetRequiredService<EntryPoint>();
await app.Executar(args).ConfigureAwait(false);

await host.RunAsync();


