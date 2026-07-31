using System.Windows.Threading;
using GonkNote.Core.Platform;

namespace GonkNote.Platform;

/// <summary>Wiederholung über einen <see cref="DispatcherTimer"/> — läuft auf dem UI-Faden.</summary>
public sealed class WpfUiScheduler : IUiScheduler
{
    public IDisposable Repeat(TimeSpan interval, Action tick)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (_, _) => tick();
        timer.Start();
        return new Subscription(timer);
    }

    private sealed class Subscription(DispatcherTimer timer) : IDisposable
    {
        public void Dispose() => timer.Stop();
    }
}
