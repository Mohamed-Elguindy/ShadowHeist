using Raylib_cs;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders solid furniture and teller counter footprints in 2D blueprint mode to match 3D colliders.
/// </summary>
public static class MapObstacleRenderer
{
    private static readonly Color SofaColor = new(148, 48, 64, 255);
    private static readonly Color WoodColor = new(166, 108, 66, 255);
    private static readonly Color PlantColor = new(56, 182, 110, 255);
    private static readonly Color LampColor = new(246, 214, 118, 255);
    private static readonly Color CounterColor = new(176, 188, 206, 255);
    private static readonly Color OutlineColor = new(18, 24, 36, 255);

    public static void DrawObstacles(IReadOnlyList<MapPropObstacle>? obstacles)
    {
        if (obstacles is null) return;

        foreach (MapPropObstacle prop in obstacles)
        {
            Color fill = SelectColor(prop.Kind);
            int x = (int)prop.X;
            int y = (int)prop.Y;
            int width = (int)prop.Width;
            int height = (int)prop.Height;

            Raylib.DrawRectangle(x, y, width, height, fill);
            Raylib.DrawRectangleLines(x, y, width, height, OutlineColor);
        }
    }

    private static Color SelectColor(string kind) => kind switch
    {
        "sofa" or "chair" => SofaColor,
        "plant" => PlantColor,
        "lamp" => LampColor,
        "counter" => CounterColor,
        _ => WoodColor
    };
}
