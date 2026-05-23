namespace NoctusExplorer.Core.Models;

public sealed class OperationProgressEventArgs : EventArgs
{
    public required double Progress { get; init; }
    public required long BytesTransferred { get; init; }
    public required long TotalBytes { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
    public string? CurrentItem { get; init; }
}

public sealed class OperationConflictEventArgs : EventArgs
{
    public required PathRef Source { get; init; }
    public required PathRef Destination { get; init; }
    public required long SourceSize { get; init; }
    public required long DestinationSize { get; init; }
    public required DateTimeOffset SourceModified { get; init; }
    public required DateTimeOffset DestinationModified { get; init; }

    /// <summary>
    /// The handler must set this before returning.
    /// </summary>
    public ConflictResolution Resolution { get; set; }
    public bool ApplyToAll { get; set; }
}

public sealed class OperationCompletedEventArgs : EventArgs
{
    public required OperationStatus FinalStatus { get; init; }
    public Exception? Error { get; init; }
}

public sealed class FileChangeEventArgs : EventArgs
{
    public required FileChangeType ChangeType { get; init; }
    public required PathRef Path { get; init; }
    public PathRef? OldPath { get; init; }
}

public enum FileChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed
}
