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

    private readonly Dictionary<string, ActorAnimState> _animStates = new();

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

        state.NormalizedSpeed = deltaTime > 0.0001f
            ? MathF.Min(1.5f, (movedDistance / deltaTime) / ReferenceMoveSpeed)
            : 0f;

        if (movedDistance > 0.05f)
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
        float legSwingDegrees = MathF.Sin(state.StridePhase) * (isSneaking ? 26f : 38f) * clampedSpeed;
        float strideBob = MathF.Abs(MathF.Sin(state.StridePhase)) * 2.4f * clampedSpeed;
        float idleBreath = MathF.Sin(elapsedSeconds * 2.5f) * 0.6f;
        float hipHeight = ((isSneaking ? SneakHipHeightUnits : HipHeightUnits) * unitScale) + strideBob + idleBreath;
        float torsoLeanDegrees = isSneaking ? 22f : 6f * clampedSpeed;
        float rightArmPitch = ResolveRightArmPitch(isGuard, isSneaking, legSwingDegrees);

        return new CharacterPose(hipHeight, legSwingDegrees, torsoLeanDegrees, rightArmPitch);
    }

    private ActorAnimState GetOrDefaultState(string actorId) =>
        _animStates.TryGetValue(actorId, out ActorAnimState? state) ? state : new ActorAnimState(Vector2.Zero);

    private static float ResolveRightArmPitch(bool isGuard, bool isSneaking, float legSwingDegrees)
    {
        if (isGuard)
        {
            return -68f - (legSwingDegrees * 0.2f);
        }

        return isSneaking ? -48f : -legSwingDegrees * 0.85f;
    }

    private sealed class ActorAnimState(Vector2 initialPosition)
    {
        public Vector2 PreviousPosition { get; set; } = initialPosition;
        public float StridePhase { get; set; }
        public float NormalizedSpeed { get; set; }
    }
}
