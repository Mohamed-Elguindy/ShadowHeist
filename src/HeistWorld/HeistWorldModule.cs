using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.Rendering;
using ShadowHeist.HeistWorld.UI;

namespace ShadowHeist.HeistWorld;

public sealed class HeistWorldModule : IGameSystem, IDisposable
{
    private const string VaultDoorId = "vault_door";

    private readonly InteractionController _interaction = new();
    private readonly HudRenderer _hud = new();
    private readonly HeistWorldRenderer _renderer;
    private readonly List<LootCrate> _crates;
    private readonly VaultDoor _vaultDoor;
    private readonly KeypadTerminal _keypad;

    public HeistWorldModule()
    {
        _crates = SpawnCrates();
        _vaultDoor = new VaultDoor(VaultDoorId, new Vector2(900f, 280f));
        _keypad = new KeypadTerminal("vault_keypad", new Vector2(830f, 360f), VaultDoorId);
        _renderer = new HeistWorldRenderer(_crates, _vaultDoor, _keypad);
    }

    public void Init(GameContext context)
    {
        _renderer.LoadSprites();
        RegisterInteractables(context);
    }

    public void Update(float deltaTime, GameContext context) =>
        _interaction.HandlePlayerInteraction(deltaTime, context);

    public void Draw(GameContext context)
    {
        _renderer.DrawWorldObjects();
        _hud.DrawHudOverlay(context);
    }

    public void Dispose() => _renderer.Dispose();

    private void RegisterInteractables(GameContext context)
    {
        foreach (LootCrate crate in _crates)
        {
            context.Interactables.Add(crate);
        }

        context.Interactables.Add(_vaultDoor);
        context.Interactables.Add(_keypad);
    }

    private static List<LootCrate> SpawnCrates() => new()
    {
        new LootCrate("loot_vault_1", new Vector2(480f, 280f)),
        new LootCrate("loot_vault_2", new Vector2(720f, 280f)),
    };
}
