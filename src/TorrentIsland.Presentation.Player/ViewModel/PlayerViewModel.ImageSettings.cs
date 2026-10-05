using LibVLCSharp.Shared;
using System.Windows.Input;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    // SETTINGS CONSTANTS
    private const float ContrastMin = 0.0f, ContrastMax = 2.0f, ContrastDefault = 1.0f;

    private const float BrightnessMin = 0.0f, BrightnessMax = 2.0f, BrightnessDefault = 1.0f;

    private const int HueMin = -180, HueMax = 180, HueDefault = 0;

    private const float SaturationMin = 0.0f, SaturationMax = 3.0f, SaturationDefault = 1.0f;

    private const float GammaMin = 0.01f, GammaMax = 10.0f, GammaDefault = 1.0f;

    // BACKING FIELDS
    private float _contrast = ContrastDefault;
    private float _brightness = BrightnessDefault;
    private int _hue = HueDefault;
    private float _saturation = SaturationDefault;
    private float _gamma = GammaDefault;

    // PROPERTIES
    public float Contrast
    {
        get => _contrast;
        private set
        {
            var estimate = Math.Clamp(value, ContrastMin, ContrastMax);
            if (Math.Abs(_contrast - estimate) < 0.001f) return;
            _contrast = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, estimate);
            OnPropertyChanged();
        }
    }

    public float Brightness
    {
        get => _brightness;
        private set
        {
            var estimate = Math.Clamp(value, BrightnessMin, BrightnessMax);
            if (Math.Abs(_brightness - estimate) < 0.001f) return;
            _brightness = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, estimate);
            OnPropertyChanged();
        }
    }

    public int Hue
    {
        get => _hue;
        private set
        {
            var estimate = Math.Clamp(value, HueMin, HueMax);
            if (_hue == estimate) return;
            _hue = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Hue, estimate);
            OnPropertyChanged();
        }
    }

    public float Saturation
    {
        get => _saturation;
        private set
        {
            var estimate = Math.Clamp(value, SaturationMin, SaturationMax);
            if (Math.Abs(_saturation - estimate) < 0.001f) return;
            _saturation = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, estimate);
            OnPropertyChanged();
        }
    }

    public float Gamma
    {
        get => _gamma;
        private set
        {
            var estimate = Math.Clamp(value, GammaMin, GammaMax);
            if (Math.Abs(_gamma - estimate) < 0.001f) return;
            _gamma = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Gamma, estimate);
            OnPropertyChanged();
        }
    }

    public string? CurrentAspectRatio
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public float CurrentZoom
    {
        get;
        private set
        {
            if (Math.Abs(field - value) < 0.001f) return;
            field = value;
            OnPropertyChanged();
        }
    }

    // COMMANDS
    private ICommand? _setAspectRatioCommand;
    public ICommand SetAspectRatioCommand =>
        _setAspectRatioCommand ??= new RelayCommand<string>(SetAspectRatio);

    private ICommand? _setZoomCommand;
    public ICommand SetZoomCommand =>
        _setZoomCommand ??= new RelayCommand<float>(SetZoom);

    private ICommand? _setContrastCommand;
    public ICommand SetContrastCommand =>
        _setContrastCommand ??= new RelayCommand<float>(value => Contrast = value);

    private ICommand? _setBrightnessCommand;
    public ICommand SetBrightnessCommand =>
        _setBrightnessCommand ??= new RelayCommand<float>(value => Brightness = value);

    private ICommand? _setHueCommand;
    public ICommand SetHueCommand =>
        _setHueCommand ??= new RelayCommand<float>(value => Hue = (int)value);

    private ICommand? _setSaturationCommand;
    public ICommand SetSaturationCommand =>
        _setSaturationCommand ??= new RelayCommand<float>(value => Saturation = value);

    private ICommand? _setGammaCommand;
    public ICommand SetGammaCommand =>
        _setGammaCommand ??= new RelayCommand<float>(value => Gamma = value);

    private ICommand? _resetImageCommand;
    public ICommand ResetImageCommand =>
        _resetImageCommand ??= new RelayCommand(() =>
        {
            Contrast = ContrastDefault;
            Brightness = BrightnessDefault;
            Hue = HueDefault;
            Saturation = SaturationDefault;
            Gamma = GammaDefault;
        });

    // METHODS
    public void IncrementBrightness(float step = 0.05f) => Brightness = _brightness + step;

    public void DecrementBrightness(float step = 0.05f) => Brightness = _brightness - step;

    public void IncrementContrast(float step = 0.05f) => Contrast = _contrast + step;

    public void DecrementContrast(float step = 0.05f) => Contrast = _contrast - step;

    public void IncrementHue(int step = 5) => Hue = _hue + step;

    public void DecrementHue(int step = 5) => Hue = _hue - step;

    public void IncrementSaturation(float step = 0.1f) => Saturation = _saturation + step;

    public void DecrementSaturation(float step = 0.1f) => Saturation = _saturation - step;

    public void IncrementGamma(float step = 0.1f) => Gamma = _gamma + step;

    public void DecrementGamma(float step = 0.1f) => Gamma = _gamma - step;

    private void SetAspectRatio(string? ratio)
    {
        CurrentAspectRatio = ratio;
        _mediaPlayer.AspectRatio = ratio;
    }

    private void SetZoom(float scale)
    {
        CurrentZoom = scale;
        _mediaPlayer.Scale = scale;
    }
}
