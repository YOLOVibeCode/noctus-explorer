namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Launches external processes. Segregated from IFileOperations
/// because custom actions need process launching but not file ops.
/// </summary>
public interface IProcessLauncher
{
    Task<int> LaunchAsync(
        string program,
        string arguments,
        string? workingDirectory = null,
        bool hidden = false,
        bool elevated = false,
        CancellationToken ct = default);

    Task<string> LaunchAndCaptureAsync(
        string command,
        string? workingDirectory = null,
        CancellationToken ct = default);
}
