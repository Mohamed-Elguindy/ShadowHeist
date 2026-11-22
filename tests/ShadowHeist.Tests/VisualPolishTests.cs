using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.Rendering;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class VisualPolishTests
{
    [Fact]
    public void FontAndCrosshairAssets_ExistInBuildOutput()
    {
        string regularFontPath = Path.Combine(AppContext.BaseDirectory, UiTypography.RegularFontPath);
        string boldFontPath = Path.Combine(AppContext.BaseDirectory, UiTypography.BoldFontPath);
        string crosshairPath = Path.Combine(AppContext.BaseDirectory, StealthCursorRenderer.CrosshairAssetPath);

        Assert.True(File.Exists(regularFontPath), $"Missing regular UI font at {regularFontPath}");
        Assert.True(File.Exists(boldFontPath), $"Missing bold UI font at {boldFontPath}");
        Assert.True(File.Exists(crosshairPath), $"Missing stealth crosshair at {crosshairPath}");
    }

    [Fact]
    public void UiTypography_ExecutesSafelyInHeadlessMode()
    {
        UiTypography.Load();
        Assert.False(UiTypography.IsLoaded);
        Assert.Equal(0, UiTypography.MeasureTextWidth("SHADOW HEIST", 16));
        Assert.Equal(50, UiTypography.CenterTextX("TEST", containerX: 0, containerWidth: 100, fontSize: 16));

        UiTypography.DrawText("SHADOW HEIST", 10, 10, 16, Raylib_cs.Color.White, bold: true);
        UiTypography.Unload();
    }

    [Fact]
    public void StealthCursor_OnlyActivatesDuringGameplayBelowHeaderBar()
    {
        float belowHeader = GameWindowChrome.HeaderHeight + 10f;
        float insideHeader = GameWindowChrome.HeaderHeight - 5f;

        Assert.True(StealthCursorRenderer.ShouldUseCustomCrosshair(GameFlowState.Playing, belowHeader));
        Assert.False(StealthCursorRenderer.ShouldUseCustomCrosshair(GameFlowState.Playing, insideHeader));
        Assert.False(StealthCursorRenderer.ShouldUseCustomCrosshair(GameFlowState.MainMenu, belowHeader));
        Assert.False(StealthCursorRenderer.ShouldUseCustomCrosshair(GameFlowState.Paused, belowHeader));
        Assert.False(StealthCursorRenderer.ShouldUseCustomCrosshair(GameFlowState.Summary, belowHeader));

        using var context = new GameContext();
        using var cursor = new StealthCursorRenderer();
        cursor.Load();
        cursor.UpdateAndDraw(context);
        Assert.Throws<ArgumentNullException>(() => cursor.UpdateAndDraw(null!));
    }

    [Fact]
    public void TacticalCamera_MenuOrbitMovesSmoothlyOverTime()
    {
        var camera = new TacticalCameraController();

        camera.UpdateMenuOrbit(0f);
        Vector3 initialPosition = camera.Camera.Position;

        camera.UpdateMenuOrbit(5f);
        Vector3 laterPosition = camera.Camera.Position;

        Assert.NotEqual(initialPosition, laterPosition);
        Assert.Equal(Viewport3DConfig.OverviewFovDegrees, camera.Camera.FovY);
    }

    [Fact]
    public void LootSparkRenderer_TracksAndExpiresCollectedCratePopupsHeadlessly()
    {
        using var context = new GameContext();
        var crate = new LootCrate("crate_1", new Vector2(400f, 300f), cashValue: 50000);
        context.Interactables.Add(crate);

        var renderer = new LootSparkRenderer();
        renderer.Update(0.016f, context);
        Assert.Equal(0, renderer.ActivePopupCount);

        crate.OnInteract(context, context.EventBus);
        renderer.Update(0.016f, context);
        Assert.Equal(1, renderer.ActivePopupCount);

        renderer.Update(2.0f, context);
        Assert.Equal(0, renderer.ActivePopupCount);
        renderer.Reset();
    }
}
