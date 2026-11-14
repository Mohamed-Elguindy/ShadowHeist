using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Renders 3D interactables: loot crates, hinged vault door, vault security seal beams, and hacking keypad.
/// </summary>
public sealed class HeistWorldRenderer : IDisposable
{
    private const string CrateTexturePath = "assets/sprites/prop_loot_crate.png";
    private const float CrateSize = 36f;
    private const float CrateHeight = 28f;
    private const float VaultFrameWidth = 92f;
    private const float VaultFrameHeight = 88f;
    private const float VaultDoorOpenSpeed = 160f;
    private const float VaultOpenAngleDegrees = 92f;
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

    public void Load(ToonModelLibrary models)
    {
        if (_crateUnitBox is not null || !Raylib.IsWindowReady())
        {
            return;
        }

        models.EnsureLoaded();
        _crateUnitBox = models.LoadTexturedBox(CrateTexturePath, 1f, 1f, 1f);
    }

    public void Reset() => _vaultSwingDegrees = 0f;

    public void Update(float deltaTime, GameContext context)
    {
        bool anyDoorOpen = context.Interactables.OfType<VaultDoor>().Any(door => door.IsOpen);
        float targetAngle = anyDoorOpen ? VaultOpenAngleDegrees : 0f;
        float maxStep = VaultDoorOpenSpeed * deltaTime;

        _vaultSwingDegrees = MathF.Abs(targetAngle - _vaultSwingDegrees) <= maxStep
            ? targetAngle
            : _vaultSwingDegrees + (MathF.Sign(targetAngle - _vaultSwingDegrees) * maxStep);
    }

    public void Draw(GameContext context)
    {
        foreach (IInteractable item in context.Interactables)
        {
            switch (item)
            {
                case LootCrate crate:
                    DrawLootCrate(crate, context.ElapsedSeconds);
                    break;
                case VaultDoor door:
                    DrawVaultDoor(door, context.ElapsedSeconds);
                    break;
                case KeypadTerminal keypad:
                    DrawKeypadTerminal(keypad, context.PlayerPosition, context.ElapsedSeconds);
                    break;
            }
        }
    }

    public void Dispose() => _crateUnitBox = null;

    private void DrawLootCrate(LootCrate crate, float timeSeconds)
    {
        Raylib3DPrimitives.DrawGroundShadow(crate.Position, 24f);
        float bodyHeight = crate.IsCollected ? CrateHeight * 0.65f : CrateHeight;
        var bodyCenter = new Vector3(crate.Position.X, bodyHeight * 0.5f, crate.Position.Y);

        if (_crateUnitBox is Model boxModel)
        {
            Color tint = crate.IsCollected ? new Color(115, 120, 130, 255) : Color.White;
            Raylib.DrawModelEx(boxModel, bodyCenter, Vector3.UnitY, 0f, new Vector3(CrateSize, bodyHeight, CrateSize), tint);
        }

        Raylib.DrawCubeWires(bodyCenter, CrateSize, bodyHeight, CrateSize, Viewport3DConfig.OutlineColor);
        if (!crate.IsCollected)
        {
            float bobOffset = MathF.Sin(timeSeconds * 3.2f) * 2.5f;
            var stackTop = new Vector3(crate.Position.X, (CrateHeight * 1.12f) + bobOffset, crate.Position.Y);
            Raylib.DrawCube(stackTop + new Vector3(-6f, 0f, 0f), 11f, 7f, 18f, GoldColor);
            Raylib.DrawCube(stackTop + new Vector3(7f, -1f, 2f), 10f, 6f, 16f, CashColor);
        }
    }

    private void DrawVaultDoor(VaultDoor door, float timeSeconds)
    {
        var frameCenter = new Vector3(door.Position.X, 0f, door.Position.Y);
        DrawVaultFrame(frameCenter);
        DrawVaultDoorLeaf(frameCenter, door.IsLocked);
        DrawVaultStatusBeacon(frameCenter, door, timeSeconds);

        if (door.IsLocked)
        {
            DrawVaultSecuritySeal(door.Position, timeSeconds);
        }
    }

