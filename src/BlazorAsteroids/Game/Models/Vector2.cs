namespace BlazorAsteroids.Game.Models;

public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Length() => MathF.Sqrt(X * X + Y * Y);

    public Vector2 Normalized()
    {
        float len = Length();
        if (len == 0) return new Vector2(0, 0);
        return new Vector2(X / len, Y / len);
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator *(Vector2 v, float scalar) => new(v.X * scalar, v.Y * scalar);
}
