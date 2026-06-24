using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for dead entity cleanup and pool return.
/// Feature: performance-optimizations, Property 11: Dead entities are returned to pool after cleanup
/// </summary>
public class DeadEntityPoolReturnPropertyTests
{
    /// <summary>
    /// Property 11: Dead bullets are returned to pool after cleanup
    ///
    /// For any set of active bullets where a subset K are marked IsAlive = false,
    /// after the cleanup step the pool count SHALL increase by |K| and the active
    /// list count SHALL decrease by |K|.
    ///
    /// **Validates: Requirements 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "11: Dead entities are returned to pool after cleanup")]
    public bool DeadBullets_AreReturnedToPool_AfterCleanup(NonNegativeInt totalCount, byte[] deathMask)
    {
        // Constrain total to a reasonable range [1, 50]
        var n = (totalCount.Get % 50) + 1;

        // Create the bullet list with all alive bullets
        var bullets = new List<Bullet>();
        for (int i = 0; i < n; i++)
        {
            var bullet = new Bullet();
            bullet.Initialize(new Vector2(i * 10f, 0f), new Vector2(1f, 0f));
            bullets.Add(bullet);
        }

        // Determine which bullets to kill using the deathMask
        var mask = deathMask ?? Array.Empty<byte>();
        int deadCount = 0;
        for (int i = 0; i < n; i++)
        {
            // Use mask bytes to decide alive/dead; if mask is shorter, treat as alive
            if (i < mask.Length && mask[i] % 2 == 0)
            {
                bullets[i].IsAlive = false;
                deadCount++;
            }
        }

        var pool = new ObjectPool<Bullet>();
        int initialPoolCount = pool.Count; // Should be 0

        // Run the cleanup loop (same pattern as GameState.Update)
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            if (!bullets[i].IsAlive)
            {
                bullets[i].Reset();
                pool.Release(bullets[i]);
                bullets.RemoveAt(i);
            }
        }

        // Verify: pool count increased by deadCount
        if (pool.Count != initialPoolCount + deadCount)
            return false;

        // Verify: list count decreased by deadCount
        if (bullets.Count != n - deadCount)
            return false;

        // Verify: all remaining items are alive
        foreach (var bullet in bullets)
        {
            if (!bullet.IsAlive)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Property 11: Dead frogs are returned to pool after cleanup
    ///
    /// For any set of active frogs where a subset K are marked IsAlive = false,
    /// after the cleanup step the pool count SHALL increase by |K| and the active
    /// list count SHALL decrease by |K|.
    ///
    /// **Validates: Requirements 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "11: Dead entities are returned to pool after cleanup")]
    public bool DeadFrogs_AreReturnedToPool_AfterCleanup(NonNegativeInt totalCount, byte[] deathMask)
    {
        // Constrain total to a reasonable range [1, 50]
        var n = (totalCount.Get % 50) + 1;

        // Create the frog list with all alive frogs
        var frogs = new List<Frog>();
        for (int i = 0; i < n; i++)
        {
            var frog = new Frog();
            frog.Initialize(new Vector2(i * 20f, i * 10f), 0f);
            frogs.Add(frog);
        }

        // Determine which frogs to kill using the deathMask
        var mask = deathMask ?? Array.Empty<byte>();
        int deadCount = 0;
        for (int i = 0; i < n; i++)
        {
            // Use mask bytes to decide alive/dead; if mask is shorter, treat as alive
            if (i < mask.Length && mask[i] % 2 == 0)
            {
                frogs[i].IsAlive = false;
                deadCount++;
            }
        }

        var pool = new ObjectPool<Frog>();
        int initialPoolCount = pool.Count; // Should be 0

        // Run the cleanup loop (same pattern as GameState.Update)
        for (int i = frogs.Count - 1; i >= 0; i--)
        {
            if (!frogs[i].IsAlive)
            {
                frogs[i].Reset();
                pool.Release(frogs[i]);
                frogs.RemoveAt(i);
            }
        }

        // Verify: pool count increased by deadCount
        if (pool.Count != initialPoolCount + deadCount)
            return false;

        // Verify: list count decreased by deadCount
        if (frogs.Count != n - deadCount)
            return false;

        // Verify: all remaining items are alive
        foreach (var frog in frogs)
        {
            if (!frog.IsAlive)
                return false;
        }

        return true;
    }
}
