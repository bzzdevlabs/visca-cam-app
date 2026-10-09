using System;

namespace TenveoPtz.Core.Execution;

public interface ITickerFactory
{
    ITicker Create(TimeSpan interval);
}
