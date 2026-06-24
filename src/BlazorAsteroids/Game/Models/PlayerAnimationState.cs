namespace BlazorAsteroids.Game.Models;

public enum FacingDirection
{
    Right = 0,
    Left = 1
}

public class PlayerAnimationState
{
    public const float BaseFrameDuration = 0.150f; // 150ms per frame
    public const float SprintAnimationMultiplier = 1.3f;
    public const int WalkCycleLength = 4; // walk1, stationary, walk2, stationary

    // Walk cycle frame sequence: indices into sprite array
    // 0 = stationary, 1 = walk1, 2 = walk2
    private static readonly int[] WalkCycleFrames = { 1, 0, 2, 0 };

    public int CurrentCyclePosition { get; private set; } = 0;
    public float ElapsedFrameTime { get; private set; } = 0f;
    public FacingDirection Facing { get; private set; } = FacingDirection.Right;

    /// <summary>
    /// The sprite index to render (0=stationary, 1=walk1, 2=walk2).
    /// </summary>
    public int CurrentSpriteIndex => WalkCycleFrames[CurrentCyclePosition];

    /// <summary>
    /// Updates the animation state for the current frame.
    /// </summary>
    public void Update(float deltaTime, Vector2 movementDirection, bool isSprinting)
    {
        // Update facing direction based on horizontal input
        if (movementDirection.X > 0)
            Facing = FacingDirection.Right;
        else if (movementDirection.X < 0)
            Facing = FacingDirection.Left;
        // If X == 0, retain previous facing direction

        // If not moving, reset to stationary
        if (movementDirection.Length() == 0)
        {
            CurrentCyclePosition = 0;
            ElapsedFrameTime = 0f;
            return;
        }

        // Accumulate elapsed time with sprint multiplier
        float effectiveDuration = BaseFrameDuration / (isSprinting ? SprintAnimationMultiplier : 1.0f);
        ElapsedFrameTime += deltaTime;

        // Advance frames
        while (ElapsedFrameTime >= effectiveDuration)
        {
            ElapsedFrameTime -= effectiveDuration;
            CurrentCyclePosition = (CurrentCyclePosition + 1) % WalkCycleLength;
        }
    }

    /// <summary>
    /// Resets animation to initial state (stationary, facing right).
    /// </summary>
    public void Reset()
    {
        CurrentCyclePosition = 0;
        ElapsedFrameTime = 0f;
        Facing = FacingDirection.Right;
    }
}
