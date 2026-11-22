using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.PlayerSystem;
using ShadowHeist.PlayerSystem.Gadgets;
using ShadowHeist.PlayerSystem.Rendering;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class GadgetHudTests
{
    [Fact]
    public void ResolveIconPath_SwitchesBetweenCoinAndEmpAsLoadoutChanges()
    {
        var loadout = new PlayerGadgetLoadout();

        Assert.Equal(GadgetHudLayout.CoinIconPath, GadgetHudPresentation.ResolveIconPath(loadout.ActiveGadget));

        loadout.SelectIndex(1);
        Assert.Equal(GadgetHudLayout.EmpIconPath, GadgetHudPresentation.ResolveIconPath(loadout.ActiveGadget));

        loadout.CycleNext();
        Assert.Equal(GadgetHudLayout.CoinIconPath, GadgetHudPresentation.ResolveIconPath(loadout.ActiveGadget));
    }

    [Fact]
    public void ResolveIconPath_SupportsCustomGadgetNamedEmp()
    {
        IGadget customEmp = new StubGadget("emp", charges: 1);

        Assert.Equal(GadgetHudLayout.EmpIconPath, GadgetHudPresentation.ResolveIconPath(customEmp));
    }

    [Fact]
    public void GadgetSpriteAssets_ExistInBuildOutput()
    {
        string coinFullPath = Path.Combine(AppContext.BaseDirectory, GadgetHudLayout.CoinIconPath);
        string empFullPath = Path.Combine(AppContext.BaseDirectory, GadgetHudLayout.EmpIconPath);

        Assert.True(File.Exists(coinFullPath), $"Missing coin HUD sprite at {coinFullPath}");
        Assert.True(File.Exists(empFullPath), $"Missing EMP HUD sprite at {empFullPath}");
    }

    [Fact]
    public void ResolveIconTintAndBorder_ReflectChargesAndEmpPulseState()
    {
        var coinReady = new CoinGadget(initialCharges: 2);
        var coinEmpty = new CoinGadget(initialCharges: 0);
        var empReady = new EmpGadget(initialCharges: 1);
        var empEmpty = new EmpGadget(initialCharges: 0);

        Assert.Equal(GadgetHudLayout.IconReadyTint, GadgetHudPresentation.ResolveIconTint(coinReady));
        Assert.Equal(GadgetHudLayout.IconEmptyTint, GadgetHudPresentation.ResolveIconTint(coinEmpty));

        Assert.Equal(GadgetHudLayout.CoinBorder, GadgetHudPresentation.ResolveBorderColor(coinReady, isEmpPulseActive: false));
        Assert.Equal(GadgetHudLayout.EmpReadyBorder, GadgetHudPresentation.ResolveBorderColor(empReady, isEmpPulseActive: false));
        Assert.Equal(GadgetHudLayout.EmptyBorder, GadgetHudPresentation.ResolveBorderColor(coinEmpty, isEmpPulseActive: false));
        Assert.Equal(GadgetHudLayout.EmpPulseBorder, GadgetHudPresentation.ResolveBorderColor(empEmpty, isEmpPulseActive: true));

        Assert.Equal(GadgetHudLayout.ActiveTextColor, GadgetHudPresentation.ResolveHeaderTextColor(coinReady, isEmpPulseActive: false));
        Assert.Equal(GadgetHudLayout.SecondaryTextColor, GadgetHudPresentation.ResolveHeaderTextColor(coinEmpty, isEmpPulseActive: false));
        Assert.Equal(GadgetHudLayout.EmpPulseTextColor, GadgetHudPresentation.ResolveHeaderTextColor(empEmpty, isEmpPulseActive: true));

        Assert.Equal(GadgetHudLayout.CoinFallbackBadge, GadgetHudPresentation.ResolveFallbackBadge(coinReady));
        Assert.Equal(GadgetHudLayout.EmpFallbackBadge, GadgetHudPresentation.ResolveFallbackBadge(empReady));
        Assert.Equal(GadgetHudLayout.CoinAccentColor, GadgetHudPresentation.ResolveFallbackColor(coinReady));
        Assert.Equal(GadgetHudLayout.EmpAccentColor, GadgetHudPresentation.ResolveFallbackColor(empReady));
        Assert.Equal(GadgetHudLayout.SecondaryTextColor, GadgetHudPresentation.ResolveFallbackColor(coinEmpty));
    }

    [Fact]
    public void GadgetHud_ThrowsArgumentNullException_WhenRequiredArgumentsAreNull()
    {
        var renderer = new GadgetHudRenderer();

        Assert.Throws<ArgumentNullException>(() => renderer.Load(null!));
        Assert.Throws<ArgumentNullException>(() => renderer.Draw(null!));
        Assert.Throws<ArgumentNullException>(() => GadgetHudPresentation.ResolveIconPath(null!));
    }

    [Fact]
    public void BuildHeaderAndSlotsText_DisplayActiveGadgetChargesAndEmpCountdown()
    {
        var loadout = new PlayerGadgetLoadout();
        Assert.Equal("EQUIPPED: COIN x3   |   [Q] USE", GadgetHudPresentation.BuildHeaderText(loadout.ActiveGadget, loadout.Emp));

        loadout.SelectIndex(1);
        Assert.True(loadout.TryUseActiveGadget(Vector2.Zero, new Vector2(100f, 0f), Array.Empty<RectangleBounds>(), new GameEventBus()) is not null);

        string empHeader = GadgetHudPresentation.BuildHeaderText(loadout.ActiveGadget, loadout.Emp);
        string slotsText = GadgetHudPresentation.BuildSlotsText(loadout);

        Assert.Contains("EQUIPPED: EMP x1", empHeader);
        Assert.Contains("ACTIVE", empHeader);
        Assert.Contains("[1] COIN x3", slotsText);
        Assert.Contains("[2] EMP x1", slotsText);
    }

    [Fact]
    public void TextureCacheAndModelLibrary_HandleHeadlessAndMissingPathsSafely()
    {
        using var cache = new TextureCache();
        using var models = new ToonModelLibrary();

        Assert.Null(cache.Load(GadgetHudLayout.CoinIconPath));
        Assert.Null(cache.Load("assets/sprites/does_not_exist.png"));
        Assert.Null(cache.Load(string.Empty));
        Assert.Equal(0, cache.CachedCount);

        Assert.Null(models.LoadTexture(GadgetHudLayout.EmpIconPath));
        Assert.Equal(0, models.CachedTextureCount);
    }

    [Fact]
    public void GadgetHudRenderer_AndPlayerSystemModule_HandleHeadlessLifecycleAndRestartSafely()
    {
        using var context = new GameContext();
        var module = new PlayerSystemModule();
        context.RegisterSystem(module);

        context.InitializeAll();
        module.DrawHud(context);

        context.RestartMission();
        module.DrawHud(context);

        var renderer = new GadgetHudRenderer();
        renderer.Load(context.Models);
        renderer.Draw(module.Gadgets);
        renderer.ClearCachedReferences();
    }

    private sealed class StubGadget(string name, int charges) : IGadget
    {
        public string Name { get; } = name;
        public int RemainingCharges { get; } = charges;
        public bool TryUse(Vector2 origin, Vector2 target, GameEventBus eventBus) => false;
    }
}
