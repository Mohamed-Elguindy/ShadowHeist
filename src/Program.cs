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
        var worldModule = new WorldLightingModule();       // Ali (Map & Floor)
        context.RegisterSystem(worldModule);
        context.RegisterSystem(new PlayerSystemModule());  // Mahmoud (Input & Player)
        var guardModule = new GuardAiModule(worldModule);  // Hatem (Guards)
        context.RegisterSystem(guardModule);
        context.RegisterSystem(new HeistWorldModule(worldModule)); // Guindy (Interactions & HUD)
        context.RegisterSystem(new Viewport3DModule(guardModule, worldModule)); // 3D tactical view (F5)

        context.InitializeAll();

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                context.RestartMission();
            }

            Camera2D camera2D = BuildCamera2D(worldModule);
            if (!context.Is3DRenderingActive)
            {
                context.AimWorldOverride = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera2D);
            }

            float deltaTime = Raylib.GetFrameTime();
            context.UpdateAll(deltaTime);

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

        Raylib.CloseWindow();
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
