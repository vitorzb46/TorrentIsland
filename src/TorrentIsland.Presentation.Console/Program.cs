using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Presentation.Console.EntryPoint;
using TorrentIsland.Presentation.Console.Renderers;

var builder = Host.CreateApplicationBuilder(args);

Console.CursorVisible = false;

builder.Services.AddTorrentIsland(builder.Configuration, maxLogs: 10);
builder.Services.AddSingleton<ConsoleLogRenderer>();
builder.Services.AddTransient<EntryPoint>();
builder.Services.AddHostedService<Loop>();
builder.Services.AddSingleton<ITorrentLoopRenderer, TorrentLoopRenderer>();

var host = builder.Build();

var app = host.Services.GetRequiredService<EntryPoint>();
await app.Executar(args).ConfigureAwait(false);

await host.RunAsync();

internal sealed class Loop(ILogger<Loop> logger) : BackgroundService
{
    private readonly ILogger<Loop> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Mantém o serviço rodando em background
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Loop de eventos encerrado.");
    }
}
