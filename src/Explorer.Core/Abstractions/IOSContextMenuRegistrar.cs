using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Registers/unregisters custom actions in the OS-level context menu
/// (Windows registry or macOS Quick Actions).
/// </summary>
public interface IOSContextMenuRegistrar
{
    void Register(CustomAction action);
    void Unregister(CustomAction action);
    bool IsRegistered(CustomAction action);
    void CleanupOrphans(IReadOnlyList<CustomAction> knownActions);
}
