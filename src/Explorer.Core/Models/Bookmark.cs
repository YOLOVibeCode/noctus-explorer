namespace NoctusExplorer.Core.Models;

public sealed record Bookmark(
    Guid Id,
    string Name,
    PathRef Target,
    string? Group,
    int Order);
