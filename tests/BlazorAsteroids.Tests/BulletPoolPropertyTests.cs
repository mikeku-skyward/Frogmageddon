using FsCheck.Xunit;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for pooled bullet initialization.
/// Feature: performance-optimizations, Property 9: Pooled bullet initialization preserves fire parameters
/// </summary>
public class BulletPoolPropertyTests
{
    /// <summary>
    /// Property 9: Pooled bullet initialization preserves fire parameters
    ///
    /// For any start position and direction vector, a bullet acquired from the pool
    /// and initialized with those parameters SHALL have Position equal to the start position,
    /// Direction equal to the normalized direction, IsAlive equal to true, and Lifetime equal to zero.
    ///
    /// **Validates: Requirements 6.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "performance-optimizations")]
    [Trait("Property", "9: Pooled bullet initialization preserves fire parameters")]
    public bool Pooled_Bullet_Initialization_Preserves_FireParameters(float startX, float startY, float dirX, float dirY)
    {
        var dirLength = MathF.Sqrt(dirX * dirX + dirY * dirY);

        // Skip zero-length direction vectors (normalization is undefined)
        if (dirLength == 0f || float.IsNaN(dirLength) || float.IsInfinity(dirLength) ||
            float.IsNaN(startX) || float.IsInfinity(startX) ||
            float.IsNaN(startY) || float.IsInfinity(startY) ||
            float.IsNaN(dirX) || float.IsInfinity(dirX) ||
            float.IsNaN(dirY) || float.IsInfinity(dirY))
            return true; // Discard invalid inputs

        var pool = new ObjectPool<Bullet>();

        // Optionally release a used bullet to test the reuse path
        var usedBullet = new Bullet();
        usedBullet.IsAlive = false;
        usedBullet.Lifetime = 99f;
        pool.Release(usedBullet);

        // Acquire from pool and initialize
        var startPosition = new Vector2(startX, startY);
        var direction = new Vector2(dirX, dirY);
        var bullet = pool.Acquire();
        bullet.Initialize(startPosition, direction);

        // Compute expected normalized direction
        var expectedDirection = direction.Normalized();

        // Verify all fire parameters are preserved
        return bullet.Position.X == startPosition.X
            && bullet.Position.Y == startPosition.Y
            && bullet.Direction.X == expectedDirection.X
            && bullet.Direction.Y == expectedDirection.Y
            && bullet.IsAlive == true
            && bullet.Lifetime == 0f;
    }
}
