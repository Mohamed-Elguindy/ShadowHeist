using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.Rendering;
using ShadowHeist.HeistWorld.UI;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.HeistWorld;

public sealed class HeistWorldModule : IGameSystem, IDisposable
{
    private readonly InteractionController _interaction = new();
    private readonly WorldLightingModule? _world;
    private readonly List<LootCrate> _crates = new();
    private VaultDoor _vaultDoor = null!;
    private KeypadTerminal _keypad = null!;
    private HeistWorldRenderer? _renderer;
    private SoundMeter? _soundMeter;
    private HudRenderer? _hud;
    private RectangleBounds _getawayZone = HeistConstants.DefaultGetawayZone;
    private bool _spritesLoaded;

    public float SoundMeterLevel => _soundMeter?.Level ?? 0f;

    public HeistWorldModule(WorldLightingModule? world = null)
    {
        _world = world;
    }

    public void Init(GameContext context)
    {
        _interaction.Reset();
        _soundMeter = new SoundMeter(context.EventBus);
        _hud = new HudRenderer(_soundMeter);

        BuildWorldObjects(context);
        _renderer ??= new HeistWorldRenderer(_crates, _vaultDoor, _keypad);
        _renderer.SetTargets(_vaultDoor, _keypad);
        if (!_spritesLoaded)
        {
            _renderer.LoadSprites();
            _spritesLoaded = true;
        }
    }

    public void Update(float deltaTime, GameContext context)
    {
        _soundMeter?.Update(deltaTime);
        if (context.Outcome != HeistOutcome.Playing) return;

        _interaction.HandlePlayerInteraction(deltaTime, context);

        if (context.StolenCash >= HeistConstants.TargetCashGoal && IsInsideZone(context.PlayerPosition, _getawayZone))
        {
            context.Outcome = HeistOutcome.Escaped;
        }
    }

    public void Draw(GameContext context)
    {
        if (context.Is3DRenderingActive) return;

        _renderer?.DrawWorldObjects();
        _hud?.DrawHudOverlay(context);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _spritesLoaded = false;
    }

    private void BuildWorldObjects(GameContext context)
    {
        _crates.Clear();
        _getawayZone = _world?.CurrentMap?.GetawayZone ?? HeistConstants.DefaultGetawayZone;
        IReadOnlyList<MapInteractableDto>? items = _world?.CurrentMap?.Interactables;

        VaultDoor? loadedDoor = null;
        KeypadTerminal? loadedKeypad = null;

        if (items is { Count: > 0 })
        {
            foreach (MapInteractableDto dto in items)
            {
                switch (dto.Type)
                {
                    case "LootCrate":
                        _crates.Add(new LootCrate(dto.Id, new Vector2(dto.X, dto.Y), dto.CashValue));
                        break;
                    case "VaultDoor":
                        loadedDoor = new VaultDoor(dto.Id, new Vector2(dto.X, dto.Y));
                        break;
                    case "KeypadTerminal":
                        loadedKeypad = new KeypadTerminal(dto.Id, new Vector2(dto.X, dto.Y), dto.TargetId ?? "vault_door");
                        break;
                }
            }
        }

        if (_crates.Count == 0)
        {
            _crates.Add(new LootCrate("loot_vault_1", new Vector2(460f, 288f)));
            _crates.Add(new LootCrate("loot_vault_2", new Vector2(720f, 288f)));
        }

        _vaultDoor = loadedDoor ?? new VaultDoor("vault_door", new Vector2(944f, 288f));
        _keypad = loadedKeypad ?? new KeypadTerminal("vault_keypad", new Vector2(988f, 368f), _vaultDoor.Id);

        foreach (LootCrate crate in _crates) context.Interactables.Add(crate);
        context.Interactables.Add(_vaultDoor);
        context.Interactables.Add(_keypad);

        if (!_vaultDoor.IsOpen && !context.Walls.Contains(_vaultDoor.DoorBounds))
        {
            context.Walls.Add(_vaultDoor.DoorBounds);
        }
    }

    private static bool IsInsideZone(Vector2 pos, RectangleBounds zone) =>
        pos.X >= zone.X && pos.X <= zone.X + zone.Width &&
        pos.Y >= zone.Y && pos.Y <= zone.Y + zone.Height;
}
