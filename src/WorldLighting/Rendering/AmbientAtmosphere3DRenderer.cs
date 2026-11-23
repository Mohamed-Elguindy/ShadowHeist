using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders subtle floating dust motes in illuminated lobby lamp zones and the vault chamber.
/// </summary>
internal static class AmbientAtmosphere3DRenderer
{
    private const int LobbyMoteCount = 16;
    private const int VaultMoteCount = 14;
    private const float MoteBaseHeight = 22f;
    private const float MoteVerticalSpan = 48f;
    private const float LobbyOrbitRadius = 72f;
    private const float VaultHalfSpanX = 270f;
    private const float VaultHalfSpanZ = 90f;
    private const float MoteSize = 2.1f;
    private const float DriftSpeed = 0.45f;
    private const float PhaseStep = 1.7f;
    private const float VerticalCycleSpeed = 0.7f;

    private static readonly Color WarmMoteColor = new(255, 224, 148, 95);
    private static readonly Color VaultMoteColor = new(56, 225, 255, 85);

    public static void Draw(float elapsedSeconds, bool areLobbyLightsPowered)
    {
        if (areLobbyLightsPowered)
        {
            DrawWarmLampMotes(elapsedSeconds);
        }

        DrawVaultMotes(elapsedSeconds);
    }

    private static void DrawWarmLampMotes(float timeSeconds)
    {
        Vector2[] lamps = Viewport3DConfig.WarmLampPositions;
        for (int i = 0; i < LobbyMoteCount; i++)
        {
            Vector2 lamp = lamps[i % lamps.Length];
            float angle = (i * PhaseStep) + (timeSeconds * DriftSpeed);
            float radius = LobbyOrbitRadius * (0.35f + (0.65f * ((i % 4) / 3f)));
            float y = MoteBaseHeight + (MoteVerticalSpan * (0.5f + (0.5f * MathF.Sin((timeSeconds * VerticalCycleSpeed) + i))));
            var pos = new Vector3(lamp.X + (MathF.Cos(angle) * radius), y, lamp.Y + (MathF.Sin(angle) * radius));
            Raylib.DrawCube(pos, MoteSize, MoteSize, MoteSize, WarmMoteColor);
        }
    }

    private static void DrawVaultMotes(float timeSeconds)
    {
        Vector2 vaultCenter = (Viewport3DConfig.VaultZoneMin + Viewport3DConfig.VaultZoneMax) * 0.5f;
        for (int i = 0; i < VaultMoteCount; i++)
        {
            float phase = i * PhaseStep;
            float x = vaultCenter.X + (MathF.Sin((timeSeconds * DriftSpeed) + phase) * VaultHalfSpanX);
            float z = vaultCenter.Y + (MathF.Cos((timeSeconds * DriftSpeed * 0.8f) + phase) * VaultHalfSpanZ);
            float y = MoteBaseHeight + (MoteVerticalSpan * (0.5f + (0.5f * MathF.Cos((timeSeconds * VerticalCycleSpeed) + phase))));
            Raylib.DrawCube(new Vector3(x, y, z), MoteSize, MoteSize, MoteSize, VaultMoteColor);
        }
    }
}
