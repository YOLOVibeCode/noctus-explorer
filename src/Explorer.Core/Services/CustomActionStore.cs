using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class CustomActionStore
{
    private readonly List<CustomAction> _actions = [];

    public IReadOnlyList<CustomAction> Actions => _actions.AsReadOnly();

    public event EventHandler? ActionsChanged;

    public void Add(CustomAction action)
    {
        _actions.Add(action);
        _actions.Sort((a, b) => a.Order.CompareTo(b.Order));
        ActionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Update(CustomAction action)
    {
        var idx = _actions.FindIndex(a => a.Id == action.Id);
        if (idx >= 0)
        {
            _actions[idx] = action;
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Remove(Guid id)
    {
        var idx = _actions.FindIndex(a => a.Id == id);
        if (idx >= 0)
        {
            _actions.RemoveAt(idx);
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Reorder(Guid id, int newIndex)
    {
        var idx = _actions.FindIndex(a => a.Id == id);
        if (idx < 0) return;

        var item = _actions[idx];
        _actions.RemoveAt(idx);
        _actions.Insert(Math.Clamp(newIndex, 0, _actions.Count), item);

        for (int i = 0; i < _actions.Count; i++)
            _actions[i] = _actions[i] with { Order = i };

        ActionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public CustomAction? Duplicate(Guid id)
    {
        var original = _actions.FirstOrDefault(a => a.Id == id);
        if (original is null) return null;

        var dupe = original with
        {
            Id = Guid.NewGuid(),
            Label = $"{original.Label} (Copy)",
            Order = _actions.Count,
            RegisterWithOS = false
        };
        _actions.Add(dupe);
        ActionsChanged?.Invoke(this, EventArgs.Empty);
        return dupe;
    }
}
