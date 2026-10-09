using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TenveoPtz.Core.Execution;

/// <summary>
/// Executes commands sequentially on a dedicated background thread so slow device I/O
/// never blocks the UI. Pending commands are drained before the thread stops.
/// </summary>
public sealed class BackgroundCommandExecutor : ICommandExecutor
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);

    private readonly object gate = new();
    private readonly LinkedList<PendingCommand> queue = new();
    private readonly Thread worker;
    private bool disposed;

    public BackgroundCommandExecutor(string threadName)
    {
        worker = new Thread(Run) { IsBackground = true, Name = threadName };
        worker.Start();
    }

    public event EventHandler<ErrorEventArgs>? Faulted;

    public void Post(Action command, string? coalesceKey = null)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        lock (gate)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundCommandExecutor));
            }

            if (coalesceKey is not null)
            {
                RemovePending(coalesceKey);
            }

            queue.AddLast(new PendingCommand(command, coalesceKey));
            Monitor.Pulse(gate);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Monitor.PulseAll(gate);
        }

        if (Thread.CurrentThread != worker)
        {
            worker.Join(ShutdownTimeout);
        }
    }

    private void RemovePending(string coalesceKey)
    {
        var node = queue.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.CoalesceKey == coalesceKey)
            {
                queue.Remove(node);
            }

            node = next;
        }
    }

    private void Run()
    {
        while (TryDequeue(out var command))
        {
            try
            {
                command.Action();
            }
            catch (Exception ex)
            {
                Faulted?.Invoke(this, new ErrorEventArgs(ex));
            }
        }
    }

    private bool TryDequeue(out PendingCommand command)
    {
        lock (gate)
        {
            while (queue.Count == 0 && !disposed)
            {
                Monitor.Wait(gate);
            }

            if (queue.Count == 0)
            {
                command = default;
                return false;
            }

            command = queue.First!.Value;
            queue.RemoveFirst();
            return true;
        }
    }

    private readonly struct PendingCommand
    {
        public PendingCommand(Action action, string? coalesceKey)
        {
            Action = action;
            CoalesceKey = coalesceKey;
        }

        public Action Action { get; }

        public string? CoalesceKey { get; }
    }
}
