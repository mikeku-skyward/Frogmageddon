namespace BlazorAsteroids.Game.Models;

public class AmmoSystem
{
    public const int MaxAmmo = 20;
    public const float ReloadDuration = 2.0f;

    public int CurrentAmmo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadTimeRemaining { get; private set; }

    /// <summary>
    /// Whether the player can fire: ammo available and not currently reloading.
    /// </summary>
    public bool CanFire => CurrentAmmo > 0 && !IsReloading;

    /// <summary>
    /// Reload progress as a ratio from 0 (just started) to 1 (complete).
    /// Returns 0 when not reloading.
    /// </summary>
    public float ReloadProgress => IsReloading
        ? 1f - (ReloadTimeRemaining / ReloadDuration)
        : 0f;

    public AmmoSystem()
    {
        CurrentAmmo = MaxAmmo;
        IsReloading = false;
        ReloadTimeRemaining = 0f;
    }

    /// <summary>
    /// Attempts to fire. Returns true and decrements ammo if CanFire.
    /// If ammo is 0 and not reloading, triggers auto-reload and returns false.
    /// If reloading, returns false without side effects.
    /// </summary>
    public bool TryFire()
    {
        if (IsReloading)
            return false;

        if (CurrentAmmo > 0)
        {
            CurrentAmmo--;
            return true;
        }

        // Ammo is 0 and not reloading: auto-reload
        StartReload();
        return false;
    }

    /// <summary>
    /// Begins reload if ammo is below max and not already reloading. No-ops otherwise.
    /// </summary>
    public void StartReload()
    {
        if (CurrentAmmo >= MaxAmmo || IsReloading)
            return;

        IsReloading = true;
        ReloadTimeRemaining = ReloadDuration;
    }

    /// <summary>
    /// Exits the reloading state without restoring ammo. Called on player death.
    /// </summary>
    public void CancelReload()
    {
        IsReloading = false;
        ReloadTimeRemaining = 0f;
    }

    /// <summary>
    /// Decrements ReloadTimeRemaining by deltaTime. Completes reload when timer hits 0.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsReloading)
            return;

        ReloadTimeRemaining -= deltaTime;

        if (ReloadTimeRemaining <= 0f)
        {
            CurrentAmmo = MaxAmmo;
            IsReloading = false;
            ReloadTimeRemaining = 0f;
        }
    }

    /// <summary>
    /// Restores the system to initial full-magazine state. Called on game restart.
    /// </summary>
    public void Reset()
    {
        CurrentAmmo = MaxAmmo;
        IsReloading = false;
        ReloadTimeRemaining = 0f;
    }

    /// <summary>
    /// Returns formatted ammo text for the HUD: "{current}/{max}".
    /// </summary>
    public string GetHudText()
    {
        return $"{CurrentAmmo}/{MaxAmmo}";
    }
}
