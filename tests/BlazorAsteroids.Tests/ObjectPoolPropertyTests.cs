using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Simple test helper class used as the generic T parameter for ObjectPool tests.
/// </summary>
public class TestPoolItem { }

/// <summary>
/// Property-based tests for ObjectPool&lt;T&gt;.
/// Feature: performance-optimizations, Property 8: Pool acquire-release round-trip preserves count
/// </summary>
public class ObjectPoolPropertyTests
{
    /// <summary>
    /// Property 8: Pool acquire-release round-trip preserves count
    ///
    /// For any count N of objects released into an ObjectPool&lt;T&gt;, the pool's Count SHALL equal N.
    /// Subsequently acquiring N objects SHALL reduce the pool's Count to zero,
    /// and each acquired object SHALL be non-null.
    ///
    /// **Validates: Requirements 6.5, 6.6**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "8: Pool acquire-release round-trip preserves count")]
    public bool Pool_AcquireRelease_RoundTrip_PreservesCount(NonNegativeInt count)
    {
        var n = count.Get % 201; // Keep N in range [0, 200]
        var pool = new ObjectPool<TestPoolItem>();

        // Release N items into the pool
        for (int i = 0; i < n; i++)
        {
            pool.Release(new TestPoolItem());
        }

        // After releasing N items, Count SHALL equal N
        if (pool.Count != n)
            return false;

        // Acquire N items back from the pool
        for (int i = 0; i < n; i++)
        {
            var item = pool.Acquire();
            if (item is null)
                return false;
        }

        // After acquiring N items, Count SHALL be zero
        return pool.Count == 0;
    }
}
