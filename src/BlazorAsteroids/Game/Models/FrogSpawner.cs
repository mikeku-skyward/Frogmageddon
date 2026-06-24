namespace BlazorAsteroids.Game.Models;

public class FrogSpawner
{
    private readonly Random _random = new();
    private float _spawnTimer;
    private float _elapsedTime;

    /// <summary>
    /// Base time between frog spawns (seconds) at 100% rate.
    /// At game start the effective interval is 3.5s, ramping down to 1.75s by 2 minutes.
    /// </summary>
    private const float BaseSpawnInterval = 3.5f;

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
    /// Ramp begins immediately for a gradual increase over the first 2 minutes.
    /// </summary>
    private const float RampStartTime = 0f;

    /// <summary>
    /// Time (seconds) when spawn rate reaches maximum (200%).
    /// </summary>
    private const float RampEndTime = 120f;

    /// <summary>
    /// Maximum spawn rate multiplier (2.0 = 200%).
    /// </summary>
    private const float MaxRateMultiplier = 2.0f;

    /// <summary>
    /// Minimum number of frogs in a spawn group (early game, first 60s).
    /// </summary>
    private const int EarlyMinGroupSize = 2;

    /// <summary>
    /// Maximum number of frogs in a spawn group (early game, first 60s).
    /// </summary>
    private const int EarlyMaxGroupSize = 3;

    /// <summary>
    /// Minimum number of frogs in a spawn group (after 60s).
    /// </summary>
    private const int LateMinGroupSize = 4;

    /// <summary>
    /// Maximum number of frogs in a spawn group (after 60s).
    /// </summary>
    private const int LateMaxGroupSize = 5;

    /// <summary>
    /// Time threshold (seconds) when group size transitions from early to late.
    /// </summary>
    private const float GroupSizeTransitionTime = 60f;

    /// <summary>
    /// Maximum offset from anchor along the edge axis for clustering.
    /// </summary>
    private const float ClusterRadius = 240f;

    /// <summary>
    /// Buffer distance (in screen pixels) from the player to the edge of the viewport.
    /// Edges closer to the player than this threshold are excluded from spawning.
    /// </summary>
    private const float PlayerEdgeBuffer = 100f;

    /// <summary>
    /// Minimum distance from edge corners for anchor placement.
    /// </summary>
    private const float AnchorCornerMargin = 120f;

    /// <summary>
    /// Perpendicular spread (half-height of the spawn rectangle).
    /// Frogs spawn within ±15px perpendicular to the edge.
    /// </summary>
    private const float PerpendicularSpread = 15f;

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
    /// Shared empty list returned on no-spawn frames to avoid per-frame allocations.
    /// </summary>
    private static readonly IReadOnlyList<Frog> EmptyFrogList = Array.Empty<Frog>();

