using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class DropStackService
{
    private readonly List<PathRef> _items = [];

    public IReadOnlyList<PathRef> Items => _items.AsReadOnly();

    public event EventHandler? ItemsChanged;

    public void Add(IReadOnlyList<PathRef> items)
    {
        bool added = false;
        foreach (var item in items)
        {
            if (!_items.Contains(item))
            {
                _items.Add(item);
                added = true;
            }
        }
        if (added) ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(PathRef item)
    {
        if (_items.Remove(item))
            ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }
}
