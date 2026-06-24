namespace BlazorAsteroids.Game.Models;

public class Player
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; }
    public float Size { get; set; }
    public float Rotation { get; set; }
    public int Health { get; set; } = 100;
    public int Score { get; set; } = 0;
}
