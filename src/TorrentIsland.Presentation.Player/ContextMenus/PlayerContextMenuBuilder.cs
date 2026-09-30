using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Presentation.Player.ViewModel;

namespace TorrentIsland.Presentation.Player.ContextMenus;

/// <summary>
/// Constrói o menu de contexto do player. Reconstruído a cada abertura
/// para refletir o estado atual do ViewModel.
/// </summary>
public sealed class PlayerContextMenuBuilder(PlayerViewModel viewModel)
{
    private readonly PlayerViewModel _viewModel = viewModel;
    private static Style _styleCtx =>
        (Style)System.Windows.Application.Current.FindResource("ClassicContextMenuStyle");

    private static Style _styleMI =>
        (Style)System.Windows.Application.Current.FindResource("ClassicMenuItemStyle");

    /// <summary>
    /// Cria um novo <see cref="ContextMenu"/> pronto para ser atribuído
    /// a um controle.
    /// </summary>
    public ContextMenu Build()
    {
        ContextMenuInstance(out var menu);
        menu.Items.Add(BuildSubmenu("Abrir", MenuToOpen()));
        menu.Items.Add(new Separator());
        menu.Items.Add(BuildSubmenu("Opções de tela", ScreenOptions()));
        menu.Items.Add(BuildSubmenu("Reprodução", Playback()));
        menu.Items.Add(BuildSubmenu("Vídeo", Video()));
        menu.Items.Add(BuildSubmenu("Áudio", AudioTrack()));
        menu.Items.Add(BuildSubmenu("Legenda", SubtitleTrack()));
        return menu;
    }

    private object[] Video() =>
    [
        BuildSubmenu("Torrent",
        [
            BuildItem("Procurar torrent na internet...", _viewModel.TorrentSearchCommand),
            BuildItem("Fila de torrents...", _viewModel.TorrentQueueCommand)
        ]),
    ];

    private object[] SubtitleTrack() =>
    [
        BuildSubmenu("Faixas", SubtitleItems()),        
        new Separator(),
        BuildItem("Ressincronizar legenda   +0,5s", customKey: "["),
        BuildItem("Ressincronizar legenda   -0,5s", customKey: "]"),
        BuildItem("Ressincronizar legenda   +5s", customKey: "Alt+["), // Todo
        BuildItem("Ressincronizar legenda   -5s", customKey: "Alt+]"), // Todo
        BuildItem("Ressincronizar legenda...", customKey: "Alt+R"), // Todo
    ];
    
    private object[] AudioTrack() =>
    [
        BuildSubmenu("Faixas", AudioItems()),
        BuildItem("Mutar / desmutar áudio", _viewModel.ToggleMuteCommand, Key.M),      
    ];
    
    private object[] Playback() =>
    [
        BuildItem("Reproduzir", _viewModel.TogglePlayCommand, customKey: "Espaço"),
        BuildItem("Pausar", _viewModel.TogglePlayCommand, customKey: "Espaço"),
        BuildItem("Parar", _viewModel.StopPlayerCommand),
    ];

    private object[] ScreenOptions() =>
    [
        BuildItem("Tela Cheia", _viewModel.ToggleFullscreenCommand, Key.F11),
    ];

    private object[] MenuToOpen() =>
    [
        // TODO all shortcuts
        BuildItem("Abrir mídia...", _viewModel.OpenMediaCommand, Key.O, ModifierKeys.Control),
        BuildItem("Abrir arquivo torrent...", _viewModel.OpenTorrentFileCommand, Key.Y, ModifierKeys.Control),
        BuildItem("Abrir URL Mídia Streaming", null, Key.P, ModifierKeys.Control), // TODO
        BuildItem("Abrir URL Youtube", null, Key.L, ModifierKeys.Control), // TODO
        BuildItem("Abrir Pasta...", null, Key.Y, ModifierKeys.Control), // TODO
        new Separator(),
        BuildItem("Carregar legenda externa...", _viewModel.OpenSubtitleExternalCommand, Key.U, ModifierKeys.Control),
        BuildItem("Carregar áudio externo...", null, Key.I, ModifierKeys.Control), // TODO
    ];

    private object[] AudioItems()
    {
        if (_viewModel.AudioTracks.Count == 0)
            return [BuildItem("Nenhum áudio disponível")];

        return [.. _viewModel.AudioTracks
            .Select(track => (object)BuildSubmenuItem(
                    track.Name,
                    _viewModel.SelectAudioCommand,
                    track.Id))];
    }

    private object[] SubtitleItems()
    {
        if (_viewModel.SubtitleTracks.Count == 0)
            return [BuildItem("Nenhuma legenda disponível")];

        return [.. _viewModel.SubtitleTracks
            .Select(track => (object)BuildSubmenuItem(
                    track.Name,
                    _viewModel.SelectSubtitleCommand,
                    track.Id))];
    }

    private static MenuItem BuildItem(string header, ICommand? command = null,
        Key key = Key.None, ModifierKeys mods = ModifierKeys.None, string? customKey = null)
    {
        var item = new MenuItem
        {
            Header = header,
            Command = command,
            Style = _styleMI
        };
        if (key != Key.None)
            item.InputGestureText = FormatGesture(key, mods);
        else
            item.InputGestureText = customKey;
        return item;
    }

    private static MenuItem BuildSubmenuItem(string header, ICommand cmd, object? cmdParam = null)
    {
        return new MenuItem
        {
            Header = header,
            IsCheckable = true,
            Command = cmd,
            CommandParameter = cmdParam,
            Style = _styleMI
        };
    }

    private static MenuItem BuildSubmenu(string header, object[] children)
    {
        var menu = new MenuItem { Header = header, Style = _styleMI };
        foreach (var child in children)
            menu.Items.Add(child);
        return menu;
    }

    private static void ContextMenuInstance(out ContextMenu menu)
    {
        menu = new ContextMenu
        {
            Style = _styleCtx
        };
    }

    private static string FormatGesture(Key key, ModifierKeys mods)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}