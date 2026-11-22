using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Computed animation weights for the mission-launch cinematic fade and briefing title card.
/// </summary>
public readonly record struct MissionIntroFrame(
    bool IsActive,
    float CurtainAlpha,
    float BannerAlpha,
    float DividerProgress,
    float LetterboxHeight);

/// <summary>
/// Renders a cinematic black fade-in, letterbox bars, and "OPERATION" title card when a mission begins or restarts.
/// </summary>
public static class MissionIntroOverlay
{
    public const float TotalDurationSeconds = 2.80f;
    private const float CurtainFadeDurationSeconds = 0.55f;
    private const float BannerFadeInStartSeconds = 0.15f;
    private const float BannerFadeInEndSeconds = 0.50f;
    private const float BannerHoldEndSeconds = 2.20f;
    private const float DividerExpandDurationSeconds = 0.55f;
    private const float MaxLetterboxHeight = 26f;

    private const int CardWidth = 640;
    private const int CardHeight = 128;
    private const int CardTopScreenDivisor = 3;
    private const int KickerFontSize = 13;
    private const int TitleFontSize = 26;
    private const int SubtitleFontSize = 14;
    private const int KickerOffsetY = 18;
    private const int TitleOffsetY = 42;
    private const int DividerOffsetY = 80;
    private const int DividerThickness = 2;
    private const int DividerMaxWidth = 420;
    private const int SubtitleOffsetY = 92;
    private const int CardMinClearance = 12;
    private const int HalfDivisor = 2;
    private const float MaxByte = 255f;
    private const float LetterboxMaxAlpha = 235f;
    private const float CardMaxAlpha = 228f;
    private const float CardBorderMaxAlpha = 185f;

    private static readonly Color CurtainRgb = new(4, 7, 14, 0);
    private static readonly Color LetterboxRgb = new(4, 7, 14, 0);
    private static readonly Color CardBgRgb = new(10, 15, 28, 0);
    private static readonly Color CardBorderRgb = new(56, 189, 248, 0);
    private static readonly Color KickerRgb = new(250, 204, 21, 0);
    private static readonly Color TitleRgb = new(224, 242, 254, 0);
    private static readonly Color SubtitleRgb = new(148, 163, 184, 0);

    public static void Draw(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.StateManager.CurrentState != GameFlowState.Playing ||
            context.Outcome != HeistOutcome.Playing ||
            context.Cutscene.IsEscapePlaying ||
            context.Cutscene.IsCaughtPlaying ||
            context.Cutscene.WasEntrySkipped)
        {
            return;
        }

        float introElapsed = context.Cutscene.IsEntryPlaying
            ? context.Cutscene.ElapsedSeconds
            : (context.Cutscene.IsEnabled ? HeistCutsceneController.EntryTotalDurationSeconds + context.ElapsedSeconds : context.ElapsedSeconds);

        MissionIntroFrame frame = EvaluateFrame(introElapsed);
        if (!frame.IsActive || !Raylib.IsWindowReady())
        {
            return;
        }

