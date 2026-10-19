using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoTorrent.Client;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;
using TorrentIsland.Infrastructure.VLC;
using TorrentIsland.Presentation.Player.Abstractions;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Main;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace TorrentIsland.Presentation.Player;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log.Salvar($"UnhandledException: {args.ExceptionObject}");
        };
        DispatcherUnhandledException += static (s, args) =>
        {
            Log.Salvar($"DispatcherUnhandledException: {args.Exception.Message} {args.Exception.StackTrace}");

            if (args.Exception is InvalidOperationException)
            {
                MessageBox.Show("Erro interno na inicialização dos serviços.", "Erro Crítico");
                Environment.Exit(1);
            }

            args.Handled = true;
        };

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);

        base.OnStartup(e);

        // Logs limpos a cada execução para o timeline não misturar sessões.
        foreach (var arquivo in new[] { $"{AppSettings.AppLog}", "vlc-errors.log" })
        {
            try
            {
                var caminho = Path.Combine(AppContext.BaseDirectory, arquivo);
                if (File.Exists(caminho)) File.Delete(caminho);
            }
            catch { /* best-effort */ }
        }

        // Uso: TorrentIsland.Presentation.Player.exe <url-do-stream>
        var mediaUrl = e.Args.Length > 0 ? e.Args[0] : null;

        try
        {
            var ce = new ClientEngineInitializer();
            await Task.Run(async () => await ce.StartAsync());
            var vlc = await Task.Run(() => new VlcPlayerService());

            var services = new ServiceCollection();
            ConfigurarServico(services, ce.Engine, vlc);
            var serviceProvider = services.BuildServiceProvider();

            var window = serviceProvider.GetRequiredService<PlayerWindow>();
            window.MediaUrl = mediaUrl!;
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Falha crítica na inicialização: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Falha ao iniciar: {ex.Message}", "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private void ConfigurarServico(IServiceCollection services, ClientEngine? clientEngine = null, VlcPlayerService? vlc = null)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .Build();

        services.AddTorrentIsland(configuration, clientEngine: clientEngine, vlc: vlc);

        services.AddSingleton(sp =>
        {
            var vlc = sp.GetRequiredService<VlcPlayerService>();
            var fb = sp.GetRequiredService<IFormattingHelper>();
            return new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer, fb);
        });

        services.AddTransient(sp =>
        {
            var vlc = sp.GetRequiredService<VlcPlayerService>();
            var dl = sp.GetRequiredService<IDLService>();
            var managers = sp.GetRequiredService<IManagers>();
            return new MediaFactory(vlc.LibVLC, dl, managers);
        });

        services.AddSingleton(sp =>
        {
            var vm = sp.GetRequiredService<PlayerViewModel>();
            var managers = sp.GetRequiredService<IManagers>();
            return new PlayerSessionTimers(vm, managers);
        });

        services.AddSingleton(Dispatcher.CurrentDispatcher);

        services.AddSingleton<IUiDispatcher>(sp =>
            new UiDispatcher(sp.GetRequiredService<Dispatcher>()));

        services.AddSingleton<IScreenMetrics, SystemScreenMetrics>();

        services.AddTransient<MediaDropHandler>();


        //Factories

        services.AddTransient<Func<IPlayerUiContext, ILocalMediaLoader>>(sp =>
            ui => new LocalMediaLoader(ui, sp.GetRequiredService<MediaFactory>()));

        services.AddTransient<Func<IPlayerUiContext, IMediasFolderLoader>>(sp =>
            ui => new MediasFolderLoader(ui, sp.GetRequiredService<MediaFactory>()));

        services.AddTransient<Func<IPlayerUiContext, IStreamTorrentLoader>>(sp =>
            ui => new StreamTorrentLoader(
                ui,
                sp.GetRequiredService<IStreamService>(),
                sp.GetRequiredService<Func<IPlayerUiContext, ILocalMediaLoader>>()(ui)));

        services.AddTransient<Func<IPlayerUiContext, IControlsWindow, ControlsWindowPositioner>>(sp =>
            (ui, controls) => new ControlsWindowPositioner(
                ui,
                controls,
                sp.GetRequiredService<PlayerViewModel>(),
                sp.GetRequiredService<IUiDispatcher>(),
                sp.GetRequiredService<IScreenMetrics>()));

        services.AddSingleton<PlayerWindow>();
        services.AddSingleton<IPlayerUiContext>(sp => sp.GetRequiredService<PlayerWindow>());
    }
}