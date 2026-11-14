using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;
using ShadowHeist.PlayerSystem;
using ShadowHeist.Viewport3D;
using ShadowHeist.WorldLighting;

namespace ShadowHeist;

/// <summary>
/// Application entry point wiring all game modules.
/// </summary>
public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int TargetFps = 60;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ShadowHeist — Raylib C#");
        Raylib.SetTargetFPS(TargetFps);

        using var context = new GameContext();
        (WorldLightingModule worldModule, Viewport3DModule viewportModule) = RegisterSystems(context);
        context.InitializeAll();

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                context.RestartMission();
            }

            Camera2D camera2D = BuildCamera2D(worldModule);
            UpdateAimTarget(context, viewportModule, camera2D);
            context.UpdateAll(Raylib.GetFrameTime());
            DrawFrame(context, camera2D);
        }

        Raylib.CloseWindow();
    }

    private static (WorldLightingModule World, Viewport3DModule Viewport) RegisterSystems(GameContext context)
    {
        var worldModule = new WorldLightingModule();
        var guardModule = new GuardAiModule(worldModule);
        var heistModule = new HeistWorldModule(worldModule);
        var viewportModule = new Viewport3DModule(guardModule, worldModule, heistModule);

        context.RegisterSystem(worldModule);
        context.RegisterSystem(new PlayerSystemModule());
        context.RegisterSystem(guardModule);
        context.RegisterSystem(heistModule);
        context.RegisterSystem(viewportModule);

        return (worldModule, viewportModule);
    }

    private static void UpdateAimTarget(GameContext context, Viewport3DModule viewportModule, Camera2D camera2D)
    {
        if (context.Is3DRenderingActive)
        {
            viewportModule.SyncAimOverride(context);
            return;
        }

        context.AimWorldOverride = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera2D);
    }

    private static void DrawFrame(GameContext context, Camera2D camera2D)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(9, 13, 22, 255));

        if (context.Is3DRenderingActive)
        {
            context.DrawAll();
        }
        else
        {
            Raylib.BeginMode2D(camera2D);
            context.DrawAll();
            Raylib.EndMode2D();
        }

        Raylib.EndDrawing();
    }

    private static Camera2D BuildCamera2D(WorldLightingModule worldModule)
    {
        int mapHeight = (worldModule.CurrentMap?.GridHeight ?? 12) * (worldModule.CurrentMap?.TileSize ?? 64);
        int mapWidth = (worldModule.CurrentMap?.GridWidth ?? 20) * (worldModule.CurrentMap?.TileSize ?? 64);
        float zoom = MathF.Min((float)ScreenWidth / mapWidth, (float)ScreenHeight / mapHeight);

        return new Camera2D
        {
            Zoom = zoom,
            Offset = new Vector2((ScreenWidth - mapWidth * zoom) * 0.5f, (ScreenHeight - mapHeight * zoom) * 0.5f)
        };
    }
}
