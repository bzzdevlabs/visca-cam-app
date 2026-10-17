using System;
using TenveoPtz.Core.Storage;

namespace TenveoPtz.Core.Tests.Fakes;

internal sealed class MemoryStore<T> : IStore<T>
    where T : class, new()
{
    public T? Saved { get; private set; }

    public int SaveCount { get; private set; }

    /// <summary>Thrown by <see cref="Save"/>, e.g. a full disk.</summary>
    public Exception? FailWith { get; set; }

    public T Load() => Saved ?? new T();

    public void Save(T value)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        Saved = value;
        SaveCount++;
    }
}
