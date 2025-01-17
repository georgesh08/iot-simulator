using System.Diagnostics;

namespace Utils;

public class PeriodicalScheduler : IDisposable
{
    private Timer? timer;
    private long intervalTicks;
    private readonly Action action;
    private readonly Stopwatch stopwatch;
    private readonly bool relative;
    private readonly object @lock = new();
    private bool isDisposed;

    public TimeSpan Interval
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref intervalTicks));
        set
        {
            if (isDisposed)
            {
                return;
            }

            if (value <= TimeSpan.Zero)
            {
	            throw new ArgumentException("Interval must be positive.", nameof(value));
            }

            Interlocked.Exchange(ref intervalTicks, value.Ticks);
        }
    }

    public bool IsRunning => timer != null;

    public PeriodicalScheduler(Action action, TimeSpan interval, bool relative = true)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        if (interval <= TimeSpan.Zero)
        {
	        throw new ArgumentException("Interval must be positive.", nameof(interval));
        }

        intervalTicks = interval.Ticks;
        this.relative = relative;
        stopwatch = new Stopwatch();
    }

    public void Start() => Start(0);

    public void Start(int dueTime)
    {
        lock (@lock)
        {
            if (IsRunning)
            {
                return;
            }

            timer = new Timer(_ => PerformAction());
            timer.Change(dueTime, Timeout.Infinite);
        }
    }

    public void Stop()
    {
        if (isDisposed)
        {
            return;
        }
        
        lock (@lock)
        {
            timer = Interlocked.Exchange(ref timer, null);
        }

        if (timer == null)
        {
	        return;
        }

        timer.Change(Timeout.Infinite, Timeout.Infinite);
        timer.Dispose();
    }
    
    public void Dispose()
    {
	    Dispose(true);
    }

    private void PerformAction()
    {
        try
        {
            stopwatch.Restart();
            action();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
        finally
        {
            stopwatch.Stop();
            
            if (timer != null)
            {
                try
                {
                    var dueTime = relative 
                        ? Interval 
                        : TimeSpan.FromMilliseconds(Math.Max(0, Interval.TotalMilliseconds - stopwatch.ElapsedMilliseconds));
                    
                    timer.Change(dueTime, new TimeSpan(Timeout.Infinite));
                }
                catch (ObjectDisposedException)
                {
                    // Timer was disposed, ignore
                }
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed)
        {
	        return;
        }

        if (disposing)
        {
            Stop();
        }
        
        isDisposed = true;
    }
    
    public event Action<Exception>? OnError;
}
