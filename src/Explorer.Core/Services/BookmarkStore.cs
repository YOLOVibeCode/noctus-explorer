using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class BookmarkStore
{
    private readonly List<Bookmark> _bookmarks = [];

    public IReadOnlyList<Bookmark> Bookmarks => _bookmarks.AsReadOnly();

    public event EventHandler? BookmarksChanged;

    public void Add(Bookmark bookmark)
    {
        _bookmarks.Add(bookmark);
        _bookmarks.Sort((a, b) => a.Order.CompareTo(b.Order));
        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Guid id)
    {
        var idx = _bookmarks.FindIndex(b => b.Id == id);
        if (idx >= 0)
        {
            _bookmarks.RemoveAt(idx);
            BookmarksChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Reorder(Guid id, int newIndex)
    {
        var idx = _bookmarks.FindIndex(b => b.Id == id);
        if (idx < 0) return;

        var item = _bookmarks[idx];
        _bookmarks.RemoveAt(idx);
        _bookmarks.Insert(Math.Clamp(newIndex, 0, _bookmarks.Count), item);

        // Renumber orders
        for (int i = 0; i < _bookmarks.Count; i++)
            _bookmarks[i] = _bookmarks[i] with { Order = i };

        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<string> GetGroups()
        => _bookmarks
            .Select(b => b.Group)
            .Where(g => g is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;

    // --- Persistence ---

    /// <summary>Serialize the current bookmarks to a JSON string for storage.</summary>
    public string ToJson()
    {
        var dtos = _bookmarks.Select(b => new BookmarkDto
        {
            Id = b.Id.ToString("N"),
            Name = b.Name,
            Path = b.Target.FullPath,
            Group = b.Group,
            Order = b.Order,
        }).ToList();
        return System.Text.Json.JsonSerializer.Serialize(dtos);
    }

    /// <summary>Replace the current bookmark list from a JSON string produced by <see cref="ToJson"/>.</summary>
    public void LoadFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var dtos = System.Text.Json.JsonSerializer.Deserialize<List<BookmarkDto>>(json);
            if (dtos is null) return;

            _bookmarks.Clear();
            foreach (var dto in dtos.OrderBy(d => d.Order))
            {
                if (string.IsNullOrWhiteSpace(dto.Path) || string.IsNullOrWhiteSpace(dto.Name))
                    continue;
                var id = Guid.TryParseExact(dto.Id, "N", out var g) ? g : Guid.NewGuid();
                _bookmarks.Add(new Bookmark(id, dto.Name,
                    new PathRef(dto.Path, isDirectory: true), dto.Group, dto.Order));
            }
            BookmarksChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Malformed JSON — silently ignore so a corrupt settings file
            // doesn't kill startup. The user's bookmarks are lost but the app launches.
        }
    }

    private sealed class BookmarkDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? Group { get; set; }
        public int Order { get; set; }
    }
}
