using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(CommandDefinition command)
    {
        if (!_commands.TryAdd(command.Id, command))
            throw new ArgumentException($"Command '{command.Id}' is already registered.");
    }

    public CommandDefinition? GetById(string id)
        => _commands.GetValueOrDefault(id);

    public IReadOnlyList<CommandDefinition> GetAll()
        => _commands.Values.ToList().AsReadOnly();

    public bool CanExecute(string id)
        => _commands.TryGetValue(id, out var cmd) && cmd.CanExecute();

    public void Execute(string id)
    {
        if (_commands.TryGetValue(id, out var cmd) && cmd.CanExecute())
            cmd.Execute();
    }
}
