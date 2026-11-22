using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Replaces the OS pointer with the tactical stealth crosshair sprite during active gameplay.
/// </summary>
public sealed class StealthCursorRenderer : IDisposable
{
    public const string CrosshairAssetPath = "assets/sprites/ui_crosshair_stealth.png";

    private const float CursorSize = 26f;
    private const float HalfDivisor = 2f;
    private const float TextureOrigin = 0f;
    private const float TextureRotation = 0f;

    private static readonly Color NormalCursorTint = new(186, 230, 253, 240);
    private static readonly Color AlarmCursorTint = new(248, 113, 113, 245);

    private Texture2D? _crosshairTexture;

    public void Load()
    {
        if (_crosshairTexture is not null || !Raylib.IsWindowReady())
        {
            return;
        }

        if (ResolveAssetPath(CrosshairAssetPath) is not string fullPath)
        {
            return;
        }

        Texture2D texture = Raylib.LoadTexture(fullPath);
        if (texture.Id == 0)
        {
            return;
        }

        Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
        _crosshairTexture = texture;
    }

    public void UpdateAndDraw(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        bool showTacticalCrosshair = ShouldUseCustomCrosshair(context.StateManager.CurrentState, mouse.Y) && !context.Cutscene.IsActive;

        SyncOsCursorVisibility(showTacticalCrosshair);
        if (showTacticalCrosshair && _crosshairTexture is Texture2D texture)
        {
            DrawCrosshairTexture(texture, mouse, context.IsAlarmTriggered);
        }
    }

    public static bool ShouldUseCustomCrosshair(GameFlowState flowState, float mouseY) =>
        flowState == GameFlowState.Playing && mouseY > GameWindowChrome.HeaderHeight;

    public void Dispose()
    {
        if (Raylib.IsWindowReady())
        {
            if (Raylib.IsCursorHidden())
            {
                Raylib.ShowCursor();
            }

            if (_crosshairTexture is Texture2D texture)
            {
                Raylib.UnloadTexture(texture);
            }
        }

        _crosshairTexture = null;
    }

    private static void SyncOsCursorVisibility(bool hideOsCursor)
    {
        if (hideOsCursor && !Raylib.IsCursorHidden())
        {
            Raylib.HideCursor();
        }
        else if (!hideOsCursor && Raylib.IsCursorHidden())
        {
            Raylib.ShowCursor();
        }
    }

    private static void DrawCrosshairTexture(Texture2D texture, Vector2 mouse, bool isAlarmTriggered)
    {
        var source = new Rectangle(TextureOrigin, TextureOrigin, texture.Width, texture.Height);
        var destination = new Rectangle(
            mouse.X - (CursorSize / HalfDivisor),
            mouse.Y - (CursorSize / HalfDivisor),
            CursorSize,
            CursorSize);
        Color tint = isAlarmTriggered ? AlarmCursorTint : NormalCursorTint;

        Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, TextureRotation, tint);
    }

    private static string? ResolveAssetPath(string relativePath)
    {
        if (File.Exists(relativePath))
        {
            return relativePath;
        }

        string baseRelativePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(baseRelativePath) ? baseRelativePath : null;
    }
}
