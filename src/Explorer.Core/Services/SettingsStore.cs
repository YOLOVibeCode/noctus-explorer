using System.Text.Json;
using System.Text.Json.Nodes;

namespace NoctusExplorer.Core.Services;

public sealed class SettingsStore
{
    private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Prefix, Action<string, object> Callback)> _subscribers = [];

    public T Get<T>(string key, T defaultValue)
    {
        if (!_values.TryGetValue(key, out var raw))
            return defaultValue;

        try
        {
            if (raw is T typed)
                return typed;

            // Handle JSON deserialized types (JsonElement, etc.)
            if (raw is JsonElement elem)
                return DeserializeElement<T>(elem, defaultValue);

            // Try conversion
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public void Set<T>(string key, T value)
    {
        _values[key] = value!;
        NotifySubscribers(key, value!);
    }

    public void Subscribe(string keyPrefix, Action<string, object> onChange)
    {
        _subscribers.Add((keyPrefix, onChange));
    }

    public void Load(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var json = File.ReadAllText(filePath);
        var doc = JsonNode.Parse(json);
        if (doc is not JsonObject root) return;

        _values.Clear();
        FlattenJsonObject(root, "");
    }

    public void Save(string filePath)
    {
        // Build nested JSON from flat keys
        var root = new JsonObject();

        foreach (var (key, value) in _values.OrderBy(kv => kv.Key))
        {
            var parts = key.Split('.');
            var current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current[parts[i]] is not JsonObject child)
                {
                    child = new JsonObject();
                    current[parts[i]] = child;
                }
                current = child;
            }

            current[parts[^1]] = JsonValue.Create(value);
        }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Write to temp file, then atomic rename
        var tmpPath = filePath + ".tmp";
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        File.WriteAllText(tmpPath, root.ToJsonString(options));
        File.Move(tmpPath, filePath, overwrite: true);
    }

    private void FlattenJsonObject(JsonObject obj, string prefix)
    {
        foreach (var prop in obj)
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Key : $"{prefix}.{prop.Key}";

            if (prop.Value is JsonObject nested)
            {
                FlattenJsonObject(nested, key);
            }
            else if (prop.Value is JsonValue val)
            {
                _values[key] = val.GetValue<JsonElement>();
            }
        }
    }

    private static T DeserializeElement<T>(JsonElement elem, T defaultValue)
    {
        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)elem.GetString()!;
            if (typeof(T) == typeof(bool))
                return (T)(object)elem.GetBoolean();
            if (typeof(T) == typeof(int))
                return (T)(object)elem.GetInt32();
            if (typeof(T) == typeof(double))
                return (T)(object)elem.GetDouble();
            if (typeof(T) == typeof(long))
                return (T)(object)elem.GetInt64();

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private void NotifySubscribers(string key, object value)
    {
        foreach (var (prefix, callback) in _subscribers)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                callback(key, value);
        }
    }
}
