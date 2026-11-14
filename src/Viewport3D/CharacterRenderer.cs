using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders outlined 6-part stylized 3D thief and guard characters with stride animation, crouch posture, and state badges.
/// </summary>
public sealed class CharacterRenderer : ISceneRenderPass
{
    private const string CharacterAssetDir = "assets/models/characters";
    private const string ThiefSkinPath = "assets/models/characters/skin_thief.png";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";
    private const float HipHeightUnits = 6f;
    private const float SneakHipHeightUnits = 4.6f;
    private const float TorsoHeightUnits = 6f;
    private const float ShoulderOffsetX = 2.0f;
    private const float ShoulderOffsetY = 5.5f;
    private const float LimbOutlineWidth = 0.21f;
    private const float TeleportSnapDistance = 120f;

    private readonly Dictionary<string, ActorAnimState> _animStates = new();
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

    public void Update(float deltaTime, GameContext context, IReadOnlyList<Guard> guards)
    {
        UpdateActorAnimation("player", context.PlayerPosition, deltaTime);
        foreach (Guard guard in guards)
        {
            UpdateActorAnimation(guard.Id, guard.Position, deltaTime);
        }
    }

    public void Draw(in Scene3DRenderFrame frame)
    {
        if (_library is null)
        {
            return;
        }

        if (_thiefRig is CharacterRig thiefRig)
        {
            Color ringColor = frame.Context.CurrentInput.IsSprintHeld
                ? Color.Orange
                : new Color(56, 225, 255, 190);

            DrawGroundRing(frame.Context.PlayerPosition, ringColor);
            DrawCharacterModel(
                _library,
                thiefRig,
                frame.Context.PlayerPosition,
                frame.PlayerRotationDegrees,
                GetAnimState("player"),
                frame.Context.IsPlayerSneaking,
                isGuard: false,
                frame.ElapsedSeconds);
        }

        if (_guardRig is CharacterRig guardRig)
        {
            foreach (Guard guard in frame.Guards)
            {
                string stateName = guard.CurrentState.StateName;
                DrawGroundRing(guard.Position, ResolveGuardRingColor(stateName));
                DrawCharacterModel(
                    _library,
                    guardRig,
                    guard.Position,
                    guard.RotationDegrees,
                    GetAnimState(guard.Id),
                    isSneaking: false,
                    isGuard: true,
                    frame.ElapsedSeconds);
                DrawAwarenessBadge(guard.Position, stateName, frame.ElapsedSeconds);
            }
        }
    }

    private void UpdateActorAnimation(string actorId, Vector2 currentPosition, float deltaTime)
    {
        if (!_animStates.TryGetValue(actorId, out ActorAnimState? state))
        {
            _animStates[actorId] = new ActorAnimState(currentPosition);
            return;
        }

        float movedDistance = Vector2.Distance(currentPosition, state.PreviousPosition);
        if (movedDistance > TeleportSnapDistance)
        {
            state.PreviousPosition = currentPosition;
            state.NormalizedSpeed = 0f;
            return;
        }

        state.NormalizedSpeed = deltaTime > 0.0001f
            ? MathF.Min(1.5f, (movedDistance / deltaTime) / 150f)
            : 0f;

        if (movedDistance > 0.05f)
        {
            state.StridePhase += movedDistance * 0.14f;
        }

        state.PreviousPosition = currentPosition;
    }

    private ActorAnimState GetAnimState(string actorId) =>
        _animStates.TryGetValue(actorId, out ActorAnimState? state) ? state : new ActorAnimState(Vector2.Zero);

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

