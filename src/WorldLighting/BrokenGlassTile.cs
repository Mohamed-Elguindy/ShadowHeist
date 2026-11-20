using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Represents a 2D floor hazard of shattered glass that emits a loud footstep noise event when stepped on.
/// </summary>
public sealed class BrokenGlassTile
{
    public const float DefaultGlassNoiseRadius = 220f;
    public const float DefaultStepTriggerDistance = 24f;
    private const float MinStepTriggerDistance = 1f;
    private const string NoiseSourcePrefix = "BrokenGlass:";

    private Vector2? _lastCrunchPosition;

    public string Id { get; }
    public RectangleBounds Bounds { get; }
    public float NoiseRadius { get; }
    public float StepTriggerDistance { get; }
    public bool IsPlayerOnGlass { get; private set; }
    public int TriggerCount { get; private set; }
    public float LastTriggerTimeSeconds { get; private set; }

    public BrokenGlassTile(
        string id,
        RectangleBounds bounds,
        float noiseRadius = DefaultGlassNoiseRadius,
        float stepTriggerDistance = DefaultStepTriggerDistance)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Broken glass tile ID cannot be null or whitespace.", nameof(id));
        }

        Id = id;
        Bounds = bounds;
        NoiseRadius = MathF.Max(0f, noiseRadius);
        StepTriggerDistance = MathF.Max(MinStepTriggerDistance, stepTriggerDistance);
    }

    public bool Update(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!IsPointInsideBounds(context.PlayerPosition, Bounds))
        {
            IsPlayerOnGlass = false;
            _lastCrunchPosition = null;
            return false;
        }

        bool justEntered = !IsPlayerOnGlass;
        IsPlayerOnGlass = true;

        if (!justEntered && !HasMovedFullStepAcrossGlass(context.PlayerPosition))
        {
            return false;
        }

        EmitGlassCrunchNoise(context);
        return true;
    }

    public void Reset()
    {
        IsPlayerOnGlass = false;
        TriggerCount = 0;
        LastTriggerTimeSeconds = 0f;
        _lastCrunchPosition = null;
    }

    private static bool IsPointInsideBounds(Vector2 point, RectangleBounds bounds) =>
        bounds.Width > 0f &&
        bounds.Height > 0f &&
        point.X >= bounds.X &&
        point.X <= bounds.X + bounds.Width &&
        point.Y >= bounds.Y &&
        point.Y <= bounds.Y + bounds.Height;

    private bool HasMovedFullStepAcrossGlass(Vector2 currentPosition)
    {
        if (_lastCrunchPosition is not Vector2 previousPosition)
        {
            return true;
        }

        float stepThresholdSquared = StepTriggerDistance * StepTriggerDistance;
        return Vector2.DistanceSquared(previousPosition, currentPosition) >= stepThresholdSquared;
    }

    private void EmitGlassCrunchNoise(GameContext context)
    {
        _lastCrunchPosition = context.PlayerPosition;
        LastTriggerTimeSeconds = context.ElapsedSeconds;
        TriggerCount++;

        context.EventBus.Publish(new NoiseEmittedEvent(
            context.PlayerPosition,
            NoiseRadius,
            $"{NoiseSourcePrefix}{Id}",
            NoiseCategory.Player));
    }
}
