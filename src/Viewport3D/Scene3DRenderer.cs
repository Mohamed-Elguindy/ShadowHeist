using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Draws the 2D logic plane (X, Y) as a 3D scene (X, 0, Z): floor, extruded walls, props, characters.
/// Pure presentation. No gameplay state is modified here.
/// </summary>
public sealed class Scene3DRenderer : IDisposable
{
    private const string MarbleFloorPath = "assets/sprites/floor_marble_dark.png";
    private const string SteelFloorPath = "assets/sprites/floor_vault_steel.png";
    private const string ThiefModelPath = "assets/models/characters/character_single.obj";
    private const string ThiefSkinPath = "assets/models/characters/skin_thief.png";
    private const string GuardSkinPath = "assets/models/characters/skin_guard.png";

    private const float FloorTileBlock = 256f;
    private const float FloorThickness = 4f;
    private const float WallHeight = 96f;
    private const float WallCapHeight = 5f;
    private const float CharacterHeight = 72f;
    private const float VisionRange = 230f;
    private const float VisionHalfAngleDegrees = 32f;
    private const int VisionSegments = 14;
    private const float ShadowRadius = 17f;

    private static readonly Color WallBodyColor = new(52, 60, 78, 255);
    private static readonly Color WallCapColor = new(122, 136, 160, 255);
    private static readonly Color OutlineColor = new(14, 18, 26, 255);
    private static readonly Color ShadowColor = new(0, 0, 0, 105);
    private static readonly Color VisionColor = new(255, 215, 90, 70);
    private static readonly Color CrateBodyColor = new(60, 70, 86, 255);
    private static readonly Color GoldColor = new(246, 190, 40, 255);
    private static readonly Color CashColor = new(94, 156, 96, 255);
    private static readonly Color SteelColor = new(150, 162, 178, 255);
    private static readonly Color BrassColor = new(196, 150, 70, 255);
    private static readonly Color KeypadColor = new(38, 48, 62, 255);
    private static readonly Color KeypadGlowColor = new(56, 225, 255, 255);

    private readonly ToonModelLibrary _library = new();
    private readonly List<FurniturePlacement> _furniture = new();

    private Model? _marbleBlock;
    private Model? _steelBlock;
    private LoadedModel? _thief;
    private LoadedModel? _guard;

    public void Load()
    {
        _library.LoadShader();
        _thief = _library.Load(ThiefModelPath, CharacterHeight, ThiefSkinPath);
        _guard = _library.Load(ThiefModelPath, CharacterHeight, GuardSkinPath);

        _marbleBlock = _library.LoadTexturedBox(MarbleFloorPath, FloorTileBlock, FloorThickness, FloorTileBlock);
        _steelBlock = _library.LoadTexturedBox(SteelFloorPath, FloorTileBlock, FloorThickness, FloorTileBlock);

        // tableCoffee
        // tableCoffee
        // loungeChair
        // pottedPlant
        // pottedPlant
        // pottedPlant
        // lamp
        // sideTable
    }

    public void Draw(GameContext context, IReadOnlyList<Guard> guards, float playerRotationDegrees)
    {
        DrawFloor();
        DrawWalls(context.Walls);
        DrawFurniture();
        DrawInteractables(context);
        DrawGuards(guards);
        DrawPlayer(context.PlayerPosition, playerRotationDegrees);
    }

    public void Dispose()
    {
        _library.Dispose();
    }

    private void AddFurniture(string name, float height, Vector2 position, float yawDegrees)
    {
        LoadedModel? model = _library.Load($"assets/models/furniture/{name}.obj", height);
        if (model is not null)
        {
            _furniture.Add(new FurniturePlacement(model.Value, position, yawDegrees));
        }
    }

    private void DrawFloor()
    {
        for (float z = 0f; z < 768f; z += FloorTileBlock)
        {
            for (float x = 0f; x < 1280f; x += FloorTileBlock)
            {
                var center = new Vector3(x + FloorTileBlock / 2f, -FloorThickness / 2f, z + FloorTileBlock / 2f);
                bool inVault = x >= 256f && x < 768f && z >= 256f && z < 512f;
                Model? block = inVault && _steelBlock is not null ? _steelBlock : _marbleBlock;
                if (block is not null)
                {
                    Raylib.DrawModel(block.Value, center, 1f, Color.White);
                }
                else
                {
                    Raylib.DrawCubeV(center, new Vector3(FloorTileBlock, FloorThickness, FloorTileBlock), WallBodyColor);
                }
            }
        }
    }

    private static void DrawWalls(IReadOnlyList<RectangleBounds> walls)
    {
        foreach (RectangleBounds wall in walls)
        {
            float h = (wall.Y > 500f || wall.X < 50f) ? 0f : WallHeight; var size = new Vector3(wall.Width, h, wall.Height);
            var center = new Vector3(wall.X + wall.Width / 2f, WallHeight / 2f, wall.Y + wall.Height / 2f);
            Raylib.DrawCubeV(center, size, WallBodyColor);

            var capSize = new Vector3(wall.Width + 2f, WallCapHeight, wall.Height + 2f);
            var capCenter = new Vector3(center.X, h + WallCapHeight / 2f, center.Z);
            Raylib.DrawCubeV(capCenter, capSize, WallCapColor);
            Raylib.DrawCubeWiresV(center, size, OutlineColor);
            Raylib.DrawCubeWiresV(capCenter, capSize, OutlineColor);
        }
    }

