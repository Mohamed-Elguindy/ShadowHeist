using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.WorldLighting.Rendering;

public sealed class LightingEffectRenderer : IDisposable
{
    private const string LaserEmitterTexturePath = "assets/sprites/prop_laser_emitter.png";
    private const float EmitterSpriteSize = 28f;
    private const float EmitterFallbackRadius = 7f;
    private const float LaserOuterGlowThickness = 6f;
    private const float LaserInnerCoreThickness = 2f;
    private const float LaserImpactDotRadius = 4f;
    private const float HalfDivisor = 0.5f;

    private static readonly Color VisionConeFillColor = new(250, 204, 21, 38);
    private static readonly Color VisionConeEdgeColor = new(250, 204, 21, 95);
    private static readonly Color LaserOuterGlowColor = new(239, 68, 68, 90);
    private static readonly Color LaserCoreColor = new(255, 75, 75, 240);
    private static readonly Color EmitterFallbackColor = new(148, 163, 184, 255);

    private Texture2D _emitterTexture;
    private bool _emitterTextureLoaded;

    public void LoadTextures()
    {
        if (Raylib.IsWindowReady() && File.Exists(LaserEmitterTexturePath))
        {
            _emitterTexture = Raylib.LoadTexture(LaserEmitterTexturePath);
            _emitterTextureLoaded = true;
        }
    }

    public void Draw(IReadOnlyList<Vector2> visionConeVertices, IReadOnlyList<LaserTripwire> lasers)
    {
        DrawVisionCone(visionConeVertices);
        DrawLaserTripwires(lasers);
    }

    public void Dispose()
    {
        if (_emitterTextureLoaded)
        {
            Raylib.UnloadTexture(_emitterTexture);
            _emitterTextureLoaded = false;
        }
    }

    private static void DrawVisionCone(IReadOnlyList<Vector2> vertices)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        Vector2 origin = vertices[0];

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            Raylib.DrawTriangle(origin, vertices[i + 1], vertices[i], VisionConeFillColor);
        }

        Raylib.DrawLineV(origin, vertices[1], VisionConeEdgeColor);
        Raylib.DrawLineV(origin, vertices[^1], VisionConeEdgeColor);
    }

    private void DrawLaserTripwires(IReadOnlyList<LaserTripwire> lasers)
    {
        foreach (var laser in lasers)
        {
            DrawLaserEmitter(laser.Origin);

            if (!laser.IsActive)
            {
                continue;
            }

            DrawLaserBeam(laser.Origin, laser.BeamEnd);
        }
    }

    private static void DrawLaserBeam(Vector2 start, Vector2 end)
    {
        Raylib.DrawLineEx(start, end, LaserOuterGlowThickness, LaserOuterGlowColor);
        Raylib.DrawLineEx(start, end, LaserInnerCoreThickness, LaserCoreColor);
        Raylib.DrawCircleV(end, LaserImpactDotRadius, LaserCoreColor);
    }

    private void DrawLaserEmitter(Vector2 position)
    {
        if (_emitterTextureLoaded)
        {
            float halfSize = EmitterSpriteSize * HalfDivisor;
            var sourceRect = new Rectangle(0f, 0f, _emitterTexture.Width, _emitterTexture.Height);
            var destRect = new Rectangle(position.X - halfSize, position.Y - halfSize, EmitterSpriteSize, EmitterSpriteSize);
            Raylib.DrawTexturePro(_emitterTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
            return;
        }

        Raylib.DrawCircleV(position, EmitterFallbackRadius, EmitterFallbackColor);
        Raylib.DrawCircleLines((int)position.X, (int)position.Y, EmitterFallbackRadius, LaserCoreColor);
    }
}
