namespace M365Trace.Web.Services;

public sealed class TraceOperationCoordinator : IDisposable
{
    private readonly object _sync = new();
    private TraceOperation? _current;
    private long _nextId;
    private bool _disposed;

    public bool HasActiveOperation
    {
        get
        {
            lock (_sync)
            {
                return _current is not null;
            }
        }
    }

    public TraceOperation Begin()
    {
        TraceOperation? previous;
        TraceOperation current;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            previous = _current;
            current = new TraceOperation(++_nextId);
            _current = current;
        }

        previous?.Cancel();
        return current;
    }

    public bool IsCurrent(TraceOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_sync)
        {
            return ReferenceEquals(_current, operation);
        }
    }

    public bool CancelCurrent()
    {
        TraceOperation? current;
        lock (_sync)
        {
            current = _current;
        }

        current?.Cancel();
        return current is not null;
    }

    public bool Complete(TraceOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        bool wasCurrent;
        lock (_sync)
        {
            wasCurrent = ReferenceEquals(_current, operation);
            if (wasCurrent)
            {
                _current = null;
            }
        }

        operation.Dispose();
        return wasCurrent;
    }

    public void Dispose()
    {
        TraceOperation? current;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            current = _current;
            _current = null;
        }

        current?.Cancel();
    }
}

public sealed class TraceOperation(long id) : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();

    public long Id { get; } = id;

    public CancellationToken CancellationToken => _cancellation.Token;

    internal void Cancel()
    {
        try
        {
            _cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Dispose() => _cancellation.Dispose();
}
