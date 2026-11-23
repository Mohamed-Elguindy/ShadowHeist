using System.Globalization;
using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.UI;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Spawns and animates 3D gold particle bursts and rising cash popups when loot crates are collected.
/// </summary>
public sealed class LootSparkRenderer
{
    private const float SparkLifetimeSeconds = 0.90f;
    private const float PopupLifetimeSeconds = 1.45f;
    private const float GravityY = -180f;
    private const float HorizontalSpeed = 55f;
    private const float BaseUpwardSpeed = 95f;
    private const float UpwardSpeedStep = 28f;
    private const float SpawnHeightY = 24f;
    private const float PopupBaseHeightY = 48f;
    private const float PopupRiseScreenPixels = 34f;
    private const float SparkCubeSize = 5f;
    private const int SparksPerBurst = 14;
    private const int PopupFontSize = 18;
    private const int CenterDivisor = 2;

    private static readonly Color SparkBaseColor = new(248, 196, 44, 255);
    private static readonly Color PopupShadowColor = new(6, 10, 18, 220);

    private readonly List<GoldSpark> _sparks = new();
    private readonly List<CashPopup> _popups = new();
    private readonly HashSet<string> _collectedCrates = new();

    public int ActivePopupCount => _popups.Count;

    public void Reset()
    {
        _sparks.Clear();
        _popups.Clear();
        _collectedCrates.Clear();
    }

    public void Update(float deltaTime, GameContext context)
    {
        UpdateSparks(deltaTime);
        UpdatePopups(deltaTime);

        foreach (LootCrate crate in context.Interactables.OfType<LootCrate>())
        {
            if (crate.IsCollected && _collectedCrates.Add(crate.Id))
            {
                SpawnLootBurst(crate.Position);
                _popups.Add(new CashPopup(crate.Position, crate.CashValue, Age: 0f));
            }
        }
    }

    public void Draw()
    {
        foreach (GoldSpark spark in _sparks)
        {
            float progress = spark.Age / SparkLifetimeSeconds;
            byte alpha = (byte)(255f * (1f - progress));
            var sparkColor = new Color(SparkBaseColor.R, SparkBaseColor.G, SparkBaseColor.B, alpha);
            Raylib.DrawCube(spark.Position, SparkCubeSize, SparkCubeSize, SparkCubeSize, sparkColor);
        }
    }

    public void DrawHudPopups(Camera3D camera)
    {
        foreach (CashPopup popup in _popups)
        {
            var worldPos = new Vector3(popup.WorldPosition.X, PopupBaseHeightY, popup.WorldPosition.Y);
            if (!HudPromptRenderer.TryProjectInFrontOfCamera(worldPos, camera, out Vector2 screenPos))
            {
                continue;
            }

            float progress = Math.Clamp(popup.Age / PopupLifetimeSeconds, 0f, 1f);
            byte alpha = (byte)(255f * (1f - progress));
            string text = string.Create(CultureInfo.InvariantCulture, $"+${popup.CashAmount:N0}");
            int x = (int)screenPos.X - (UiTypography.MeasureTextWidth(text, PopupFontSize, bold: true) / CenterDivisor);
            int y = (int)(screenPos.Y - (progress * PopupRiseScreenPixels));

            UiTypography.DrawText(text, x + 1, y + 1, PopupFontSize, new Color(PopupShadowColor.R, PopupShadowColor.G, PopupShadowColor.B, alpha), bold: true);
            UiTypography.DrawText(text, x, y, PopupFontSize, new Color(SparkBaseColor.R, SparkBaseColor.G, SparkBaseColor.B, alpha), bold: true);
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        var gravityStep = new Vector3(0f, GravityY, 0f) * deltaTime;
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            GoldSpark current = _sparks[i];
            GoldSpark updated = new(
                current.Position + (current.Velocity * deltaTime),
                current.Velocity + gravityStep,
                current.Age + deltaTime);

            if (updated.Age >= SparkLifetimeSeconds)
            {
                _sparks.RemoveAt(i);
            }
            else
            {
                _sparks[i] = updated;
            }
        }
    }

    private void UpdatePopups(float deltaTime)
    {
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            CashPopup current = _popups[i];
            float nextAge = current.Age + deltaTime;
            if (nextAge >= PopupLifetimeSeconds)
            {
                _popups.RemoveAt(i);
            }
            else
            {
                _popups[i] = current with { Age = nextAge };
            }
        }
    }

    private void SpawnLootBurst(Vector2 origin)
    {
        for (int i = 0; i < SparksPerBurst; i++)
        {
            float angle = (i / (float)SparksPerBurst) * MathF.PI * 2f;
            var velocity = new Vector3(
                MathF.Cos(angle) * HorizontalSpeed,
                BaseUpwardSpeed + ((i % 3) * UpwardSpeedStep),
                MathF.Sin(angle) * HorizontalSpeed);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, SpawnHeightY, origin.Y), velocity, Age: 0f));
        }
    }

    private readonly record struct GoldSpark(Vector3 Position, Vector3 Velocity, float Age);
    private readonly record struct CashPopup(Vector2 WorldPosition, int CashAmount, float Age);
}
