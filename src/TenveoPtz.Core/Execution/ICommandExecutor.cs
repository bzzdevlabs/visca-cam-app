using System;
using System.IO;

namespace TenveoPtz.Core.Execution;

/// <summary>Runs device commands one at a time, away from the caller's thread.</summary>
public interface ICommandExecutor : IDisposable
{
    /// <summary>Raised when a posted command throws. May be raised on any thread.</summary>
    event EventHandler<ErrorEventArgs>? Faulted;

    /// <summary>
    /// Queues <paramref name="command"/>. When <paramref name="coalesceKey"/> is set, any queued
    /// command with the same key that has not started yet is replaced (latest intent wins).
    /// </summary>
    void Post(Action command, string? coalesceKey = null);
}
