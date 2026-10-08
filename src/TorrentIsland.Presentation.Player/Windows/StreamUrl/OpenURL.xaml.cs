using System.Windows;
using System.Windows.Input;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.Windows.Main;
using Wpf.Ui.Controls;

namespace TorrentIsland.Presentation.Player.Windows.StreamUrl;

public partial class OpenURL : FluentWindow
{
    private readonly PlayerWindow _playerWindow;
    public OpenURL(PlayerWindow playerWindow, string? title)
    {
        _playerWindow = playerWindow;
        InitializeComponent();
        TitleName.Title = title;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) _playerWindow.AbrirMenu();
            else _playerWindow.FecharMenu();

            _playerWindow.ReiniciarTimerInatividade();
        };
    }

    private void OpenURL_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OpenURLSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnBuscar_OpenURL_Click(sender, e);
            Close();
            e.Handled = true;
        }
    }

    private async void BtnBuscar_OpenURL_Click(object sender, RoutedEventArgs e)
    {
        string url = OpenURLSearch.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        var mediaLoader = new MediaLoader(_playerWindow);

        await mediaLoader.InputAsync(url);
        Close();
    }
}