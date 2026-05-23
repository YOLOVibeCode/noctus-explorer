namespace NoctusExplorer.Core.Models;

public sealed record CustomAction(
    Guid Id,
    string Label,
    string? Icon,
    string? Group,
    int Order,
    ActionConditions Conditions,
    ActionType ActionType,
    IReadOnlyDictionary<string, string> ActionConfig,
    bool RegisterWithOS,
    bool Enabled);
