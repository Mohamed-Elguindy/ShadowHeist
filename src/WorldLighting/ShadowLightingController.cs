using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Manages dynamic 3D room darkness intensity, FuseBox blackout transitions, and zone-based shadow mood levels.
/// </summary>
public sealed class ShadowLightingController
{
    public const float DefaultPoweredDarkness = 0.30f;
    public const float BlackoutDarkness = 0.72f;
    public const float VaultDarknessBonus = 0.14f;
    public const float LampLightInfluenceRadius = 160f;
    public const float MaxLampDarknessReduction = 0.22f;

    private const float DarknessTransitionSpeed = 3.2f;
    private const float AlarmPulseFrequency = 4.5f;
    private const float AlarmPulseBase = 0.5f;
    private const float AlarmPulseAmplitude = 0.5f;

    public bool IsMainPowerOnline { get; private set; } = true;
    public float AmbientDarknessIntensity { get; private set; } = DefaultPoweredDarkness;
    public float EmergencyPulseIntensity { get; private set; }

    public void SetPowerState(bool isPowered)
    {
        IsMainPowerOnline = isPowered;
    }

    public void Update(float deltaTime, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        float safeDelta = MathF.Max(0f, deltaTime);
        float targetDarkness = IsMainPowerOnline ? DefaultPoweredDarkness : BlackoutDarkness;
        float interpolationFactor = Math.Clamp(safeDelta * DarknessTransitionSpeed, 0f, 1f);

        AmbientDarknessIntensity += (targetDarkness - AmbientDarknessIntensity) * interpolationFactor;

        EmergencyPulseIntensity = context.IsAlarmTriggered
            ? AlarmPulseBase + (AlarmPulseAmplitude * MathF.Sin(context.ElapsedSeconds * AlarmPulseFrequency))
            : 0f;
    }

    public float GetZoneDarkness(Vector2 worldPosition)
    {
        float darkness = AmbientDarknessIntensity;

        if (IsInsideVaultZone(worldPosition))
        {
            darkness += VaultDarknessBonus;
        }

        if (IsMainPowerOnline)
        {
            darkness -= ComputeLampIlluminationReduction(worldPosition);
        }

        return Math.Clamp(darkness, 0f, 1f);
    }

    public void Reset()
    {
        IsMainPowerOnline = true;
        AmbientDarknessIntensity = DefaultPoweredDarkness;
        EmergencyPulseIntensity = 0f;
    }

    private static bool IsInsideVaultZone(Vector2 position) =>
        position.X >= Viewport3DConfig.VaultZoneMin.X &&
        position.X <= Viewport3DConfig.VaultZoneMax.X &&
        position.Y >= Viewport3DConfig.VaultZoneMin.Y &&
        position.Y <= Viewport3DConfig.VaultZoneMax.Y;

    private static float ComputeLampIlluminationReduction(Vector2 position)
    {
        float strongestReduction = 0f;

        foreach (Vector2 lampPosition in Viewport3DConfig.WarmLampPositions)
        {
            float distance = Vector2.Distance(position, lampPosition);
            if (distance >= LampLightInfluenceRadius)
            {
                continue;
            }

            float falloff = 1f - (distance / LampLightInfluenceRadius);
            float reduction = falloff * MaxLampDarknessReduction;
            if (reduction > strongestReduction)
            {
                strongestReduction = reduction;
            }
        }

        return strongestReduction;
    }
}
