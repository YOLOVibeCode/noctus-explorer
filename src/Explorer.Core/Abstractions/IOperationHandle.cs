using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Tracks a single in-flight file operation (copy, move, delete, etc.).
/// </summary>
public interface IOperationHandle : IDisposable
{
    Guid Id { get; }
    string Description { get; }
    OperationStatus Status { get; }
    double Progress { get; }
    long BytesTransferred { get; }
    long TotalBytes { get; }
    TimeSpan? EstimatedRemaining { get; }

    void Pause();
    void Resume();
    void Cancel();

    event EventHandler<OperationProgressEventArgs> ProgressChanged;
    event EventHandler<OperationConflictEventArgs> ConflictEncountered;
    event EventHandler<OperationCompletedEventArgs> Completed;
}
