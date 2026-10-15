using System.Timers;
using System.Windows.Threading;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.ViewModel;
using Timer = System.Timers.Timer;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public class PlayerSessionTimers : IDisposable
{
    private readonly PlayerViewModel _viewModel;
    private readonly IManagers _managers;
    private readonly DispatcherTimer _inactivityTimer = new();
    private readonly DispatcherTimer _osdTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly Timer _autosaveTimer = new() { Interval = TimeSpan.FromSeconds(30).TotalMilliseconds };
    
    private bool _disposed;
    private int _saving;
    private int _menusAbertos;

    public bool MenuAberto => _menusAbertos > 0;

    public PlayerSessionTimers(PlayerViewModel viewModel, IManagers managers)
    {
        _viewModel = viewModel;
        _managers = managers;
        _autosaveTimer.Elapsed += OnAutosaveElapsed;
        AutoSaverStart();
    }
    
    private async Task<bool> TryAutoSaveEngineAsync(IManagers managers)
    {
        if (Interlocked.CompareExchange(ref _saving, 1, 0) != 0) return false;
        return await AutoSaveAsync(managers, "Autosave").ConfigureAwait(false);
    }

    public async Task<bool> SaveEngineOnCloseAsync(IManagers managers, string msg)
    {
        await AutoSaveAsync(managers, msg).ConfigureAwait(false);
        AutoSaverDispose();
        return true;
    }

    private async Task<bool> AutoSaveAsync(IManagers managers, string msg)
    {
        try
        {
            if (!managers.GetEngine()) return false;
            await managers.SaveEngine().ConfigureAwait(false);
        }
        catch (Exception ex) { Log.Salvar($"[{msg}] {ex.Message}"); }

        finally { Interlocked.Exchange(ref _saving, 0); }

        return true;
    }

    public void ReiniciarTimerInatividade(double time = 5)
    {
        if (!_viewModel.IsFullscreen ||
            (MenuAberto && Math.Abs(time - 5) < 0.01)) return;
        InactivityStop();
        _inactivityTimer.Interval = TimeSpan.FromSeconds(time);
        InactivityStart();
    }

    public void InactivityTick(Action action)
    {
        _inactivityTimer.Tick += (_, _) =>
        {
            action();
        };
    }

    public void InactivityStart() => _inactivityTimer?.Start();

    public void InactivityStop() => _inactivityTimer?.Stop();

    public void AutoSaverStart() => _autosaveTimer?.Start();

    public void AutoSaverStop() => _autosaveTimer?.Stop();

    public void AutoSaverDispose() => _autosaveTimer?.Dispose();

    public void ReiniciarTimerOsd()
    {
        OsdStop();
        OsdStart();
    }

    public void OsdTick(Action action)
    {
        _osdTimer.Tick += (_, _) =>
        {
            action();
        };
    }

    public void OsdStart() => _osdTimer?.Start();

    public void OsdStop() => _osdTimer?.Stop();

    public void AbrirMenu() => _menusAbertos++;

    public void FecharMenu()
    {
        if (_menusAbertos == 0) return;

        _menusAbertos--;
    }

    private async void OnAutosaveElapsed(object? sender, ElapsedEventArgs e)
    {
        await TryAutoSaveEngineAsync(_managers); 
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        InactivityStop();
        AutoSaverStop();
        OsdStop();

        _autosaveTimer.Elapsed -= OnAutosaveElapsed;
        AutoSaverDispose();

        GC.SuppressFinalize(this);
    }
}