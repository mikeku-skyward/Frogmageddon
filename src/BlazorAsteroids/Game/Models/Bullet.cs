namespace BlazorAsteroids.Game.Models;

public class Bullet
{
    /// <summary>
    /// Bullet speed in world units per second.
    /// </summary>
    private const float Speed = 1200f;

    /// <summary>
    /// How long a bullet lives before being removed (seconds).
    /// </summary>
    private const float MaxLifetime = 3.0f;

    public Vector2 Position { get; set; }
    public Vector2 PreviousPosition { get; set; }
    public Vector2 Direction { get; private set; }
    public float Radius { get; set; } = 5f;
    public float Lifetime { get; private set; }
    public bool IsAlive { get; set; } = true;

    public Bullet(Vector2 startPosition, Vector2 direction)
    {
        Position = startPosition;
        PreviousPosition = startPosition;
        Direction = direction.Normalized();
        Lifetime = 0f;
    }

    public void Update(float deltaTime)
    {
        PreviousPosition = Position;
        Position = Position + Direction * Speed * deltaTime;

        Lifetime += deltaTime;
        if (Lifetime >= MaxLifetime)
        {
            IsAlive = false;
        }
    }

    /// <summary>
    /// Checks if the line segment from PreviousPosition to Position
    /// intersects a circle (frog hitbox) using closest-point-on-segment.
    /// </summary>
    public bool IntersectsCircle(Vector2 circleCenter, float circleRadius)
    {
        float totalRadius = Radius + circleRadius;

        // Segment: PreviousPosition -> Position
        Vector2 segStart = PreviousPosition;
        Vector2 segEnd = Position;

        Vector2 seg = new Vector2(segEnd.X - segStart.X, segEnd.Y - segStart.Y);
        Vector2 toCircle = new Vector2(circleCenter.X - segStart.X, circleCenter.Y - segStart.Y);

        float segLenSq = seg.X * seg.X + seg.Y * seg.Y;

        if (segLenSq == 0)
        {
            // Degenerate segment (bullet didn't move) - point vs circle
            float distSq = toCircle.X * toCircle.X + toCircle.Y * toCircle.Y;
            return distSq <= totalRadius * totalRadius;
        }

        // Project circle center onto segment, clamped to [0, 1]
        float t = (toCircle.X * seg.X + toCircle.Y * seg.Y) / segLenSq;
        t = Math.Clamp(t, 0f, 1f);

        // Closest point on segment
        float closestX = segStart.X + t * seg.X;
        float closestY = segStart.Y + t * seg.Y;

        float dx = circleCenter.X - closestX;
        float dy = circleCenter.Y - closestY;
        float distanceSquared = dx * dx + dy * dy;

        return distanceSquared <= totalRadius * totalRadius;
    }
}