    private static void DrawVaultFrame(Vector3 center)
    {
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight * 0.5f, -VaultFrameWidth * 0.5f), 32f, VaultFrameHeight, 12f, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight * 0.5f, VaultFrameWidth * 0.5f), 32f, VaultFrameHeight, 12f, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, VaultFrameHeight + 5f, 0f), 34f, 14f, VaultFrameWidth + 14f, SteelColor);
    }

    private void DrawVaultDoorLeaf(Vector3 frameCenter, bool isLocked)
    {
        float swingRadians = _vaultSwingDegrees * HeistConstants.DegToRad;
        float cos = MathF.Cos(swingRadians);
        float sin = MathF.Sin(swingRadians);

        Vector3 leafCenter = frameCenter + new Vector3(sin * 40f, VaultFrameHeight * 0.48f, -40f + (cos * 40f));
        float leafWidth = (MathF.Abs(cos) * 16f) + (MathF.Abs(sin) * 80f);
        float leafLength = (MathF.Abs(sin) * 16f) + (MathF.Abs(cos) * 80f);

        Color doorTint = isLocked ? SteelColor : new Color(172, 198, 212, 255);
        Raylib3DPrimitives.DrawOutlinedCube(leafCenter, new Vector3(leafWidth, VaultFrameHeight - 6f, leafLength), doorTint, Viewport3DConfig.OutlineColor);

        if (_vaultSwingDegrees < 40f)
        {
            Vector3 wheelCenter = leafCenter + new Vector3(9f, 0f, 0f);
            Raylib.DrawCylinderEx(wheelCenter + new Vector3(-2f, 0f, 0f), wheelCenter + new Vector3(3f, 0f, 0f), 13f, 13f, 12, BrassColor);
        }
    }

    private static void DrawVaultStatusBeacon(Vector3 center, VaultDoor door, float timeSeconds)
    {
        Color statusColor = door.IsOpen ? EmeraldGlowColor : (door.IsLocked ? RedAlertColor : CyanGlowColor);
        byte alpha = (byte)(255f * (0.6f + 0.4f * MathF.Sin(timeSeconds * 5f)));
        Raylib.DrawSphere(center + new Vector3(12f, VaultFrameHeight + 16f, 0f), 6f, new Color(statusColor.R, statusColor.G, statusColor.B, alpha));
    }

    private static void DrawVaultSecuritySeal(Vector2 doorPosition, float timeSeconds)
    {
        byte glowAlpha = (byte)(95f + (50f * MathF.Sin(timeSeconds * 4.5f)));
        byte coreAlpha = (byte)(190f + (60f * MathF.Sin(timeSeconds * 4.5f)));
        var glow = new Color((byte)244, (byte)63, (byte)94, glowAlpha);
        var core = new Color((byte)255, (byte)130, (byte)150, coreAlpha);

        for (int i = 0; i < 3; i++)
        {
            float beamHeight = 24f + (i * 18f);
            var start = new Vector3(doorPosition.X - 16f, beamHeight, doorPosition.Y - 40f);
            var end = new Vector3(doorPosition.X - 16f, beamHeight, doorPosition.Y + 40f);
            Raylib3DPrimitives.DrawVolumetricBeam(start, end, glowRadius: 1.4f, coreRadius: 0.45f, glow, core);
        }
    }

    private static void DrawKeypadTerminal(KeypadTerminal keypad, Vector2 playerPosition, float timeSeconds)
    {
        var terminalCenter = new Vector3(keypad.Position.X, KeypadHeight * 0.5f, keypad.Position.Y);
        Raylib3DPrimitives.DrawGroundShadow(keypad.Position, 15f);
        Raylib3DPrimitives.DrawOutlinedCube(terminalCenter, new Vector3(18f, KeypadHeight, 18f), KeypadBodyColor, Viewport3DConfig.OutlineColor);

        Color screenColor = keypad.IsHacked ? EmeraldGlowColor : (keypad.HoldProgress > 0f ? GoldColor : CyanGlowColor);
        Raylib.DrawCube(terminalCenter + new Vector3(0f, 12f, 9.5f), 12f, 14f, 2f, screenColor);

        if (keypad.HoldProgress > 0f && !keypad.IsHacked)
        {
            DrawActiveHackingEffect(keypad, terminalCenter, playerPosition, timeSeconds);
        }

        byte ledAlpha = (byte)(keypad.IsHacked ? 255f : 150f + 100f * MathF.Sin(timeSeconds * 4f));
        Raylib.DrawSphere(terminalCenter + new Vector3(0f, (KeypadHeight * 0.5f) + 4f, 0f), 3.5f, new Color(screenColor.R, screenColor.G, screenColor.B, ledAlpha));
    }

    private static void DrawActiveHackingEffect(KeypadTerminal keypad, Vector3 terminalCenter, Vector2 playerPosition, float timeSeconds)
    {
        Vector3 barOrigin = new(keypad.Position.X, KeypadHeight + 18f, keypad.Position.Y);
        float fillWidth = 34f * keypad.HoldProgress;

        Raylib.DrawCube(barOrigin, 38f, 7f, 4f, KeypadBodyColor);
        Raylib.DrawCube(barOrigin + new Vector3((fillWidth - 34f) * 0.5f, 0f, 0.5f), fillWidth, 5f, 4f, GoldColor);
        Raylib.DrawLine3D(new Vector3(playerPosition.X, 40f, playerPosition.Y), terminalCenter + new Vector3(0f, 12f, 0f), CyanGlowColor);

        float ringRadius = 28f + (MathF.Sin(timeSeconds * 8f) * 4f);
        Raylib.DrawCylinderWires(new Vector3(keypad.Position.X, 2f, keypad.Position.Y), ringRadius, ringRadius, 1f, 18, CyanGlowColor);
    }
}
