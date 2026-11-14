using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;
using ShadowHeist.PlayerSystem;
using ShadowHeist.WorldLighting;

namespace ShadowHeist;

/// <summary>
/// Application entry point wiring the 4 3D game modules and running the main Raylib 3D loop.
/// </summary>
public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int TargetFps = 60;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ShadowHeist 3D — Raylib C#");
        Raylib.SetTargetFPS(TargetFps);

        try
        {
            RunGameLoop();
        }
        finally
        {
            Raylib.CloseWindow();
        }
    }

    private static void RunGameLoop()
    {
        using var context = new GameContext();
        RegisterSystems(context);
        context.InitializeAll();
        context.Camera.Reset(context.PlayerPosition);

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            HandleGlobalHotkeys(context);
            UpdateCameraAndLighting(context, deltaTime);
            context.UpdateAll(deltaTime);
            DrawFrame(context);
        }
    }

    private static void RegisterSystems(GameContext context)
    {
        var worldModule = new WorldLightingModule();
        var guardModule = new GuardAiModule(worldModule);
        var heistModule = new HeistWorldModule(worldModule);

        context.RegisterSystem(worldModule);
        context.RegisterSystem(new PlayerSystemModule());
        context.RegisterSystem(guardModule);
        context.RegisterSystem(heistModule);
    }

    private static void HandleGlobalHotkeys(GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            context.RestartMission();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F6))
        {
            context.Camera.ToggleOverviewMode();
        }
    }

    private static void UpdateCameraAndLighting(GameContext context, float deltaTime)
    {
        context.Camera.ApplyZoomWheel(Raylib.GetMouseWheelMove());
        context.Camera.Update(deltaTime, context.PlayerPosition);
        context.AimWorldOverride = context.Camera.ScreenToGroundPoint(Raylib.GetMousePosition());
        context.Models.UpdateFrameLighting(context.Camera.Camera.Position, context);
    }

    private static void DrawFrame(GameContext context)
    {
        Color sky = context.IsAlarmTriggered ? Viewport3DConfig.AlarmSkyColor : Viewport3DConfig.SkyColor;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(sky);

        Raylib.BeginMode3D(context.Camera.Camera);
        context.DrawAll();
        Raylib.EndMode3D();

        context.DrawHudAll();
        Raylib.EndDrawing();
    }
}
