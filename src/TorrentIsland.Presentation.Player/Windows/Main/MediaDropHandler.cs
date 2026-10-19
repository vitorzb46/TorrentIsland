using System.IO;
using System.Windows;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public sealed class MediaDropHandler
{
    private readonly List<string> _files = [];
    private string? _file;
    private bool _video;
    private bool _torrent;

    private DragEventArgs? _e;

    public void OnDrop(object sender, DragEventArgs e)
    {
        if (!Utils.TryGetDataObject(e.Data, out string[] arquivos)) return;

        if (TryProcessMultipleFiles(arquivos)) return;

        ProcessSingleFile(arquivos);

        _e = e;
    }

    private bool TryProcessMultipleFiles(string[] arquivos)
    {
        if (arquivos.Length == 1) return false;

        foreach (string f in arquivos)
        {
            _files.Add(f);
        }

        return true;
    }

    private void ProcessSingleFile(string[] arquivos)
    {
        _file = arquivos[0];
        _video = Utils.ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant());
        _torrent = Utils.ExtensaoTorrent.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant());
    }

    public bool HandleDragDrop()
    {
        if (_file == null) return false;
        if (!_video && !_torrent) return false;

        _e?.Effects = DragDropEffects.Copy;
        _e?.Handled = true;
        return true;
    }

    public async Task LoadDroppedMediaAsync(PlayerWindow window)
    {
        bool v = await TryLoadMultipleFilesAsync(window);
        if (v) return;

        await LoadSingleFileAsync(window);
    }

    private async Task<bool> TryLoadMultipleFilesAsync(PlayerWindow window)
    {
        if (_files?.Count == 1 || _files == null) return false;

        RemoveNonVideoFiles();

        var primeiro = _files[0];
        await window.CarregarMidiaAsync(primeiro).ConfigureAwait(false);

        await window.CarregarPastaAsync(_files).ConfigureAwait(false);

        return true;
    }

    private void RemoveNonVideoFiles()
    {
        for (int i = 1; i < _files?.Count; i++)
        {
            bool isVideo = Utils.ExtensoesVideo.Contains(Path.GetExtension(_files[i]).ToLowerInvariant());
            if (!isVideo) _files.RemoveAt(i);
        }
    }

    private async Task LoadSingleFileAsync(PlayerWindow window)
    {
        if (_file == null) return;

        if (_video) await window.CarregarMidiaAsync(_file);

        else if (_torrent) await window.CarregarStreamTorrentAsync(_file);

        else
        {
            MessageBox.Show("Formato de arquivo não suportado.", "Player",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}