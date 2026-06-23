using BlazorAsteroids.Game.Interfaces;

namespace BlazorAsteroids.Game.Models;

public class GameState : IGameState
{
    public Player Player { get; set; } = new();
    public Camera Camera { get; set; } = new();
    public List<Frog> Frogs { get; set; } = new();
    public FrogSpawner FrogSpawner { get; set; } = new();

    /// <summary>
    /// The viewport (canvas) width — what the player sees on screen.
    /// </summary>
    public int CanvasWidth { get; set; }

    /// <summary>
    /// The viewport (canvas) height — what the player sees on screen.
    /// </summary>
    public int CanvasHeight { get; set; }

    /// <summary>
    /// Full world width (the background image size).
    /// </summary>
    public float WorldWidth { get; set; }

    /// <summary>
    /// Full world height (the background image size).
    /// </summary>
    public float WorldHeight { get; set; }

    // Explicit interface implementation to satisfy IGameState.Player (read-only)
    Player IGameState.Player => Player;

    public void Update(float deltaTime, Vector2 movementDirection)
    {
        // Update player movement
        if (movementDirection.Length() > 0)
        {
            Vector2 velocity = movementDirection * Player.Speed * deltaTime;
            Player.Position = Player.Position + velocity;

            // Clamp position to world bounds
            Player.Position = new Vector2(
                Math.Clamp(Player.Position.X, Player.Size, WorldWidth - Player.Size),
                Math.Clamp(Player.Position.Y, Player.Size, WorldHeight - Player.Size)
            );

            // Update rotation to face movement direction
            Player.Rotation = MathF.Atan2(movementDirection.Y, movementDirection.X);
        }

        // Update camera to follow player
        Camera.Follow(Player.Position);

        // Spawn frogs
        var newFrog = FrogSpawner.TrySpawn(deltaTime, Camera, Frogs.Count);
        if (newFrog != null)
            Frogs.Add(newFrog);

        // Update all frogs
        foreach (var frog in Frogs)
        {
            frog.Update(deltaTime, Player.Position);
        }
    }
}
