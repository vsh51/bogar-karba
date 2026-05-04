namespace Web.Services;

public sealed class ConnectionTracker
{
    private int _count;

    public int Increment() => Interlocked.Increment(ref _count);

    public int Decrement() => Interlocked.Decrement(ref _count);
}