        DrawCurtainFade(frame.CurtainAlpha);
        DrawLetterboxBars(frame.LetterboxHeight, frame.BannerAlpha);
        DrawOperationCard(context.StateManager.SelectedLevel, frame);
    }

    public static MissionIntroFrame EvaluateFrame(float elapsedSeconds)
    {
        if (elapsedSeconds < 0f || elapsedSeconds >= TotalDurationSeconds)
        {
            return new MissionIntroFrame(IsActive: false, CurtainAlpha: 0f, BannerAlpha: 0f, DividerProgress: 0f, LetterboxHeight: 0f);
        }

        float curtainT = Math.Clamp(elapsedSeconds / CurtainFadeDurationSeconds, 0f, 1f);
        float curtainAlpha = 1f - EaseOutCubic(curtainT);

        float bannerAlpha = ComputeBannerAlpha(elapsedSeconds);
        float dividerT = Math.Clamp((elapsedSeconds - BannerFadeInStartSeconds) / DividerExpandDurationSeconds, 0f, 1f);
        float dividerProgress = EaseOutCubic(dividerT);
        float letterboxHeight = MaxLetterboxHeight * bannerAlpha;

        return new MissionIntroFrame(IsActive: true, curtainAlpha, bannerAlpha, dividerProgress, letterboxHeight);
    }

    private static float ComputeBannerAlpha(float elapsedSeconds)
    {
        if (elapsedSeconds < BannerFadeInStartSeconds)
        {
            return 0f;
        }

        if (elapsedSeconds < BannerFadeInEndSeconds)
        {
            float inProgress = (elapsedSeconds - BannerFadeInStartSeconds) / (BannerFadeInEndSeconds - BannerFadeInStartSeconds);
            return EaseOutCubic(Math.Clamp(inProgress, 0f, 1f));
        }

        if (elapsedSeconds <= BannerHoldEndSeconds)
        {
            return 1f;
        }

        float outProgress = (elapsedSeconds - BannerHoldEndSeconds) / (TotalDurationSeconds - BannerHoldEndSeconds);
        return 1f - EaseOutCubic(Math.Clamp(outProgress, 0f, 1f));
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private static void DrawCurtainFade(float alpha)
    {
        if (alpha <= 0f)
        {
            return;
        }

        byte byteAlpha = (byte)(MaxByte * Math.Clamp(alpha, 0f, 1f));
        Color fill = WithAlpha(CurtainRgb, byteAlpha);
        Raylib.DrawRectangle(0, GameWindowChrome.HeaderHeight, Raylib.GetScreenWidth(), Raylib.GetScreenHeight() - GameWindowChrome.HeaderHeight, fill);
    }

    private static void DrawLetterboxBars(float barHeight, float alpha)
    {
        int pixelHeight = (int)MathF.Round(barHeight);
        if (pixelHeight <= 0 || alpha <= 0f)
        {
            return;
        }

        Color barColor = WithAlpha(LetterboxRgb, (byte)(LetterboxMaxAlpha * alpha));
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, GameWindowChrome.HeaderHeight, screenWidth, pixelHeight, barColor);
        Raylib.DrawRectangle(0, screenHeight - pixelHeight, screenWidth, pixelHeight, barColor);
    }

    private static void DrawOperationCard(LevelDescriptor level, MissionIntroFrame frame)
    {
        if (frame.BannerAlpha <= 0f)
        {
            return;
        }

        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int cardX = (screenWidth - CardWidth) / HalfDivisor;
        int cardY = Math.Max(GameWindowChrome.HeaderHeight + (int)MaxLetterboxHeight + CardMinClearance, screenHeight / CardTopScreenDivisor);
        var bounds = new Rectangle(cardX, cardY, CardWidth, CardHeight);

        Color background = WithAlpha(CardBgRgb, (byte)(CardMaxAlpha * frame.BannerAlpha));
        Color border = WithAlpha(CardBorderRgb, (byte)(CardBorderMaxAlpha * frame.BannerAlpha));
        Color kickerColor = WithAlpha(KickerRgb, (byte)(MaxByte * frame.BannerAlpha));
        Color titleColor = WithAlpha(TitleRgb, (byte)(MaxByte * frame.BannerAlpha));
        Color subtitleColor = WithAlpha(SubtitleRgb, (byte)(MaxByte * frame.BannerAlpha));

        UiPanelDrawer.DrawTacticalCard(bounds, background, border);
        UiPanelDrawer.DrawBriefingCorners(bounds, kickerColor);

        const string kicker = "OPERATION // SHADOW HEIST";
        UiTypography.DrawText(kicker, UiTypography.CenterTextX(kicker, cardX, CardWidth, KickerFontSize, bold: true), cardY + KickerOffsetY, KickerFontSize, kickerColor, bold: true);

        string title = level.Title.ToUpperInvariant();
        UiTypography.DrawText(title, UiTypography.CenterTextX(title, cardX, CardWidth, TitleFontSize, bold: true), cardY + TitleOffsetY, TitleFontSize, titleColor, bold: true);

        int dividerWidth = Math.Max(1, (int)(DividerMaxWidth * frame.DividerProgress));
        int dividerX = cardX + ((CardWidth - dividerWidth) / HalfDivisor);
        Raylib.DrawRectangle(dividerX, cardY + DividerOffsetY, dividerWidth, DividerThickness, border);

        string subtitle = level.Subtitle.ToUpperInvariant();
        UiTypography.DrawText(subtitle, UiTypography.CenterTextX(subtitle, cardX, CardWidth, SubtitleFontSize), cardY + SubtitleOffsetY, SubtitleFontSize, subtitleColor);
    }

    private static Color WithAlpha(Color rgb, byte alpha) => new(rgb.R, rgb.G, rgb.B, alpha);
}
