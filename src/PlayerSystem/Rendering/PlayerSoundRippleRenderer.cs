using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders expanding 3D acoustic footstep/distraction ripples and the ground aim reticle.
/// </summary>
public sealed class PlayerSoundRippleRenderer
{
    private const int MaxRipples = 16;
    private const float RippleLifetimeSeconds = 0.85f;
    private const float LoudSprintNoiseThreshold = 180f;

    private readonly List<SoundRipple> _ripples = new();

    public void SubscribeEvents(GameEventBus eventBus)
    {
        _ripples.Clear();
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        for (int i = _ripples.Count - 1; i >= 0; i--)
        {
            SoundRipple updated = _ripples[i] with { Age = _ripples[i].Age + deltaTime };
            if (updated.Age >= RippleLifetimeSeconds)
            {
                _ripples.RemoveAt(i);
            }
            else
            {
                _ripples[i] = updated;
            }
        }
    }

    public void Draw(GameContext context)
    {
        DrawAcousticRipples();
        DrawAimReticle(context.PlayerPosition, context.AimWorldOverride, context.ElapsedSeconds);
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noiseEvent)
    {
        if (noiseEvent.Radius <= 0f)
        {
            return;
        }

        if (_ripples.Count >= MaxRipples)
        {
            _ripples.RemoveAt(0);
        }

        _ripples.Add(new SoundRipple(
            noiseEvent.Origin,
            MathF.Max(noiseEvent.Radius, 40f),
            ResolveRippleColor(noiseEvent),
            Age: 0f));
    }

    private static Color ResolveRippleColor(NoiseEmittedEvent noiseEvent) => noiseEvent.Category switch
    {
        NoiseCategory.Distraction => new Color(216, 180, 254, 255),
        NoiseCategory.Environment => new Color(244, 63, 94, 255),
        _ when noiseEvent.Radius >= LoudSprintNoiseThreshold => new Color(251, 146, 60, 255),
        _ => new Color(56, 225, 255, 255)
    };

    private void DrawAcousticRipples()
    {
        foreach (SoundRipple ripple in _ripples)
        {
            float progress = ripple.Age / RippleLifetimeSeconds;
            float radius = 14f + ((ripple.MaxRadius - 14f) * progress);
            byte alpha = (byte)(215f * (1f - progress));
            var ringColor = new Color(ripple.BaseColor.R, ripple.BaseColor.G, ripple.BaseColor.B, alpha);

            Raylib.DrawCylinderWires(new Vector3(ripple.Origin.X, 2f, ripple.Origin.Y), radius, radius, 0.6f, 28, ringColor);
        }
    }

    private static void DrawAimReticle(Vector2 playerPosition, Vector2? aimPoint, float timeSeconds)
    {
        if (aimPoint is not Vector2 aim)
        {
            return;
        }

        float pulseRadius = 10f + (MathF.Sin(timeSeconds * 5f) * 1.5f);
        Raylib.DrawCylinderWires(new Vector3(aim.X, 1.5f, aim.Y), pulseRadius, pulseRadius, 0.5f, 18, new Color(56, 225, 255, 190));
        Raylib.DrawLine3D(new Vector3(playerPosition.X, 22f, playerPosition.Y), new Vector3(aim.X, 2f, aim.Y), new Color(56, 225, 255, 65));
    }

    private readonly record struct SoundRipple(Vector2 Origin, float MaxRadius, Color BaseColor, float Age);
}
