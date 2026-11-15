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

    public void Init(GameContext context)
    {
        _interaction.Reset();
        _lootSparks.Reset();
        _soundMeter = new SoundMeter(context.EventBus);
        _hud = new HudRenderer(_soundMeter);

        BuildWorldObjects(context);
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
    }

    public void DrawHud(GameContext context) => _hud?.DrawHudOverlay(context);

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
        if (!_worldObjects.OfType<LootCrate>().Any())
        {
            _worldObjects.Add(new LootCrate("loot_vault_1", new Vector2(460f, 288f)));
            _worldObjects.Add(new LootCrate("loot_vault_2", new Vector2(720f, 288f)));
        }

        VaultDoor? door = _worldObjects.OfType<VaultDoor>().FirstOrDefault();
        if (door is null)
        {
            door = new VaultDoor("vault_door", new Vector2(944f, 288f));
            _worldObjects.Add(door);
        }

        if (!_worldObjects.OfType<KeypadTerminal>().Any())
        {
            _worldObjects.Add(new KeypadTerminal("vault_keypad", new Vector2(988f, 368f), door.Id));
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
        _ => null
    };

    private static bool IsInsideZone(Vector2 pos, RectangleBounds zone) =>
        pos.X >= zone.X && pos.X <= zone.X + zone.Width &&
        pos.Y >= zone.Y && pos.Y <= zone.Y + zone.Height;
}
