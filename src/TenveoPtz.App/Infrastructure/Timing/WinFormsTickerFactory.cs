using System;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.App.Infrastructure.Timing;

internal sealed class WinFormsTickerFactory : ITickerFactory
{
    public ITicker Create(TimeSpan interval) => new WinFormsTicker(interval);
}
