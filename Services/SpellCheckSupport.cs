using System.Runtime.InteropServices;

namespace GonkNote.Services;

/// <summary>
/// Prüft über die Windows-Rechtschreib-API (ISpellCheckerFactory), ob für eine Sprache
/// überhaupt ein Wörterbuch installiert ist. WPF nutzt ab Windows 8.1 dieselbe Plattform-
/// Prüfung – fehlt die Sprache in den Windows-Spracheinstellungen, gibt es keinerlei
/// Markierungen (dann ist nicht die App schuld, sondern das fehlende Sprachpaket).
/// </summary>
public static class SpellCheckSupport
{
    /// <summary>True, wenn Windows für <paramref name="bcp47"/> (z. B. "de-DE") ein Wörterbuch hat.</summary>
    public static bool IsSupported(string bcp47)
    {
        try
        {
            var factory = (ISpellCheckerFactory)new SpellCheckerFactoryClass();
            try
            {
                return factory.IsSupported(bcp47, out int supported) == 0 && supported != 0;
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }
        catch
        {
            return true; // Im Zweifel nicht blockieren/warnen
        }
    }

    [ComImport, Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
    private class SpellCheckerFactoryClass { }

    [ComImport, Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellCheckerFactory
    {
        // Vtable-Reihenfolge einhalten: get_SupportedLanguages steht vor IsSupported.
        [PreserveSig] int GetSupportedLanguages(out IntPtr ppEnum);
        [PreserveSig] int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag, out int value);
        // CreateSpellChecker(…) folgt danach – hier nicht benötigt.
    }
}
