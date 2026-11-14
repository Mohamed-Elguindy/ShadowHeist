using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.WorldLighting;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Renders articulated 3D guard rigs, wall-clipped 3D vision cones, flashlights, and overhead awareness badges.
/// </summary>
public sealed class GuardRenderer : IDisposable
{
    private const string CharacterAssetDir = "assets/models/characters";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";

    private readonly CharacterPoseAnimator _poseAnimator = new();
    private CharacterRig? _guardRig;

    public void Load(ToonModelLibrary models)
    {
        if (_guardRig is not null || !Raylib.IsWindowReady())
        {
            return;
        }

        models.EnsureLoaded();
        _guardRig = models.LoadCharacterRig(CharacterAssetDir, GuardSkinPath, Viewport3DConfig.CharacterUnitScale);
    }

    public void Update(float deltaTime, IReadOnlyList<Guard> guards, ToonModelLibrary models)
    {
        for (int i = 0; i < guards.Count; i++)
        {
            Guard guard = guards[i];
            _poseAnimator.UpdateActor(guard.Id, guard.Position, deltaTime);
            models.SetGuardFlashlight(i, guard.Position, guard.RotationDegrees);
        }

        models.ClearUnusedGuardFlashlights(guards.Count);
    }

    public void Draw(IReadOnlyList<Guard> guards, IReadOnlyList<RectangleBounds> visionBlockers, GameContext context)
    {
        foreach (Guard guard in guards)
        {
            DrawGuardVisionCone(guard, visionBlockers, context.IsAlarmTriggered, context.ElapsedSeconds);

            if (_guardRig is not CharacterRig guardRig)
            {
                continue;
            }

            string stateName = guard.CurrentState.StateName;
            CharacterPose pose = _poseAnimator.ComputePose(guard.Id, guardRig.Scale, isSneaking: false, isGuard: true, context.ElapsedSeconds);

            Raylib3DPrimitives.DrawGroundRing(guard.Position, ResolveGuardRingColor(stateName));
            CharacterMeshDrawer.DrawCharacterRig(context.Models, guardRig, guard.Position, guard.RotationDegrees, pose, DrawGuardFlashlight);
            DrawAwarenessBadge(guard.Position, stateName, context.ElapsedSeconds);
        }
    }

    public void Dispose() => _guardRig = null;

    private static void DrawGuardVisionCone(
        Guard guard,
        IReadOnlyList<RectangleBounds> visionBlockers,
        bool alarmActive,
        float elapsedSeconds)
    {
        (Color fill, Color edge) = ResolveGuardConeColors(guard.CurrentState.StateName, alarmActive, elapsedSeconds);
        IReadOnlyList<Vector2> cone = VisionRaycaster.ComputeVisionCone(
            guard.Position,
            guard.RotationDegrees,
            visionBlockers,
            fovDegrees: Viewport3DConfig.VisionHalfAngleDegrees * 2f,
            maxDistance: Viewport3DConfig.VisionRange,
            rayCount: Viewport3DConfig.VisionRayCount);

        VisionCone3DRenderer.DrawProjectedVisionPolygon(cone, groundY: 2.0f, sourceY: 36f, fill, edge);
    }

    private static (Color Fill, Color Edge) ResolveGuardConeColors(string stateName, bool alarmActive, float timeSeconds)
    {
        if (alarmActive || stateName == "Alert")
        {
            byte pulseAlpha = (byte)(95f + (35f * MathF.Sin(timeSeconds * 8f)));
            return (new Color((byte)244, (byte)63, (byte)94, pulseAlpha), new Color(255, 110, 125, 220));
        }

        if (stateName == "Suspicious")
        {
            return (new Color(251, 146, 60, 112), new Color(253, 186, 116, 215));
        }

        return (new Color(255, 214, 78, 108), new Color(255, 238, 145, 210));
    }

    private static Color ResolveGuardRingColor(string stateName) => stateName switch
    {
        "Alert" => new Color(244, 63, 94, 225),
        "Suspicious" => new Color(250, 204, 21, 210),
        _ => new Color(255, 208, 92, 165)
    };

    private static void DrawGuardFlashlight(Vector3 handPosition)
    {
        Raylib.DrawCylinderEx(handPosition, handPosition + new Vector3(0f, -12f, 2f), 2.2f, 3.2f, 8, new Color(24, 28, 36, 255));
        Raylib.DrawSphere(handPosition + new Vector3(0f, -13f, 2.2f), 2.8f, new Color(255, 240, 160, 235));
    }

    private static void DrawAwarenessBadge(Vector2 position, string stateName, float timeSeconds)
    {
        bool isAlert = stateName == "Alert";
        bool isSuspicious = stateName == "Suspicious";
        float bobY = 84f + (MathF.Sin(timeSeconds * (isAlert ? 10f : 3f)) * 2.5f);
        var badgeBase = new Vector3(position.X, bobY, position.Y);
        Color badgeColor = isAlert
            ? new Color(244, 63, 94, 245)
            : isSuspicious ? new Color(250, 204, 21, 240) : new Color(255, 214, 78, 175);

        float coneHeight = isAlert ? 12f : 6f;
        float coneRadius = isAlert ? 4.5f : 3.2f;
        Raylib.DrawCylinderEx(badgeBase, badgeBase + new Vector3(0f, coneHeight, 0f), 0.5f, coneRadius, 6, badgeColor);

        if (isAlert || isSuspicious)
        {
            Raylib.DrawSphere(badgeBase + new Vector3(0f, -4f, 0f), 2.5f, badgeColor);
        }
    }
}
