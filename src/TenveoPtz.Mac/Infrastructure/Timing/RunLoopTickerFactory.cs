using System;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Mac.Infrastructure.Timing;

internal sealed class RunLoopTickerFactory : ITickerFactory
{
    public ITicker Create(TimeSpan interval) => new RunLoopTicker(interval);
}
