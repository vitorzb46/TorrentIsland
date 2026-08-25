using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Presentation.Console.EntryPoint
{
    internal sealed class EntryPoint(ITorrentService torrent, IStringLocalizer<EntryPoint> localizer, ILogger<EntryPoint> logger)
    {
        public ILogger<EntryPoint> Logger { get; } = logger;

        public async Task Executar(string[] args)
        {

            args = ["stream", "magnet:?xt=urn:btih:A2405A183F1451C0DE8963D4EC408A1194628331&dn=Lioness+2023+S03E01+1080p+HEVC+x265-MeGusta&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Fexodus.desync.com%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Ftracker.dler.org%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce&tr=udp%3A%2F%2Fexplodie.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.ololosh.space%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.dump.cl%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.bittor.pw%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker-udp.gbitt.info%3A80%2Fannounce&tr=udp%3A%2F%2Fretracker01-msk-virt.corbina.net%3A80%2Fannounce&tr=udp%3A%2F%2Fopen.free-tracker.ga%3A6969%2Fannounce&tr=udp%3A%2F%2Fns-1.x-fins.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fleet-tracker.moe%3A1337%2Fannounce&tr=udp%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.leechers-paradise.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.open-internet.nl%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.pirateparty.gr%3A6969%2Fannounce&tr=udp%3A%2F%2Fdenis.stalker.upeer.me%3A6969%2Fannounce"];

            if (args.Length == 0)
            {
                System.Console.WriteLine(localizer["Console_Uso"]);
                return;
            }

            try
            {
                switch (args[0].ToUpperInvariant())
                {
                    case "VIDEO":
                        await BaixarVideoAsync(args).ConfigureAwait(false);
                        break;
                    case "AUDIO":
                        await BaixarAudioAsync(args).ConfigureAwait(false);
                        break;
                    case "PLAYLIST":
                        await BaixarPlaylistAsync(args).ConfigureAwait(false);
                        break;
                    case "MOSTRAR":
                        await MostrarPlaylistAsync(args).ConfigureAwait(false);
                        break;
                    case "TORRENT":
                        await BaixarTorrentAsync(args).ConfigureAwait(false);
                        break;
                    case "STREAM":
                        await StreamTorrentAsync(args).ConfigureAwait(false);
                        break;
                    case "HELP" or "-H" or "--HELP":
                        System.Console.WriteLine(localizer["Console_Uso"]);
                        break;
                    default:
                        System.Console.WriteLine(localizer["Console_ComandoInvalido", args[0]]);
                        System.Console.WriteLine(localizer["Console_Uso"]);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Erros de download ou de rede chegam aqui; exibe mensagem amigável.
                Logger.LogError(localizer["Console_Erro", ex.Message]);
                throw;
            }
        }

        private async Task BaixarVideoAsync(string[] args)
        {
            string? url = ObterUrl(args, "Console_UsoVideo");

            if (url is null) return;

            //await ydl.VideoDLAsync(url).ConfigureAwait(false);
            return;
        }

        private async Task BaixarAudioAsync(string[] args)
        {
            string? url = ObterUrl(args, "Console_UsoAudio");

            if (url is null) return;

            //await ys.DownloadAudioAsync(url).ConfigureAwait(false);
        }

        private async Task BaixarPlaylistAsync(string[] args)
        {
            string? url = ObterUrl(args, "Console_UsoPlaylist");

            if (url is null) return;

            //await ydl.VideoDLAsync(url).ConfigureAwait(false);
        }

        private async Task MostrarPlaylistAsync(string[] args)
        {
            string? url = ObterUrl(args, "Console_UsoMostrar");

            if (url is null) return;

            //await ys.MostrarPlaylistAsync(url).ConfigureAwait(false);
        }

        private async Task BaixarTorrentAsync(string[] args)
        {
            if (args.Length < 2) Logger.LogInformation(localizer["Console_UsoTorrent"]);

            await torrent.CriarTorrentAsync(args[1]).ConfigureAwait(false);
        }

        private async Task StreamTorrentAsync(string[] args)
        {
            if (args.Length < 2) Logger.LogInformation(localizer["Console_UsoStream"]);

            await torrent.StreamTorrentAsync(args[1]).ConfigureAwait(false);
        }

        private string? ObterUrl(string[] args, string chaveUso)
        {
            if (args.Length < 2)
            {
                Logger.LogInformation(localizer[chaveUso]);
                return null;
            }

            if (!EhUrlValida(args[1]))
            {
                Logger.LogWarning(localizer["Console_UrlInvalida", args[1]]);
                return null;
            }

            return args[1];
        }

        private static bool EhUrlValida(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

    }
}
