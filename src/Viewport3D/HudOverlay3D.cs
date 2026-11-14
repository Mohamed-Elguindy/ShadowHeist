using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.UI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders vignette edges, 3D-projected hold rings & prompts, sound meter, alarm banner, and control hint bar.
/// </summary>
public static class HudOverlay3D
{
    private const int HintFontSize = 18;
    private const int HudMargin = 20;
    private const int VignetteBandSize = 64;
    private const float HoldRingHeightOverGround = 42f;
    private const float PromptHeightOverGround = 88f;

    private static readonly Color HintTextColor = new(110, 225, 255, 255);
    private static readonly Color VignetteEdge = new(4, 6, 12, 130);

    public static void DrawFullHud(GameContext context, Camera3D camera, bool isOverviewCamera, float soundMeterLevel = 0f)
    {
        DrawVignette();
        SoundMeterBarRenderer.Draw(soundMeterLevel);
        HudRenderer.DrawAlarmBanner(context.IsAlarmTriggered, context.Outcome);
        HudRenderer.DrawStolenCashBanner(context.StolenCash);

        string cameraLabel = isOverviewCamera ? "FOLLOW" : "OVERVIEW";
        DrawHintBar($"[F5] 2D VIEW   |   [F6] {cameraLabel}   |   [SCROLL] ZOOM   |   [R] RESTART");
        DrawHoldProgressRing(context, camera);
        DrawInteractionPrompt(context, camera);
        HudRenderer.DrawOutcomeOverlay(context.Outcome);
    }

    public static void DrawHintBar(string text) =>
        Raylib.DrawText(text, HudMargin, Raylib.GetScreenHeight() - 30, HintFontSize, HintTextColor);

    private static void DrawVignette()
    {
        int width = Raylib.GetScreenWidth();
        int height = Raylib.GetScreenHeight();
        Color transparent = new(0, 0, 0, 0);

        Raylib.DrawRectangleGradientV(0, 0, width, VignetteBandSize, VignetteEdge, transparent);
        Raylib.DrawRectangleGradientV(0, height - VignetteBandSize, width, VignetteBandSize, transparent, VignetteEdge);
        Raylib.DrawRectangleGradientH(0, 0, VignetteBandSize, height, VignetteEdge, transparent);
        Raylib.DrawRectangleGradientH(width - VignetteBandSize, 0, VignetteBandSize, height, transparent, VignetteEdge);
    }

    private static void DrawHoldProgressRing(GameContext context, Camera3D camera)
    {
        IHoldInteractable? activeHold = InteractableQuery.FindHoldInProgress(context);
        if (activeHold is null)
        {
            return;
        }

        var worldPos = new Vector3(activeHold.Position.X, HoldRingHeightOverGround, activeHold.Position.Y);
        if (TryProjectInFrontOfCamera(worldPos, camera, out Vector2 screenPosition))
        {
            HoldProgressRingRenderer.Draw(screenPosition, activeHold.HoldProgress);
        }
    }

    private static void DrawInteractionPrompt(GameContext context, Camera3D camera)
    {
        IInteractable? active = InteractableQuery.FindInPlayerRange(context);
        if (active is null || string.IsNullOrWhiteSpace(active.PromptText))
        {
            return;
        }

        var worldPos = new Vector3(active.Position.X, PromptHeightOverGround, active.Position.Y);
        if (TryProjectInFrontOfCamera(worldPos, camera, out Vector2 screenPosition))
        {
            HudPromptRenderer.DrawPromptBox(active.PromptText, screenPosition);
        }
    }

    private static bool TryProjectInFrontOfCamera(Vector3 worldPos, Camera3D camera, out Vector2 screenPos)
    {
        Vector3 forward = camera.Target - camera.Position;
        if (Vector3.Dot(worldPos - camera.Position, forward) <= 0f)
        {
            screenPos = Vector2.Zero;
            return false;
        }

        screenPos = Raylib.GetWorldToScreen(worldPos, camera);
        return true;
    }
}
