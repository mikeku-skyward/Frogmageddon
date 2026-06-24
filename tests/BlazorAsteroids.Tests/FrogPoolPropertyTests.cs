using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for pooled Frog initialization.
/// Feature: performance-optimizations, Property 10: Pooled frog initialization preserves spawn parameters
/// </summary>
public class FrogPoolPropertyTests
{
    /// <summary>
    /// Property 10: Pooled frog initialization preserves spawn parameters
    ///
    /// For any spawn position and rotation value, a frog acquired from the pool and initialized
    /// with those parameters SHALL have Position equal to the spawn position, Rotation equal to
    /// the given rotation, IsAlive equal to true, and be in the Sitting state (IsHopping == false).
    ///
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "10: Pooled frog initialization preserves spawn parameters")]
    public bool PooledFrog_Initialize_PreservesSpawnParameters(float x, float y, float rotation)
    {
        // Filter out NaN/Infinity which are not valid game positions
        if (float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y) ||
            float.IsNaN(rotation) || float.IsInfinity(rotation))
            return true; // Discard invalid inputs

        var spawnPosition = new Vector2(x, y);
        var pool = new ObjectPool<Frog>();

        // Acquire from pool (fresh instance since pool is empty)
        var frog = pool.Acquire();
        frog.Initialize(spawnPosition, rotation);

        // Verify all spawn parameters are preserved
        return frog.Position.X == spawnPosition.X
            && frog.Position.Y == spawnPosition.Y
            && frog.Rotation == rotation
            && frog.IsAlive == true
            && frog.IsHopping == false;
    }

    /// <summary>
    /// Property 10 (reuse case): After a frog has been used, reset, and returned to the pool,
    /// acquiring and reinitializing it SHALL still preserve the new spawn parameters.
    ///
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "10: Pooled frog initialization preserves spawn parameters")]
    public bool PooledFrog_Reused_Initialize_PreservesSpawnParameters(float x, float y, float rotation)
    {
        // Filter out NaN/Infinity which are not valid game positions
        if (float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y) ||
            float.IsNaN(rotation) || float.IsInfinity(rotation))
            return true; // Discard invalid inputs

        var spawnPosition = new Vector2(x, y);
        var pool = new ObjectPool<Frog>();

        // Simulate prior usage: acquire, use, reset, release back to pool
        var usedFrog = pool.Acquire();
        usedFrog.Initialize(new Vector2(999f, 999f), 3.14f);
        usedFrog.Reset();
        pool.Release(usedFrog);

        // Now acquire from pool (should get the reused instance)
        var frog = pool.Acquire();
        frog.Initialize(spawnPosition, rotation);

        // Verify all spawn parameters are preserved even after reuse
        return frog.Position.X == spawnPosition.X
            && frog.Position.Y == spawnPosition.Y
            && frog.Rotation == rotation
            && frog.IsAlive == true
            && frog.IsHopping == false;
    }
}
