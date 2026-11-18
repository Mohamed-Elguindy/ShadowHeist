using System.Numerics;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Tracks trauma-based quadratic alarm camera shake and rising-edge alarm activation.
/// </summary>
public sealed class CameraShakeEffect
{
    private const float SecondaryAxisRatio = 1.37f;

    private float _trauma;
    private float _elapsedSeconds;
    private bool _wasAlarmActive;

    public float Trauma => _trauma;
    public Vector3 CurrentOffset { get; private set; }

    public void Reset()
    {
        _trauma = 0f;
        _elapsedSeconds = 0f;
        _wasAlarmActive = false;
        CurrentOffset = Vector3.Zero;
    }

    public void Trigger(float intensity = 1f)
    {
        float clampedIntensity = Math.Clamp(intensity, 0f, 1f);
        _trauma = MathF.Max(_trauma, clampedIntensity);
    }

    public void NotifyAlarmState(bool isAlarmActive)
    {
        if (isAlarmActive && !_wasAlarmActive)
        {
            Trigger();
        }

        _wasAlarmActive = isAlarmActive;
    }

    public void Update(float deltaTime)
    {
        if (_trauma <= 0f)
        {
            CurrentOffset = Vector3.Zero;
            return;
        }

        float safeDelta = MathF.Max(0f, deltaTime);
        _elapsedSeconds += safeDelta;
        CurrentOffset = ComputeShakeOffset(_trauma);
        _trauma = MathF.Max(0f, _trauma - (Viewport3DConfig.AlarmShakeDecayRate * safeDelta));
    }

    private Vector3 ComputeShakeOffset(float trauma)
    {
        float magnitude = trauma * trauma * Viewport3DConfig.MaxAlarmShakeOffset;
        float phase = _elapsedSeconds * Viewport3DConfig.AlarmShakeFrequency;
        return new Vector3(
            MathF.Sin(phase) * magnitude,
            0f,
            MathF.Cos(phase * SecondaryAxisRatio) * magnitude);
    }
}
