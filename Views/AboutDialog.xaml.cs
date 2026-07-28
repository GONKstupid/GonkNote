using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using GonkNote.Services;

namespace GonkNote.Views;

/// <summary>
/// Über-Dialog: App-Infos plus das eingebettete README — gesetzt als formatierter Text,
/// nicht als roher Markdown-Quelltext (<see cref="MarkdownFlow"/>).
/// </summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "?"} – Phase 3";

        try
        {
            var res = Application.GetResourceStream(new Uri("pack://application:,,,/README.md"));
            if (res != null)
            {
                using var reader = new StreamReader(res.Stream);
                ReadmeView.Document = MarkdownFlow.ToFlowDocument(reader.ReadToEnd());
            }
        }
        catch
        {
            ReadmeView.Document = new FlowDocument(
                new Paragraph(new Run("README konnte nicht geladen werden.")));
        }
    }
}
