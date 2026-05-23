using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed class KeyBindingResolver
{
    // chord → command id
    private readonly Dictionary<KeyChord, string> _chordToCommand = [];
    // command id → chord (reverse lookup)
    private readonly Dictionary<string, KeyChord> _commandToChord = new(StringComparer.OrdinalIgnoreCase);

    public void LoadBindings(Dictionary<string, KeyChord> bindings)
    {
        _chordToCommand.Clear();
        _commandToChord.Clear();
        foreach (var (commandId, chord) in bindings)
        {
            _chordToCommand[chord] = commandId;
            _commandToChord[commandId] = chord;
        }
    }

    public string? Resolve(KeyChord chord)
        => _chordToCommand.GetValueOrDefault(chord);

    public KeyChord? GetBinding(string commandId)
        => _commandToChord.GetValueOrDefault(commandId);

    public void SetBinding(string commandId, KeyChord chord)
    {
        // Remove old binding for this command if any
        if (_commandToChord.TryGetValue(commandId, out var oldChord))
            _chordToCommand.Remove(oldChord);

        // Remove old command for this chord if any
        if (_chordToCommand.TryGetValue(chord, out var oldCmd))
            _commandToChord.Remove(oldCmd);

        _chordToCommand[chord] = commandId;
        _commandToChord[commandId] = chord;
    }

    public void RemoveBinding(string commandId)
    {
        if (_commandToChord.TryGetValue(commandId, out var chord))
        {
            _chordToCommand.Remove(chord);
            _commandToChord.Remove(commandId);
        }
    }
}
