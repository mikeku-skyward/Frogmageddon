namespace BlazorAsteroids.Game.Models;

/// <summary>
/// Implemented by objects that can be reset for reuse from an object pool.
/// </summary>
public interface IPoolable
{
    void Reset();
}
