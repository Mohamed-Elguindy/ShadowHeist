using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.Viewport3D;

/// <summary>Loaded single-mesh prop plus uniform scale, vertical grounding offset, and XZ center offset.</summary>
public readonly record struct LoadedModel(
    Model Model,
    float Scale,
    float BaseOffsetY,
    Vector2 CenterOffsetXZ);

/// <summary>Articulated 6-part character rig whose pivots live at each joint origin.</summary>
public readonly record struct CharacterRig(
    Model Body,
    Model Head,
    Model ArmLeft,
    Model ArmRight,
    Model LegLeft,
    Model LegRight,
    float Scale);

/// <summary>Placement descriptor for a 3D furniture prop in world coordinates.</summary>
public readonly record struct FurnitureSpec(
    string AssetKey,
    Vector2 Position,
    float RotationDegrees,
    float TargetMaxExtent,
    bool EmitsWarmGlow = false);

/// <summary>Immutable per-frame render state passed cleanly to every 3D scene render pass.</summary>
public readonly record struct Scene3DRenderFrame(
    GameContext Context,
    IReadOnlyList<RectangleBounds> ArchitecturalWalls,
    IReadOnlyList<RectangleBounds> VisionBlockers,
    IReadOnlyList<Guard> Guards,
    IReadOnlyList<LaserTripwire> Lasers,
    RectangleBounds? GetawayZone,
    float PlayerRotationDegrees,
    float ElapsedSeconds);

/// <summary>Contract for each modular 3D rendering layer in Scene3DRenderer (OCP / SRP).</summary>
public interface ISceneRenderPass
{
    void Draw(in Scene3DRenderFrame frame);
}
