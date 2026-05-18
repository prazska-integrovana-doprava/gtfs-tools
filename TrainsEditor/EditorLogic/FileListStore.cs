using System;
using System.Collections.Generic;
using System.IO;

public class FileListStore
{
    private readonly string _filePath;
    private readonly HashSet<string> _files;

    // buffer pro nové položky (čekající na zápis)
    private readonly List<string> _pending = new List<string>();

    public FileListStore(string filePath)
    {
        _filePath = filePath;

        _files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        foreach (var line in File.ReadAllLines(_filePath))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                _files.Add(trimmed);
            }
        }
    }

    public bool Contains(string fileName)
    {
        return _files.Contains(fileName);
    }

    public bool Add(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // HashSet zajistí unikátnost
        if (_files.Add(fileName))
        {
            _pending.Add(fileName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Zapíše všechny nové položky do souboru.
    /// </summary>
    public void Flush()
    {
        if (_pending.Count == 0)
            return;

        // zajistí existenci adresáře
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using (var writer = new StreamWriter(_filePath, true))
        {
            foreach (var item in _pending)
            {
                writer.WriteLine(item);
            }
        }

        _pending.Clear();
    }

    public IReadOnlyCollection<string> GetAll()
    {
        return _files;
    }
}