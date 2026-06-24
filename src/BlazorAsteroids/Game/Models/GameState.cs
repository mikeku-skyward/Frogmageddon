using BlazorAsteroids.Game.Interfaces;

namespace BlazorAsteroids.Game.Models;

public class GameState : IGameState
{
    public Player Player { get; set; } = new();
    public Camera Camera { get; set; } = new();
    public List<Frog> Frogs { get; set; } = new();
    public List<Bullet> Bullets { get; set; } = new();
    public FrogSpawner FrogSpawner { get; set; } = new();
    public AmmoSystem AmmoSystem { get; set; } = new();

    /// <summary>
    /// The viewport (canvas) width — what the player sees on screen.
    /// </summary>
    public int CanvasWidth { get; set; }

    /// <summary>
    /// The viewport (canvas) height — what the player sees on screen.
    /// </summary>
    public int CanvasHeight { get; set; }

    /// <summary>
    /// Full world width (the background image size).
    /// </summary>
    public float WorldWidth { get; set; }

    /// <summary>
    /// Full world height (the background image size).
    /// </summary>
    public float WorldHeight { get; set; }

    // Explicit interface implementation to satisfy IGameState.Player (read-only)
    Player IGameState.Player => Player;

    /// <summary>
    /// Minimum speed multiplier when moving directly away from cursor.
    /// </summary>
    private const float MinBackpedalMultiplier = 0.55f;

    public void Update(float deltaTime, Vector2 movementDirection, Vector2 cursorWorldPosition)
    {
        // Tick down the damage flash timer
        if (Player.DamageFlashTimer > 0f)
        {
            Player.DamageFlashTimer = MathF.Max(0f, Player.DamageFlashTimer - deltaTime);
        }

        // Tick down the invincibility timer
        if (Player.InvincibilityTimer > 0f)
        {
            Player.InvincibilityTimer = MathF.Max(0f, Player.InvincibilityTimer - deltaTime);
        }

        // Update player movement
        if (movementDirection.Length() > 0)
        {
            // Calculate speed multiplier based on angle between movement and cursor direction.
            // Full speed if cursor is in the front 180° of movement.
            // Gradually slows as cursor moves behind (toward directly opposite movement).
            float speedMultiplier = 1f;

            Vector2 toCursor = new Vector2(
                cursorWorldPosition.X - Player.Position.X,
                cursorWorldPosition.Y - Player.Position.Y
            );

            if (toCursor.Length() > 1f)
            {
                // Dot product of normalized movement and normalized cursor direction
                Vector2 moveNorm = movementDirection.Normalized();
                Vector2 cursorNorm = toCursor.Normalized();
                float dot = moveNorm.X * cursorNorm.X + moveNorm.Y * cursorNorm.Y;

                // dot = 1 means cursor is directly ahead (full speed)
                // dot = 0 means cursor is perpendicular (full speed — front 180°)
                // dot = -1 means cursor is directly behind (minimum speed)
                if (dot < 0)
                {
                    // Remap dot from [-1, 0] to [MinBackpedalMultiplier, 1]
                    // dot=0 -> multiplier=1, dot=-1 -> multiplier=MinBackpedalMultiplier
                    speedMultiplier = 1f + dot * (1f - MinBackpedalMultiplier);
                }
            }

            Vector2 velocity = movementDirection * (Player.Speed * speedMultiplier) * deltaTime;
            Player.Position = Player.Position + velocity;

            // Clamp position to world bounds
            Player.Position = new Vector2(
                Math.Clamp(Player.Position.X, Player.Size, WorldWidth - Player.Size),
                Math.Clamp(Player.Position.Y, Player.Size, WorldHeight - Player.Size)
            );

            // Update rotation to face movement direction
            Player.Rotation = MathF.Atan2(movementDirection.Y, movementDirection.X);
        }

        // Update camera to smoothly follow midpoint between player and cursor
        Camera.Follow(Player.Position, cursorWorldPosition, deltaTime);

        // Spawn frogs (groups of 5-7)
        var newFrogs = FrogSpawner.TrySpawn(deltaTime, Camera, Frogs.Count, Player.Position);
        if (newFrogs.Count > 0)
            Frogs.AddRange(newFrogs);

        // Update all frogs
        foreach (var frog in Frogs)
        {
            frog.Update(deltaTime, Player.Position);
        }

        // Update bullets
        foreach (var bullet in Bullets)
        {
            bullet.Update(deltaTime);
        }

        // Check bullet-frog collisions
        foreach (var bullet in Bullets)
        {
            if (!bullet.IsAlive) continue;

            foreach (var frog in Frogs)
            {
                if (bullet.IntersectsCircle(frog.Position, frog.Size))
                {
                    bullet.IsAlive = false;
                    frog.IsAlive = false;
                    Player.Score += 25;
                    break;
                }
            }
        }

        // Check frog-player collisions (skip if player is invincible)
        if (!Player.IsInvincible)
        {
            foreach (var frog in Frogs)
            {
                if (!frog.IsAlive) continue;

                float dx = frog.Position.X - Player.Position.X;
                float dy = frog.Position.Y - Player.Position.Y;
                float distSq = dx * dx + dy * dy;
                float combinedRadius = frog.Size + Player.Size;

                if (distSq <= combinedRadius * combinedRadius)
                {
                    frog.IsAlive = false;
                    Player.Health -= 25;
                    Player.DamageFlashTimer = 1f;
                    Player.InvincibilityTimer = 1.5f;
                    break; // Only take one hit per frame
                }
            }
        }

        // Remove dead bullets and frogs
        Bullets.RemoveAll(b => !b.IsAlive);
        Frogs.RemoveAll(f => !f.IsAlive);

        // Update ammo system reload timer
        AmmoSystem.Update(deltaTime);
    }

    /// <summary>
    /// Fires a bullet from the player toward a world-space target position.
    /// Gated through AmmoSystem.TryFire() — returns early if ammo is unavailable or reloading.
    /// </summary>
    public void FireBullet(Vector2 targetWorld)
    {
        if (!AmmoSystem.TryFire())
            return;

        Vector2 direction = new Vector2(
            targetWorld.X - Player.Position.X,
            targetWorld.Y - Player.Position.Y
        );

        if (direction.Length() > 0)
        {
            Bullets.Add(new Bullet(Player.Position, direction));
        }
    }
}
