using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.UI;
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
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ShadowHeist 3D — Raylib C#");
        Raylib.SetExitKey(KeyboardKey.Null);
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

        while (!Raylib.WindowShouldClose() && !context.StateManager.ExitRequested)
        {
            float deltaTime = MathF.Min(Raylib.GetFrameTime(), 0.05f);
            bool stateChanged = GameFlowInputRouter.ProcessInput(context);
            PrepareFrameViewport(context, deltaTime);
            if (!stateChanged)
            {
                UpdateSimulationIfPlaying(context, deltaTime);
            }
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

    private static void PrepareFrameViewport(GameContext context, float deltaTime)
    {
        context.Camera.ApplyZoomWheel(Raylib.GetMouseWheelMove());
        context.Camera.Update(deltaTime, context.PlayerPosition);
        context.AimWorldOverride = context.Camera.ScreenToGroundPoint(Raylib.GetMousePosition());
        context.Models.UpdateFrameLighting(context.Camera.Camera.Position, context);
    }

    private static void UpdateSimulationIfPlaying(GameContext context, float deltaTime)
    {
        if (!context.StateManager.ShouldUpdateSimulation)
        {
            return;
        }

        context.UpdateAll(deltaTime);
        context.StateManager.SyncWithMissionOutcome(context.Outcome);
    }

    private static void DrawFrame(GameContext context)
    {
        Color sky = context.IsAlarmTriggered ? Viewport3DConfig.AlarmSkyColor : Viewport3DConfig.SkyColor;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(sky);

        Raylib.BeginMode3D(context.Camera.Camera);
        ApplyWideClipPlanes(context.Camera.Camera.FovY);
        context.DrawAll();
        Raylib.EndMode3D();

        context.DrawHudAll();
        Raylib.EndDrawing();
    }

    private static void ApplyWideClipPlanes(float fovYDegrees)
    {
        double aspect = (double)Raylib.GetScreenWidth() / Math.Max(1, Raylib.GetScreenHeight());
        Rlgl.SetMatrixProjection(Raymath.MatrixPerspective(fovYDegrees * HeistConstants.DegToRad, aspect, 15.0, 5000.0));
    }
}
