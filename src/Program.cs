using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;
using ShadowHeist.PlayerSystem;
using ShadowHeist.WorldLighting;

namespace ShadowHeist;

/// <summary>
/// Day-1 Locked Entry Point.
/// Team Rule: Do NOT put game logic here. Add features inside your assigned module folder!
/// </summary>
public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int TargetFps = 60;

    public static void Main()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ShadowHeist — Raylib C#");
        Raylib.SetTargetFPS(TargetFps);

        using var context = new GameContext();
        context.RegisterSystem(new PlayerSystemModule());  // Mahmoud (Input & Player)
        context.RegisterSystem(new WorldLightingModule()); // Ali
        context.RegisterSystem(new HeistWorldModule());    // Guindy (Interactions)
        context.RegisterSystem(new GuardAiModule());       // Hatem

        context.InitializeAll();

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            context.UpdateAll(deltaTime);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(9, 13, 22, 255));
            context.DrawAll();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
