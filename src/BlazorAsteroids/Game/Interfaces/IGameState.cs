using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Game.Interfaces;

public interface IGameState
{
    Player Player { get; }
    int CanvasWidth { get; }
    int CanvasHeight { get; }
    void Update(float deltaTime, Vector2 movementDirection, Vector2 cursorWorldPosition);
}
