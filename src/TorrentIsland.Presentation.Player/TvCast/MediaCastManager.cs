using LibVLCSharp.Shared;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Presentation.Player.TvCast;

// Tv cast (WIP)
public sealed class MediaCastManager : IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer? _mediaPlayer;
    private readonly RendererDiscoverer _rendererDiscoverer;    
    private readonly List<CastDevice> _devices  = [];
    private readonly Lock _lock = new();
    private bool _disposed;
    private bool _isDiscovering;
    
    public IReadOnlyList<CastDevice> Devices
    {
        get { lock(_lock) return [.. _devices]; }
    }

    public CastDevice? CurrentDevice { get; private set; }
    
    public bool IsDiscovering => _isDiscovering;

    public MediaCastManager(LibVLC libVLC, MediaPlayer mediaPlayer)
    {
        _libVLC = libVLC ?? throw new ArgumentNullException(nameof(libVLC));
        _mediaPlayer = mediaPlayer ?? throw new ArgumentNullException(nameof(mediaPlayer));
        _rendererDiscoverer = new(_libVLC);
        _rendererDiscoverer.ItemAdded += OnItemAdded;
        _rendererDiscoverer.ItemDeleted += OnItemDeleted;
    }

    public bool Start()
    {
        if (_disposed) return false;

        var started = _rendererDiscoverer.Start();
        _isDiscovering = started;
        Log.Salvar($"[Cast] Start => {started}");

        if (started)
        {
            foreach (var r in _libVLC.RendererList)
                Log.Salvar($"[Cast] Protocolo disponível: {r.Name} <-> {r.LongName}");
        }

        return started;
    }

    public void Stop()
    {
        if (_disposed) return;

        if (_isDiscovering)
            _rendererDiscoverer.Stop();
            _isDiscovering = false;

        _devices.Clear();
    }

    public bool CastTo(CastDevice? device)
    {
        if (_disposed || device?.Item is null) return false;

        var ok = SetCast(device.Item);
        if (ok)
        {
            CurrentDevice = device;
            Log.Salvar($"[Cast] Reprodução enviada para {device.Name}");
        }
        else
        {
            Log.Salvar($"[Cast] Falha ao enviar para {device.Name}");
        }

        return ok;
    }

    public void StopCasting()
    {
        if (_disposed) return;

        SetCast(null);
        CurrentDevice = null;
        Log.Salvar("[Cast] Renderização local restaurada");
    }

    private bool SetCast(RendererItem? item)
    {
        if (_mediaPlayer is null) return false;
        var ok = _mediaPlayer.SetRenderer(item);
        return ok;
    }

    private void OnItemAdded(object? sender, RendererDiscovererItemAddedEventArgs e)
    {
        var device = new CastDevice
        {
            Name = e.RendererItem.Name,
            Item = e.RendererItem,
        };

        lock(_lock) _devices.Add(device);
        Log.Salvar($"[Cast] Descoberto: {device.Name} ({e.RendererItem.Type})");
    }

    private void OnItemDeleted(object? sender, RendererDiscovererItemDeletedEventArgs e)
    {
        CastDevice? removido;
        lock (_lock)
        {
            removido = _devices.FirstOrDefault(d =>
            d.Item is not null &&
            d.Item.NativeReference == e.RendererItem.NativeReference);

            if (removido is null) return;

            _devices.Remove(removido);
        }
        
        Log.Salvar($"[Cast] Removido: {removido.Name}");
        
        if (CurrentDevice == removido)
            StopCasting();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _rendererDiscoverer.ItemAdded -= OnItemAdded;
        _rendererDiscoverer.ItemDeleted -= OnItemDeleted;

        if (_isDiscovering)
            _rendererDiscoverer.Stop();

        _rendererDiscoverer.Dispose();
        GC.SuppressFinalize(this);
    }
}