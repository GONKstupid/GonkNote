using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Threading;

namespace GonkNote.StylusProbe;

public partial class MainWindow : Window
{
    private readonly string? _berichtPfad =
        Environment.GetEnvironmentVariable("STYLUS_BERICHT");

    private DispatcherTimer? _schreiber;

    public MainWindow()
    {
        InitializeComponent();

        if (string.IsNullOrWhiteSpace(_berichtPfad))
            return;

        // Laufend statt nur beim Schliessen wegschreiben: wird der Prozess
        // abgeraeumt (Timeout, Kill, Absturz), waere die Messung sonst verloren.
        _schreiber = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _schreiber.Tick += (_, _) => BerichtSchreiben();
        _schreiber.Start();
    }

    private void BerichtSchreiben()
    {
        try
        {
            File.WriteAllText(_berichtPfad!, Flaeche.Bericht() + Environment.NewLine);
        }
        catch (IOException)
        {
            // Naechster Tick versucht es erneut
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _schreiber?.Stop();

        var bericht = Flaeche.Bericht();
        Console.WriteLine(bericht);
        if (!string.IsNullOrWhiteSpace(_berichtPfad))
            BerichtSchreiben();
    }
}
