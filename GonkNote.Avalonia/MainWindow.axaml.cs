using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace GonkNote.Avalonia;

public partial class MainWindow : Window
{
    /// <summary>Breite des linken Baum-Panels (siehe MainWindow.axaml, Border Dock=Left).</summary>
    private const double SidebarWidth = 300;

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

        // Workaround gegen den Fill-Panel-Measure-Quirk (§9.5): dem Inhaltsbereich eine
        // explizite Breite geben (= Fensterbreite − Seitenleiste), damit Umbruch/Zentrierung
        // greifen. Die Arrange-Breite stimmt ohnehin; nur der Measure braucht die feste Breite.
        SizeChanged += (_, _) => UpdateContentWidth();
        Loaded += (_, _) => UpdateContentWidth();
    }

    private void UpdateContentWidth() =>
        ContentHost.Width = Math.Max(0, ClientSize.Width - SidebarWidth - 1); // -1 = Trennlinie
}
