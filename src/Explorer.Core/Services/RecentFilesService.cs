using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

/// <summary>
/// Tracks files the user has actively accessed (selected, opened, copied/moved).
/// Most-recent first, deduplicating by path. Same semantics as
/// <see cref="RecentLocationsService"/> but for files rather than folders.
/// </summary>
public sealed class RecentFilesService
{
    private readonly List<PathRef> _items = [];

    public int MaxItems { get; init; } = 20;

    public IReadOnlyList<PathRef> Recent => _items.AsReadOnly();

    public event EventHandler? Changed;

    /// <summary>Record an access. Promotes the file to the top if already present.</summary>
    public void Access(PathRef file)
    {
        var existing = _items.FindIndex(p => p.Equals(file));
        if (existing == 0) return;

        if (existing > 0)
            _items.RemoveAt(existing);

        _items.Insert(0, file);

        while (_items.Count > MaxItems)
            _items.RemoveAt(_items.Count - 1);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string ToJson() => System.Text.Json.JsonSerializer.Serialize(_items.Select(p => p.FullPath).ToList());

    public void LoadFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            if (paths is null) return;
            _items.Clear();
            foreach (var p in paths)
                if (!string.IsNullOrWhiteSpace(p))
                    _items.Add(new PathRef(p, isDirectory: false));
            if (_items.Count > MaxItems) _items.RemoveRange(MaxItems, _items.Count - MaxItems);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch { /* corrupt settings — ignore */ }
    }
}
