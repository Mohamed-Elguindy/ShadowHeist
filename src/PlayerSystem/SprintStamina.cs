namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Tracks sprint stamina depletion, exhaustion cooldown, and regeneration.
/// </summary>
public sealed class SprintStamina
{
    public const float MaxStamina = 1f;
    public const float DrainSeconds = 3f;
    public const float RegenSeconds = 4f;
    public const float CooldownSeconds = 2.5f;

    private float _stamina = MaxStamina;
    private float _cooldownRemaining;

    public float Ratio => _stamina / MaxStamina;
    public bool IsOnCooldown => _cooldownRemaining > 0f;
    public bool CanSprint => !IsOnCooldown && _stamina > 0f;

    public void Reset()
    {
        _stamina = MaxStamina;
        _cooldownRemaining = 0f;
    }

    public void Update(float deltaTime, bool isSprinting)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - deltaTime);
            return;
        }

        if (isSprinting)
        {
            _stamina = MathF.Max(0f, _stamina - (deltaTime / DrainSeconds));
            if (_stamina <= 0f)
            {
                _cooldownRemaining = CooldownSeconds;
            }

            return;
        }

        _stamina = MathF.Min(MaxStamina, _stamina + (deltaTime / RegenSeconds));
    }
}

