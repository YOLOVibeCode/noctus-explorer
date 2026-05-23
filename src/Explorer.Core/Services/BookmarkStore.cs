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
}
