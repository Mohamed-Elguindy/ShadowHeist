namespace ShadowHeist.Core;

/// <summary>
/// Immutable metadata describing a playable heist level entry for Level Select.
/// </summary>
public sealed record LevelDescriptor(
    string Id,
    string Title,
    string Subtitle,
    string MapPath)
{
    public bool IsMapFilePresent => File.Exists(MapPath);
}

/// <summary>
/// Central catalog of heist levels supported by the Level Select screen.
/// </summary>
public static class LevelCatalog
{
    public static IReadOnlyList<LevelDescriptor> AllLevels { get; } =
    [
        new LevelDescriptor(
            Id: "level1_city_bank",
            Title: "Level 1: City Central Bank",
            Subtitle: "Standard Vault & Laser Grid — $500,000 Target",
            MapPath: HeistConstants.DefaultMapPath),
        new LevelDescriptor(
            Id: "level2_penthouse",
            Title: "Level 2: High-Rise Penthouse",
            Subtitle: "Multi-Room Security Sweep (Uses Level 1 Until Map Added)",
            MapPath: HeistConstants.PenthouseMapPath)
    ];
}
