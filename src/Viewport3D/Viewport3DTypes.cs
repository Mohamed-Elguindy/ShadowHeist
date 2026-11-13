using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>Loaded single-mesh prop plus uniform scale, vertical grounding offset, and XZ center offset.</summary>
public readonly record struct LoadedModel(Model Model, float Scale, float BaseOffsetY, Vector2 CenterOffsetXZ);

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
