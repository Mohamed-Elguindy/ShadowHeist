using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders detailed 3D interactable props: loot crates, hinged vault door, and hacking keypad + link beam.
/// </summary>
public sealed class PropsRenderer
{
    private const string CrateTexturePath = "assets/sprites/prop_loot_crate.png";
    private const float CrateSize = 36f;
    private const float CrateHeight = 28f;
    private const float VaultFrameWidth = 92f;
    private const float VaultFrameHeight = 88f;
    private const float VaultDoorOpenSpeed = 160f;
    private const float KeypadHeight = 46f;

    private static readonly Color GoldColor = new(248, 196, 44, 255);
    private static readonly Color CashColor = new(78, 172, 102, 255);
    private static readonly Color SteelColor = new(138, 152, 172, 255);
    private static readonly Color DarkSteelColor = new(54, 64, 80, 255);
    private static readonly Color BrassColor = new(212, 162, 68, 255);
    private static readonly Color KeypadBodyColor = new(34, 42, 56, 255);
    private static readonly Color CyanGlowColor = new(56, 225, 255, 255);
    private static readonly Color EmeraldGlowColor = new(74, 222, 128, 255);
    private static readonly Color RedAlertColor = new(244, 63, 94, 255);

    private Model? _crateUnitBox;
    private float _vaultSwingDegrees;

    public void Load(ToonModelLibrary library) =>
        _crateUnitBox = library.LoadTexturedBox(CrateTexturePath, 1f, 1f, 1f);

    public void Update(float deltaTime, GameContext context)
    {
        bool isOpen = context.Interactables.OfType<VaultDoor>().Any(door => door.IsOpen);
        float target = isOpen ? 92f : 0f;
        float step = VaultDoorOpenSpeed * deltaTime;
        _vaultSwingDegrees = MathF.Abs(target - _vaultSwingDegrees) <= step
            ? target
            : _vaultSwingDegrees + MathF.Sign(target - _vaultSwingDegrees) * step;
    }

    public void Draw(GameContext context, float timeSeconds)
    {
        foreach (IInteractable item in context.Interactables)
        {
            if (item is LootCrate crate) DrawLootCrate(crate, timeSeconds);
            else if (item is VaultDoor door) DrawVaultDoor(door, timeSeconds);
            else if (item is KeypadTerminal keypad) DrawKeypad(keypad, context.PlayerPosition, timeSeconds);
        }
    }

    private void DrawLootCrate(LootCrate crate, float timeSeconds)
    {
        Raylib.DrawCylinder(new Vector3(crate.Position.X, 0.4f, crate.Position.Y), 24f, 24f, 0.5f, 12, new Color(0, 0, 0, 95));
        float bodyHeight = crate.IsCollected ? CrateHeight * 0.65f : CrateHeight;
        var bodyCenter = new Vector3(crate.Position.X, bodyHeight * 0.5f, crate.Position.Y);

        if (_crateUnitBox is Model box)
        {
            Color tint = crate.IsCollected ? new Color(115, 120, 130, 255) : Color.White;
            Raylib.DrawModelEx(box, bodyCenter, Vector3.UnitY, 0f, new Vector3(CrateSize, bodyHeight, CrateSize), tint);
        }

        Raylib.DrawCubeWires(bodyCenter, CrateSize, bodyHeight, CrateSize, Viewport3DConfig.OutlineColor);
        if (!crate.IsCollected)
        {
            float bob = MathF.Sin(timeSeconds * 3.2f) * 2.5f;
            var top = new Vector3(crate.Position.X, CrateHeight * 1.12f + bob, crate.Position.Y);
            Raylib.DrawCube(top + new Vector3(-6f, 0f, 0f), 11f, 7f, 18f, GoldColor);
            Raylib.DrawCube(top + new Vector3(7f, -1f, 2f), 10f, 6f, 16f, CashColor);
        }
    }