    private void DrawFurniture()
    {
        foreach (FurniturePlacement item in _furniture)
        {
            DrawLoadedModel(item.Model, item.Position, item.YawDegrees);
        }
    }

    private void DrawInteractables(GameContext context)
    {
        foreach (var interactable in context.Interactables)
        {
            var ground = new Vector3(interactable.Position.X, 0f, interactable.Position.Y);
            switch (interactable)
            {
                case LootCrate { IsCollected: false }:
                    DrawLootCrate(ground);
                    break;
                case VaultDoor door:
                    DrawVaultDoor(ground, door.IsOpen);
                    break;
                case KeypadTerminal keypad:
                    DrawKeypad(ground, keypad.IsHacked);
                    break;
            }
        }
    }

    private static void DrawLootCrate(Vector3 ground)
    {
        var bodySize = new Vector3(52f, 26f, 38f);
        var bodyCenter = ground + new Vector3(0f, bodySize.Y / 2f, 0f);
        Raylib.DrawCubeV(bodyCenter, bodySize, CrateBodyColor);
        Raylib.DrawCubeV(bodyCenter + new Vector3(-10f, bodySize.Y / 2f + 1f, 0f), new Vector3(24f, 3f, 32f), CashColor);
        Raylib.DrawCubeV(bodyCenter + new Vector3(14f, bodySize.Y / 2f + 1f, 0f), new Vector3(20f, 3f, 32f), GoldColor);
        Raylib.DrawCubeWiresV(bodyCenter, bodySize, OutlineColor);
    }

    private static void DrawVaultDoor(Vector3 ground, bool isOpen)
    {
        float lift = isOpen ? 28f : 0f;
        var start = ground + new Vector3(0f, 46f + lift, 0f);
        var end = start + new Vector3(0f, 0f, 14f);
        Raylib.DrawCylinderEx(start, end, 44f, 44f, 28, SteelColor);
        Raylib.DrawCylinderWiresEx(start, end, 44f, 44f, 28, OutlineColor);
        Raylib.DrawCylinderEx(start + new Vector3(0f, 0f, 14f), end + new Vector3(0f, 0f, 4f), 30f, 30f, 24, BrassColor);
        Raylib.DrawSphere(end + new Vector3(0f, 0f, 8f), 8f, BrassColor);
    }

    private static void DrawKeypad(Vector3 ground, bool isHacked)
    {
        var size = new Vector3(18f, 40f, 12f);
        var center = ground + new Vector3(0f, size.Y / 2f, 0f);
        Raylib.DrawCubeV(center, size, KeypadColor);
        Raylib.DrawCubeV(center + new Vector3(0f, 8f, size.Z / 2f + 0.5f), new Vector3(12f, 8f, 1f), isHacked ? Color.Green : KeypadGlowColor);
        Raylib.DrawCubeWiresV(center, size, OutlineColor);
    }

    private void DrawGuards(IReadOnlyList<Guard> guards)
    {
        foreach (Guard guard in guards)
        {
            DrawVisionCone(guard);
            if (_guard is not null)
            {
                DrawLoadedModel(_guard.Value, guard.Position, 90f - guard.RotationDegrees, withShadow: true);
            }
        }
    }

    private void DrawPlayer(Vector2 position, float rotationDegrees)
    {
        if (_thief is not null)
        {
            DrawLoadedModel(_thief.Value, position, 90f - rotationDegrees, withShadow: true);
        }
    }

    private static void DrawVisionCone(Guard guard)
    {
        float baseAngle = guard.RotationDegrees * Raylib.DEG2RAD;
        float halfAngle = VisionHalfAngleDegrees * Raylib.DEG2RAD;
        var origin = new Vector3(guard.Position.X, 1.5f, guard.Position.Y);

        for (int i = 0; i < VisionSegments; i++)
        {
            float a0 = baseAngle - halfAngle + (2f * halfAngle * i / VisionSegments);
            float a1 = baseAngle - halfAngle + (2f * halfAngle * (i + 1) / VisionSegments);
            var p0 = origin + new Vector3(MathF.Cos(a0), 0f, MathF.Sin(a0)) * VisionRange;
            var p1 = origin + new Vector3(MathF.Cos(a1), 0f, MathF.Sin(a1)) * VisionRange;
            Raylib.DrawTriangle3D(origin, p1, p0, VisionColor);
        }
    }

    private static void DrawLoadedModel(LoadedModel loaded, Vector2 position, float yawDegrees, bool withShadow = false)
    {
        if (withShadow)
        {
            Raylib.DrawCylinder(new Vector3(position.X, 0.4f, position.Y), ShadowRadius, ShadowRadius, 0.4f, 24, ShadowColor);
        }

        var worldPosition = new Vector3(position.X, loaded.FootOffsetY, position.Y);
        Raylib.DrawModelEx(loaded.Model, worldPosition, Vector3.UnitY, yawDegrees, new Vector3(loaded.Scale), Color.White);
    }

    private readonly record struct FurniturePlacement(LoadedModel Model, Vector2 Position, float YawDegrees);
}
