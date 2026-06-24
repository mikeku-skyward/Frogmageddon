using BlazorAsteroids.Game.Interfaces;
using BlazorAsteroids.Game.Models;

namespace BlazorAsteroids.Game.Engine;

public class InputManager : IInputManager
{
    private static readonly HashSet<string> ValidKeys = new() { "w", "a", "s", "d", "enter", "r" };
    private readonly HashSet<string> _pressedKeys = new();
    private (float X, float Y)? _pendingClick;
    private (float X, float Y) _mousePosition;

    /// <summary>
    /// Current mouse position in screen (canvas) coordinates.
    /// </summary>
    public (float X, float Y) MousePosition => _mousePosition;

    public void SetKeyDown(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        var lower = key.ToLowerInvariant();

        if (ValidKeys.Contains(lower))
        {
            _pressedKeys.Add(lower);
        }
    }

    public void SetKeyUp(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        var lower = key.ToLowerInvariant();
        _pressedKeys.Remove(lower);
    }

    public Vector2 GetMovementDirection()
    {
        float x = 0;
        float y = 0;

        if (IsKeyPressed("w")) y -= 1; // Up
        if (IsKeyPressed("s")) y += 1; // Down
        if (IsKeyPressed("a")) x -= 1; // Left
        if (IsKeyPressed("d")) x += 1; // Right

        Vector2 direction = new Vector2(x, y);

        if (direction.Length() > 0)
        {
            direction = direction.Normalized();
        }

        return direction;
    }

    public bool IsKeyPressed(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return _pressedKeys.Contains(key.ToLowerInvariant());
    }

    public void SetMouseClick(float x, float y)
    {
        _pendingClick = (x, y);
    }

    public void SetMousePosition(float x, float y)
    {
        _mousePosition = (x, y);
    }

    public (float X, float Y)? ConsumePendingClick()
    {
        var click = _pendingClick;
        _pendingClick = null;
        return click;
    }
}
