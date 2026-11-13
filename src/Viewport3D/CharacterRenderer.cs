using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders articulated 6-part cel-shaded 3D thief and guard characters with stride, sneak crouch, and held gear.
/// </summary>
public sealed class CharacterRenderer
{
    private const string CharDir = "assets/models/characters";
    private const string ThiefSkinPath = "assets/models/characters/skin_thief.png";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";
    private const float HipHeightUnits = 6f;
    private const float TorsoHeightUnits = 6f;
    private const float ShoulderOffsetXUnits = 2f;
    private const float ShoulderOffsetYUnits = 5.5f;

    private readonly Dictionary<string, ActorAnimState> _animStates = new();
    private CharacterRig? _thiefRig;
    private CharacterRig? _guardRig;
    private bool _isPlayerSneaking;

    public void Load(ToonModelLibrary library)
    {
        float scale = Viewport3DConfig.CharacterUnitScale;
        _thiefRig = library.LoadCharacterRig(CharDir, ThiefSkinPath, scale);
        _guardRig = library.LoadCharacterRig(CharDir, GuardSkinPath, scale);
    }

    public void Update(float deltaTime, GameContext context, IReadOnlyList<Guard> guards)
    {
        if (context.CurrentInput.IsSprintHeld) _isPlayerSneaking = false;
        else if (context.CurrentInput.IsSneakToggled) _isPlayerSneaking = !_isPlayerSneaking;

        UpdateActorState("player", context.PlayerPosition, deltaTime);
        foreach (Guard guard in guards) UpdateActorState(guard.Id, guard.Position, deltaTime);
    }

    public void Draw(GameContext context, IReadOnlyList<Guard> guards, float playerRotationDegrees, float timeSeconds)
    {
        if (_thiefRig is CharacterRig thief)
        {
            Color ring = context.CurrentInput.IsSprintHeld ? Color.Orange : new Color(56, 225, 255, 190);
            DrawShadowAndRing(context.PlayerPosition, ring);
            DrawCharacter(thief, context.PlayerPosition, playerRotationDegrees, GetAnim("player"), _isPlayerSneaking, false, timeSeconds);
        }

        if (_guardRig is CharacterRig guardRig)
        {
            Color ring = context.IsAlarmTriggered ? new Color(244, 63, 94, 210) : new Color(255, 208, 92, 175);
            foreach (Guard guard in guards)
            {
                DrawShadowAndRing(guard.Position, ring);
                DrawCharacter(guardRig, guard.Position, guard.RotationDegrees, GetAnim(guard.Id), false, true, timeSeconds);
            }
        }
    }

    private void UpdateActorState(string id, Vector2 position, float deltaTime)
    {
        if (!_animStates.TryGetValue(id, out ActorAnimState? state))
        {
            _animStates[id] = new ActorAnimState(position);
            return;
        }

        float dist = Vector2.Distance(position, state.PreviousPosition);
        state.NormalizedSpeed = MathF.Min(1.5f, deltaTime > 0.0001f ? (dist / deltaTime) / 150f : 0f);
        if (dist > 0.05f) state.StridePhase += dist * 0.14f;
        state.PreviousPosition = position;
    }

    private ActorAnimState GetAnim(string id) =>
        _animStates.TryGetValue(id, out ActorAnimState? state) ? state : new ActorAnimState(Vector2.Zero);

    private static void DrawShadowAndRing(Vector2 position, Color ringColor)
    {
        var shadowCenter = new Vector3(position.X, 0.7f, position.Y);
        Raylib.DrawCylinder(shadowCenter, Viewport3DConfig.ShadowRadius, Viewport3DConfig.ShadowRadius, 0.8f, 16, Viewport3DConfig.ShadowColor);
        Raylib.DrawCylinderWires(shadowCenter + new Vector3(0f, 0.6f, 0f), Viewport3DConfig.ShadowRadius + 2f, Viewport3DConfig.ShadowRadius + 2f, 0.4f, 20, ringColor);
    }

    private static void DrawCharacter(CharacterRig rig, Vector2 pos, float rotDeg, ActorAnimState state, bool isSneak, bool isGuard, float t)
    {
        float s = rig.Scale;
        float speed = MathF.Min(1f, state.NormalizedSpeed);
        float legSwing = MathF.Sin(state.StridePhase) * (isSneak ? 26f : 38f) * speed;
        float hipY = (isSneak ? 4.6f : HipHeightUnits) * s + MathF.Abs(MathF.Sin(state.StridePhase)) * 2.4f * speed + MathF.Sin(t * 2.5f) * 0.6f;
        float lean = isSneak ? 22f : 6f * speed;

        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, hipY, pos.Y);
        Rlgl.Rotatef(90f - rotDeg, 0f, 1f, 0f);

        DrawLimb(rig.LegLeft, new Vector3(s, 0f, 0f), legSwing, s);
        DrawLimb(rig.LegRight, new Vector3(-s, 0f, 0f), -legSwing, s);

        Rlgl.PushMatrix();
        Rlgl.Rotatef(lean, 1f, 0f, 0f);
        Raylib.DrawModel(rig.Body, Vector3.Zero, s, Color.White);
        DrawLimb(rig.Head, new Vector3(0f, TorsoHeightUnits * s, 0f), -lean * 0.6f, s);
        DrawLimb(rig.ArmLeft, new Vector3(ShoulderOffsetXUnits * s, ShoulderOffsetYUnits * s, 0f), -legSwing * 0.85f, s);
        DrawRightArm(rig, new Vector3(-ShoulderOffsetXUnits * s, ShoulderOffsetYUnits * s, 0f), isGuard ? -68f - legSwing * 0.2f : (isSneak ? -48f : -legSwing * 0.85f), isGuard, s);
        Rlgl.PopMatrix();
        Rlgl.PopMatrix();
    }

    private static void DrawLimb(Model part, Vector3 pivot, float pitchDegrees, float scale)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        Raylib.DrawModel(part, Vector3.Zero, scale, Color.White);
        Rlgl.PopMatrix();
    }

    private static void DrawRightArm(CharacterRig rig, Vector3 pivot, float pitchDegrees, bool isGuard, float scale)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        Raylib.DrawModel(rig.ArmRight, Vector3.Zero, scale, Color.White);

        Vector3 hand = new(-scale, -5.2f * scale, 0.8f * scale);
        if (isGuard)
        {
            Raylib.DrawCylinderEx(hand, hand + new Vector3(0f, -12f, 2f), 2.2f, 3.2f, 8, new Color(24, 28, 36, 255));
            Raylib.DrawSphere(hand + new Vector3(0f, -13f, 2.2f), 2.8f, new Color(255, 240, 160, 235));
        }
        else
        {
            Raylib.DrawCube(hand + new Vector3(0f, -4f, 4f), 2.5f, 3.5f, 11f, new Color(22, 26, 34, 255));
        }

        Rlgl.PopMatrix();
    }

    private sealed class ActorAnimState(Vector2 initialPos)
    {
        public Vector2 PreviousPosition { get; set; } = initialPos;
        public float StridePhase { get; set; }
        public float NormalizedSpeed { get; set; }
    }
}
