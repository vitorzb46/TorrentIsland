using System.Windows;
using System.Windows.Input;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Presentation.Player.ViewModel;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace TorrentIsland.Presentation.Player.Windows.Settings;

public partial class Preferences : FluentWindow
{
    private readonly PreferencesModel _model;
    private bool _isPropChanged = false;

    public Preferences()
    {
        InitializeComponent();
        _model = new PreferencesModel();
        DataContext = _model;
        _model.LoadFromJsonOrDefaults();
        _model.PropertyChanged += (s, e) => _isPropChanged = true;
    }

    public void ReloadData()
    {
        _model.LoadFromAppSettings();
        _isPropChanged = false;
    }
    
    private async void Salvar_Click(object sender, RoutedEventArgs e)
    {
        await SalvarAlteracoesAsync();
        Close();
    }

    private async Task SalvarAlteracoesAsync()
    {
        _model.SaveInAppSettings();
        await JsonFiles.SaveToFileAsync(_model, AppSettings.UserPreferencesJson);
        _isPropChanged = false;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        _isPropChanged = false; 
        Close();
    }

    private void Preferences_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _isPropChanged = false;
            Close();
            e.Handled = true;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isPropChanged)
        {
            var result = MessageBox.Show(
                "Você tem alterações não salvas. Deseja salvar antes de sair?", 
                "Alterações não salvas", 
                MessageBoxButton.YesNoCancel, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _model.SaveInAppSettings();
                JsonFiles.SaveToFile(_model, AppSettings.UserPreferencesJson);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true; 
            }
        }

        base.OnClosing(e);
    }
}