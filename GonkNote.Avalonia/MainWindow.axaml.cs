using Avalonia.Controls;
using Avalonia.Styling;

namespace GonkNote.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(ShellViewModel.DefaultDbPath);

        ThemeToggle.Click += (_, _) =>
            RequestedThemeVariant = RequestedThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        OpenEditor.Click += (_, _) => new EditorPrototypeWindow().Show(this);
        OpenProbe.Click += (_, _) => new MarkdownProbe().Show(this);
    }
}
