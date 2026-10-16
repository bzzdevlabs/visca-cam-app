using System;
using System.Collections.Generic;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>
/// Virtual time for the tickers of a test: <see cref="Advance"/> fires every running ticker as
/// often as its interval allows, in time order, without waiting.
/// </summary>
internal sealed class SimulatedClock : ITickerFactory
{
    private readonly List<ClockTicker> tickers = new();

    public TimeSpan Now { get; private set; }

    public ITicker Create(TimeSpan interval)
    {
        var ticker = new ClockTicker(this, interval);
        tickers.Add(ticker);
        return ticker;
    }

    public void Advance(TimeSpan duration)
    {
        var end = Now + duration;
        while (true)
        {
            ClockTicker? next = null;
            foreach (var ticker in tickers)
            {
                if (ticker.IsRunning && ticker.Due <= end && (next is null || ticker.Due < next.Due))
                {
                    next = ticker;
                }
            }

            if (next is null)
            {
                break;
            }

            Now = next.Due;
            next.Fire();
        }

        Now = end;
    }

    public void Advance(double seconds) => Advance(TimeSpan.FromSeconds(seconds));

    private sealed class ClockTicker : ITicker
    {
        private readonly SimulatedClock clock;
        private readonly TimeSpan interval;

        public ClockTicker(SimulatedClock clock, TimeSpan interval)
        {
            this.clock = clock;
            this.interval = interval;
        }

        public event EventHandler? Tick;

        public bool IsRunning { get; private set; }

        public TimeSpan Due { get; private set; }

        public void Start()
        {
            IsRunning = true;
            Due = clock.Now + interval;
        }

        public void Stop() => IsRunning = false;

        public void Fire()
        {
            Due += interval;
            Tick?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            IsRunning = false;
            clock.tickers.Remove(this);
        }
    }
}
