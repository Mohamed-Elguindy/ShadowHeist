using Raylib_cs;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

public sealed class HeistWorldRenderer : IDisposable
{
    private const string CrateTexturePath = "assets/sprites/prop_loot_crate.png";
    private const string DoorTexturePath = "assets/sprites/prop_vault_door.png";
    private const string KeypadTexturePath = "assets/sprites/prop_keypad_terminal.png";
    private const float CrateSpriteSize = 64f;
    private const float DoorSpriteSize = 96f;
    private const float KeypadSpriteSize = 40f;

    private static readonly Color OpenDoorTint = new(255, 255, 255, 70);

    private readonly SpriteDrawer _sprites = new();
    private readonly IReadOnlyList<LootCrate> _crates;
    private readonly VaultDoor _vaultDoor;
    private readonly KeypadTerminal _keypad;

    public HeistWorldRenderer(IReadOnlyList<LootCrate> crates, VaultDoor vaultDoor, KeypadTerminal keypad)
    {
        _crates = crates;
        _vaultDoor = vaultDoor;
        _keypad = keypad;
    }

    public void LoadSprites()
    {
        _sprites.LoadTexture(CrateTexturePath);
        _sprites.LoadTexture(DoorTexturePath);
        _sprites.LoadTexture(KeypadTexturePath);
    }

    public void DrawWorldObjects()
    {
        DrawCrates();
        DrawVaultDoor();
        DrawKeypad();
    }

    public void Dispose() => _sprites.Dispose();

    private void DrawCrates()
    {
        foreach (LootCrate crate in _crates.Where(crate => !crate.IsCollected))
        {
            _sprites.DrawCentered(CrateTexturePath, crate.Position, CrateSpriteSize, Color.White);
        }
    }

    private void DrawVaultDoor()
    {
        Color doorTint = _vaultDoor.IsOpen ? OpenDoorTint : Color.White;
        _sprites.DrawCentered(DoorTexturePath, _vaultDoor.Position, DoorSpriteSize, doorTint);
    }

    private void DrawKeypad()
    {
        Color keypadTint = _keypad.IsHacked ? Color.Green : Color.White;
        _sprites.DrawCentered(KeypadTexturePath, _keypad.Position, KeypadSpriteSize, keypadTint);
    }
}
