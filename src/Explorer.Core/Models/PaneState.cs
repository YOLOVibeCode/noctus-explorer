namespace NoctusExplorer.Core.Models;

public sealed record PaneState(
    IReadOnlyList<TabState> Tabs,
    int ActiveTabId,
    bool IsActive);