    /// <summary>
    /// Checks if it's time to spawn a group of frogs and returns spawn positions
    /// just outside the camera viewport on a single edge away from the player,
    /// clustered around an anchor point. Returns a shared empty list if spawning is suppressed.
    /// </summary>
    public IReadOnlyList<Frog> TrySpawn(float deltaTime, Camera camera, int currentFrogCount, Vector2 playerPosition, ObjectPool<Frog> frogPool)
    {
        _elapsedTime += deltaTime;

        // If at max capacity, suppress spawning and reset timer
        if (currentFrogCount >= MaxFrogs)
        {
            _spawnTimer = BaseSpawnInterval / GetSpawnRateMultiplier();
            return EmptyFrogList;
        }

        _spawnTimer -= deltaTime;
        if (_spawnTimer > 0)
            return EmptyFrogList;

        // Reset timer for next spawn event
        _spawnTimer = BaseSpawnInterval / GetSpawnRateMultiplier();

        // Choose group size based on elapsed time, clamp to remaining capacity
        int minGroup = _elapsedTime < GroupSizeTransitionTime ? EarlyMinGroupSize : LateMinGroupSize;
        int maxGroup = _elapsedTime < GroupSizeTransitionTime ? EarlyMaxGroupSize : LateMaxGroupSize;
        int groupSize = _random.Next(minGroup, maxGroup + 1);
        int remaining = MaxFrogs - currentFrogCount;
        if (groupSize > remaining)
            groupSize = remaining;

        float camLeft = camera.Position.X;
        float camTop = camera.Position.Y;
        float camRight = camLeft + camera.ViewportWidth;
        float camBottom = camTop + camera.ViewportHeight;

        // Determine player's screen-space position relative to viewport
        float playerScreenX = playerPosition.X - camLeft;
        float playerScreenY = playerPosition.Y - camTop;

        // Only spawn from edges that are away from the player (with buffer).
        // An edge is "away" if the player is far enough from that side of the viewport.
        var validEdges = new List<int>(4);

        // Top edge: valid if player is NOT near the top (player is in lower portion)
        if (playerScreenY > PlayerEdgeBuffer)
            validEdges.Add(0);
        // Bottom edge: valid if player is NOT near the bottom
        if (playerScreenY < camera.ViewportHeight - PlayerEdgeBuffer)
            validEdges.Add(1);
        // Left edge: valid if player is NOT near the left
        if (playerScreenX > PlayerEdgeBuffer)
            validEdges.Add(2);
        // Right edge: valid if player is NOT near the right
        if (playerScreenX < camera.ViewportWidth - PlayerEdgeBuffer)
            validEdges.Add(3);

        // Fallback: if no edges are valid (player is in center), allow all edges
        if (validEdges.Count == 0)
        {
            validEdges.Add(0);
            validEdges.Add(1);
            validEdges.Add(2);
            validEdges.Add(3);
        }

        // Pick a random valid edge
        int edge = validEdges[_random.Next(validEdges.Count)];

        // Determine edge length and compute anchor point
        float edgeLength;
        if (edge == 0 || edge == 1) // Top or Bottom: horizontal edge
            edgeLength = camera.ViewportWidth;
        else // Left or Right: vertical edge
            edgeLength = camera.ViewportHeight;

        // Compute usable range for anchor (edge length minus 2 × AnchorCornerMargin)
        float usableRange = edgeLength - 2f * AnchorCornerMargin;
        float anchor;

        if (usableRange <= 0)
        {
            // Edge too short: fallback to midpoint
            anchor = edgeLength / 2f;
        }
        else
        {
            // Pick anchor within usable range, offset by AnchorCornerMargin from start
            anchor = AnchorCornerMargin + (float)_random.NextDouble() * usableRange;
        }

        // Generate frogs clustered around the anchor
        var frogs = new List<Frog>(groupSize);

        for (int i = 0; i < groupSize; i++)
        {
            // Offset along edge axis: anchor ± ClusterRadius
            float offset = anchor + ((float)_random.NextDouble() * 2f - 1f) * ClusterRadius;
            // Perpendicular offset: ±PerpendicularSpread for rectangle spawn area
            float perpOffset = ((float)_random.NextDouble() * 2f - 1f) * PerpendicularSpread;
            float x, y;

            switch (edge)
            {
                case 0: // Top edge: y fixed outside top, x varies
                    x = camLeft + offset;
                    y = camTop - SpawnMargin + perpOffset;
                    break;
                case 1: // Bottom edge: y fixed outside bottom, x varies
                    x = camLeft + offset;
                    y = camBottom + SpawnMargin + perpOffset;
                    break;
                case 2: // Left edge: x fixed outside left, y varies
                    x = camLeft - SpawnMargin + perpOffset;
                    y = camTop + offset;
                    break;
                default: // Right edge: x fixed outside right, y varies
                    x = camRight + SpawnMargin + perpOffset;
                    y = camTop + offset;
                    break;
            }

            var frog = frogPool.Acquire();
            frog.Initialize(new Vector2(x, y), (float)_random.NextDouble() * MathF.PI * 2f);
            frogs.Add(frog);
        }

        return frogs;
    }
}
