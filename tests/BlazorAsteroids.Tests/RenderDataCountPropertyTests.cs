using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Engine;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for render data count correctness.
/// Validates: Requirements 1.4
/// </summary>
[Trait("Feature", "performance-optimizations")]
public class RenderDataCountPropertyTests
{
    /// <summary>
    /// Property 3: Render data count matches entity count.
    /// For any game state with N frogs and M bullets, the render buffers
    /// SHALL have capacity >= N * 5 for frog data and >= M * 3 for bullet data
    /// after EnsureCapacity is called with the appropriate counts.
    /// This confirms the render data capacity always matches or exceeds the entity count requirement.
    /// Validates: Requirements 1.4
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "3: Render data count matches entity count")]
    public bool RenderDataCapacity_AlwaysMatchesOrExceeds_EntityCountRequirement(NonNegativeInt frogCount, NonNegativeInt bulletCount)
    {
        var n = frogCount.Get % 500; // Keep frog count reasonable
        var m = bulletCount.Get % 500; // Keep bullet count reasonable

        // Create two RenderBuffers like CanvasRenderer does
        var frogBuffer = new RenderBuffer(50 * 5);   // initial capacity for 50 frogs
        var bulletBuffer = new RenderBuffer(20 * 3); // initial capacity for 20 bullets

        // Call EnsureCapacity as CanvasRenderer.RenderAsync does
        frogBuffer.EnsureCapacity(n * 5);
        bulletBuffer.EnsureCapacity(m * 3);

        // Verify: frog buffer Data.Length >= N * 5
        if (frogBuffer.Data.Length < n * 5)
            return false;

        // Verify: bullet buffer Data.Length >= M * 3
        if (bulletBuffer.Data.Length < m * 3)
            return false;

        return true;
    }
}
