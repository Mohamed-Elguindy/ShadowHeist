using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Coordinates 3D rendering for all world interactables: loot crates, vault doors, and hacking keypads.
/// </summary>
public sealed class HeistWorldRenderer
{
    private const string CrateTexturePath = "assets/sprites/prop_loot_crate.png";
    private const float CrateSize = 36f;
    private const float CrateHeight = 28f;
    private const float CollectedHeightFactor = 0.65f;
    private const float CrateShadowRadius = 24f;
    private const float LootBobFrequency = 3.2f;
    private const float LootBobAmplitude = 2.5f;

    private static readonly Color CollectedCrateTint = new(115, 120, 130, 255);
    private static readonly Color GoldColor = new(248, 196, 44, 255);
    private static readonly Color CashColor = new(78, 172, 102, 255);

    private readonly VaultDoor3DRenderer _vaultDoorRenderer = new();
    private Model? _crateUnitBox;

    public void Load(ToonModelLibrary models)
    {
        if (_crateUnitBox is not null || !Raylib.IsWindowReady())
        {
            return;
        }

        models.EnsureLoaded();
        _crateUnitBox = models.LoadTexturedBox(CrateTexturePath, 1f, 1f, 1f);
    }

    public void Reset() => _vaultDoorRenderer.Reset();

    public void Update(float deltaTime, GameContext context) =>
        _vaultDoorRenderer.Update(deltaTime, context);

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
                    _vaultDoorRenderer.Draw(door, context.ElapsedSeconds);
                    break;
                case KeypadTerminal keypad:
                    KeypadTerminal3DRenderer.Draw(keypad, context.PlayerPosition, context.ElapsedSeconds);
                    break;
            }
        }
    }

    public void ClearCachedReferences() => _crateUnitBox = null;

    private void DrawLootCrate(LootCrate crate, float timeSeconds)
    {
        Raylib3DPrimitives.DrawGroundShadow(crate.Position, CrateShadowRadius);
        float bodyHeight = crate.IsCollected ? CrateHeight * CollectedHeightFactor : CrateHeight;
        var bodyCenter = new Vector3(crate.Position.X, bodyHeight * 0.5f, crate.Position.Y);

        if (_crateUnitBox is Model boxModel)
        {
            Color tint = crate.IsCollected ? CollectedCrateTint : Color.White;
            Raylib.DrawModelEx(boxModel, bodyCenter, Vector3.UnitY, 0f, new Vector3(CrateSize, bodyHeight, CrateSize), tint);
        }

        Raylib.DrawCubeWires(bodyCenter, CrateSize, bodyHeight, CrateSize, Viewport3DConfig.OutlineColor);
        if (!crate.IsCollected)
        {
            DrawUncollectedLootStacks(crate.Position, timeSeconds);
        }
    }

    private static void DrawUncollectedLootStacks(Vector2 position, float timeSeconds)
    {
        float bobOffset = MathF.Sin(timeSeconds * LootBobFrequency) * LootBobAmplitude;
        var stackTop = new Vector3(position.X, (CrateHeight * 1.12f) + bobOffset, position.Y);
        Raylib.DrawCube(stackTop + new Vector3(-6f, 0f, 0f), 11f, 7f, 18f, GoldColor);
        Raylib.DrawCube(stackTop + new Vector3(7f, -1f, 2f), 10f, 6f, 16f, CashColor);
    }
}
