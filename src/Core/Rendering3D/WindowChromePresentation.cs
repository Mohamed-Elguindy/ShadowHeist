using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Bounding rectangles for the custom tactical header's window control buttons.
/// </summary>
public readonly record struct WindowControlBounds(
    Rectangle MinimizeButton,
    Rectangle MaximizeButton,
    Rectangle CloseButton);

/// <summary>
/// Pure presentation helpers for window titles, header status tags, and control button geometry.
/// </summary>
public static class WindowChromePresentation
{
    private const int CloseButtonIndex = 2;

    public static string ResolveTitle(GameStateManager stateManager, HeistOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(stateManager);
        return stateManager.CurrentState switch
        {
            GameFlowState.Playing => $"{WindowChromeLayout.BaseTitle} — {stateManager.SelectedLevel.Title}",
            GameFlowState.Paused => $"{WindowChromeLayout.BaseTitle} — Paused",
            GameFlowState.Summary => ResolveSummaryTitle(outcome),
            _ => WindowChromeLayout.BaseTitle
        };
    }

    public static string ResolveHeaderTag(GameStateManager stateManager, HeistOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(stateManager);
        return stateManager.CurrentState switch
        {
            GameFlowState.MainMenu => "TACTICAL STEALTH",
            GameFlowState.LevelSelect => "SELECT CONTRACT",
            GameFlowState.Playing => stateManager.SelectedLevel.Title.ToUpperInvariant(),
            GameFlowState.Paused => "MISSION PAUSED",
            GameFlowState.Summary => outcome == HeistOutcome.Escaped ? "EXTRACTION COMPLETE" : "MISSION FAILED",
            _ => string.Empty
        };
    }

    public static Color ResolveTagColor(GameStateManager stateManager, HeistOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(stateManager);
        return stateManager.CurrentState switch
        {
            GameFlowState.Playing => WindowChromeLayout.ActiveMissionTagColor,
            GameFlowState.Paused => WindowChromeLayout.PausedTagColor,
            GameFlowState.Summary => outcome == HeistOutcome.Escaped
                ? WindowChromeLayout.SuccessTagColor
                : WindowChromeLayout.AlertTagColor,
            _ => WindowChromeLayout.NeutralTagColor
        };
    }

    public static WindowControlBounds ComputeButtonBounds(int screenWidth)
    {
        int startX = screenWidth - (WindowChromeLayout.ButtonWidth * WindowChromeLayout.ButtonCount);
        var minimize = new Rectangle(startX, 0, WindowChromeLayout.ButtonWidth, WindowChromeLayout.HeaderHeight);
        var maximize = new Rectangle(startX + WindowChromeLayout.ButtonWidth, 0, WindowChromeLayout.ButtonWidth, WindowChromeLayout.HeaderHeight);
        var close = new Rectangle(startX + (WindowChromeLayout.ButtonWidth * CloseButtonIndex), 0, WindowChromeLayout.ButtonWidth, WindowChromeLayout.HeaderHeight);
        return new WindowControlBounds(minimize, maximize, close);
    }

    public static string? ResolveIconPath() =>
        UiVisualHelpers.ResolveExistingAssetPath(WindowChromeLayout.WindowIconPath);

    private static string ResolveSummaryTitle(HeistOutcome outcome) => outcome switch
    {
        HeistOutcome.Escaped => $"{WindowChromeLayout.BaseTitle} — Extraction Complete",
        HeistOutcome.Caught => $"{WindowChromeLayout.BaseTitle} — Mission Failed",
        _ => WindowChromeLayout.BaseTitle
    };
}