    private void DrawVaultDoor(VaultDoor door, float timeSeconds)
    {
        var center = new Vector3(door.Position.X, 0f, door.Position.Y);
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight * 0.5f, -VaultFrameWidth * 0.5f), 32f, VaultFrameHeight, 12f, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight * 0.5f, VaultFrameWidth * 0.5f), 32f, VaultFrameHeight, 12f, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight + 5f, 0f), 34f, 14f, VaultFrameWidth + 14f, SteelColor);

        float rad = _vaultSwingDegrees * Raylib.DEG2RAD;
        Vector3 leafPos = center + new Vector3(MathF.Sin(rad) * 40f, VaultFrameHeight * 0.48f, -40f + MathF.Cos(rad) * 40f);
        float w = MathF.Abs(MathF.Cos(rad)) * 16f + MathF.Abs(MathF.Sin(rad)) * 80f;
        float l = MathF.Abs(MathF.Sin(rad)) * 16f + MathF.Abs(MathF.Cos(rad)) * 80f;

        Color doorTint = door.IsLocked ? SteelColor : new Color(172, 198, 212, 255);
        Raylib.DrawCube(leafPos, w, VaultFrameHeight - 6f, l, doorTint);
        Raylib.DrawCubeWires(leafPos, w, VaultFrameHeight - 6f, l, Viewport3DConfig.OutlineColor);

        if (_vaultSwingDegrees < 40f)
        {
            Vector3 wc = leafPos + new Vector3(9f, 0f, 0f);
            Raylib.DrawCylinderEx(wc + new Vector3(-2f, 0f, 0f), wc + new Vector3(3f, 0f, 0f), 13f, 13f, 12, BrassColor);
        }

        Color status = door.IsOpen ? EmeraldGlowColor : (door.IsLocked ? RedAlertColor : CyanGlowColor);
        byte alpha = (byte)(255 * (0.6f + 0.4f * MathF.Sin(timeSeconds * 5f)));
        Raylib.DrawSphere(center + new Vector3(12f, VaultFrameHeight + 16f, 0f), 6f, new Color(status.R, status.G, status.B, alpha));
    }

    private static void DrawKeypad(KeypadTerminal keypad, Vector2 playerPos, float timeSeconds)
    {
        var pos = new Vector3(keypad.Position.X, KeypadHeight * 0.5f, keypad.Position.Y);
        Raylib.DrawCylinder(new Vector3(keypad.Position.X, 0.4f, keypad.Position.Y), 15f, 15f, 0.5f, 10, new Color(0, 0, 0, 95));
        Raylib.DrawCube(pos, 18f, KeypadHeight, 18f, KeypadBodyColor);
        Raylib.DrawCubeWires(pos, 18f, KeypadHeight, 18f, Viewport3DConfig.OutlineColor);

        Color screenColor = keypad.IsHacked ? EmeraldGlowColor : (keypad.HoldProgress > 0f ? GoldColor : CyanGlowColor);
        Raylib.DrawCube(pos + new Vector3(0f, 12f, 9.5f), 12f, 14f, 2f, screenColor);

        if (keypad.HoldProgress > 0f && !keypad.IsHacked)
        {
            Vector3 bar = new(keypad.Position.X, KeypadHeight + 18f, keypad.Position.Y);
            float fillW = 34f * keypad.HoldProgress;
            Raylib.DrawCube(bar, 38f, 7f, 4f, KeypadBodyColor);
            Raylib.DrawCube(bar + new Vector3((fillW - 34f) * 0.5f, 0f, 0.5f), fillW, 5f, 4f, GoldColor);
            Raylib.DrawLine3D(new Vector3(playerPos.X, 40f, playerPos.Y), pos + new Vector3(0f, 12f, 0f), CyanGlowColor);
            Raylib.DrawCylinderWires(new Vector3(keypad.Position.X, 2f, keypad.Position.Y), 28f + MathF.Sin(timeSeconds * 8f) * 4f, 28f, 1f, 18, CyanGlowColor);
        }

        byte ledAlpha = (byte)(keypad.IsHacked ? 255f : 150f + 100f * MathF.Sin(timeSeconds * 4f));
        Raylib.DrawSphere(pos + new Vector3(0f, KeypadHeight * 0.5f + 4f, 0f), 3.5f,
            new Color(screenColor.R, screenColor.G, screenColor.B, ledAlpha));
    }
}
