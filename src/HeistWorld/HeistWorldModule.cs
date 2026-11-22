using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.Rendering;
using ShadowHeist.HeistWorld.UI;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.HeistWorld;

/// <summary>
/// Owned by: Guindy (ShadowHeist.HeistWorld)
/// Builds mission interactables, handles hold interactions and escape win conditions, and draws 3D props and HUD overlays.
/// </summary>
public sealed class HeistWorldModule : IGameSystem, IDisposable
{
    private const string DefaultFuseBoxId = "fusebox_lobby";
    private const string DefaultFuseBoxTargetGroupId = "lobby_lights";
    private const string DefaultLaserSwitchId = "laser_switch_hall";
    private const string DefaultLaserTargetGroupId = "laser_hall";
    private static readonly Vector2 DefaultFuseBoxPosition = new(290f, 288f);
    private static readonly Vector2 DefaultLaserSwitchPosition = new(284f, 444f);

    private readonly InteractionController _interaction = new();
    private readonly LootSparkRenderer _lootSparks = new();
    private readonly WorldLightingModule? _world;
    private readonly List<IInteractable> _worldObjects = new();
    private HeistWorldRenderer? _renderer;
    private SoundMeter? _soundMeter;
    private HudRenderer? _hud;
    private RectangleBounds _getawayZone = HeistConstants.DefaultGetawayZone;

    public HeistWorldModule(WorldLightingModule? world = null)
    {
        _world = world;
    }

    public float SoundMeterLevel => _soundMeter?.Level ?? 0f;
    public RectangleBounds GetawayZone => _getawayZone;

    public bool IsExtractionReady(GameContext context) =>
        context.StolenCash >= HeistConstants.TargetCashGoal;

    public void Init(GameContext context)
    {
        _interaction.Reset();
        _lootSparks.Reset();
        _soundMeter = new SoundMeter(context.EventBus);

        BuildWorldObjects(context);
        _hud = new HudRenderer(_soundMeter, _getawayZone);
        _renderer ??= new HeistWorldRenderer();
        _renderer.Reset();
        _renderer.Load(context.Models);
    }

    public void Update(float deltaTime, GameContext context)
    {
        _soundMeter?.Update(deltaTime);
        _renderer?.Update(deltaTime, context);
        _lootSparks.Update(deltaTime, context);
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        _interaction.HandlePlayerInteraction(deltaTime, context);

        if (context.StolenCash >= HeistConstants.TargetCashGoal && IsInsideZone(context.PlayerPosition, _getawayZone))
        {
            context.Outcome = HeistOutcome.Escaped;
        }
    }

    public void Draw(GameContext context)
    {
        _renderer?.Draw(context);
        _lootSparks.Draw();
        GetawayExtraction3DRenderer.Draw(_getawayZone, context);
    }

    public void DrawHud(GameContext context)
    {
        if (context.StateManager.CurrentState == GameFlowState.Playing && context.Outcome == HeistOutcome.Playing)
        {
            _lootSparks.DrawHudPopups(context.Camera.Camera);
        }

        _hud?.DrawHudOverlay(context);
    }

    public void Dispose()
    {
        _renderer?.ClearCachedReferences();
        _renderer = null;
    }

    private void BuildWorldObjects(GameContext context)
    {
        _worldObjects.Clear();
        _getawayZone = _world?.CurrentMap?.GetawayZone ?? HeistConstants.DefaultGetawayZone;

        PopulateFromMap(_world?.CurrentMap?.Interactables);
        EnsureRequiredFallbackObjects();
        RegisterWorldObjects(context);
    }

    private void PopulateFromMap(IReadOnlyList<MapInteractableDto>? items)
    {
        if (items is not { Count: > 0 })
        {
            return;
        }

        foreach (MapInteractableDto dto in items)
        {
            IInteractable? created = CreateInteractable(dto);
            if (created is not null)
            {
                _worldObjects.Add(created);
            }
        }
    }

    private void EnsureRequiredFallbackObjects()
    {
        EnsureLootCratesExist();
        VaultDoor door = EnsureVaultDoorExists();
        EnsureFallbackExists(() => new KeypadTerminal("vault_keypad", new Vector2(988f, 368f), door.Id));
        EnsureFallbackExists(() => new FuseBox(DefaultFuseBoxId, DefaultFuseBoxPosition, DefaultFuseBoxTargetGroupId));
        EnsureFallbackExists(() => new LaserSwitch(DefaultLaserSwitchId, DefaultLaserSwitchPosition, DefaultLaserTargetGroupId));
    }

    private void EnsureLootCratesExist()
    {
        if (_worldObjects.OfType<LootCrate>().Any())
        {
            return;
        }

        _worldObjects.Add(new LootCrate("loot_vault_1", new Vector2(460f, 288f)));
        _worldObjects.Add(new LootCrate("loot_vault_2", new Vector2(720f, 288f)));
    }

    private VaultDoor EnsureVaultDoorExists()
    {
        VaultDoor? existing = _worldObjects.OfType<VaultDoor>().FirstOrDefault();
        if (existing is not null)
        {
            return existing;
        }

        var door = new VaultDoor("vault_door", new Vector2(944f, 288f));
        _worldObjects.Add(door);
        return door;
    }

    private void EnsureFallbackExists<TInteractable>(Func<TInteractable> factory)
        where TInteractable : IInteractable
    {
        if (!_worldObjects.OfType<TInteractable>().Any())
        {
            _worldObjects.Add(factory());
        }
    }

    private void RegisterWorldObjects(GameContext context)
    {
        foreach (IInteractable item in _worldObjects)
        {
            context.Interactables.Add(item);
            if (item is VaultDoor door && !door.IsOpen && !context.Walls.Contains(door.DoorBounds))
            {
                context.Walls.Add(door.DoorBounds);
            }
        }
    }

    private static IInteractable? CreateInteractable(MapInteractableDto dto) => dto.Type switch
    {
        "LootCrate" => new LootCrate(dto.Id, new Vector2(dto.X, dto.Y), dto.CashValue),
        "VaultDoor" => new VaultDoor(dto.Id, new Vector2(dto.X, dto.Y)),
        "KeypadTerminal" => new KeypadTerminal(dto.Id, new Vector2(dto.X, dto.Y), dto.TargetId ?? "vault_door"),
        "FuseBox" => new FuseBox(dto.Id, new Vector2(dto.X, dto.Y), dto.TargetId ?? DefaultFuseBoxTargetGroupId),
        "LaserSwitch" => new LaserSwitch(dto.Id, new Vector2(dto.X, dto.Y), dto.TargetId ?? DefaultLaserTargetGroupId),
        _ => null
    };

    private static bool IsInsideZone(Vector2 pos, RectangleBounds zone) =>
        pos.X >= zone.X && pos.X <= zone.X + zone.Width &&
        pos.Y >= zone.Y && pos.Y <= zone.Y + zone.Height;
}
