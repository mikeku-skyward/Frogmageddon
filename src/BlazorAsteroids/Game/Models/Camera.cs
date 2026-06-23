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

    /// <summary>
    /// Updates the camera to follow a target position, keeping it centered
    /// and clamped within world bounds.
    /// </summary>
    public void Follow(Vector2 targetPosition)
    {
        // Center the camera on the target
        float x = targetPosition.X - ViewportWidth / 2f;
        float y = targetPosition.Y - ViewportHeight / 2f;

        // Clamp so camera doesn't go outside the world
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
