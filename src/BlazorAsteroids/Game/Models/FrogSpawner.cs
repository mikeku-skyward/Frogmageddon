namespace BlazorAsteroids.Game.Models;

public class FrogSpawner
{
    private readonly Random _random = new();
    private float _spawnTimer;
    private float _elapsedTime;

    /// <summary>
    /// Base time between frog spawns (seconds) at 100% rate.
    /// </summary>
    private const float BaseSpawnInterval = 3.0f;

    /// <summary>
    /// Maximum number of frogs alive at once.
    /// </summary>
    public int MaxFrogs { get; set; } = 30;

    /// <summary>
    /// How far outside the viewport edge frogs spawn.
    /// </summary>
    private const float SpawnMargin = 50f;

    /// <summary>
    /// Time (seconds) before spawn rate starts increasing.
    /// </summary>
    private const float RampStartTime = 30f;

    /// <summary>
    /// Time (seconds) when spawn rate reaches maximum (200%).
    /// </summary>
    private const float RampEndTime = 120f;

    /// <summary>
    /// Maximum spawn rate multiplier (2.0 = 200%).
    /// </summary>
    private const float MaxRateMultiplier = 2.0f;

    public FrogSpawner()
    {
        _spawnTimer = BaseSpawnInterval;
        _elapsedTime = 0f;
    }

    /// <summary>
    /// Gets the current spawn rate multiplier based on elapsed time.
    /// Returns 1.0 for the first 30s, then linearly ramps to 2.0 by 120s.
    /// </summary>
    private float GetSpawnRateMultiplier()
    {
        if (_elapsedTime <= RampStartTime)
            return 1.0f;

        if (_elapsedTime >= RampEndTime)
            return MaxRateMultiplier;

        // Linear interpolation from 1.0 to 2.0 between 30s and 120s
        float progress = (_elapsedTime - RampStartTime) / (RampEndTime - RampStartTime);
        return 1.0f + progress * (MaxRateMultiplier - 1.0f);
    }

    /// <summary>
    /// Checks if it's time to spawn a new frog and returns a spawn position
    /// just outside the camera viewport edges.
    /// </summary>
    public Frog? TrySpawn(float deltaTime, Camera camera, int currentFrogCount)
    {
        _elapsedTime += deltaTime;

        if (currentFrogCount >= MaxFrogs)
            return null;

        // Higher multiplier = faster spawns = shorter interval
        float currentInterval = BaseSpawnInterval / GetSpawnRateMultiplier();

        _spawnTimer -= deltaTime;
        if (_spawnTimer > 0)
            return null;

        _spawnTimer = currentInterval;

        // Pick a random edge: 0=top, 1=bottom, 2=left, 3=right
        int edge = _random.Next(4);
        float x, y;

        float camLeft = camera.Position.X;
        float camTop = camera.Position.Y;
        float camRight = camLeft + camera.ViewportWidth;
        float camBottom = camTop + camera.ViewportHeight;

        switch (edge)
        {
            case 0: // Top edge
                x = camLeft + (float)_random.NextDouble() * camera.ViewportWidth;
                y = camTop - SpawnMargin;
                break;
            case 1: // Bottom edge
                x = camLeft + (float)_random.NextDouble() * camera.ViewportWidth;
                y = camBottom + SpawnMargin;
                break;
            case 2: // Left edge
                x = camLeft - SpawnMargin;
                y = camTop + (float)_random.NextDouble() * camera.ViewportHeight;
                break;
            default: // Right edge
                x = camRight + SpawnMargin;
                y = camTop + (float)_random.NextDouble() * camera.ViewportHeight;
                break;
        }

        return new Frog(new Vector2(x, y));
    }
}
