namespace BlazorAsteroids.Game.Models;

public record StartButtonBounds(float X, float Y, float Width, float Height)
{
    public bool Contains(float clickX, float clickY)
    {
        return clickX >= X && clickX <= X + Width
            && clickY >= Y && clickY <= Y + Height;
    }

    public static StartButtonBounds Create(int canvasWidth, int canvasHeight)
    {
        const float buttonWidth = 160f;
        const float buttonHeight = 50f;
        float x = (canvasWidth - buttonWidth) / 2f;
        float y = (canvasHeight / 2f) - 10f; // Centered vertically
        return new StartButtonBounds(x, y, buttonWidth, buttonHeight);
    }
}
