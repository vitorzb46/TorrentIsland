using System.Windows.Input;
using Wpf.Ui.Controls;

namespace TorrentIsland.Presentation.Player.Windows.Settings;

public sealed partial class Preferences : FluentWindow
{
    public Preferences()
    {
        InitializeComponent();
    }
    
    private void Preferences_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
