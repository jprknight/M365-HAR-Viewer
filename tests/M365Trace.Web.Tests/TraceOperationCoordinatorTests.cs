using M365Trace.Web.Services;

namespace M365Trace.Web.Tests;

public sealed class TraceOperationCoordinatorTests : IDisposable
{
    private readonly TraceOperationCoordinator _coordinator = new();

    [Fact]
    public void Begin_CancelsPreviousOperationAndKeepsNewestCurrent()
    {
        var first = _coordinator.Begin();
        var second = _coordinator.Begin();

        Assert.True(first.CancellationToken.IsCancellationRequested);
        Assert.False(second.CancellationToken.IsCancellationRequested);
        Assert.False(_coordinator.IsCurrent(first));
        Assert.True(_coordinator.IsCurrent(second));
    }

    [Fact]
    public void CompletingStaleOperation_DoesNotClearCurrentOperation()
    {
        var first = _coordinator.Begin();
        var second = _coordinator.Begin();

        Assert.False(_coordinator.Complete(first));
        Assert.True(_coordinator.HasActiveOperation);
        Assert.True(_coordinator.IsCurrent(second));

        Assert.True(_coordinator.Complete(second));
        Assert.False(_coordinator.HasActiveOperation);
    }

    [Fact]
    public void CancelCurrent_CancelsActiveOperation()
    {
        var operation = _coordinator.Begin();

        Assert.True(_coordinator.CancelCurrent());

        Assert.True(operation.CancellationToken.IsCancellationRequested);
    }

    public void Dispose() => _coordinator.Dispose();
}
