namespace BlazorAsteroids.Game.Models;

/// <summary>
/// A simple object pool with no maximum capacity.
/// </summary>
public sealed class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _pool = new();

    public T Acquire()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }

    public void Release(T instance)
    {
        if (instance is null) return;
        _pool.Push(instance);
    }

    public int Count => _pool.Count;
}
