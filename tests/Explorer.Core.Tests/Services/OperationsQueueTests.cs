using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using NSubstitute;
using FluentAssertions;

namespace NoctusExplorer.Core.Tests.Services;

public class OperationsQueueTests
{
    private static IOperationHandle MakeMockHandle(OperationStatus status = OperationStatus.Queued)
    {
        var handle = Substitute.For<IOperationHandle>();
        handle.Id.Returns(Guid.NewGuid());
        handle.Status.Returns(status);
        handle.Description.Returns("Test operation");
        return handle;
    }

    [Fact]
    public void Initially_Empty()
    {
        var queue = new OperationsQueue();
        queue.Operations.Should().BeEmpty();
    }

    [Fact]
    public void Enqueue_AddsToOperations()
    {
        var queue = new OperationsQueue();
        queue.Enqueue(MakeMockHandle());
        queue.Operations.Should().HaveCount(1);
    }

    [Fact]
    public void Enqueue_RaisesOperationAdded()
    {
        var queue = new OperationsQueue();
        IOperationHandle? added = null;
        queue.OperationAdded += (_, h) => added = h;
        var handle = MakeMockHandle();
        queue.Enqueue(handle);
        added.Should().Be(handle);
    }

    [Fact]
    public void MaxConcurrent_DefaultIsTwo()
    {
        var queue = new OperationsQueue();
        queue.MaxConcurrent.Should().Be(2);
    }

    [Fact]
    public void CancelAll_CancelsAllOperations()
    {
        var queue = new OperationsQueue();
        var h1 = MakeMockHandle();
        var h2 = MakeMockHandle();
        queue.Enqueue(h1);
        queue.Enqueue(h2);
        queue.CancelAll();
        h1.Received(1).Cancel();
        h2.Received(1).Cancel();
    }

    [Fact]
    public void PauseAll_PausesAllOperations()
    {
        var queue = new OperationsQueue();
        var h1 = MakeMockHandle();
        queue.Enqueue(h1);
        queue.PauseAll();
        h1.Received(1).Pause();
    }

    [Fact]
    public void ResumeAll_ResumesAllOperations()
    {
        var queue = new OperationsQueue();
        var h1 = MakeMockHandle();
        queue.Enqueue(h1);
        queue.ResumeAll();
        h1.Received(1).Resume();
    }
}
