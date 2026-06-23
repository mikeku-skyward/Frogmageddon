using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Game.Interfaces;

public interface IInputManager
{
    void SetKeyDown(string key);
    void SetKeyUp(string key);
    Vector2 GetMovementDirection();
    bool IsKeyPressed(string key);
    void SetMouseClick(float x, float y);
    void SetMousePosition(float x, float y);
    (float X, float Y) MousePosition { get; }
    (float X, float Y)? ConsumePendingClick();
}
