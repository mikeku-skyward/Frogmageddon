namespace BlazorAsteroids.Game.Models;

public class Camera
{
    /// <summary>
    /// The top-left corner of the camera in world coordinates.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Width of the viewport (what the player sees on screen).
    /// </summary>
    public float ViewportWidth { get; set; }

    /// <summary>
    /// Height of the viewport (what the player sees on screen).
    /// </summary>
    public float ViewportHeight { get; set; }

    /// <summary>
    /// Total world width (the full background map size).
    /// </summary>
    public float WorldWidth { get; set; }

    /// <summary>
    /// Total world height (the full background map size).
    /// </summary>
    public float WorldHeight { get; set; }

    // --- Enter the Gungeon-style camera tuning ---

    /// <summary>
    /// How much the cursor influences the camera target (0 = player only, 1 = full midpoint).
    /// Gungeon uses roughly 30% cursor weight — enough look-ahead without losing the player.
    /// </summary>
    private const float CursorWeight = 0.3f;

    /// <summary>
    /// Base smoothing factor for exponential interpolation.
    /// </summary>
    private const float BaseSmoothFactor = 8f;

    /// <summary>
    /// Maximum distance (in pixels) the cursor offset is allowed to pull the camera.
    /// Prevents wild camera swings when the cursor is at the screen edge.
    /// </summary>
    private const float MaxCursorOffset = 150f;

    /// <summary>
    /// Enter the Gungeon-style camera: weighted blend between player and cursor
    /// with clamped cursor influence, smooth exponential follow, and a soft
    /// dead-zone that keeps the player comfortably on screen.
    /// </summary>
    public void Follow(Vector2 playerPosition, Vector2 cursorWorldPosition, float deltaTime)
    {
        // Clamp cursor to world bounds
        Vector2 clampedCursor = ClampToWorldEdge(playerPosition, cursorWorldPosition);

        // Compute cursor offset from player, clamped to max distance
        float offsetX = clampedCursor.X - playerPosition.X;
        float offsetY = clampedCursor.Y - playerPosition.Y;
        float offsetDist = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);

        if (offsetDist > MaxCursorOffset)
        {
            float scale = MaxCursorOffset / offsetDist;
            offsetX *= scale;
            offsetY *= scale;
        }

        // Camera target: player + weighted cursor offset
        float targetCenterX = playerPosition.X + offsetX * CursorWeight;
        float targetCenterY = playerPosition.Y + offsetY * CursorWeight;

        // Desired top-left
        float desiredX = targetCenterX - ViewportWidth / 2f;
        float desiredY = targetCenterY - ViewportHeight / 2f;

        // Clamp to world bounds
        desiredX = Math.Clamp(desiredX, 0, WorldWidth - ViewportWidth);
        desiredY = Math.Clamp(desiredY, 0, WorldHeight - ViewportHeight);

        // Smooth follow with exponential interpolation
        float t = 1f - MathF.Exp(-BaseSmoothFactor * deltaTime);

        float newX = Position.X + (desiredX - Position.X) * t;
        float newY = Position.Y + (desiredY - Position.Y) * t;

        // Soft dead-zone enforcement: if the player is near the outer 1/4,
        // use a stronger pull that scales with how far into the margin they are.
        float marginX = ViewportWidth * 0.25f;
        float marginY = ViewportHeight * 0.25f;

        float playerScreenX = playerPosition.X - newX;
        float playerScreenY = playerPosition.Y - newY;

        // Horizontal correction
        if (playerScreenX < marginX)
        {
            float overshoot = marginX - playerScreenX;
            float strength = overshoot / marginX; // 0..1 how deep into margin
            float correctionT = 1f - MathF.Exp(-20f * strength * deltaTime);
            float boundedX = playerPosition.X - marginX;
            newX = newX + (boundedX - newX) * correctionT;
        }
        else if (playerScreenX > ViewportWidth - marginX)
        {
            float overshoot = playerScreenX - (ViewportWidth - marginX);
            float strength = overshoot / marginX;
            float correctionT = 1f - MathF.Exp(-20f * strength * deltaTime);
            float boundedX = playerPosition.X - ViewportWidth + marginX;
            newX = newX + (boundedX - newX) * correctionT;
        }

        // Vertical correction
        if (playerScreenY < marginY)
        {
            float overshoot = marginY - playerScreenY;
            float strength = overshoot / marginY;
            float correctionT = 1f - MathF.Exp(-20f * strength * deltaTime);
            float boundedY = playerPosition.Y - marginY;
            newY = newY + (boundedY - newY) * correctionT;
        }
        else if (playerScreenY > ViewportHeight - marginY)
        {
            float overshoot = playerScreenY - (ViewportHeight - marginY);
            float strength = overshoot / marginY;
            float correctionT = 1f - MathF.Exp(-20f * strength * deltaTime);
            float boundedY = playerPosition.Y - ViewportHeight + marginY;
            newY = newY + (boundedY - newY) * correctionT;
        }

        // Final world clamp
        newX = Math.Clamp(newX, 0, WorldWidth - ViewportWidth);
        newY = Math.Clamp(newY, 0, WorldHeight - ViewportHeight);

        Position = new Vector2(newX, newY);
    }

    /// <summary>
    /// If the cursor is inside the world, returns it as-is.
    /// If outside, returns the point where the line from player to cursor
    /// intersects the world boundary.
    /// </summary>
    private Vector2 ClampToWorldEdge(Vector2 player, Vector2 cursor)
    {
        if (cursor.X >= 0 && cursor.X <= WorldWidth &&
            cursor.Y >= 0 && cursor.Y <= WorldHeight)
        {
            return cursor;
        }

        float dx = cursor.X - player.X;
        float dy = cursor.Y - player.Y;

        float tMin = 0f;
        float tMax = 1f;

        if (dx != 0)
        {
            float tLeft = (0 - player.X) / dx;
            float tRight = (WorldWidth - player.X) / dx;
            if (dx < 0)
                (tLeft, tRight) = (tRight, tLeft);
            tMin = MathF.Max(tMin, tLeft);
            tMax = MathF.Min(tMax, tRight);
        }

        if (dy != 0)
        {
            float tTop = (0 - player.Y) / dy;
            float tBottom = (WorldHeight - player.Y) / dy;
            if (dy < 0)
                (tTop, tBottom) = (tBottom, tTop);
            tMin = MathF.Max(tMin, tTop);
            tMax = MathF.Min(tMax, tBottom);
        }

        float t = Math.Clamp(tMax, 0f, 1f);

        return new Vector2(
            player.X + dx * t,
            player.Y + dy * t
        );
    }

    /// <summary>
    /// Instantly centers the camera on a target position (used for initialization).
    /// </summary>
    public void SnapTo(Vector2 targetPosition)
    {
        float x = targetPosition.X - ViewportWidth / 2f;
        float y = targetPosition.Y - ViewportHeight / 2f;

        x = Math.Clamp(x, 0, WorldWidth - ViewportWidth);
        y = Math.Clamp(y, 0, WorldHeight - ViewportHeight);

        Position = new Vector2(x, y);
    }

    /// <summary>
    /// Converts a world position to screen (viewport) position.
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return new Vector2(
            worldPosition.X - Position.X,
            worldPosition.Y - Position.Y
        );
    }
}
