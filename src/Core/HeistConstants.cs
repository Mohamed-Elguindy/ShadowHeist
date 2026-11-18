namespace ShadowHeist.Core;

/// <summary>
/// Shared mission constants and angle conversion multipliers used across game modules.
/// </summary>
public static class HeistConstants
{
    public const int TargetCashGoal = 500_000;
    public const float DegToRad = MathF.PI / 180f;
    public const float RadToDeg = 180f / MathF.PI;
    public const string DefaultMapPath = "assets/maps/level1_city_bank.json";
    public const string PenthouseMapPath = "assets/maps/level2_penthouse.json";

    public static readonly RectangleBounds DefaultGetawayZone = new(64f, 640f, 128f, 96f);
}
