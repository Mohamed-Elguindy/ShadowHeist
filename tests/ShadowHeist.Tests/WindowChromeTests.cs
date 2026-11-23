using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class WindowChromeTests
{
    [Fact]
    public void BaseTitle_IsCleanBrandNameWithoutFrameworkSuffix()
    {
        Assert.Equal("ShadowHeist", WindowChromeLayout.BaseTitle);
        Assert.DoesNotContain("Raylib", WindowChromeLayout.BaseTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C#", WindowChromeLayout.BaseTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowIconAsset_ExistsInBuildOutput()
    {
        string iconFullPath = Path.Combine(AppContext.BaseDirectory, WindowChromeLayout.WindowIconPath);

        Assert.True(File.Exists(iconFullPath), $"Missing window icon at {iconFullPath}");
    }

    [Fact]
    public void ResolveTitleAndHeaderTag_ReflectFlowStateSelectedLevelAndOutcome()
    {
        using var context = new GameContext();
        GameStateManager state = context.StateManager;

        Assert.Equal("ShadowHeist", WindowChromePresentation.ResolveTitle(state, context.Outcome));
        Assert.Equal("TACTICAL STEALTH", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));
        Assert.Equal(WindowChromeLayout.NeutralTagColor, WindowChromePresentation.ResolveTagColor(state, context.Outcome));

        state.OpenLevelSelect();
        Assert.Equal("SELECT CONTRACT", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));

        state.StartQuickPlay(context);
        Assert.Equal("ShadowHeist — Level 1: City Central Bank", WindowChromePresentation.ResolveTitle(state, context.Outcome));
        Assert.Equal("LEVEL 1: CITY CENTRAL BANK", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));
        Assert.Equal(WindowChromeLayout.ActiveMissionTagColor, WindowChromePresentation.ResolveTagColor(state, context.Outcome));

        state.TogglePause();
        Assert.Equal("ShadowHeist — Paused", WindowChromePresentation.ResolveTitle(state, context.Outcome));
        Assert.Equal("MISSION PAUSED", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));
        Assert.Equal(WindowChromeLayout.PausedTagColor, WindowChromePresentation.ResolveTagColor(state, context.Outcome));

        state.SelectLevel(1);
        state.LaunchSelectedLevel(context);
        Assert.Equal("ShadowHeist — Level 2: High-Rise Penthouse", WindowChromePresentation.ResolveTitle(state, context.Outcome));

        context.Outcome = HeistOutcome.Escaped;
        state.SyncWithMissionOutcome(context.Outcome);
        Assert.Equal("ShadowHeist — Extraction Complete", WindowChromePresentation.ResolveTitle(state, context.Outcome));
        Assert.Equal("EXTRACTION COMPLETE", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));
        Assert.Equal(WindowChromeLayout.SuccessTagColor, WindowChromePresentation.ResolveTagColor(state, context.Outcome));

        state.RestartMission(context);
        context.Outcome = HeistOutcome.Caught;
        state.SyncWithMissionOutcome(context.Outcome);
        Assert.Equal("ShadowHeist — Mission Failed", WindowChromePresentation.ResolveTitle(state, context.Outcome));
        Assert.Equal("MISSION FAILED", WindowChromePresentation.ResolveHeaderTag(state, context.Outcome));
        Assert.Equal(WindowChromeLayout.AlertTagColor, WindowChromePresentation.ResolveTagColor(state, context.Outcome));
    }

    [Fact]
    public void ComputeButtonBounds_AlignsThreeControlsFlushRightInsideHeader()
    {
        const int screenWidth = 1280;
        WindowControlBounds bounds = WindowChromePresentation.ComputeButtonBounds(screenWidth);

        Assert.Equal(screenWidth, bounds.CloseButton.X + bounds.CloseButton.Width);
        Assert.Equal(bounds.MaximizeButton.X + bounds.MaximizeButton.Width, bounds.CloseButton.X);
        Assert.Equal(bounds.MinimizeButton.X + bounds.MinimizeButton.Width, bounds.MaximizeButton.X);
        Assert.Equal(GameWindowChrome.HeaderHeight, bounds.CloseButton.Height);
    }

    [Fact]
    public void HeadlessLifecycleAndNullGuards_ExecuteSafely()
    {
        using var context = new GameContext();
        using var chrome = new GameWindowChrome();

        chrome.LoadLogo();
        Assert.False(chrome.HandleInput(context));
        chrome.SyncTitle(context);
        chrome.Draw(context);

        Assert.Throws<ArgumentNullException>(() => chrome.HandleInput(null!));
        Assert.Throws<ArgumentNullException>(() => chrome.SyncTitle(null!));
        Assert.Throws<ArgumentNullException>(() => chrome.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => WindowChromePresentation.ResolveTitle(null!, HeistOutcome.Playing));
    }
}
