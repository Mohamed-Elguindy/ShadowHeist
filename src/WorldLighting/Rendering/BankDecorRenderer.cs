using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders architectural storytelling props: getaway van, teller security counter, CCTV cameras, vault fusebox, and bank plaque.
/// </summary>
public sealed class BankDecorRenderer
{
    private const string VanModelPath = "assets/models/props/getaway_van.obj";
    private const float VanLength = 148f;
    private const float VanYawDegrees = 14f;
    private const float VanOutlineWidth = 0.045f;

    private static readonly Vector2 VanPosition = new(134f, 828f);
    private static readonly Vector2 TellerPosition = new(560f, 536f);
    private static readonly Vector3 PlaquePosition = new(640f, 60f, 35f);

    private static readonly Color AsphaltColor = new(22, 26, 34, 255);
    private static readonly Color WalnutColor = new(126, 78, 48, 255);
    private static readonly Color MarbleTopColor = new(184, 194, 208, 255);
    private static readonly Color BulletGlassColor = new(96, 210, 245, 78);
    private static readonly Color BulletGlassOutlineColor = new(140, 232, 255, 190);
    private static readonly Color SteelDarkColor = new(44, 52, 66, 255);
    private static readonly Color MonitorHousingColor = new(30, 36, 48, 255);
    private static readonly Color PlaqueBorderColor = new(212, 162, 68, 255);

    private ToonModelLibrary? _library;
    private LoadedModel? _vanModel;

    public void Load(ToonModelLibrary library)
    {
        _library = library;
        _vanModel = library.LoadProp(VanModelPath, VanLength);
    }

    public void Draw(float elapsedSeconds)
    {
        DrawVanParkingBay();
        DrawGetawayVanModel(elapsedSeconds);
        DrawTellerStation(elapsedSeconds);
        DrawSecurityCameras(elapsedSeconds);
        DrawBankPlaque();
    }

    private static void DrawVanParkingBay()
    {
        Raylib.DrawCube(new Vector3(VanPosition.X + 8f, -1.5f, VanPosition.Y), 220f, 3f, 112f, AsphaltColor);
        Raylib.DrawCube(new Vector3(VanPosition.X + 8f, 0.2f, VanPosition.Y - 54f), 200f, 0.5f, 3f, new Color(246, 196, 54, 150));
    }

    private void DrawGetawayVanModel(float timeSeconds)
    {
        if (_vanModel is not LoadedModel van || _library is null)
        {
            return;
        }

        var origin = new Vector3(VanPosition.X, van.BaseOffsetY, VanPosition.Y);
        _library.DrawCenteredProp(van, origin, VanYawDegrees, Color.White, VanOutlineWidth);
        DrawVanLightsAndHazards(origin, timeSeconds);
    }

    private static void DrawVanLightsAndHazards(Vector3 origin, float timeSeconds)
    {
        Raylib.DrawTriangle3D(
            origin + new Vector3(72f, 4f, -14f),
            origin + new Vector3(140f, 1f, -34f),
            origin + new Vector3(140f, 1f, 8f),
            new Color((byte)255, (byte)242, (byte)168, (byte)36));

        byte blinkerAlpha = (byte)(150f + 90f * MathF.Sin(timeSeconds * 4f));
        var hazardRed = new Color((byte)244, (byte)63, (byte)94, blinkerAlpha);
        Raylib.DrawSphere(origin + new Vector3(-66f, 24f, -20f), 4f, hazardRed);
        Raylib.DrawSphere(origin + new Vector3(-66f, 24f, 12f), 4f, hazardRed);
    }

    private static void DrawTellerStation(float timeSeconds)
    {
        var baseCenter = new Vector3(TellerPosition.X, 0f, TellerPosition.Y);
        Raylib3DPrimitives.DrawGroundShadow(TellerPosition, 68f);

        Raylib3DPrimitives.DrawOutlinedCube(baseCenter + new Vector3(0f, 18f, 0f), new Vector3(124f, 36f, 40f), WalnutColor, Viewport3DConfig.OutlineColor);
        Raylib.DrawCube(baseCenter + new Vector3(0f, 37.5f, 0f), 130f, 4f, 44f, MarbleTopColor);

        DrawTellerGlassShield(baseCenter);
        DrawTellerMonitor(baseCenter, timeSeconds);
    }

    private static void DrawTellerGlassShield(Vector3 baseCenter)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Raylib.DrawCube(baseCenter + new Vector3(side * 58f, 54f, -14f), 5f, 30f, 5f, SteelDarkColor);
        }

        Raylib3DPrimitives.DrawOutlinedCube(
            baseCenter + new Vector3(0f, 54f, -14f),
            new Vector3(112f, 26f, 3f),
            BulletGlassColor,
            BulletGlassOutlineColor);
    }

    private static void DrawTellerMonitor(Vector3 baseCenter, float timeSeconds)
    {
        byte screenAlpha = (byte)(180f + 60f * MathF.Sin(timeSeconds * 3f));
        Raylib.DrawCube(baseCenter + new Vector3(-24f, 46f, 2f), 18f, 13f, 3f, MonitorHousingColor);
        Raylib.DrawCube(baseCenter + new Vector3(-24f, 46f, 4f), 14f, 9f, 1f, new Color((byte)56, (byte)225, (byte)255, screenAlpha));
    }

    private static void DrawSecurityCameras(float timeSeconds)
    {
        DrawSingleCamera(new Vector3(364f, 84f, 196f), 32f + MathF.Sin(timeSeconds * 1.3f) * 26f, timeSeconds);
        DrawSingleCamera(new Vector3(916f, 84f, 196f), 148f + MathF.Cos(timeSeconds * 1.1f) * 26f, timeSeconds);
    }

    private static void DrawSingleCamera(Vector3 mountPosition, float yawDegrees, float timeSeconds)
    {
        Raylib.DrawCube(mountPosition, 8f, 8f, 8f, SteelDarkColor);

        float radians = yawDegrees * HeistConstants.DegToRad;
        Vector3 lookDirection = new(MathF.Cos(radians), -0.36f, MathF.Sin(radians));
        Vector3 lensTip = mountPosition + (lookDirection * 17f);

        Raylib.DrawCylinderEx(mountPosition, mountPosition + (lookDirection * 16f), 4.5f, 3.5f, 8, new Color(210, 220, 234, 255));
        byte recordAlpha = (byte)(120f + 135f * MathF.Abs(MathF.Sin(timeSeconds * 4f)));
        Raylib.DrawSphere(lensTip, 2.4f, new Color((byte)255, (byte)55, (byte)75, recordAlpha));
        Raylib.DrawLine3D(
            lensTip,
            new Vector3(mountPosition.X + (lookDirection.X * 145f), 2f, mountPosition.Z + (lookDirection.Z * 145f)),
            new Color(255, 70, 90, 42));
    }

    private static void DrawBankPlaque()
    {
        Raylib3DPrimitives.DrawOutlinedCube(PlaquePosition, new Vector3(190f, 34f, 5f), new Color(28, 36, 52, 255), PlaqueBorderColor);
        Raylib.DrawCube(PlaquePosition + new Vector3(0f, 0f, 3f), 164f, 6f, 2f, new Color(248, 196, 44, 220));
    }
}
