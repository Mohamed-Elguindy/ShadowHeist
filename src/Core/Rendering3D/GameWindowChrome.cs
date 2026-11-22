using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Manages undecorated window initialization, custom logo/title lifecycle, drag-to-move, and window controls.
/// </summary>
public sealed class GameWindowChrome : IDisposable
{
    public const int HeaderHeight = WindowChromeLayout.HeaderHeight;

    public static Texture2D? BrandLogoTexture { get; private set; }

    private Texture2D? _logoTexture;
    private string? _lastAppliedTitle;
    private bool _isDraggingWindow;
    private Vector2 _dragGrabOffset;

    public static void InitializeWindow()
    {
        Raylib.SetConfigFlags(
            ConfigFlags.Msaa4xHint |
            ConfigFlags.ResizableWindow |
            ConfigFlags.UndecoratedWindow);
        Raylib.InitWindow(WindowChromeLayout.DefaultWidth, WindowChromeLayout.DefaultHeight, WindowChromeLayout.BaseTitle);
        Raylib.SetWindowMinSize(WindowChromeLayout.MinWidth, WindowChromeLayout.MinHeight);
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(WindowChromeLayout.TargetFps);
        Raylib.MaximizeWindow();
        ApplyWindowIcon();
    }

    public static void ToggleMaximizeRestore()
    {
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        if (Raylib.IsWindowMaximized())
        {
            Raylib.RestoreWindow();
            return;
        }

        Raylib.MaximizeWindow();
    }

    public void LoadLogo()
    {
        UiTypography.Load();
        _logoTexture ??= UiVisualHelpers.LoadBilinearTexture(WindowChromeLayout.WindowIconPath);
        BrandLogoTexture = _logoTexture;
    }

    public bool HandleInput(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Raylib.IsWindowReady())
        {
            return false;
        }

        Vector2 mouse = Raylib.GetMousePosition();
        UpdateWindowDrag(mouse);

        if (mouse.Y > HeaderHeight)
        {
            return false;
        }

        WindowControlBounds controls = WindowChromePresentation.ComputeButtonBounds(Raylib.GetScreenWidth());
        return !Raylib.IsMouseButtonPressed(MouseButton.Left) || HandleHeaderClick(context, mouse, controls);
    }

    public void SyncTitle(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        string nextTitle = WindowChromePresentation.ResolveTitle(context.StateManager, context.Outcome);
        if (string.Equals(nextTitle, _lastAppliedTitle, StringComparison.Ordinal))
        {
            return;
        }

        Raylib.SetWindowTitle(nextTitle);
        _lastAppliedTitle = nextTitle;
    }

    public void Draw(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        WindowControlBounds controls = WindowChromePresentation.ComputeButtonBounds(Raylib.GetScreenWidth());
        WindowChromeRenderer.Draw(_logoTexture, context.StateManager, context.Outcome, controls);
    }

    public void Dispose()
    {
        if (_logoTexture is Texture2D texture && Raylib.IsWindowReady())
        {
            Raylib.UnloadTexture(texture);
        }

        UiTypography.Unload();
        _logoTexture = null;
        BrandLogoTexture = null;
        _isDraggingWindow = false;
    }

    private void UpdateWindowDrag(Vector2 mouse)
    {
        if (!_isDraggingWindow)
        {
            return;
        }

        if (!Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            _isDraggingWindow = false;
            return;
        }

        Vector2 delta = mouse - _dragGrabOffset;
        if (delta == Vector2.Zero)
        {
            return;
        }

        Vector2 windowPos = Raylib.GetWindowPosition();
        Raylib.SetWindowPosition((int)(windowPos.X + delta.X), (int)(windowPos.Y + delta.Y));
    }

    private bool HandleHeaderClick(GameContext context, Vector2 mouse, WindowControlBounds controls)
    {
        if (Raylib.CheckCollisionPointRec(mouse, controls.CloseButton))
        {
            context.StateManager.RequestExit();
            return true;
        }

        if (Raylib.CheckCollisionPointRec(mouse, controls.MaximizeButton))
        {
            ToggleMaximizeRestore();
            return true;
        }

        if (Raylib.CheckCollisionPointRec(mouse, controls.MinimizeButton))
        {
            Raylib.MinimizeWindow();
            return true;
        }

        if (!Raylib.IsWindowMaximized())
        {
            _isDraggingWindow = true;
            _dragGrabOffset = mouse;
        }

        return true;
    }

    private static void ApplyWindowIcon()
    {
        if (WindowChromePresentation.ResolveIconPath() is not string iconPath)
        {
            return;
        }

        unsafe
        {
            Image icon = Raylib.LoadImage(iconPath);
            if (icon.Data == null)
            {
                return;
            }

            try
            {
                Raylib.SetWindowIcon(icon);
            }
            finally
            {
                Raylib.UnloadImage(icon);
            }
        }
    }
}
