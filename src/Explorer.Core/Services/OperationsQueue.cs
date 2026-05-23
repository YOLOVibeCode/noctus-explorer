using NoctusExplorer.Core.Abstractions;

namespace NoctusExplorer.Core.Services;

public sealed class OperationsQueue
{
    private readonly List<IOperationHandle> _operations = [];

    public IReadOnlyList<IOperationHandle> Operations => _operations.AsReadOnly();
    public int MaxConcurrent { get; set; } = 2;

    public event EventHandler<IOperationHandle>? OperationAdded;
    public event EventHandler<IOperationHandle>? OperationCompleted;

    public void Enqueue(IOperationHandle handle)
    {
        _operations.Add(handle);
        handle.Completed += OnOperationCompleted;
        OperationAdded?.Invoke(this, handle);
    }

    public void PauseAll()
    {
        foreach (var op in _operations)
            op.Pause();
    }

    public void ResumeAll()
    {
        foreach (var op in _operations)
            op.Resume();
    }

    public void CancelAll()
    {
        foreach (var op in _operations)
            op.Cancel();
    }

    private void OnOperationCompleted(object? sender, Models.OperationCompletedEventArgs e)
    {
        if (sender is IOperationHandle handle)
        {
            handle.Completed -= OnOperationCompleted;
            OperationCompleted?.Invoke(this, handle);
        }
    }
}
