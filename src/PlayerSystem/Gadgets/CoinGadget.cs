using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Gadgets;

/// <summary>
/// Distraction coin gadget that clips against walls on any map and lures guards without spiking SoundMeter.
/// </summary>
public sealed class CoinGadget : IGadget
{
    public const int DefaultCharges = 3;
    public const float DefaultMaxThrowDistance = 260f;
    public const float DefaultNoiseRadius = 280f;

    private const string CoinNoiseSource = "Gadget:Coin";
    private const float CoinCollisionRadius = 8f;

    private IReadOnlyList<RectangleBounds>? _walls;

    public string Name => "Coin";
    public int RemainingCharges { get; private set; }
    public float MaxThrowDistance { get; }
    public float NoiseRadius { get; }
    public Vector2 LastThrowOrigin { get; private set; }
    public Vector2 LastLandingPoint { get; private set; }

    public CoinGadget(
        int initialCharges = DefaultCharges,
        float maxThrowDistance = DefaultMaxThrowDistance,
        float noiseRadius = DefaultNoiseRadius)
    {
        RemainingCharges = Math.Max(0, initialCharges);
        MaxThrowDistance = MathF.Max(0f, maxThrowDistance);
        NoiseRadius = MathF.Max(0f, noiseRadius);
    }

    public void SetWalls(IReadOnlyList<RectangleBounds>? walls) => _walls = walls;

    public bool TryUse(Vector2 origin, Vector2 target, GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        if (RemainingCharges <= 0)
        {
            return false;
        }

        Vector2 landingPoint = GadgetThrowHelper.ResolveLandingPoint(
            origin,
            target,
            MaxThrowDistance,
            CoinCollisionRadius,
            _walls);

        RemainingCharges--;
        LastThrowOrigin = origin;
        LastLandingPoint = landingPoint;

        eventBus.Publish(new NoiseEmittedEvent(
            landingPoint,
            NoiseRadius,
            CoinNoiseSource,
            NoiseCategory.Distraction));
        return true;
    }
}
