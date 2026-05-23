namespace NoctusExplorer.Core.Models;

/// <summary>
/// Represents a keyboard shortcut combination.
/// </summary>
public sealed class KeyChord : IEquatable<KeyChord>
{
    public string Key { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }

    public KeyChord(string key, bool ctrl = false, bool shift = false, bool alt = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key.ToUpperInvariant();
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
    }

    public bool Equals(KeyChord? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt;
    }

    public override bool Equals(object? obj) => Equals(obj as KeyChord);

    public override int GetHashCode() => HashCode.Combine(Key, Ctrl, Shift, Alt);

    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(Key);
        return string.Join("+", parts);
    }

    /// <summary>
    /// Parse a string like "Ctrl+Shift+P" into a KeyChord.
    /// </summary>
    public static KeyChord Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool ctrl = false, shift = false, alt = false;
        string? key = null;

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL": ctrl = true; break;
                case "ALT": alt = true; break;
                case "SHIFT": shift = true; break;
                default:
                    if (key is not null)
                        throw new FormatException($"Multiple keys in chord: '{text}'");
                    key = part;
                    break;
            }
        }

        return key is null
            ? throw new FormatException($"No key found in chord: '{text}'")
            : new KeyChord(key, ctrl, shift, alt);
    }

    public static bool TryParse(string text, out KeyChord? result)
    {
        try { result = Parse(text); return true; }
        catch { result = null; return false; }
    }
}
