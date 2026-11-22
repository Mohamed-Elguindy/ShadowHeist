using System.Numerics;
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
    public static void Main()
    {
        GameWindowChrome.InitializeWindow();

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
        context.Cutscene.IsEnabled = true;
        using var windowChrome = new GameWindowChrome();
        using var stealthCursor = new StealthCursorRenderer();
        windowChrome.LoadLogo();
        stealthCursor.Load();
        RegisterSystems(context);
        context.InitializeAll();
        context.Camera.Reset(context.PlayerPosition);

        while (!Raylib.WindowShouldClose() && !context.StateManager.ExitRequested)
        {
            float deltaTime = MathF.Min(Raylib.GetFrameTime(), 0.05f);
            bool headerConsumedInput = windowChrome.HandleInput(context);
            bool stateChanged = !headerConsumedInput && GameFlowInputRouter.ProcessInput(context);
            if (!stateChanged)
            {
                UpdateSimulationAndCutscene(context, deltaTime);
            }
            PrepareFrameViewport(context, deltaTime);
            windowChrome.SyncTitle(context);
            DrawFrame(context, windowChrome, stealthCursor);
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
        if (context.StateManager.CurrentState is GameFlowState.MainMenu or GameFlowState.LevelSelect)
        {
            context.Camera.UpdateMenuOrbit((float)Raylib.GetTime());
            context.Models.UpdateFrameLighting(context.Camera.Camera.Position, context);
            return;
        }

        Vector2 cameraTarget = context.Cutscene.ResolveCameraFocus(context.PlayerPosition);
        Vector2 mousePosition = Raylib.GetMousePosition();
        context.Camera.ApplyZoomWheel(Raylib.GetMouseWheelMove());
        context.Camera.UpdatePeekFromScreen(
            !context.Cutscene.IsActive && Raylib.IsMouseButtonDown(MouseButton.Right),
            mousePosition,
            cameraTarget);
        context.Camera.NotifyAlarmState(context.IsAlarmTriggered);
        context.Camera.Update(deltaTime, cameraTarget);
        context.AimWorldOverride = context.Camera.ScreenToGroundPoint(mousePosition);
        context.Models.UpdateFrameLighting(context.Camera.Camera.Position, context);
    }

    private static void UpdateSimulationAndCutscene(GameContext context, float deltaTime)
    {
        if (context.StateManager.CurrentState is GameFlowState.Playing or GameFlowState.Summary)
        {
            context.Cutscene.Update(deltaTime);
        }

        if (!context.StateManager.ShouldUpdateSimulation)
        {
            return;
        }

        context.UpdateAll(deltaTime);
        if (context.Cutscene.IsReadyForSummary)
        {
            context.StateManager.SyncWithMissionOutcome(context.Outcome);
        }
    }

    private static void DrawFrame(GameContext context, GameWindowChrome windowChrome, StealthCursorRenderer stealthCursor)
    {
        Color sky = context.IsAlarmTriggered ? Viewport3DConfig.AlarmSkyColor : Viewport3DConfig.SkyColor;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(sky);

        Raylib.BeginMode3D(context.Camera.Camera);
        ApplyWideClipPlanes(context.Camera.Camera.FovY);
        context.DrawAll();
        Raylib.EndMode3D();

        context.DrawHudAll();
        windowChrome.Draw(context);
        stealthCursor.UpdateAndDraw(context);
        Raylib.EndDrawing();
    }

    private static void ApplyWideClipPlanes(float fovYDegrees)
    {
        double aspect = (double)Raylib.GetScreenWidth() / Math.Max(1, Raylib.GetScreenHeight());
        Rlgl.SetMatrixProjection(Raymath.MatrixPerspective(fovYDegrees * HeistConstants.DegToRad, aspect, 15.0, 5000.0));
    }
}
