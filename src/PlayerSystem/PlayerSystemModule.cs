using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Input;
using ShadowHeist.PlayerSystem.Inventory;
using ShadowHeist.PlayerSystem.Rendering;
using ShadowHeist.PlayerSystem.Stances;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Owned by: Mahmoud (ShadowHeist.PlayerSystem)
/// Manages player input, stance FSM, inventory weight slowdown, movement, footstep noise events, and 3D character rendering.
/// </summary>
public sealed class PlayerSystemModule : IGameSystem, IDisposable
{
    private const string LootCrateNoisePrefix = "LootCrate:";
    private const string FallbackLootIdPrefix = "loot_";

    private readonly IInputProvider _input;
    private readonly PlayerSoundRippleRenderer _rippleRenderer = new();
    private readonly SprintStamina _stamina = new();
    private readonly PlayerInventory _inventory = new();
    private readonly Queue<string> _pendingLootCrateIds = new();
    private Player _player = null!;
    private PlayerRenderer? _renderer;
    private int _syncedStolenCash;

    public PlayerInventory Inventory => _inventory;

    public PlayerSystemModule(IInputProvider? input = null)
    {
        _input = input ?? new KeyboardInputProvider();
    }

    public void Init(GameContext context)
    {
        _stamina.Reset();
        _inventory.Clear();
        _pendingLootCrateIds.Clear();
        _syncedStolenCash = 0;

        _player = new Player(context.PlayerPosition);
        _renderer ??= new PlayerRenderer();
        _renderer.Load(context.Models);
        _rippleRenderer.SubscribeEvents(context.EventBus);
        context.EventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);

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

        SyncInventoryWithContextCash(context.StolenCash);

        PlayerInputCommand input = ReadPlayerInput(context);
        context.CurrentInput = input;
        _player.Update(input, deltaTime, context.Walls, context.EventBus, _inventory.MovementSpeedMultiplier);
        _stamina.Update(deltaTime, _player.CurrentStance is SprintStance && input.MoveDirection != System.Numerics.Vector2.Zero);
        _renderer?.UpdateAnimation(_player.Position, deltaTime);

        context.PlayerPosition = _player.Position;
        context.PlayerRotationDegrees = _player.RotationDegrees;
        context.IsPlayerSneaking = _player.CurrentStance is SneakStance;
    }

    public void Draw(GameContext context)
    {
        _rippleRenderer.Draw(context);
        PlayerAim3DRenderer.Draw(context);
        _renderer?.Draw(_player, context);
    }

    public void DrawHud(GameContext context)
    {
        if (context.StateManager.CurrentState != GameFlowState.Playing || context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        SprintBarRenderer.Draw(_stamina.Ratio, _stamina.IsOnCooldown);
        InventoryHudRenderer.Draw(_inventory.Count, _inventory.TotalWeightKg, _inventory.MovementSpeedMultiplier);
    }

    public void Dispose()
    {
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

    private void OnNoiseEmitted(NoiseEmittedEvent evt)
    {
        if (!evt.SourceName.StartsWith(LootCrateNoisePrefix, StringComparison.Ordinal))
        {
            return;
        }

        string crateId = evt.SourceName[LootCrateNoisePrefix.Length..];
        if (!string.IsNullOrWhiteSpace(crateId))
        {
            _pendingLootCrateIds.Enqueue(crateId);
        }
    }

    private void SyncInventoryWithContextCash(int currentStolenCash)
    {
        if (currentStolenCash <= _syncedStolenCash)
        {
            return;
        }

        int gainedCash = currentStolenCash - _syncedStolenCash;
        if (_pendingLootCrateIds.Count > 1)
        {
            int splitCount = _pendingLootCrateIds.Count;
            int cashPerCrate = gainedCash / splitCount;
            int remainder = gainedCash % splitCount;
            while (_pendingLootCrateIds.Count > 0)
            {
                int share = cashPerCrate + (_pendingLootCrateIds.Count == 1 ? remainder : 0);
                _inventory.TryAddCashLoot(_pendingLootCrateIds.Dequeue(), share);
            }
        }
        else
        {
            string lootId = _pendingLootCrateIds.Count == 1
                ? _pendingLootCrateIds.Dequeue()
                : $"{FallbackLootIdPrefix}{_inventory.Count + 1}";
            _inventory.TryAddCashLoot(lootId, gainedCash);
        }

        _syncedStolenCash = currentStolenCash;
    }
}
