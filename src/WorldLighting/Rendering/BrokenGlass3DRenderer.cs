using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders 3D shattered glass floor hazards with crystalline shard geometry, specular glints, and step crunch rings.
/// </summary>
public sealed class BrokenGlass3DRenderer
{
    private const float HalfScale = 0.5f;
    private const float BasePatchElevationY = 0.28f;
    private const float BasePatchHeight = 0.18f;
    private const float ShardBaseElevationY = 0.42f;
    private const float ShardPeakElevationY = 2.4f;
    private const float ShardRadius = 6.5f;
    private const float ShardSideSpanScale = 0.7f;
    private const float ShardApexOffsetScale = 0.4f;
    private const float ShardAngleStepRadians = 1.15f;
    private const float ShardInitialAngleRadians = 0.4f;
    private const float GlintPulseSpeed = 3.8f;
    private const float GlintPhaseStep = 1.3f;
    private const float GlintShimmerBase = 0.55f;
    private const float GlintShimmerAmplitude = 0.45f;
    private const float GlintMinAlpha = 120f;
    private const float GlintAlphaRange = 125f;
    private const float CrunchRingDurationSeconds = 0.85f;
    private const float CrunchRingElevationY = 1.1f;
    private const float CrunchRingHeight = 0.35f;
    private const float CrunchRingExtentScale = 0.75f;
    private const float CrunchRingMinRadiusFraction = 0.45f;
    private const float CrunchRingGrowthFraction = 0.55f;
    private const float CrunchRingBaseAlpha = 150f;
    private const float CrunchRingFadeFraction = 0.7f;
    private const int CrunchRingSegments = 20;

    private static readonly Vector2[] NormalizedShardOffsets =
    [
        new(0.22f, 0.28f),
        new(0.48f, 0.20f),
        new(0.76f, 0.32f),
        new(0.30f, 0.68f),
        new(0.62f, 0.58f),
        new(0.82f, 0.74f)
    ];

    private static readonly Color FrostedPatchFillColor = new(148, 226, 255, 28);
    private static readonly Color FrostedPatchBorderColor = new(186, 240, 255, 85);
    private static readonly Color ShardBodyColor = new(195, 242, 255, 135);
    private static readonly Color ShardGlintBaseColor = new(230, 252, 255, 255);
    private static readonly Color CrunchRingBaseColor = new(110, 235, 255, 255);

    public void Draw(IReadOnlyList<BrokenGlassTile> glassTiles, float elapsedSeconds)
    {
        Rlgl.DisableDepthMask();
        foreach (BrokenGlassTile tile in glassTiles)
        {
            DrawGlassPatchBase(tile.Bounds);
            DrawCrystallineShards(tile.Bounds, elapsedSeconds);
            DrawCrunchRippleIfActive(tile, elapsedSeconds);
        }

        Rlgl.EnableDepthMask();
    }

    private static void DrawGlassPatchBase(RectangleBounds bounds)
    {
        var center = new Vector3(
            bounds.X + (bounds.Width * HalfScale),
            BasePatchElevationY,
            bounds.Y + (bounds.Height * HalfScale));

        Raylib.DrawCube(center, bounds.Width, BasePatchHeight, bounds.Height, FrostedPatchFillColor);
        Raylib.DrawCubeWires(center, bounds.Width, BasePatchHeight, bounds.Height, FrostedPatchBorderColor);
    }

    private static void DrawCrystallineShards(RectangleBounds bounds, float elapsedSeconds)
    {
        for (int index = 0; index < NormalizedShardOffsets.Length; index++)
        {
            Vector2 normalized = NormalizedShardOffsets[index];
            float shardX = bounds.X + (bounds.Width * normalized.X);
            float shardZ = bounds.Y + (bounds.Height * normalized.Y);
            float angleRadians = (index * ShardAngleStepRadians) + ShardInitialAngleRadians;

            DrawSingleShard(new Vector2(shardX, shardZ), angleRadians, index, elapsedSeconds);
        }
    }

    private static void DrawSingleShard(Vector2 center2D, float angleRadians, int shardIndex, float elapsedSeconds)
    {
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);
        float sideSpan = ShardRadius * ShardSideSpanScale;
        float apexOffset = ShardRadius * ShardApexOffsetScale;

        var tipA = new Vector3(center2D.X + (cos * ShardRadius), ShardBaseElevationY, center2D.Y + (sin * ShardRadius));
        var tipB = new Vector3(center2D.X - (sin * sideSpan), ShardBaseElevationY, center2D.Y + (cos * sideSpan));
        var apex = new Vector3(center2D.X - (cos * apexOffset), ShardPeakElevationY, center2D.Y - (sin * apexOffset));

        Raylib.DrawTriangle3D(tipA, tipB, apex, ShardBodyColor);
        Raylib.DrawTriangle3D(tipB, tipA, apex, ShardBodyColor);

        float shimmer = GlintShimmerBase + (GlintShimmerAmplitude * MathF.Sin((elapsedSeconds * GlintPulseSpeed) + (shardIndex * GlintPhaseStep)));
        byte glintAlpha = (byte)(GlintMinAlpha + (GlintAlphaRange * shimmer));
        Color glintColor = WithAlpha(ShardGlintBaseColor, glintAlpha);

        Raylib.DrawLine3D(tipA, apex, glintColor);
        Raylib.DrawLine3D(tipB, apex, glintColor);
    }

    private static void DrawCrunchRippleIfActive(BrokenGlassTile tile, float elapsedSeconds)
    {
        if (tile.TriggerCount <= 0)
        {
            return;
        }

        float timeSinceTrigger = elapsedSeconds - tile.LastTriggerTimeSeconds;
        if (timeSinceTrigger < 0f || timeSinceTrigger > CrunchRingDurationSeconds)
        {
            return;
        }

        float progress = Math.Clamp(timeSinceTrigger / CrunchRingDurationSeconds, 0f, 1f);
        float maxRadius = MathF.Max(tile.Bounds.Width, tile.Bounds.Height) * CrunchRingExtentScale;
        float radius = (maxRadius * CrunchRingMinRadiusFraction) + (maxRadius * CrunchRingGrowthFraction * progress);
        byte alpha = (byte)(CrunchRingBaseAlpha * (1f - (progress * CrunchRingFadeFraction)));

        var center = new Vector3(
            tile.Bounds.X + (tile.Bounds.Width * HalfScale),
            CrunchRingElevationY,
            tile.Bounds.Y + (tile.Bounds.Height * HalfScale));

        Raylib.DrawCylinderWires(center, radius, radius, CrunchRingHeight, CrunchRingSegments, WithAlpha(CrunchRingBaseColor, alpha));
    }

    private static Color WithAlpha(Color color, byte alpha) => new(color.R, color.G, color.B, alpha);
}
