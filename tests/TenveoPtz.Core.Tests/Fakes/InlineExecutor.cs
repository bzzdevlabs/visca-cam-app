using System;
using System.IO;
using TenveoPtz.Core.Execution;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Executor running commands immediately on the caller's thread.</summary>
internal sealed class InlineExecutor : ICommandExecutor
{
    public event EventHandler<ErrorEventArgs>? Faulted;

    public bool Disposed { get; private set; }

    public void Post(Action command, string? coalesceKey = null)
    {
        try
        {
            command();
        }
        catch (Exception ex)
        {
            Faulted?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    public void Dispose() => Disposed = true;
}
