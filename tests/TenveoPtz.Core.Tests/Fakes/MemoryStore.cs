using TenveoPtz.Core.Storage;

namespace TenveoPtz.Core.Tests.Fakes;

internal sealed class MemoryStore<T> : IStore<T>
    where T : class, new()
{
    public T? Saved { get; private set; }

    public int SaveCount { get; private set; }

    public T Load() => Saved ?? new T();

    public void Save(T value)
    {
        Saved = value;
        SaveCount++;
    }
}
