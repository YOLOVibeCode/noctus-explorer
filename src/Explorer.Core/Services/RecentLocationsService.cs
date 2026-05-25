using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

/// <summary>
/// Tracks recently visited folder locations. Most-recent first.
/// Deduplicates on visit and caps the list at <see cref="MaxItems"/>.
/// </summary>
public sealed class RecentLocationsService
{
    private readonly List<PathRef> _items = [];

    public int MaxItems { get; init; } = 20;

    public IReadOnlyList<PathRef> Recent => _items.AsReadOnly();

    public event EventHandler? Changed;

    /// <summary>Record a visit. Promotes the location to the top if already present.</summary>
    public void Visit(PathRef location)
    {
        // Move-to-front if already present
        var existing = _items.FindIndex(p => p.Equals(location));
        if (existing == 0) return;   // already at top — no change

        if (existing > 0)
            _items.RemoveAt(existing);

        _items.Insert(0, location);

        // Cap the list
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
                    _items.Add(new PathRef(p, isDirectory: true));
            if (_items.Count > MaxItems) _items.RemoveRange(MaxItems, _items.Count - MaxItems);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch { /* corrupt settings — ignore */ }
    }
}
