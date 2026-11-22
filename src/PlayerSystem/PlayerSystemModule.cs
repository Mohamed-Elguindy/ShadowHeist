using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Gadgets;
using ShadowHeist.PlayerSystem.Input;
using ShadowHeist.PlayerSystem.Rendering;
using ShadowHeist.PlayerSystem.Stances;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Owned by: Mahmoud (ShadowHeist.PlayerSystem)
/// Manages player input, stance FSM, gadgets, movement, footstep noise events, and 3D character rendering.
/// </summary>
public sealed class PlayerSystemModule : IGameSystem, IDisposable
{
    private readonly IInputProvider _input;
    private readonly PlayerSoundRippleRenderer _rippleRenderer = new();
    private readonly Gadget3DVfxRenderer _gadgetVfx = new();
    private readonly GadgetHudRenderer _gadgetHud = new();
    private readonly SprintStamina _stamina = new();
    private readonly PlayerGadgetLoadout _gadgets = new();
    private Player _player = null!;
    private PlayerRenderer? _renderer;

    public PlayerGadgetLoadout Gadgets => _gadgets;

    public PlayerSystemModule(IInputProvider? input = null)
    {
        _input = input ?? new KeyboardInputProvider();
    }

    public void Init(GameContext context)
    {
        _stamina.Reset();
        _gadgets.Reset();
        _gadgetVfx.Reset();

        _player = new Player(context.PlayerPosition);
        _renderer ??= new PlayerRenderer();
        _renderer.Load(context.Models);
        _gadgetHud.Load(context.Models);
        _rippleRenderer.SubscribeEvents(context.EventBus);
        context.EventBus.Subscribe<PowerStateChangedEvent>(_gadgets.OnPowerStateChanged);

        context.PlayerRotationDegrees = _player.RotationDegrees;
        context.IsPlayerSneaking = _player.CurrentStance is SneakStance;
    }

    public void Update(float deltaTime, GameContext context)
    {
        _rippleRenderer.Update(deltaTime);
        _gadgetVfx.Update(deltaTime);
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        _gadgets.HandleKeyboardSelection();
        _gadgets.Update(deltaTime, context.EventBus);

        PlayerInputCommand input = ReadPlayerInput(context);
        context.CurrentInput = input;
        _player.Update(input, deltaTime, context.Walls, context.EventBus);
        HandleGadgetUse(input, context);

        _stamina.Update(deltaTime, _player.CurrentStance is SprintStance && input.MoveDirection != System.Numerics.Vector2.Zero);
        _renderer?.UpdateAnimation(_player.Position, deltaTime);

        context.PlayerPosition = _player.Position;
        context.PlayerRotationDegrees = _player.RotationDegrees;
        context.IsPlayerSneaking = _player.CurrentStance is SneakStance;
    }

    public void Draw(GameContext context)
    {
        _rippleRenderer.Draw(context);
        _gadgetVfx.Draw(_gadgets.Emp, context.ElapsedSeconds);
        _renderer?.Draw(_player, context);
    }

    public void DrawHud(GameContext context)
    {
        if (context.StateManager.CurrentState is GameFlowState.MainMenu or GameFlowState.LevelSelect)
        {
            return;
        }

        SprintBarRenderer.Draw(_stamina.Ratio, _stamina.IsOnCooldown);
        if (context.Outcome == HeistOutcome.Playing)
        {
            _gadgetHud.Draw(_gadgets);
        }
    }

    public void Dispose()
    {
        _gadgetHud.ClearCachedReferences();
        _renderer?.Dispose();
        _renderer = null;
    }

    private PlayerInputCommand ReadPlayerInput(GameContext context)
    {
        PlayerInputCommand input = _input.ReadInput();
        if (context.AimWorldOverride is System.Numerics.Vector2 aimOverride)
        {
            input = input with { AimWorldPosition = aimOverride };
        }

        if (!_stamina.CanSprint)
        {
            input = input with { IsSprintHeld = false };
        }

        return input;
    }

    private void HandleGadgetUse(PlayerInputCommand input, GameContext context)
    {
        if (!input.IsUseGadgetPressed)
        {
            return;
        }

        IGadget? usedGadget = _gadgets.TryUseActiveGadget(
            _player.Position,
            input.AimWorldPosition,
            context.Walls,
            context.EventBus);

        if (usedGadget is CoinGadget coin)
        {
            _gadgetVfx.RecordCoinThrow(coin.LastThrowOrigin, coin.LastLandingPoint);
        }
        else if (usedGadget is EmpGadget emp)
        {
            _gadgetVfx.RecordEmpThrow(emp.LastThrowOrigin, emp.LastLandingPoint, emp.EmpRadius);
        }
    }
}
