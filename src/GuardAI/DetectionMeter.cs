namespace ShadowHeist.GuardAI;

/// <summary>
/// Progressive 0% -> 100% guard detection meter scaling with target distance, sneak stance, and close proximity (Ticket #22 — Hatem).
/// </summary>
public sealed class DetectionMeter
{
    public const float MaxProgress = 1f;
    public const float BaseFillRatePerSecond = 0.825f;
    public const float ProximityBonusFillRate = 0.675f;
    public const float DecayRatePerSecond = 0.8f;
    public const float SneakRateMultiplier = 0.6f;
    public const float MinDistanceMultiplier = 0.85f;
    public const float MaxDistanceMultiplier = 3.2f;

    public float Progress { get; private set; }
    public bool IsFullyDetected => Progress >= MaxProgress;

    public void Reset() => Progress = 0f;

    public void FillInstantly() => Progress = MaxProgress;

    public bool Update(
        float deltaTime,
        bool isVisibleInCone,
        bool isInProximityCircle,
        float distanceToPlayer,
        float activeVisionRange,
        bool isPlayerSneaking)
    {
        if (deltaTime <= 0f)
        {
            return IsFullyDetected;
        }

        if (!isVisibleInCone && !isInProximityCircle)
        {
            Progress = MathF.Max(0f, Progress - (DecayRatePerSecond * deltaTime));
            return false;
        }

        float fillRate = ComputeFillRate(
            isVisibleInCone,
            isInProximityCircle,
            distanceToPlayer,
            activeVisionRange,
            isPlayerSneaking);

        Progress = Math.Clamp(Progress + (fillRate * deltaTime), 0f, MaxProgress);
        return IsFullyDetected;
    }

    public static float ComputeFillRate(
        bool isVisibleInCone,
        bool isInProximityCircle,
        float distanceToPlayer,
        float activeVisionRange,
        bool isPlayerSneaking)
    {
        float safeRange = MathF.Max(1f, activeVisionRange);
        float normalizedDistance = Math.Clamp(distanceToPlayer / safeRange, 0f, 1f);
        float proximityFactor = 1f - normalizedDistance;
        float distanceMultiplier = MinDistanceMultiplier + ((MaxDistanceMultiplier - MinDistanceMultiplier) * proximityFactor * proximityFactor);

        float rate = 0f;
        if (isVisibleInCone)
        {
            rate += BaseFillRatePerSecond * distanceMultiplier;
        }

        if (isInProximityCircle)
        {
            rate += ProximityBonusFillRate * distanceMultiplier;
        }

        if (isPlayerSneaking)
        {
            rate *= SneakRateMultiplier;
        }

        return rate;
    }
}
