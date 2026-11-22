using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.WorldLighting;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Renders articulated 3D guard rigs, patrol/A* gizmos, wall-clipped 3D vision cones, flashlights, and awareness badges.
/// </summary>
public sealed class GuardRenderer : IDisposable
{
    private const string CharacterAssetDir = "assets/models/characters";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";
    private const float VisionConeGroundY = 2.0f;
    private const float VisionConeSourceY = 36f;
    private const float FullConeFovScale = 2f;
    private const float AlertPulseBaseAlpha = 95f;
    private const float AlertPulseAmplitude = 35f;
    private const float AlertPulseSpeed = 8f;
    private const float FlashlightTipOffsetY = -12f;
    private const float FlashlightTipOffsetZ = 2f;
    private const float FlashlightStartRadius = 2.2f;
    private const float FlashlightEndRadius = 3.2f;
    private const int FlashlightCylinderSides = 8;
    private const float FlashlightBulbOffsetY = -13f;
    private const float FlashlightBulbOffsetZ = 2.2f;
    private const float FlashlightBulbRadius = 2.8f;

    private readonly CharacterPoseAnimator _poseAnimator = new();
    private readonly GuardPathGizmo3DRenderer _pathGizmoRenderer = new();
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

    public void RecordNoisePulse(Vector2 origin, float radius) =>
        _pathGizmoRenderer.RecordNoisePulse(origin, radius);

    public void Update(float deltaTime, IReadOnlyList<Guard> guards, ToonModelLibrary models)
    {
        for (int guardIndex = 0; guardIndex < guards.Count; guardIndex++)
        {
            Guard guard = guards[guardIndex];
            _poseAnimator.UpdateActor(guard.Id, guard.Position, deltaTime);
            models.SetGuardFlashlight(guardIndex, guard.Position, guard.RotationDegrees);
        }

        models.ClearUnusedGuardFlashlights(guards.Count);
    }

    public void Draw(IReadOnlyList<Guard> guards, IReadOnlyList<RectangleBounds> visionBlockers, GameContext context)
    {
        _pathGizmoRenderer.Draw(guards, context.ElapsedSeconds);

        foreach (Guard guard in guards)
        {
            DrawGuardVisionCone(guard, visionBlockers, context.IsAlarmTriggered, context.ElapsedSeconds);
            GuardDetectionBadge3DRenderer.DrawProximityCircle(guard, context.IsAlarmTriggered);

            if (_guardRig is not CharacterRig guardRig)
            {
                continue;
            }

            string stateName = guard.CurrentState.StateName;
            CharacterPose pose = _poseAnimator.ComputePose(guard.Id, guardRig.Scale, isSneaking: false, isGuard: true, context.ElapsedSeconds);

            Raylib3DPrimitives.DrawGroundRing(guard.Position, ResolveGuardRingColor(stateName));
            CharacterMeshDrawer.DrawCharacterRig(context.Models, guardRig, guard.Position, guard.RotationDegrees, pose, DrawGuardFlashlight);
            GuardDetectionBadge3DRenderer.DrawAwarenessBadgeAndGauge(guard, context.ElapsedSeconds);
        }
    }

    public void Dispose() => _guardRig = null;

    private static void DrawGuardVisionCone(
        Guard guard,
        IReadOnlyList<RectangleBounds> visionBlockers,
        bool alarmActive,
        float elapsedSeconds)
    {
        bool isAlertCone = alarmActive || guard.CurrentState.StateName == "Alert";
        (Color fill, Color edge) = ResolveGuardConeColors(guard.CurrentState.StateName, alarmActive, elapsedSeconds);
        IReadOnlyList<Vector2> cone = VisionRaycaster.ComputeVisionCone(
            guard.Position,
            guard.RotationDegrees,
            visionBlockers,
            fovDegrees: GuardSensorHelper.ResolveHalfAngleDegrees(isAlertCone) * FullConeFovScale,
            maxDistance: GuardSensorHelper.ResolveVisionRange(isAlertCone),
            rayCount: Viewport3DConfig.VisionRayCount);

        VisionCone3DRenderer.DrawProjectedVisionPolygon(cone, groundY: VisionConeGroundY, sourceY: VisionConeSourceY, fill, edge);
    }

    private static (Color Fill, Color Edge) ResolveGuardConeColors(string stateName, bool alarmActive, float timeSeconds)
    {
        if (alarmActive || stateName == "Alert")
        {
            byte pulseAlpha = (byte)(AlertPulseBaseAlpha + (AlertPulseAmplitude * MathF.Sin(timeSeconds * AlertPulseSpeed)));
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
        Vector3 flashlightTip = handPosition + new Vector3(0f, FlashlightTipOffsetY, FlashlightTipOffsetZ);
        Vector3 bulbCenter = handPosition + new Vector3(0f, FlashlightBulbOffsetY, FlashlightBulbOffsetZ);
        Raylib.DrawCylinderEx(handPosition, flashlightTip, FlashlightStartRadius, FlashlightEndRadius, FlashlightCylinderSides, new Color(24, 28, 36, 255));
        Raylib.DrawSphere(bulbCenter, FlashlightBulbRadius, new Color(255, 240, 160, 235));
    }
}
