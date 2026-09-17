using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoTorrent.Client;
using Spectre.Console;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.Services;

internal sealed class TorrentLoopRenderer(ClientEngine engine,
                                          IConsoleLogRenderer renderer,
                                          ILogger<TorrentLoopRenderer> logger,
                                          IEntityMapping map,
                                          AppSettings app,
                                          IFormattingHelper fb,
                                          IManagers managers) : BackgroundService, ITorrentLoopRenderer
{
    private readonly ILogger<TorrentLoopRenderer> Logger = logger;
    private readonly AppSettings app = app;
    private readonly IFormattingHelper fb = fb;
    private readonly IManagers managers = managers;

    public ClientEngine Engine { get; } = engine;
    public IConsoleLogRenderer Renderer { get; } = renderer;
    public IEntityMapping Map { get; } = map;

    public async Task TorrentInfoRender(IProgress<double>? progress = null)
    {
        await ExecuteAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        System.Console.Clear();

        // Mantém o serviço rodando em background
        while (!stoppingToken.IsCancellationRequested && Engine.IsRunning)
        {
            var torrents = await managers.ObterTorrentsAsync().ConfigureAwait(false);

            if (DeveEncerrarEngine(torrents))
            {
                await EncerrarEngineAsync(stoppingToken);
                break;
            }

            RenderizarInterface(torrents);

            if (stoppingToken.IsCancellationRequested) break;

            await AguardarProximoCicloAsync(Renderer.TempoRender, stoppingToken).ConfigureAwait(false);

        }
    }

    private bool DeveEncerrarEngine(IReadOnlyDictionary<Guid, TorrentDto> managers)
    {
        return app.Semeando == false &&
               managers.Count == 0 &&
               managers.Select(m => m.Value.Estado).All(s => s == TorrentEstado.Parado || s == TorrentEstado.Pausado);
    }

    private async Task EncerrarEngineAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Nenhum torrent ativo, encerrando...");
        await Engine.SaveStateAsync(AppSettings.ArquivoEngineState).ConfigureAwait(false);
        await AguardarProximoCicloAsync(1000, stoppingToken);
    }

    private static async Task AguardarProximoCicloAsync(int milissegundos, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // Aguarda o delay ou o cancelamento, sem estourar exceção para o console
        await Task.Delay(milissegundos, CancellationToken.None)
                  .WaitAsync(token)
                  .ContinueWith(_ => { }, CancellationToken.None);
    }

    private void RenderizarInterface(IReadOnlyDictionary<Guid, TorrentDto> managers)
    {
        Renderer.Painel.Limpar();

        int largura = Renderer.Largura;

        string headerFormat = $" [cyan]{managers.Count} torrent(s) ativo(s) | ↓ {fb.FormatarBytes(Engine.TotalDownloadRate)}/s | ↑ {fb.FormatarBytes(Engine.TotalUploadRate)}/s[/]".PadRight(largura);

        Renderer.Painel.Adicionar($"[cyan]{Renderer.Multi(largura, '=')}[/]");
        Renderer.Painel.Adicionar(headerFormat);
        Renderer.Painel.Adicionar("".PadRight(largura));
        Renderer.Painel.Adicionar("  [[Q]] Abortar todos | [[A]] Abortar por id | Ctrl+C para sair".PadRight(largura));
        Renderer.Painel.Adicionar(" >: ".PadRight(largura));
        Renderer.Painel.Adicionar($"[cyan]{Renderer.Multi(largura, '=')}[/]");

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
            var cor = p!.CorEstado ?? "[white]";

            string msg = $"[LightSalmon1]{id}[/] | [cyan]{nomeEscape}[/] \n[cyan]Status:[/] {cor}{estado} {progresso:0.0}%[/] | {eta} | {fb.FormatarBytes(download)}/s ↓ | {fb.FormatarBytes(upload)}/s ↑ | Peers: {seeds}/{peers}".PadRight(largura);
            Renderer.Painel.Adicionar(msg);
        }
    }
}