using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoTorrent.Client;
using Spectre.Console;
using System.Runtime.InteropServices;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Presentation.Console.Helpers;

namespace TorrentIsland.Presentation.Console.Renderers;

internal sealed class TorrentLoopRenderer(ClientEngine engine, ConsoleLogRenderer renderer, ILogger<TorrentLoopRenderer> logger, IEntityMapping map) : BackgroundService, ITorrentLoopRenderer
{
    private readonly ILogger<TorrentLoopRenderer> Logger = logger;

    public ClientEngine Engine { get; } = engine;
    public ConsoleLogRenderer Renderer { get; } = renderer;
    public IEntityMapping Map { get; } = map;
    private FormattingHelper FB { get; } = new FormattingHelper();

    public async Task TorrentInfoRender(IProgress<double>? progress = null)
    {
        await ExecuteAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        // Mantém o serviço rodando em background
        while (!stoppingToken.IsCancellationRequested)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.Console.WindowWidth = 110;
                System.Console.WindowHeight = 25;
                System.Console.BufferWidth = 110;
            }

            while (Engine.IsRunning)
            {
                var managers = await Map.ObterManagersAsync();

                if (managers is null || managers.Count == 0 || managers.Select(m => m.Value.Estado)
                                                                       .All(s => s == TorrentEstado.Semeando || s == TorrentEstado.Pausado))
                {
                    Logger.LogInformation("Nenhum torrent ativo, encerrando...");
                    await Task.Delay(1000).ConfigureAwait(false);
                    break;
                }

                string headerFormat = $" [cyan]{managers.Count} torrent(s) ativo(s) | ↓ {FB.FormatarBytes(Engine.TotalDownloadRate)}/s | ↑ {FB.FormatarBytes(Engine.TotalUploadRate)}/s[/]".PadRight(110);

                Renderer.Painel.Adicionar($"[cyan]{Renderer.Multi(110, '=')}[/]");
                Renderer.Painel.Adicionar(headerFormat);
                Renderer.Painel.Adicionar("");
                Renderer.Painel.Adicionar("  [[Q]] Abortar todos | [[A]] Abortar por id | Ctrl+C para sair");
                Renderer.Painel.Adicionar(" >: ");
                Renderer.Painel.Adicionar($"[cyan]{Renderer.Multi(110, '=')}[/]");

                foreach (var (id, p) in managers)
                {
                    var nome = p?.Nome ?? "Desconhecido";
                    var nomeEscape = Markup.Escape(nome);
                    var progresso = p?.Progresso ?? 0.0;
                    var download = p!.VelocidadeDownload;
                    var upload = p!.VelocidadeUpload;
                    var seeds = p?.Seeds ?? 0;
                    var peers = p?.ParesDisponiveis ?? 0;
                    var eta = p?.TempoEstimado ?? "Desconhecido";
                    var estado = p?.Estado ?? TorrentEstado.Aguardando;
                    var cor = p!.CorEstado ?? "white";
                    string msg = $"{id} | {nomeEscape} \n Status: {cor}{estado}[/] {progresso}% | {eta} | {FB.FormatarBytes(download)}/s | {FB.FormatarBytes(upload)}/s | Peers: {seeds}/{peers}";
                    // Id | Nome | Estado | Progresso | TempoEstimado | VelocidadeDownload | VelocidadeUpload | Seeds/Peers
                    Renderer.Painel.Adicionar(msg);
                }
            }

            Logger.LogInformation("Loop de eventos encerrado.");
        }
    }
}