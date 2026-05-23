using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Builds and shows the composite context menu (Noctus items + custom actions + OS native items).
/// </summary>
public interface IContextMenuService
{
    void ShowContextMenu(
        IReadOnlyList<PathRef> items,
        ScreenPoint position,
        IReadOnlyList<CustomAction> customActions);
}

public readonly record struct ScreenPoint(int X, int Y);
