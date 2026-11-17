using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Stances;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Player entity handling movement, stances, wall collisions, instant aim, and footstep noise events.
/// </summary>
public sealed class Player
{
    private const float BaseSpeed = 160f;
    private const float MinAimDistance = 16f;
    private const float WalkStepIntervalSeconds = 0.40f;

    private float _stepTimer;

    public Vector2 Position { get; set; }
    public float RotationDegrees { get; private set; }
    public IPlayerStance CurrentStance { get; private set; } = new WalkStance();
    public float Radius => CollisionHelper.DefaultActorRadius;
    public float MoveSpeed => BaseSpeed * CurrentStance.SpeedMultiplier;

    public Player(Vector2 startPosition) => Position = startPosition;

    public void Update(PlayerInputCommand input, float deltaTime, IEnumerable<RectangleBounds> walls, GameEventBus eventBus)
    {
        CurrentStance = CurrentStance.HandleInput(input);
        MoveWithCollision(input.MoveDirection, deltaTime, walls);
        FaceAimPosition(input.AimWorldPosition);
        UpdateFootstepNoise(input.MoveDirection, deltaTime, eventBus);
    }

    private void MoveWithCollision(Vector2 moveDirection, float deltaTime, IEnumerable<RectangleBounds> walls)
    {
        if (moveDirection != Vector2.Zero)
        {
            Position += moveDirection * MoveSpeed * deltaTime;
        }

        Position = CollisionHelper.ResolveWallCollision(Position, Radius, walls);
    }

    private void FaceAimPosition(Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - Position;
        if (delta.LengthSquared() < MinAimDistance * MinAimDistance)
        {
            return;
        }

        RotationDegrees = MathF.Atan2(delta.Y, delta.X) * HeistConstants.RadToDeg;
    }

    private void UpdateFootstepNoise(Vector2 moveDirection, float deltaTime, GameEventBus eventBus)
    {
        bool isSilentOrStill = moveDirection == Vector2.Zero || CurrentStance.NoiseRadius <= 0f;
        if (isSilentOrStill)
        {
            _stepTimer = Math.Max(0f, _stepTimer - deltaTime);
            return;
        }

        _stepTimer -= deltaTime;
        if (_stepTimer > 0f)
        {
            return;
        }

        _stepTimer = WalkStepIntervalSeconds / CurrentStance.SpeedMultiplier;
        eventBus.Publish(new NoiseEmittedEvent(
            Position,
            CurrentStance.NoiseRadius,
            $"PlayerFootstep:{CurrentStance.Name}"));
    }
}
