namespace BlazorAsteroids.Game.Models;

public class Frog : IPoolable
{
    private enum FrogState
    {
        Sitting,
        Hopping
    }

    private static readonly Random _random = new();

    private FrogState _state = FrogState.Sitting;
    private float _stateTimer;
    private Vector2 _hopDirection;

    /// <summary>
    /// How long a hop lasts (seconds). Reduced for shorter jumps.
    /// </summary>
    private const float HopDuration = 0.2f;

    /// <summary>
    /// Speed during a hop.
    /// </summary>
    private const float HopSpeed = 690f;

    /// <summary>
    /// Half-size of the invisible square around the player that the frog targets.
    /// </summary>
    private const float TargetSpread = 80f;

    public Vector2 Position { get; set; }
    public float Size { get; set; } = 24f;
    public float Rotation { get; set; }
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// Whether the frog is currently in its hopping state (used for sprite selection).
    /// </summary>
    public bool IsHopping => _state == FrogState.Hopping;

    /// <summary>
    /// Parameterless constructor for object pool usage. Creates an uninitialized instance.
    /// </summary>
    public Frog()
    {
    }

    public Frog(Vector2 spawnPosition)
    {
        Initialize(spawnPosition, 0f);
    }

    /// <summary>
    /// Initializes or reinitializes the frog with the given spawn parameters.
    /// Used by the object pool to reuse instances.
    /// </summary>
    public void Initialize(Vector2 spawnPosition, float rotation)
    {
        Position = spawnPosition;
        Rotation = rotation;
        IsAlive = true;
        _state = FrogState.Sitting;
        _stateTimer = RandomSitDuration();
        _hopDirection = new Vector2(0, 0);
    }

    /// <summary>
    /// Resets the frog for return to the object pool.
    /// </summary>
    public void Reset()
    {
        IsAlive = false;
    }

    public void Update(float deltaTime, Vector2 playerPosition)
    {
        _stateTimer -= deltaTime;

        switch (_state)
        {
            case FrogState.Sitting:
                if (_stateTimer <= 0)
                {
                    // 20% chance to hop in a completely random direction
                    if (_random.NextDouble() < 0.2)
                    {
                        float angle = (float)_random.NextDouble() * MathF.PI * 2f;
                        _hopDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    }
                    else
                    {
                        // Pick a random point within a square around the player
                        float targetX = playerPosition.X + ((float)_random.NextDouble() * 2f - 1f) * TargetSpread;
                        float targetY = playerPosition.Y + ((float)_random.NextDouble() * 2f - 1f) * TargetSpread;

                        Vector2 toTarget = new Vector2(
                            targetX - Position.X,
                            targetY - Position.Y
                        );

                        float dist = toTarget.Length();
                        if (dist > 0)
                        {
                            _hopDirection = toTarget.Normalized();
                        }
                    }

                    Rotation = MathF.Atan2(_hopDirection.Y, _hopDirection.X);
                    _state = FrogState.Hopping;
                    _stateTimer = HopDuration;
                }
                break;

            case FrogState.Hopping:
                Position = Position + _hopDirection * HopSpeed * deltaTime;

                if (_stateTimer <= 0)
                {
                    _state = FrogState.Sitting;
                    _stateTimer = RandomSitDuration();
                }
                break;
        }
    }

    private static float RandomSitDuration()
    {
        // Random between 1.0 and 1.5 seconds
        return 1.0f + (float)_random.NextDouble() * 0.5f;
    }
}
