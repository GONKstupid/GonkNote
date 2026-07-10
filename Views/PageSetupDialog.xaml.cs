using System.Windows;
using GonkNote.Models;

namespace GonkNote.Views;

/// <summary>Dialog für Seitenmuster, Farbton und (bei Notizbüchern) Format/Ausrichtung.</summary>
public partial class PageSetupDialog : Window
{
    public PageBackground Pattern { get; private set; }
    public PageShade Shade { get; private set; }
    public float PageWidth { get; private set; }
    public float PageHeight { get; private set; }
    public bool ApplyAsDefault => AsDefault.IsChecked == true;

    public PageSetupDialog(WbPage page, bool showSize)
    {
        InitializeComponent();
        if (!showSize) SizeSection.Visibility = Visibility.Collapsed;

        (page.Background switch
        {
            PageBackground.Lines => BgLines,
            PageBackground.Grid => BgGrid,
            PageBackground.Dots => BgDots,
            _ => BgBlank,
        }).IsChecked = true;

        (page.Shade switch
        {
            PageShade.Light => ShadeLight,
            PageShade.Dark => ShadeDark,
            _ => ShadeAuto,
        }).IsChecked = true;

        bool landscape = page.Width > page.Height;
        float longSide = Math.Max(page.Width, page.Height);
        (longSide > WhiteboardDoc.A4Height + 1 ? SizeA3 : SizeA4).IsChecked = true;
        (landscape ? OrientLandscape : OrientPortrait).IsChecked = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Pattern = BgLines.IsChecked == true ? PageBackground.Lines
            : BgGrid.IsChecked == true ? PageBackground.Grid
            : BgDots.IsChecked == true ? PageBackground.Dots
            : PageBackground.Blank;

        Shade = ShadeLight.IsChecked == true ? PageShade.Light
            : ShadeDark.IsChecked == true ? PageShade.Dark
            : PageShade.Auto;

        float w = SizeA3.IsChecked == true ? WhiteboardDoc.A3Width : WhiteboardDoc.A4Width;
        float h = SizeA3.IsChecked == true ? WhiteboardDoc.A3Height : WhiteboardDoc.A4Height;
        bool landscape = OrientLandscape.IsChecked == true;
        PageWidth = landscape ? h : w;
        PageHeight = landscape ? w : h;

        DialogResult = true;
    }
}
