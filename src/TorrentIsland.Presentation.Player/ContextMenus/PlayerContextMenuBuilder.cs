using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        return menu;
    }

    private object[] MenuToOpen() =>
    [
        // TODO all shortcuts
        BuildItem("Abrir mídia...", _viewModel.OpenMediaCommand, Key.O, ModifierKeys.Control),
        BuildItem("Abrir arquivo torrent...", _viewModel.OpenTorrentFileCommand, Key.Y, ModifierKeys.Control), // TODO
        BuildItem("Abrir URL Mídia Streaming", null, Key.P, ModifierKeys.Control), // TODO
        BuildItem("Abrir URL Youtube", null, Key.L, ModifierKeys.Control), // TODO
        BuildItem("Abrir Pasta...", null, Key.Y, ModifierKeys.Control), // TODO
        new Separator(),
        BuildItem("Carregar legenda externa...", _viewModel.OpenSubtitleExternalCommand, Key.U, ModifierKeys.Control),
        BuildItem("Carregar áudio externo...", null, Key.I, ModifierKeys.Control), // TODO
    ];

    private static MenuItem BuildItem(string header, ICommand? command = null,
        Key key = Key.None, ModifierKeys mods = ModifierKeys.None)
    {
        var item = new MenuItem
        {
            Header = header,
            Command = command,
            Style = _styleMI
        };
        if (key != Key.None)
            item.InputGestureText = FormatGesture(key, mods);
        return item;
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