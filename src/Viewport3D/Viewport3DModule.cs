using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// 3D tactical viewport test room (presentation only). F5 toggles between the 2D blueprint and the 3D view.
/// Reads GameContext and guard positions but never mutates gameplay state.
/// </summary>
public sealed class Viewport3DModule : IGameSystem, IDisposable
{
    private const float CameraHeight = 560f;
    private const float CameraBackOffset = 430f;
    private const float CameraFieldOfView = 50f;
    private const float CameraFollowSpeed = 6f;
    private const int ScreenshotFrame = 120;
    private const int HudFontSize = 20;
    private const int PromptFontSize = 16;
    private const float PromptHeightOverGround = 70f;

    private static readonly Color SkyColor = new(18, 24, 36, 255);

    private readonly GuardAiModule _guards;
    private readonly Scene3DRenderer _scene = new();
    private Camera3D _camera;
    private Vector3 _cameraFocus;
    private bool _is3DMode = true;
    private bool _hasFocus;
    private int _frameCounter;
    private readonly string? _screenshotPath = Environment.GetEnvironmentVariable("SHADOWHEIST_SCREENSHOT");

    public Viewport3DModule(GuardAiModule guards)
    {
        _guards = guards;
    }

    public void Init(GameContext context)
    {
        _scene.Load();
        _camera = new Camera3D
        {
            Up = Vector3.UnitY,
            FovY = CameraFieldOfView,
            Projection = CameraProjection.Perspective
        };
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F5))
        {
            _is3DMode = !_is3DMode;
        }

        var target = new Vector3(context.PlayerPosition.X, 0f, context.PlayerPosition.Y);
        _cameraFocus = _hasFocus
            ? Vector3.Lerp(_cameraFocus, target, MathF.Min(1f, CameraFollowSpeed * deltaTime))
            : target;
        _hasFocus = true;

        _camera.Target = _cameraFocus;
        _camera.Position = _cameraFocus + new Vector3(-160f, 220f, 180f);
    }

    public void Draw(GameContext context)
    {
        if (!_is3DMode)
        {
            DrawToggleHint("[F5] 3D VIEW");
            return;
        }

        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), SkyColor);

        Raylib.BeginMode3D(_camera);
        _scene.Draw(context, _guards.Guards, ReadPlayerRotation(context));
        Raylib.EndMode3D();

        DrawHud(context);
        CaptureScreenshotIfRequested();
    }

    /// <summary>Dev aid: set SHADOWHEIST_SCREENSHOT=/path/file.png to save one frame after start-up.</summary>
    private void CaptureScreenshotIfRequested()
    {
        _frameCounter++;
        if (_screenshotPath is not null && _frameCounter == ScreenshotFrame)
        {
            Raylib.TakeScreenshot(_screenshotPath);
        }
    }

    public void Dispose() => _scene.Dispose();

    private float ReadPlayerRotation(GameContext context)
    {
        // Project the mouse onto the Y = 0 ground plane so the character faces the cursor in 3D.
        Ray ray = Raylib.GetMouseRay(Raylib.GetMousePosition(), _camera);
        if (MathF.Abs(ray.Direction.Y) < 0.0001f)
        {
            return 0f;
        }

        float t = -ray.Position.Y / ray.Direction.Y;
        Vector3 hit = ray.Position + ray.Direction * t;
        Vector2 toMouse = new Vector2(hit.X, hit.Z) - context.PlayerPosition;
        return toMouse == Vector2.Zero ? 0f : MathF.Atan2(toMouse.Y, toMouse.X) * Raylib.RAD2DEG;
    }

    private void DrawHud(GameContext context)
    {
        string cash = $"STOLEN CASH: ${context.StolenCash:N0} / $500,000";
        int width = Raylib.MeasureText(cash, HudFontSize) + 28;
        int x = Raylib.GetScreenWidth() - width - 20;
        Raylib.DrawRectangle(x, 20, width, 36, new Color(15, 23, 42, 220));
        Raylib.DrawRectangleLines(x, 20, width, 36, new Color(56, 189, 248, 180));
        Raylib.DrawText(cash, x + 14, 28, HudFontSize, Color.Green);

        DrawToggleHint("[F5] 2D VIEW");
        DrawInteractionPrompt(context);
    }

    private void DrawInteractionPrompt(GameContext context)
    {
        IInteractable? active = InteractableQuery.FindInPlayerRange(context);
        if (active is null || string.IsNullOrWhiteSpace(active.PromptText))
        {
            return;
        }

        Vector2 screen = Raylib.GetWorldToScreen(
            new Vector3(active.Position.X, PromptHeightOverGround, active.Position.Y), _camera);
        int textWidth = Raylib.MeasureText(active.PromptText, PromptFontSize);
        int px = (int)screen.X - textWidth / 2;
        int py = (int)screen.Y;
        Raylib.DrawRectangle(px - 10, py - 5, textWidth + 20, PromptFontSize + 10, new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(px - 10, py - 5, textWidth + 20, PromptFontSize + 10, Color.White);
        Raylib.DrawText(active.PromptText, px, py, PromptFontSize, Color.Yellow);
    }

    private static void DrawToggleHint(string text)
    {
        Raylib.DrawText(text, 20, Raylib.GetScreenHeight() - 30, 18, new Color(110, 225, 255, 255));
    }
}