    private static void DrawAwarenessBadge(Vector2 position, string stateName, float timeSeconds)
    {
        bool isAlert = stateName == "Alert";
        bool isSuspicious = stateName == "Suspicious";
        float bobY = 84f + (MathF.Sin(timeSeconds * (isAlert ? 10f : 3f)) * 2.5f);
        var badgeBase = new Vector3(position.X, bobY, position.Y);

        Color badgeColor = isAlert
            ? new Color(244, 63, 94, 245)
            : isSuspicious
                ? new Color(250, 204, 21, 240)
                : new Color(255, 214, 78, 175);

        float coneHeight = isAlert ? 12f : 6f;
        float coneRadius = isAlert ? 4.5f : 3.2f;
        Raylib.DrawCylinderEx(badgeBase, badgeBase + new Vector3(0f, coneHeight, 0f), 0.5f, coneRadius, 6, badgeColor);

        if (isAlert || isSuspicious)
        {
            Raylib.DrawSphere(badgeBase + new Vector3(0f, -4f, 0f), 2.5f, badgeColor);
        }
    }

    private static void DrawCharacterModel(
        ToonModelLibrary library,
        CharacterRig rig,
        Vector2 position,
        float rotationDegrees,
        ActorAnimState animState,
        bool isSneaking,
        bool isGuard,
        float timeSeconds)
    {
        float unitScale = rig.Scale;
        float clampedSpeed = MathF.Min(1f, animState.NormalizedSpeed);
        float legSwingDegrees = MathF.Sin(animState.StridePhase) * (isSneaking ? 26f : 38f) * clampedSpeed;
        float strideBob = MathF.Abs(MathF.Sin(animState.StridePhase)) * 2.4f * clampedSpeed;
        float idleBreath = MathF.Sin(timeSeconds * 2.5f) * 0.6f;
        float hipHeight = ((isSneaking ? SneakHipHeightUnits : HipHeightUnits) * unitScale) + strideBob + idleBreath;
        float torsoLeanDegrees = isSneaking ? 22f : 6f * clampedSpeed;
        float rightArmPitch = ResolveRightArmPitch(isGuard, isSneaking, legSwingDegrees);

        Rlgl.PushMatrix();
        Rlgl.Translatef(position.X, hipHeight, position.Y);
        Rlgl.Rotatef(90f - rotationDegrees, 0f, 1f, 0f);

        DrawLimbPart(library, rig.LegLeft, new Vector3(unitScale, 0f, 0f), legSwingDegrees, unitScale);
        DrawLimbPart(library, rig.LegRight, new Vector3(-unitScale, 0f, 0f), -legSwingDegrees, unitScale);

        Rlgl.PushMatrix();
        Rlgl.Rotatef(torsoLeanDegrees, 1f, 0f, 0f);
        library.DrawOutlined(rig.Body, Vector3.Zero, 0f, unitScale, Color.White, LimbOutlineWidth);
        DrawLimbPart(library, rig.Head, new Vector3(0f, TorsoHeightUnits * unitScale, 0f), -torsoLeanDegrees * 0.6f, unitScale);
        DrawLimbPart(library, rig.ArmLeft, new Vector3(ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), -legSwingDegrees * 0.85f, unitScale);
        DrawRightArmWithTool(library, rig, new Vector3(-ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), rightArmPitch, isGuard, unitScale);
        Rlgl.PopMatrix();

        Rlgl.PopMatrix();
    }

    private static float ResolveRightArmPitch(bool isGuard, bool isSneaking, float legSwingDegrees)
    {
        if (isGuard)
        {
            return -68f - (legSwingDegrees * 0.2f);
        }

        return isSneaking ? -48f : -legSwingDegrees * 0.85f;
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
        if (isGuard)
        {
            Raylib.DrawCylinderEx(handPosition, handPosition + new Vector3(0f, -12f, 2f), 2.2f, 3.2f, 8, new Color(24, 28, 36, 255));
            Raylib.DrawSphere(handPosition + new Vector3(0f, -13f, 2.2f), 2.8f, new Color(255, 240, 160, 235));
        }
        else
        {
            Raylib.DrawCube(handPosition + new Vector3(0f, -4f, 4f), 2.5f, 3.5f, 11f, new Color(22, 26, 34, 255));
        }

        Rlgl.PopMatrix();
    }

    private sealed class ActorAnimState(Vector2 initialPosition)
    {
        public Vector2 PreviousPosition { get; set; } = initialPosition;
        public float StridePhase { get; set; }
        public float NormalizedSpeed { get; set; }
    }
}
