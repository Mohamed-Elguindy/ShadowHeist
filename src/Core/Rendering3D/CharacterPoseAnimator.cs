using System.Numerics;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Computed per-frame articulation angles and vertical hip placement for an actor rig.
/// </summary>
public readonly record struct CharacterPose(
    float HipHeight,
    float LegSwingDegrees,
    float TorsoLeanDegrees,
    float RightArmPitchDegrees);

/// <summary>
/// Tracks actor locomotion displacement and computes procedural stride, crouch, and arm poses.
/// </summary>
public sealed class CharacterPoseAnimator
{
    private const float HipHeightUnits = 6f;
    private const float SneakHipHeightUnits = 4.6f;
    private const float TeleportSnapDistance = 120f;
    private const float StrideCycleScale = 0.14f;
    private const float ReferenceMoveSpeed = 150f;
    private const float MaxNormalizedSpeed = 1.5f;
    private const float MinMoveDistanceThreshold = 0.05f;
    private const float MinDeltaTimeEpsilon = 0.0001f;
    private const float SneakLegSwingDegrees = 26f;
    private const float WalkLegSwingDegrees = 38f;
    private const float StrideBobAmplitude = 2.4f;
    private const float IdleBreathFrequency = 2.5f;
    private const float IdleBreathAmplitude = 0.6f;
    private const float SneakTorsoLeanDegrees = 22f;
    private const float WalkTorsoLeanFactor = 6f;
    private const float GuardArmPitchDegrees = -68f;
    private const float GuardArmSwingFactor = 0.2f;
    private const float SneakArmPitchDegrees = -48f;
    private const float WalkArmSwingFactor = 0.85f;

    private readonly Dictionary<string, ActorAnimState> _animStates = new();

    public void Reset() => _animStates.Clear();

    public void UpdateActor(string actorId, Vector2 currentPosition, float deltaTime)
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

        state.NormalizedSpeed = deltaTime > MinDeltaTimeEpsilon
            ? MathF.Min(MaxNormalizedSpeed, (movedDistance / deltaTime) / ReferenceMoveSpeed)
            : 0f;

        if (movedDistance > MinMoveDistanceThreshold)
        {
            state.StridePhase += movedDistance * StrideCycleScale;
        }

        state.PreviousPosition = currentPosition;
    }

    public CharacterPose ComputePose(
        string actorId,
        float unitScale,
        bool isSneaking,
        bool isGuard,
        float elapsedSeconds)
    {
        ActorAnimState state = GetOrDefaultState(actorId);
        float clampedSpeed = MathF.Min(1f, state.NormalizedSpeed);
        float legAmplitude = isSneaking ? SneakLegSwingDegrees : WalkLegSwingDegrees;
        float legSwingDegrees = MathF.Sin(state.StridePhase) * legAmplitude * clampedSpeed;
        float strideBob = MathF.Abs(MathF.Sin(state.StridePhase)) * StrideBobAmplitude * clampedSpeed;
        float idleBreath = MathF.Sin(elapsedSeconds * IdleBreathFrequency) * IdleBreathAmplitude;
        float baseHipUnits = isSneaking ? SneakHipHeightUnits : HipHeightUnits;
        float hipHeight = (baseHipUnits * unitScale) + strideBob + idleBreath;
        float torsoLeanDegrees = isSneaking ? SneakTorsoLeanDegrees : WalkTorsoLeanFactor * clampedSpeed;
        float rightArmPitch = ResolveRightArmPitch(isGuard, isSneaking, legSwingDegrees);

        return new CharacterPose(hipHeight, legSwingDegrees, torsoLeanDegrees, rightArmPitch);
    }

    private ActorAnimState GetOrDefaultState(string actorId) =>
        _animStates.TryGetValue(actorId, out ActorAnimState? state) ? state : new ActorAnimState(Vector2.Zero);

    private static float ResolveRightArmPitch(bool isGuard, bool isSneaking, float legSwingDegrees)
    {
        if (isGuard)
        {
            return GuardArmPitchDegrees - (legSwingDegrees * GuardArmSwingFactor);
        }

        return isSneaking ? SneakArmPitchDegrees : -legSwingDegrees * WalkArmSwingFactor;
    }

    private sealed class ActorAnimState(Vector2 initialPosition)
    {
        public Vector2 PreviousPosition { get; set; } = initialPosition;
        public float StridePhase { get; set; }
        public float NormalizedSpeed { get; set; }
    }
}
