namespace BlazorAsteroids.Game.Models;

public class StaminaSystem
{
    public const float SprintMultiplier = 1.3f;
    public const float DepletionRate = 0.2f;           // per second
    public const float RechargeRate = 0.0667f;         // per second
    public const float RechargeDelayDuration = 2.0f;   // seconds

    public float Stamina { get; private set; } = 1.0f;
    public StaminaState State { get; private set; } = StaminaState.Idle;
    public float RechargeDelayTimer { get; private set; } = 0f;

    /// <summary>
    /// Whether sprint is currently active.
    /// </summary>
    public bool IsSprinting => State == StaminaState.Sprinting;

    /// <summary>
    /// Returns 1.3 if sprinting, else 1.0.
    /// </summary>
    public float SpeedMultiplier => IsSprinting ? SprintMultiplier : 1.0f;

    /// <summary>
    /// Advances the stamina state machine one frame.
    /// </summary>
    public void Update(float deltaTime, bool shiftPressed)
    {
        // Defensive: treat negative deltaTime as 0
        if (deltaTime < 0f)
            deltaTime = 0f;

        switch (State)
        {
            case StaminaState.Idle:
                if (shiftPressed && Stamina > 0f)
                {
                    State = StaminaState.Sprinting;
                }
                break;

            case StaminaState.Sprinting:
                // Deplete stamina
                Stamina = MathF.Max(0f, Stamina - DepletionRate * deltaTime);

                if (!shiftPressed || Stamina == 0f)
                {
                    // Sprint ends — transition to recharge delay
                    State = StaminaState.RechargeDelay;
                    RechargeDelayTimer = RechargeDelayDuration;
                }
                break;

            case StaminaState.RechargeDelay:
                if (shiftPressed && Stamina > 0f)
                {
                    // Resume sprinting mid-delay
                    State = StaminaState.Sprinting;
                    RechargeDelayTimer = 0f;
                }
                else
                {
                    // Count down delay timer
                    RechargeDelayTimer -= deltaTime;

                    if (RechargeDelayTimer <= 0f)
                    {
                        RechargeDelayTimer = 0f;
                        State = StaminaState.Recharging;
                    }
                }
                break;

            case StaminaState.Recharging:
                if (shiftPressed && Stamina > 0f)
                {
                    // Interrupt recharge to sprint
                    State = StaminaState.Sprinting;
                }
                else
                {
                    // Recharge stamina
                    Stamina = MathF.Min(1.0f, Stamina + RechargeRate * deltaTime);

                    if (Stamina >= 1.0f)
                    {
                        Stamina = 1.0f;
                        State = StaminaState.Idle;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Resets to full stamina, Idle state. Called on game restart.
    /// </summary>
    public void Reset()
    {
        Stamina = 1.0f;
        State = StaminaState.Idle;
        RechargeDelayTimer = 0f;
    }
}
