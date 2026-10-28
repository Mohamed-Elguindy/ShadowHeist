using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Stances;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Player entity (Ticket #4, #11 & #12 - Mahmoud).
/// Handles movement, stances, wall collisions, smooth aim, and footstep noise events.
/// </summary>
public sealed class Player
{
    private const float BaseSpeed = 160f;
    private const float TurnSpeed = 720f; // Degrees per second
    private const float WalkStepInterval = 0.40f;   // Seconds between walk footstep sounds
    private const float SprintStepInterval = 0.25f; // Seconds between sprint footstep sounds

    private float _stepTimer;

    public Vector2 Position { get; set; }
    public float RotationDegrees { get; private set; }
    public IPlayerStance CurrentStance { get; private set; } = new WalkStance();
    public float Radius => CollisionHelper.DefaultActorRadius;
    public float MoveSpeed => BaseSpeed * CurrentStance.SpeedMultiplier;

    public Player(Vector2 startPosition) => Position = startPosition;

    public void Update(PlayerInputCommand input, float deltaTime, IEnumerable<RectangleBounds> walls, GameEventBus eventBus)
    {
        // 1. Update stance (Walk, Sneak, Sprint)
        CurrentStance = CurrentStance.HandleInput(input);

        // 2. Move with WASD
        if (input.MoveDirection != Vector2.Zero)
        {
            Position += input.MoveDirection * MoveSpeed * deltaTime;
        }

        // 3. Prevent walking through walls
        Position = CollisionHelper.ResolveWallCollision(Position, Radius, walls);

        // 4. Aim towards mouse
        RotateTowards(input.AimWorldPosition, deltaTime);

        // 5. Emit footstep noise based on active stance
        UpdateFootstepNoise(input.MoveDirection, deltaTime, eventBus);
    }

    private void UpdateFootstepNoise(Vector2 moveDirection, float deltaTime, GameEventBus eventBus)
    {
        // Silent crouch or standing still produces zero footstep noise
        if (moveDirection == Vector2.Zero || CurrentStance.NoiseRadius <= 0f)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= deltaTime;
        if (_stepTimer > 0f) return;

        _stepTimer = WalkStepInterval;
        if (CurrentStance is SprintStance)
        {
            _stepTimer = SprintStepInterval;
        }

            eventBus.Publish(new NoiseEmittedEvent(
                Position,
                CurrentStance.NoiseRadius,
                $"PlayerFootstep:{CurrentStance.Name}"));
        }
    }

    private void RotateTowards(Vector2 targetPosition, float deltaTime)
    {
        Vector2 diff = targetPosition - Position;
        if (diff.LengthSquared() < 0.0001f) return;

        // Atan2 takes (Y, X) and returns radians; * (180 / PI) converts to degrees
        float targetAngle = MathF.Atan2(diff.Y, diff.X) * (180f / MathF.PI);

        // Shortest angle difference (-180 to +180)
        float deltaAngle = (targetAngle - RotationDegrees) % 360f;
        if (deltaAngle > 180f) deltaAngle -= 360f;
        if (deltaAngle < -180f) deltaAngle += 360f;

        // Clamp rotation step to smoothly reach target without overshooting
        float maxStep = TurnSpeed * deltaTime;
        RotationDegrees += Math.Clamp(deltaAngle, -maxStep, maxStep);
    }
}
