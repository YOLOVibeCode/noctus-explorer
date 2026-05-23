namespace NoctusExplorer.Core.Models;

public sealed record ActionConditions(
    FileType AppliesTo = FileType.Both,
    string[]? Extensions = null,
    SelectionCount SelectionCount = SelectionCount.Any,
    string? PathContains = null);
