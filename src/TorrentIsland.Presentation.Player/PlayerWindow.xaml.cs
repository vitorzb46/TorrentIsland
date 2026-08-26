using LibVLCSharp.Shared;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TorrentIsland.Infrastructure.VLC;
namespace TorrentIsland.Presentation.Player;

public partial class PlayerWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly DispatcherTimer _inactivityTimer;

    public PlayerWindow(string mediaUrl)
    {
        Log.Salvar($"PlayerWindow ctor | mediaUrl={mediaUrl}");
        InitializeComponent();

        var vlc = new VlcPlayerService();

        _viewModel = new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer);
        DataContext = _viewModel;

        VideoView.MediaPlayer = vlc.MediaPlayer;
        Log.Salvar("MediaPlayer associado ao VideoView");

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).
        _controls = new ControlsWindow(_viewModel);
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => ReiniciarTimerInatividade();
        _controls.Closed += (_, _) => Close();

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _inactivityTimer.Tick += (_, _) => HideControls();

        Loaded += (_, _) =>
        {
            Log.Salvar("Loaded disparado — posicionando controles");
            // Owner garante que a janela de controles fique sempre à frente do player.
            _controls.Owner = this;

            // --- RESOLUÇÃO DO BUG DO MENU FLUTUANTE (Foco do Windows) ---
            this.Activated += (s, e) => _controls.Topmost = true;
            this.Deactivated += (s, e) => _controls.Topmost = false;

            PosicionarControles();
            _controls.Show();
        };
        Loaded += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(mediaUrl))
            {
                await CarregarMidia(mediaUrl);
            }
            else
            {
                // Talvez overlay no futuro
                Log.Salvar("Nenhuma mídia inicial fornecida.");
            }
        };
        Closed += (_, _) =>
        {
            Log.Salvar("Window fechada — dispose do ViewModel");
            _inactivityTimer.Stop();
            _controls.Close();
            _viewModel.Dispose();
            Environment.Exit(0);
        };

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();
    }

    // private async Task IniciarAsync(string mediaUrl)
    // {
    //     _viewModel.IsLoading = true;
    //     Log.Salvar("Criando Media");
        
    //     var media = new Media(_viewModel.LibVLC, mediaUrl, FromType.FromLocation);

    //     // Opções de rede para streaming
    //     media.AddOption(":network-caching=3000");
    //     media.AddOption(":file-caching=3000");
    //     media.AddOption(":live-caching=3000");
    //     media.AddOption(":skip-frames");
    //     media.AddOption(":clock-synchro=0");
    //     media.AddOption(":clock-jitter=5000");

    //     _viewModel.SetMedia(media);
    //     await _viewModel.PopulateTracksAsync();
    //     ShowControls();
    // }

    private async Task CarregarMidia(string caminhoOuUrl)
    {
        try
        {
            _viewModel.IsLoading = true;
            Log.Salvar($"Carregando mídia: {caminhoOuUrl}");

            Media media;
            if (File.Exists(caminhoOuUrl))
            {
                var infoArquivo = new FileInfo(caminhoOuUrl);
                long tamanhoBytes = infoArquivo.Length;
                string tamanhoFormatado;

                if (tamanhoBytes >= 1024 * 1024 * 1024) // 1 GB
                {
                    tamanhoFormatado = $"{tamanhoBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
                }
                else
                {
                    tamanhoFormatado = $"{tamanhoBytes / (1024.0 * 1024.0):F2} MB";
                }

                Log.Salvar($"Arquivo existe. Tamanho: {tamanhoFormatado} bytes");
                media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromPath);
            }
            else if (Uri.TryCreate(caminhoOuUrl, UriKind.Absolute, out _))
            {
                Log.Salvar("Origem é uma URL");
                media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromLocation);
            }
            else
            {
                MessageBox.Show("Caminho de mídia inválido.", "Player", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Opções de rede para streaming
            media.AddOption(":network-caching=3000");
            media.AddOption(":file-caching=3000");
            media.AddOption(":live-caching=3000");
            media.AddOption(":skip-frames");
            media.AddOption(":clock-synchro=0");
            media.AddOption(":clock-jitter=5000");

            _viewModel.SetMedia(media);
            await _viewModel.PopulateTracksAsync();
            ShowControls();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar mídia: {ex.Message}");
            MessageBox.Show($"Não foi possível carregar a mídia: {caminhoOuUrl}", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    /// <summary>Posiciona a janela de controles na parte inferior da janela de vídeo.</summary>
    private void PosicionarControles()
    {
        if (_controls is null) return;

        // Se estiver em modo cinema, usamos a matemática baseada na tela cheia
        if (_viewModel.IsFullscreen)
        {
            _controls.Width = SystemParameters.PrimaryScreenWidth;
            _controls.Left = 0; // Zera a propriedade esquerda permanentemente na tela cheia
            _controls.Top = SystemParameters.PrimaryScreenHeight - _controls.Height;
        }
        // Janela Maximizada (Botão maximizar do windows (do player))
        else if (this.WindowState == WindowState.Maximized)
        {
            _controls.Width = SystemParameters.WorkArea.Width;
            _controls.Left = SystemParameters.WorkArea.Left;
            _controls.Top = SystemParameters.WorkArea.Bottom - _controls.Height;
        }
        // Se estiver em modo janela normal, usamos a matemática baseada no Player (this)
        else
        {
            _controls.Width = this.ActualWidth;
            _controls.Left = this.Left;
            _controls.Top = this.Top + this.ActualHeight - _controls.Height;
        }
    }

    private void ReiniciarTimerInatividade()
    {
        if (_viewModel.IsFullscreen)
        {
            ShowControls();
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }
    }

    // --- Modo cinema ---
    private void ShowControls()
    {
        _controls.Visibility = Visibility.Visible;
        Mouse.OverrideCursor = Cursors.Arrow;

        _inactivityTimer.Stop();
        _inactivityTimer.Start();
    }

    private void HideControls()
    {
        _inactivityTimer.Stop();

        if (_viewModel.IsFullscreen)
        {
            _controls.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = Cursors.None;
        }
    }
    private void Player_Mouse(object sender, MouseEventArgs e)
    {
        if (_controls != null && _controls.Visibility != Visibility.Visible)
        {
            ShowControls();
        }

        // Se estiver em tela cheia, avisa o sistema para resetar o timer de inatividade
        if (_viewModel.IsFullscreen)
        {
            MouseDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;

            if (MyTitleBar != null)
            {
                MyTitleBar.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            // Volta para o modo janela
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _viewModel.IsFullscreen = false;

            if (MyTitleBar != null)
            {
                MyTitleBar.Visibility = Visibility.Visible;
            }
        }

        _viewModel.IsFullscreen = WindowState == WindowState.Maximized;

        // Executa o posicionamento um milissegundo depois, garantindo que o Windows já mudou de tamanho
        Dispatcher.BeginInvoke(new Action(() =>
        {
            PosicionarControles();
            ShowControls();
        }), DispatcherPriority.Render);
    }

    private void PlayerWindow_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] arquivos = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null && arquivos.Length > 0 &&
                ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant()))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }
    }

    private void PlayerWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] arquivos = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null && arquivos.Length > 0 &&
                ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant()))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }
    }

    private async void PlayerWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] arquivos = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null && arquivos.Length > 0)
            {
                string caminhoArquivo = arquivos[0];
                string extensao = Path.GetExtension(caminhoArquivo).ToLowerInvariant();

                if (ExtensoesVideo.Contains(extensao))
                {
                    Log.Salvar($"Arquivo de vídeo solto: {caminhoArquivo}");
                    await CarregarMidia(caminhoArquivo);
                }
                else
                {
                    MessageBox.Show("Formato de arquivo não suportado.", "Player",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && _viewModel.IsFullscreen)
        {
            ToggleFullscreen();
        }
    }
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;

    private static readonly string[] ExtensoesVideo = {
    ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm",
    ".m4v", ".ts", ".m2ts", ".vob", ".mpg", ".mpeg", ".3gp", ".ogv"
};
}
