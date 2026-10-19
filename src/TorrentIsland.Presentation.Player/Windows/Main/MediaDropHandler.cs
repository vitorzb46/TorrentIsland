using System.IO;
using System.Windows;
using TorrentIsland.Presentation.Player.Common;
using DragEventArgs = System.Windows.DragEventArgs;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public sealed class MediaDropHandler
{
    private readonly List<string> _files = [];
    private string? _file;
    private bool _video;
    private bool _torrent;

    public void OnDrop(object sender, DragEventArgs e)
    {
        NewIteration(e);

        if (!Utils.TryGetDataObject(e.Data, out string[] arquivos)) return;

        if (TryProcessMultipleFiles(arquivos)) return;

        ProcessSingleFile(arquivos);
    }

    private void NewIteration(DragEventArgs e)
    {
        _files.Clear();
        _file = null;
        _video = false;
        _torrent = false;
        e.Handled = true;
    }

    private bool TryProcessMultipleFiles(string[] arquivos)
    {
        if (arquivos.Length == 1) return false;

        _files.AddRange(arquivos);

        return true;
    }

    private void ProcessSingleFile(string[] arquivos)
    {
        _file = arquivos[0];
        string ext = Path.GetExtension(arquivos[0]).ToLowerInvariant();
        _video = Utils.ExtensoesVideo.Contains(ext);
        _torrent = Utils.ExtensaoTorrent.Contains(ext);
    }

    public bool HandleDragDrop(object sender, DragEventArgs e)
    {
        if (_file == null) return false;
        if (!_video && !_torrent) return false;

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
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
        if (_files.Count <= 1 || _files == null) return false;

        RemoveNonVideoFiles();

        if (_files.Count == 0) return false;

        var primeiro = _files[0];
        await window.CarregarMidiaAsync(primeiro).ConfigureAwait(false);

        await window.CarregarPastaAsync(_files).ConfigureAwait(false);

        return true;
    }

    private void RemoveNonVideoFiles()
    {
        for (int i = _files.Count - 1; i >= 0; i--)
        {
            bool isVideo = Utils.ExtensoesVideo.Contains(Path.GetExtension(_files[i]).ToLowerInvariant());
            if (!isVideo) _files.RemoveAt(i);
        }
    }

    private async Task LoadSingleFileAsync(PlayerWindow window)
    {
        if (_file == null) throw new FileFormatException("Arquivo não reconhecido!");

        if (_video) await window.CarregarMidiaAsync(_file);

        else if (_torrent) await window.CarregarStreamTorrentAsync(_file);

        else
        {
            MessageBox.Show("Formato de arquivo não suportado.", "Player",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}