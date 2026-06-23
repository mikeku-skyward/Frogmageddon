using BlazorAsteroids.Game.Interfaces;

namespace BlazorAsteroids.Game.Models;

public class GameState : IGameState
{
    public Player Player { get; set; } = new();
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }

    // Explicit interface implementation to satisfy IGameState.Player (read-only)
    Player IGameState.Player => Player;

    public void Update(float deltaTime, Vector2 movementDirection)
    {
        if (movementDirection.Length() == 0)
            return;

        // Apply velocity: position += direction * speed * deltaTime
        Vector2 velocity = movementDirection * Player.Speed * deltaTime;
        Player.Position = Player.Position + velocity;

        // Clamp position to canvas bounds
        Player.Position = new Vector2(
            Math.Clamp(Player.Position.X, Player.Size, CanvasWidth - Player.Size),
            Math.Clamp(Player.Position.Y, Player.Size, CanvasHeight - Player.Size)
        );

        // Update rotation to face movement direction
        Player.Rotation = MathF.Atan2(movementDirection.Y, movementDirection.X);
    }
}
