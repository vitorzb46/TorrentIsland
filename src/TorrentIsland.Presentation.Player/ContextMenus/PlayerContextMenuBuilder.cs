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
        BuildItem("Ressincronizar legenda   +0,5s", _viewModel.DelaySpuCommand, (true, 0.5), customKey: "["),
        BuildItem("Ressincronizar legenda   -0,5s", _viewModel.DelaySpuCommand, (false, 0.5), customKey: "]"),
        BuildItem("Ressincronizar legenda   +5s", _viewModel.DelaySpuCommand, (true, 5.0), customKey: "Alt+["),
        BuildItem("Ressincronizar legenda   -5s", _viewModel.DelaySpuCommand, (false, 5.0), customKey: "Alt+]"),
        BuildItem("Ressincronizar legenda...", _viewModel.DelaySpuCommand, (false, 0.0), customKey: "Alt+R"),
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
        new Separator(),
        BuildSubmenu("Proporção (Ratio)", AspectRatioOptions()),
        BuildSubmenu("Zoom", ZoomOptions()),
        BuildSubmenu("Imagem", ImageOptions()),
    ];

    private object[] MenuToOpen() =>
    [
        // TODO all shortcuts
        BuildItem("Abrir mídia...", _viewModel.OpenMediaCommand, null, Key.O, ModifierKeys.Control),
        BuildItem("Abrir arquivo torrent...", _viewModel.OpenTorrentFileCommand, null, Key.Y, ModifierKeys.Control),
        BuildItem("Abrir URL Mídia Streaming", null, null, Key.P, ModifierKeys.Control), // TODO
        BuildItem("Abrir URL Youtube", null, null, Key.L, ModifierKeys.Control), // TODO
        BuildItem("Abrir Pasta...", null, null, Key.Y, ModifierKeys.Control), // TODO
        new Separator(),
        BuildItem("Carregar legenda externa...", _viewModel.OpenSubtitleExternalCommand, null, Key.U, ModifierKeys.Control),
        BuildItem("Carregar áudio externo...", null, null, Key.I, ModifierKeys.Control), // TODO
    ];

    private object[] ImageOptions()
    {
        var contrast = _viewModel.Contrast;
        var brightness = _viewModel.Brightness;
        var hue = _viewModel.Hue;
        var saturation = _viewModel.Saturation;
        var gamma = _viewModel.Gamma;

        return
        [
            BuildSubmenu("Brilho",
            [
                BuildRadioItem("25%",  _viewModel.SetBrightnessCommand, 0.25f, Math.Abs(brightness - 0.25f) < 0.01f),
                BuildRadioItem("50%",  _viewModel.SetBrightnessCommand, 0.50f, Math.Abs(brightness - 0.50f) < 0.01f),
                BuildRadioItem("75%",  _viewModel.SetBrightnessCommand, 0.75f, Math.Abs(brightness - 0.75f) < 0.01f),
                BuildRadioItem("100% (padrão)", _viewModel.SetBrightnessCommand, 1.00f, Math.Abs(brightness - 1.00f) < 0.01f),
                BuildRadioItem("125%", _viewModel.SetBrightnessCommand, 1.25f, Math.Abs(brightness - 1.25f) < 0.01f),
                BuildRadioItem("150%", _viewModel.SetBrightnessCommand, 1.50f, Math.Abs(brightness - 1.50f) < 0.01f),
                BuildRadioItem("175%", _viewModel.SetBrightnessCommand, 1.75f, Math.Abs(brightness - 1.75f) < 0.01f),
                BuildRadioItem("200%", _viewModel.SetBrightnessCommand, 2.00f, Math.Abs(brightness - 2.00f) < 0.01f),
            ]),

            BuildSubmenu("Contraste",
            [
                BuildRadioItem("0%",   _viewModel.SetContrastCommand, 0.00f, Math.Abs(contrast - 0.00f) < 0.01f),
                BuildRadioItem("50%",  _viewModel.SetContrastCommand, 0.50f, Math.Abs(contrast - 0.50f) < 0.01f),
                BuildRadioItem("100% (padrão)", _viewModel.SetContrastCommand, 1.00f, Math.Abs(contrast - 1.00f) < 0.01f),
                BuildRadioItem("150%", _viewModel.SetContrastCommand, 1.50f, Math.Abs(contrast - 1.50f) < 0.01f),
                BuildRadioItem("200%", _viewModel.SetContrastCommand, 2.00f, Math.Abs(contrast - 2.00f) < 0.01f),
            ]),

            BuildSubmenu("Matiz",
            [
                BuildRadioItem("-180", _viewModel.SetHueCommand, -180f, hue == -180),
                BuildRadioItem("-90",  _viewModel.SetHueCommand,  -90f, hue == -90),
                BuildRadioItem("0 (padrão)", _viewModel.SetHueCommand, 0f, hue == 0),
                BuildRadioItem("90",   _viewModel.SetHueCommand,   90f, hue == 90),
                BuildRadioItem("180",  _viewModel.SetHueCommand,  180f, hue == 180),
            ]),

            BuildSubmenu("Saturação",
            [
                BuildRadioItem("0%",   _viewModel.SetSaturationCommand, 0.0f, Math.Abs(saturation - 0.0f) < 0.01f),
                BuildRadioItem("50%",  _viewModel.SetSaturationCommand, 0.5f, Math.Abs(saturation - 0.5f) < 0.01f),
                BuildRadioItem("100% (padrão)", _viewModel.SetSaturationCommand, 1.0f, Math.Abs(saturation - 1.0f) < 0.01f),
                BuildRadioItem("150%", _viewModel.SetSaturationCommand, 1.5f, Math.Abs(saturation - 1.5f) < 0.01f),
                BuildRadioItem("200%", _viewModel.SetSaturationCommand, 2.0f, Math.Abs(saturation - 2.0f) < 0.01f),
                BuildRadioItem("250%", _viewModel.SetSaturationCommand, 2.5f, Math.Abs(saturation - 2.5f) < 0.01f),
                BuildRadioItem("300%", _viewModel.SetSaturationCommand, 3.0f, Math.Abs(saturation - 3.0f) < 0.01f),
            ]),

            BuildSubmenu("Gamma",
            [
                BuildRadioItem("0.5",  _viewModel.SetGammaCommand, 0.5f, Math.Abs(gamma - 0.5f) < 0.01f),
                BuildRadioItem("0.7",  _viewModel.SetGammaCommand, 0.7f, Math.Abs(gamma - 0.7f) < 0.01f),
                BuildRadioItem("1.0 (padrão)", _viewModel.SetGammaCommand, 1.0f, Math.Abs(gamma - 1.0f) < 0.01f),
                BuildRadioItem("1.5",  _viewModel.SetGammaCommand, 1.5f, Math.Abs(gamma - 1.5f) < 0.01f),
                BuildRadioItem("2.0",  _viewModel.SetGammaCommand, 2.0f, Math.Abs(gamma - 2.0f) < 0.01f),
                BuildRadioItem("3.0",  _viewModel.SetGammaCommand, 3.0f, Math.Abs(gamma - 3.0f) < 0.01f),
            ]),

            new Separator(),

            BuildItem("Resetar ajustes de imagem", _viewModel.ResetImageCommand, customKey: "Alt+R"),
        ];
    }

    private object[] AspectRatioOptions()
    {
        var current = _viewModel.CurrentAspectRatio;

        return
        [
            BuildRadioItem("Ajuste automático (padrão)",
                _viewModel.SetAspectRatioCommand, null, current is null),
            new Separator(),
            BuildRadioItem("Quadrado (1:1)",
                _viewModel.SetAspectRatioCommand, "1:1", current == "1:1"),
            BuildRadioItem("Formato clássico (4:3)",
                _viewModel.SetAspectRatioCommand, "4:3", current == "4:3"),
            BuildRadioItem("Monitor antigo (5:4)",
                _viewModel.SetAspectRatioCommand, "5:4", current == "5:4"),
            BuildRadioItem("HDTV (16:9)",
                _viewModel.SetAspectRatioCommand, "16:9", current == "16:9"),
            BuildRadioItem("Widescreen (16:10)",
                _viewModel.SetAspectRatioCommand, "16:10", current == "16:10"),            
            new Separator(),
            BuildRadioItem("Cinema largo (2.21:1)",
                _viewModel.SetAspectRatioCommand, "2.21:1", current == "2.21:1"),
            BuildRadioItem("CinemaScope (2.35:1)",
                _viewModel.SetAspectRatioCommand, "2.35:1", current == "2.35:1"),
            BuildRadioItem("Cinema moderno (2.39:1)",
                _viewModel.SetAspectRatioCommand, "2.39:1", current == "2.39:1"),
        ];
    }

    private object[] ZoomOptions()
    {
        var current = _viewModel.CurrentZoom;

        return
        [
            BuildRadioItem("Ajuste automático (padrão)",
                _viewModel.SetZoomCommand, 0f, Math.Abs(current) < 0.001f),
            BuildRadioItem("50%",
                _viewModel.SetZoomCommand, 0.5f, Math.Abs(current - 0.5f) < 0.001f),
            BuildRadioItem("100%",
                _viewModel.SetZoomCommand, 1.0f, Math.Abs(current - 1.0f) < 0.001f),
            BuildRadioItem("150%",
                _viewModel.SetZoomCommand, 1.5f, Math.Abs(current - 1.5f) < 0.001f),
            BuildRadioItem("200%",
                _viewModel.SetZoomCommand, 2.0f, Math.Abs(current - 2.0f) < 0.001f),
        ];
    }

    private object[] AudioItems()
    {
        if (_viewModel.AudioTracks.Count == 0)
            return [BuildItem("Nenhum áudio disponível")];

        return [.. _viewModel.AudioTracks
            .Select(track => (object)BuildRadioItem(
                    track.Name,
                    _viewModel.SelectAudioCommand,
                    track.Id,
                    _viewModel.CurrentAudio == track.Id))];
    }

    private object[] SubtitleItems()
    {
        if (_viewModel.SubtitleTracks.Count == 0)
            return [BuildItem("Nenhuma legenda disponível")];

        return [.. _viewModel.SubtitleTracks
            .Select(track => (object)BuildRadioItem(
                    track.Name,
                    _viewModel.SelectSubtitleCommand,
                    track.Id,
                    _viewModel.CurrentSpu == track.Id))];
    }

    private static MenuItem BuildItem(
        string header,
        ICommand? command = null,
        object? cmdParam = null,
        Key key = Key.None,
        ModifierKeys mods = ModifierKeys.None,
        string? customKey = null)
    {
        var item = new MenuItem
        {
            Header = header,
            Command = command,
            CommandParameter = cmdParam,
            Style = _styleMI
        };
        Gesture(key, mods, customKey, item);
        return item;
    }
    
    private static MenuItem BuildRadioItem(
        string header,
        ICommand cmd,
        object? cmdParam,
        bool isChecked,
        Key key = Key.None,
        ModifierKeys mods = ModifierKeys.None,
        string? customKey = null)
    {
        var item =  new MenuItem
        {
            Header = header,
            IsCheckable = true,
            IsChecked = isChecked,
            Command = cmd,
            CommandParameter = cmdParam,
            Style = _styleMI,
        };
        Gesture(key, mods, customKey, item);
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

    private static void Gesture(Key key, ModifierKeys mods, string? customKey, MenuItem item)
    {
        if (key != Key.None)
            item.InputGestureText = FormatGesture(key, mods);
        else
            item.InputGestureText = customKey;
    }
}