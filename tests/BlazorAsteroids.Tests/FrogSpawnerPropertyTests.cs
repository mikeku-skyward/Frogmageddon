using FsCheck;
using FsCheck.Xunit;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Tests;

/// <summary>
/// Property-based tests for the FrogSpawner class.
/// Validates: Requirements 4.1, 4.3
/// </summary>
[Trait("Feature", "performance-optimizations")]
public class FrogSpawnerPropertyTests
{
    private static Camera CreateTestCamera()
    {
        return new Camera
        {
            Position = new Vector2(0, 0),
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            WorldWidth = 5000,
            WorldHeight = 5000
        };
    }

    /// <summary>
    /// Property 6: FrogSpawner returns static empty instance if and only if no frogs are spawned.
    /// For any call to TrySpawn, if the result contains zero frogs then the returned reference
    /// SHALL be the shared static empty list; if the result contains one or more frogs then the
    /// returned reference SHALL NOT be the shared static empty list.
    /// Validates: Requirements 4.1, 4.3
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "6: FrogSpawner returns static empty instance if and only if no frogs are spawned")]
    public bool FrogSpawner_ReturnsStaticEmptyInstance_IfAndOnlyIfNoFrogsSpawned(
        PositiveInt deltaTimeSeed,
        NonNegativeInt frogCountSeed)
    {
        var spawner = new FrogSpawner();
        var camera = CreateTestCamera();
        var playerPosition = new Vector2(500, 500);

        // Generate a deltaTime between 0.001 and 10.0 seconds
        var deltaTime = (deltaTimeSeed.Get % 10000) / 1000.0f + 0.001f;

        // Generate a currentFrogCount between 0 and 40 (covers both below and above MaxFrogs=30)
        var currentFrogCount = frogCountSeed.Get % 41;

        var result = spawner.TrySpawn(deltaTime, camera, currentFrogCount, playerPosition, new ObjectPool<Frog>());

        if (result.Count == 0)
        {
            // If no frogs spawned, the returned reference must be the shared static empty list
            return ReferenceEquals(result, Array.Empty<Frog>());
        }
        else
        {
            // If frogs were spawned, the returned reference must NOT be the shared static empty list
            return !ReferenceEquals(result, Array.Empty<Frog>());
        }
    }
}
