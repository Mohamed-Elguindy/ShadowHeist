using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders outlined 6-part stylized 3D thief and guard characters with ground rings, tools, and state badges.
/// </summary>
public sealed class CharacterRenderer : ISceneRenderPass
{
    private const string CharacterAssetDir = "assets/models/characters";
    private const string ThiefSkinPath = "assets/models/characters/skin_thief.png";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";
    private const float TorsoHeightUnits = 6f;
    private const float ShoulderOffsetX = 2.0f;
    private const float ShoulderOffsetY = 5.5f;
    private const float LimbOutlineWidth = 0.21f;

    private readonly CharacterPoseAnimator _animator = new();
    private ToonModelLibrary? _library;
    private CharacterRig? _thiefRig;
    private CharacterRig? _guardRig;

    public void Load(ToonModelLibrary library)
    {
        _library = library;
        float unitScale = Viewport3DConfig.CharacterUnitScale;
        _thiefRig = library.LoadCharacterRig(CharacterAssetDir, ThiefSkinPath, unitScale);
        _guardRig = library.LoadCharacterRig(CharacterAssetDir, GuardSkinPath, unitScale);
    }

    public void Update(float deltaTime, GameContext context, IReadOnlyList<Guard> guards) =>
        _animator.Update(deltaTime, context, guards);

    public void Draw(in Scene3DRenderFrame frame)
    {
        if (_library is null)
        {
            return;
        }

        DrawPlayerCharacter(_library, frame);
        DrawGuardCharacters(_library, frame);
    }

    private void DrawPlayerCharacter(ToonModelLibrary library, in Scene3DRenderFrame frame)
    {
        if (_thiefRig is not CharacterRig thiefRig)
        {
            return;
        }

        Color ringColor = frame.Context.CurrentInput.IsSprintHeld
            ? Color.Orange
            : new Color(56, 225, 255, 190);
        CharacterPose pose = _animator.ComputePose("player", thiefRig.Scale, frame.Context.IsPlayerSneaking, false, frame.ElapsedSeconds);

        DrawGroundRing(frame.Context.PlayerPosition, ringColor);
        DrawCharacterRig(library, thiefRig, frame.Context.PlayerPosition, frame.PlayerRotationDegrees, pose, isGuard: false);
    }

    private void DrawGuardCharacters(ToonModelLibrary library, in Scene3DRenderFrame frame)
    {
        if (_guardRig is not CharacterRig guardRig)
        {
            return;
        }

        foreach (Guard guard in frame.Guards)
        {
            string stateName = guard.CurrentState.StateName;
            CharacterPose pose = _animator.ComputePose(guard.Id, guardRig.Scale, false, true, frame.ElapsedSeconds);

            DrawGroundRing(guard.Position, ResolveGuardRingColor(stateName));
            DrawCharacterRig(library, guardRig, guard.Position, guard.RotationDegrees, pose, isGuard: true);
            DrawAwarenessBadge(guard.Position, stateName, frame.ElapsedSeconds);
        }
    }

    private static Color ResolveGuardRingColor(string stateName) => stateName switch
    {
        "Alert" => new Color(244, 63, 94, 225),
        "Suspicious" => new Color(250, 204, 21, 210),
        _ => new Color(255, 208, 92, 165)
    };

    private static void DrawGroundRing(Vector2 position, Color ringColor)
    {
        Raylib3DPrimitives.DrawGroundShadow(position, Viewport3DConfig.ShadowRadius);
        var ringCenter = new Vector3(position.X, 1.3f, position.Y);
        float ringRadius = Viewport3DConfig.ShadowRadius + 2f;
        Raylib.DrawCylinderWires(ringCenter, ringRadius, ringRadius, 0.4f, 20, ringColor);
    }

    private static void DrawCharacterRig(
        ToonModelLibrary library,
        CharacterRig rig,
        Vector2 position,
        float rotationDegrees,
        CharacterPose pose,
        bool isGuard)
    {
        float unitScale = rig.Scale;

        Rlgl.PushMatrix();
        Rlgl.Translatef(position.X, pose.HipHeight, position.Y);
        Rlgl.Rotatef(90f - rotationDegrees, 0f, 1f, 0f);

        DrawLimbPart(library, rig.LegLeft, new Vector3(unitScale, 0f, 0f), pose.LegSwingDegrees, unitScale);
        DrawLimbPart(library, rig.LegRight, new Vector3(-unitScale, 0f, 0f), -pose.LegSwingDegrees, unitScale);

        Rlgl.PushMatrix();
        Rlgl.Rotatef(pose.TorsoLeanDegrees, 1f, 0f, 0f);
        library.DrawOutlined(rig.Body, Vector3.Zero, 0f, unitScale, Color.White, LimbOutlineWidth);
        DrawLimbPart(library, rig.Head, new Vector3(0f, TorsoHeightUnits * unitScale, 0f), -pose.TorsoLeanDegrees * 0.6f, unitScale);
        DrawLimbPart(library, rig.ArmLeft, new Vector3(ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), -pose.LegSwingDegrees * 0.85f, unitScale);
        DrawRightArmWithTool(library, rig, new Vector3(-ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), pose.RightArmPitchDegrees, isGuard, unitScale);
        Rlgl.PopMatrix();

        Rlgl.PopMatrix();
    }

    private static void DrawLimbPart(ToonModelLibrary library, Model partModel, Vector3 pivot, float pitchDegrees, float scale)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        library.DrawOutlined(partModel, Vector3.Zero, 0f, scale, Color.White, LimbOutlineWidth);
        Rlgl.PopMatrix();
    }

    private static void DrawRightArmWithTool(
        ToonModelLibrary library,
        CharacterRig rig,
        Vector3 pivot,
        float pitchDegrees,
        bool isGuard,
        float scale)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        library.DrawOutlined(rig.ArmRight, Vector3.Zero, 0f, scale, Color.White, LimbOutlineWidth);

        Vector3 handPosition = new(-scale, -5.2f * scale, 0.8f * scale);
        if (isGuard) DrawGuardFlashlight(handPosition);
        else DrawPlayerPistol(handPosition);

        Rlgl.PopMatrix();
    }

    private static void DrawGuardFlashlight(Vector3 handPosition)
    {
        Raylib.DrawCylinderEx(handPosition, handPosition + new Vector3(0f, -12f, 2f), 2.2f, 3.2f, 8, new Color(24, 28, 36, 255));
        Raylib.DrawSphere(handPosition + new Vector3(0f, -13f, 2.2f), 2.8f, new Color(255, 240, 160, 235));
    }

    private static void DrawPlayerPistol(Vector3 handPosition) =>
        Raylib.DrawCube(handPosition + new Vector3(0f, -4f, 4f), 2.5f, 3.5f, 11f, new Color(22, 26, 34, 255));

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
