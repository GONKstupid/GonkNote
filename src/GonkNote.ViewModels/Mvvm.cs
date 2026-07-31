using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GonkNote.ViewModels;

// System.Windows.Input.ICommand ist trotz des Namensraums kein WPF: der Typ liegt in
// System.ObjectModel und steht auch unter Linux und iOS zur Verfügung. Avalonia bindet
// gegen genau diese Schnittstelle — sie darf deshalb bleiben.

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Hing bis Phase 2 an WPFs <c>CommandManager.RequerySuggested</c> — der fragt nach
    /// jeder Eingabe von sich aus neu und existiert außerhalb von WPF nicht.
    /// <para>
    /// Der Ersatz ist <see cref="RaiseCanExecuteChanged"/>: wer etwas ändert, sagt es.
    /// Für die Befehle hier fällt das nicht ins Gewicht — <b>keiner</b> von ihnen
    /// gibt heute ein <c>canExecute</c> an, sie sind also immer ausführbar. Der
    /// Weckruf steht bereit, falls sich das ändert; wer ihn dann vergisst, bekommt
    /// einen Knopf, der grau bleibt, obwohl er dürfte.
    /// </para>
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
