using System.IO;
using System.Reflection;
using System.Windows;

namespace GonkNote.Views;

/// <summary>Über-Dialog: App-Infos plus eingebettetes README.</summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "?"} – Phase 2";

        try
        {
            var res = Application.GetResourceStream(new Uri("pack://application:,,,/README.md"));
            if (res != null)
            {
                using var reader = new StreamReader(res.Stream);
                ReadmeText.Text = reader.ReadToEnd();
            }
        }
        catch
        {
            ReadmeText.Text = "README konnte nicht geladen werden.";
        }
    }
}
