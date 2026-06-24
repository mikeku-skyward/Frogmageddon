namespace BlazorAsteroids.Game.Models;

public class Player
{
    public Vector2 Position { get; set; }
    public float Speed { get; set; }
    public float Size { get; set; }
    public float Rotation { get; set; }
    public int Health { get; set; } = 100;
    public int Score { get; set; } = 0;

    /// <summary>
    /// Remaining time (in seconds) the player should flash red after taking damage.
    /// </summary>
    public float DamageFlashTimer { get; set; } = 0f;

    /// <summary>
    /// Remaining invincibility time (in seconds) after taking damage.
    /// While active, the player cannot take further damage from frogs.
    /// </summary>
    public float InvincibilityTimer { get; set; } = 0f;

    /// <summary>
    /// Whether the player is currently in the damage flash state.
    /// </summary>
    public bool IsFlashing => DamageFlashTimer > 0f;

    /// <summary>
    /// Whether the player is currently invincible.
    /// </summary>
    public bool IsInvincible => InvincibilityTimer > 0f;
}
