using System.Windows.Input;

namespace TorrentIsland.Presentation.Player.Common;

/// <summary>
/// ICommand síncrono sem parâmetro. Reavalia CanExecute via CommandManager.
/// </summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// ICommand síncrono com parâmetro.
/// </summary>
public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<T?, bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter is T t)  return _canExecute?.Invoke(t) ?? true;
        if (parameter is null) return _canExecute?.Invoke(default) ?? true;
        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T t) _execute(t);
        else if (parameter is null) _execute(default);
    }
}