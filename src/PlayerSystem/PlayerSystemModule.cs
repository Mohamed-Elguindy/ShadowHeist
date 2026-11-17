using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Input;
using ShadowHeist.PlayerSystem.Rendering;
using ShadowHeist.PlayerSystem.Stances;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Owned by: Mahmoud (ShadowHeist.PlayerSystem)
/// Manages player input, stance FSM, movement, footstep noise events, and 3D character rendering.
/// </summary>
public sealed class PlayerSystemModule : IGameSystem, IDisposable
{
    private readonly IInputProvider _input;
    private readonly PlayerSoundRippleRenderer _rippleRenderer = new();
    private readonly SprintStamina _stamina = new();
    private Player _player = null!;
    private PlayerRenderer? _renderer;

    public PlayerSystemModule(IInputProvider? input = null)
    {
        _input = input ?? new KeyboardInputProvider();
    }

    public void Init(GameContext context)
    {
        _stamina.Reset();
        _player = new Player(context.PlayerPosition);
        _renderer ??= new PlayerRenderer();
        _renderer.Load(context.Models);
        _rippleRenderer.SubscribeEvents(context.EventBus);
        context.PlayerRotationDegrees = _player.RotationDegrees;
        context.IsPlayerSneaking = _player.CurrentStance is SneakStance;
    }

    public void Update(float deltaTime, GameContext context)
    {
        _rippleRenderer.Update(deltaTime);
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        PlayerInputCommand input = _input.ReadInput();
        if (context.AimWorldOverride is System.Numerics.Vector2 aimOverride)
        {
            input = input with { AimWorldPosition = aimOverride };
        }

        if (!_stamina.CanSprint)
        {
            input = input with { IsSprintHeld = false };
        }

        context.CurrentInput = input;
        _player.Update(input, deltaTime, context.Walls, context.EventBus);
        _stamina.Update(deltaTime, _player.CurrentStance is SprintStance && input.MoveDirection != System.Numerics.Vector2.Zero);
        _renderer?.UpdateAnimation(_player.Position, deltaTime);

        context.PlayerPosition = _player.Position;
        context.PlayerRotationDegrees = _player.RotationDegrees;
        context.IsPlayerSneaking = _player.CurrentStance is SneakStance;
    }

    public void Draw(GameContext context)
    {
        _rippleRenderer.Draw(context);
        _renderer?.Draw(_player, context);
    }

    public void DrawHud(GameContext context) =>
        SprintBarRenderer.Draw(_stamina.Ratio, _stamina.IsOnCooldown);

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
    }
}
