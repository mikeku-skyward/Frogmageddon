using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Game.Interfaces;

public interface IInputManager
{
    void SetKeyDown(string key);
    void SetKeyUp(string key);
    Vector2 GetMovementDirection();
    bool IsKeyPressed(string key);
}
