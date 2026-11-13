using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders bank storytelling props: getaway van, teller security glass counter, CCTV cameras, fusebox, wall art.
/// </summary>
public sealed class BankDecorRenderer
{
    private const string VanModelPath = "assets/models/props/getaway_van.obj";
    private const float VanLength = 148f;

    private static readonly Vector2 VanPosition = new(134f, 804f);
    private static readonly Vector2 TellerPos = new(560f, 540f);
    private static readonly Color AsphaltColor = new(22, 26, 34, 255);
    private static readonly Color WalnutColor = new(126, 78, 48, 255);
    private static readonly Color MarbleTopColor = new(184, 194, 208, 255);
    private static readonly Color BulletGlassColor = new(96, 210, 245, 78);
    private static readonly Color SteelDark = new(44, 52, 66, 255);

    private ToonModelLibrary? _library;
    private LoadedModel? _van;

    public void Load(ToonModelLibrary library)
    {
        _library = library;
        _van = library.LoadProp(VanModelPath, VanLength);
    }

    public void Draw(float timeSeconds)
    {
        DrawGetawayVan(timeSeconds);
        DrawTellerStation(timeSeconds);
        DrawWallSecurityCameras(timeSeconds);
        DrawWallCrestAndFusebox(timeSeconds);
    }

    private void DrawGetawayVan(float timeSeconds)
    {
        Raylib.DrawCube(new Vector3(VanPosition.X + 8f, -1.5f, VanPosition.Y - 4f), 220f, 3f, 130f, AsphaltColor);
        Raylib.DrawCube(new Vector3(VanPosition.X + 8f, 0.2f, VanPosition.Y - 64f), 200f, 0.5f, 3f, new Color(246, 196, 54, 150));

        if (_van is LoadedModel van && _library is not null)
        {
            Vector3 pos = new(VanPosition.X, van.BaseOffsetY, VanPosition.Y);
            _library.DrawOutlined(van.Model, pos, 14f, van.Scale, Color.White, 0.045f);
            byte blink = (byte)(150 + 90 * MathF.Sin(timeSeconds * 4f));
            Raylib.DrawTriangle3D(pos + new Vector3(72f, 4f, -14f), pos + new Vector3(140f, 1f, -34f), pos + new Vector3(140f, 1f, 8f), new Color((byte)255, (byte)242, (byte)168, (byte)36));
            Raylib.DrawSphere(pos + new Vector3(-66f, 24f, -20f), 4f, new Color((byte)244, (byte)63, (byte)94, blink));
            Raylib.DrawSphere(pos + new Vector3(-66f, 24f, 12f), 4f, new Color((byte)244, (byte)63, (byte)94, blink));
        }
    }

    private static void DrawTellerStation(float timeSeconds)
    {
        Vector3 p = new(TellerPos.X, 0f, TellerPos.Y);
        Raylib.DrawCylinder(p + new Vector3(0f, 0.5f, 0f), 68f, 68f, 0.8f, 12, new Color(0, 0, 0, 95));
        Raylib.DrawCube(p + new Vector3(0f, 18f, 0f), 124f, 36f, 40f, WalnutColor);
        Raylib.DrawCubeWires(p + new Vector3(0f, 18f, 0f), 124f, 36f, 40f, Viewport3DConfig.OutlineColor);
        Raylib.DrawCube(p + new Vector3(0f, 37.5f, 0f), 130f, 4f, 44f, MarbleTopColor);

        for (int i = -1; i <= 1; i += 2)
        {
            Raylib.DrawCube(p + new Vector3(i * 58f, 54f, -14f), 5f, 30f, 5f, SteelDark);
        }

        Raylib.DrawCube(p + new Vector3(0f, 54f, -14f), 112f, 26f, 3f, BulletGlassColor);
        Raylib.DrawCubeWires(p + new Vector3(0f, 54f, -14f), 112f, 26f, 3f, new Color(140, 232, 255, 190));

        byte glow = (byte)(180 + 60 * MathF.Sin(timeSeconds * 3f));
        Raylib.DrawCube(p + new Vector3(-24f, 46f, 2f), 18f, 13f, 3f, new Color((byte)30, (byte)36, (byte)48, (byte)255));
        Raylib.DrawCube(p + new Vector3(-24f, 46f, 4f), 14f, 9f, 1f, new Color((byte)56, (byte)225, (byte)255, glow));
    }

    private static void DrawWallSecurityCameras(float timeSeconds)
    {
        DrawSingleCamera(new Vector3(356f, 84f, 200f), 32f + MathF.Sin(timeSeconds * 1.3f) * 26f, timeSeconds);
        DrawSingleCamera(new Vector3(956f, 84f, 200f), 148f + MathF.Cos(timeSeconds * 1.1f) * 26f, timeSeconds);
    }

    private static void DrawSingleCamera(Vector3 mount, float yawDeg, float timeSeconds)
    {
        Raylib.DrawCube(mount, 8f, 8f, 8f, SteelDark);
        float rad = yawDeg * Raylib.DEG2RAD;
        Vector3 dir = new(MathF.Cos(rad), -0.36f, MathF.Sin(rad));
        Raylib.DrawCylinderEx(mount, mount + dir * 16f, 4.5f, 3.5f, 8, new Color(210, 220, 234, 255));
        byte rec = (byte)(120 + 135 * MathF.Abs(MathF.Sin(timeSeconds * 4f)));
        Raylib.DrawSphere(mount + dir * 17f, 2.4f, new Color((byte)255, (byte)55, (byte)75, rec));
        Raylib.DrawLine3D(mount + dir * 17f, new Vector3(mount.X + dir.X * 145f, 2f, mount.Z + dir.Z * 145f), new Color(255, 70, 90, 42));
    }

    private static void DrawWallCrestAndFusebox(float timeSeconds)
    {
        // Vault Fusebox mounted cleanly on the vault south wall near (384, 196)
        Vector3 fusePos = new(410f, 52f, 196f);
        Raylib.DrawCube(fusePos, 24f, 30f, 8f, new Color(46, 56, 72, 255));
        Raylib.DrawCubeWires(fusePos, 24f, 30f, 8f, Viewport3DConfig.OutlineColor);
        Raylib.DrawCube(fusePos + new Vector3(0f, 0f, 4.5f), 16f, 4f, 1f, new Color(248, 196, 44, 255));
        byte led = (byte)(140 + 115 * MathF.Sin(timeSeconds * 6f));
        Raylib.DrawSphere(fusePos + new Vector3(6f, 9f, 5f), 2.3f, new Color((byte)74, (byte)222, (byte)128, led));

        // Bank architectural accent plaque along the north concrete wall
        Vector3 plaquePos = new(640f, 60f, 35f);
        Raylib.DrawCube(plaquePos, 190f, 34f, 5f, new Color(28, 36, 52, 255));
        Raylib.DrawCubeWires(plaquePos, 190f, 34f, 5f, new Color(212, 162, 68, 255));
        Raylib.DrawCube(plaquePos + new Vector3(0f, 0f, 3f), 164f, 6f, 2f, new Color(248, 196, 44, 220));
    }
}
