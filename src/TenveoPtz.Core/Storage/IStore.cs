namespace TenveoPtz.Core.Storage;

/// <summary>Repository persisting a single document of type <typeparamref name="T"/>.</summary>
public interface IStore<T>
    where T : class
{
    /// <summary>Loads the stored document, or a new default instance when none exists.</summary>
    T Load();

    void Save(T value);
}
