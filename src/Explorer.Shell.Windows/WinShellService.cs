using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using Vanara.Windows.Shell;

namespace NoctusExplorer.Shell.Windows;

public sealed class WinShellService : IShellService
{
    public async Task<IReadOnlyList<FileEntry>> EnumerateAsync(PathRef directory, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var entries = new List<FileEntry>();
            var dirInfo = new DirectoryInfo(directory.FullPath);

            if (!dirInfo.Exists) return entries;

            foreach (var fsi in dirInfo.EnumerateFileSystemInfos())
            {
                ct.ThrowIfCancellationRequested();

                var isDir = fsi is DirectoryInfo;
                long? size = fsi is FileInfo fi ? fi.Length : null;

                entries.Add(new FileEntry(
                    Path: new PathRef(fsi.FullName, isDirectory: isDir),
                    Name: fsi.Name,
                    Extension: fsi.Extension,
                    Size: size,
                    DateModified: fsi.LastWriteTimeUtc,
                    DateCreated: fsi.CreationTimeUtc,
                    IsHidden: fsi.Attributes.HasFlag(FileAttributes.Hidden),
                    IsSystem: fsi.Attributes.HasFlag(FileAttributes.System),
                    Kind: isDir ? "Folder" : GetKindDescription(fsi.Extension)
                ));
            }

            return entries;
        }, ct);
    }

    public Task<PathRef> ResolveAsync(string path, CancellationToken ct = default)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        var full = Path.GetFullPath(expanded);
        var isDir = Directory.Exists(full);
        return Task.FromResult(new PathRef(full, isDirectory: isDir));
    }

    public PathRef GetSpecialFolder(SpecialFolder folder) => folder switch
    {
        SpecialFolder.Home => new PathRef(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), isDirectory: true),
        SpecialFolder.Desktop => new PathRef(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), isDirectory: true),
        SpecialFolder.Downloads => new PathRef(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), isDirectory: true),
        SpecialFolder.Documents => new PathRef(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), isDirectory: true),
        SpecialFolder.Root => new PathRef("C:\\", isDirectory: true),
        SpecialFolder.Trash => new PathRef("::{645FF040-5081-101B-9F08-00AA002F954E}", displayName: "Recycle Bin", isDirectory: true),
        _ => new PathRef(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), isDirectory: true),
    };

    public string GetDisplayName(PathRef item)
    {
        if (string.IsNullOrEmpty(item.DisplayName) || item.DisplayName == item.FullPath)
            return Path.GetFileName(item.FullPath) ?? item.FullPath;
        return item.DisplayName;
    }

    public bool Exists(PathRef item)
    {
        return item.IsDirectory ? Directory.Exists(item.FullPath) : File.Exists(item.FullPath);
    }

    private static string GetKindDescription(string extension_) => extension_.ToLowerInvariant() switch
    {
        ".txt" => "Text Document",
        ".pdf" => "PDF Document",
        ".doc" or ".docx" => "Word Document",
        ".xls" or ".xlsx" => "Excel Spreadsheet",
        ".jpg" or ".jpeg" => "JPEG Image",
        ".png" => "PNG Image",
        ".gif" => "GIF Image",
        ".mp3" => "MP3 Audio",
        ".mp4" => "MP4 Video",
        ".zip" => "ZIP Archive",
        ".exe" => "Application",
        ".dll" => "DLL Library",
        ".cs" => "C# Source",
        _ => string.IsNullOrEmpty(extension_) ? "File" : $"{extension_.TrimStart('.').ToUpperInvariant()} File"
    };
}
