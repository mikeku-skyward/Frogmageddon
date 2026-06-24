namespace BlazorAsteroids.Game.Models;

public enum StaminaState
{
    Idle,           // Full stamina, not sprinting
    Sprinting,      // Active sprint, depleting stamina
    RechargeDelay,  // Cooldown period after sprint ends
    Recharging      // Stamina actively recovering
}
