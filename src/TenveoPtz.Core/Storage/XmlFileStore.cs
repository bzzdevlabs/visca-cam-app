using System;
using System.IO;
using System.Xml.Serialization;

namespace TenveoPtz.Core.Storage;

/// <summary>
/// Stores a document as an XML file. Writes are atomic (temp file + replace) and an unreadable
/// file is kept aside as <c>.corrupt</c> instead of being silently overwritten.
/// </summary>
public sealed class XmlFileStore<T> : IStore<T>
    where T : class, new()
{
    private readonly string path;
    private readonly XmlSerializer serializer = new(typeof(T));

    public XmlFileStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path is required.", nameof(path));
        }

        this.path = path;
    }

    public T Load()
    {
        if (!File.Exists(path))
        {
            return new T();
        }

        try
        {
            using var stream = File.OpenRead(path);
            return serializer.Deserialize(stream) as T ?? new T();
        }
        catch (InvalidOperationException)
        {
            File.Copy(path, path + ".corrupt", overwrite: true);
            return new T();
        }
    }

    public void Save(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            serializer.Serialize(stream, value);
        }

        if (File.Exists(path))
        {
            File.Replace(temp, path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temp, path);
        }
    }
}
