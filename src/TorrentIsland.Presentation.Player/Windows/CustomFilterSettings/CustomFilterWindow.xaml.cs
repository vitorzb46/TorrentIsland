using System.Windows;
using System.Windows.Input;
using TorrentIsland.Application.Settings;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Infrastructure.Services;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace TorrentIsland.Presentation.Player.Windows.CustomFilterSettings;

public partial class CustomFilterWindow : FluentWindow
{
    public enum FilterType { Sharpen, Sepia, Gradfun }
    private readonly FilterType _filterType;

    public CustomFilterWindow(FilterType filterType)
    {
        InitializeComponent();
        _filterType = filterType;
        LoadCurrentValues();
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        PanelSharpen.Visibility = _filterType == FilterType.Sharpen ? Visibility.Visible : Visibility.Collapsed;
        PanelSepia.Visibility = _filterType == FilterType.Sepia ? Visibility.Visible : Visibility.Collapsed;
        PanelGradfun.Visibility = _filterType == FilterType.Gradfun ? Visibility.Visible : Visibility.Collapsed;

        TitleName.Title = _filterType switch
        {
            FilterType.Sharpen => "Filtro Sharpen",
            FilterType.Sepia => "Filtro Sepia",
            FilterType.Gradfun => "Filtro Gradfun",
            _ => "Filtro"
        };
    }

    private void LoadCurrentValues()
    {
        TxtSharpenSigma.Text = AppSettings.VlcSharpenSigma?.ToString("0.00");
        TxtSepiaIntensity.Text = AppSettings.VlcSepiaIntensity.ToString();
        TxtGradfunRadius.Text = AppSettings.VlcGradfunRadius?.ToString();
        TxtGradfunStrength.Text = AppSettings.VlcGradfunStrength?.ToString("0.0");
    }

    private async void Salvar_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateAndSaveValues()) return;

        var tempModel = new PreferencesModel();
        tempModel.LoadFromAppSettings();        
        await JsonFiles.SaveToFileAsync(tempModel, AppSettings.UserPreferencesJson);

        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();

    private void FilterWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private bool ValidateAndSaveValues()
    {
        try
        {
            switch (_filterType)
            {
                case FilterType.Sharpen:
                    if (float.TryParse(TxtSharpenSigma.Text, out float sigma))
                    {
                        if (sigma == -1 || (sigma >= 0.01f && sigma <= 2.0f))
                        {
                            AppSettings.VlcSharpenSigma = sigma;
                            AppSettings.VlcSharpenFilter = sigma != -1;
                            return true;
                        }
                    }
                    MessageBox.Show("Valor inválido. Digite entre 0.01 e 2.0, ou -1 para desativar.", "Erro");
                    return false;

                case FilterType.Sepia:
                    if (int.TryParse(TxtSepiaIntensity.Text, out int intensity))
                    {
                        if (intensity >= 0 && intensity <= 255)
                        {
                            AppSettings.VlcSepiaIntensity = intensity;
                            AppSettings.VlcSepiaFilter = intensity > 0;
                            return true;
                        }
                    }
                    MessageBox.Show("Valor inválido. Digite entre 0 e 255.", "Erro");
                    return false;

                case FilterType.Gradfun:
                    // Se ambos vazios, desativa
                    if (string.IsNullOrWhiteSpace(TxtGradfunRadius.Text) && string.IsNullOrWhiteSpace(TxtGradfunStrength.Text))
                    {
                        AppSettings.VlcGradfunFilter = false;
                        return true;
                    }

                    if (int.TryParse(TxtGradfunRadius.Text, out int radius) && float.TryParse(TxtGradfunStrength.Text, out float strength))
                    {
                        if (radius > 0 && strength > 0)
                        {
                            AppSettings.VlcGradfunRadius = radius;
                            AppSettings.VlcGradfunStrength = strength;
                            AppSettings.VlcGradfunFilter = true;
                            return true;
                        }
                    }
                    MessageBox.Show("Valores inválidos. Preencha ambos os campos com números positivos.", "Erro");
                    return false;

                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro");
            return false;
        }
    }
}