namespace NoctusExplorer.Core.Models;

public sealed class CommandDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public KeyChord? DefaultBinding { get; init; }
    public string? IconId { get; init; }
    public required Func<bool> CanExecute { get; init; }
    public required Action Execute { get; init; }
}
