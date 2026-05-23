using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class NavigationHistory
{
    private readonly List<PathRef> _history = [];
    private int _position;

    public NavigationHistory(PathRef initialLocation)
    {
        _history.Add(initialLocation);
        _position = 0;
    }

    public PathRef Current => _history[_position];

    public bool CanGoBack => _position > 0;

    public bool CanGoForward => _position < _history.Count - 1;

    public void Push(PathRef location)
    {
        if (location == Current) return;

        // Truncate forward history
        if (_position < _history.Count - 1)
            _history.RemoveRange(_position + 1, _history.Count - _position - 1);

        _history.Add(location);
        _position = _history.Count - 1;
    }

    public PathRef GoBack()
    {
        if (!CanGoBack)
            throw new InvalidOperationException("No back history available.");
        _position--;
        return Current;
    }

    public PathRef GoForward()
    {
        if (!CanGoForward)
            throw new InvalidOperationException("No forward history available.");
        _position++;
        return Current;
    }
}
