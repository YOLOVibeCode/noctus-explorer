namespace NoctusExplorer.Core.Models;

/// <summary>
/// Immutable reference to a filesystem location. Wraps a canonical path
/// and an optional platform-opaque handle (PIDL on Windows, bookmark data on macOS).
/// </summary>
public sealed class PathRef : IEquatable<PathRef>
{
    public string FullPath { get; }
    public string DisplayName { get; }
    public bool IsDirectory { get; }
    public byte[]? PlatformHandle { get; }

    public PathRef(string fullPath, string? displayName = null, bool isDirectory = false, byte[]? platformHandle = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        FullPath = NormalizePath(fullPath);
        DisplayName = displayName ?? System.IO.Path.GetFileName(fullPath) ?? fullPath;
        IsDirectory = isDirectory;
        PlatformHandle = platformHandle;
    }

    public bool Equals(PathRef? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as PathRef);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath);

    public override string ToString() => FullPath;

    public static bool operator ==(PathRef? left, PathRef? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PathRef? left, PathRef? right) => !(left == right);

    public PathRef? GetParent()
    {
        var parent = System.IO.Path.GetDirectoryName(FullPath);
        return parent is null ? null : new PathRef(parent, isDirectory: true);
    }

    private static string NormalizePath(string path)
    {
        // Normalize separators to platform convention, trim trailing separators
        path = path.Replace('\\', '/').TrimEnd('/');
        // Preserve root slash for unix paths
        if (path.Length == 0 && path.StartsWith('/'))
            path = "/";
        return path;
    }
}
