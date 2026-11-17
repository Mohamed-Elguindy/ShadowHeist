using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Renders 3D props and inverted-hull cel-shaded outlines using the active shader pair.
/// </summary>
internal static class ToonOutlineDrawer
{
    public static void DrawCenteredProp(
        ToonShaderUniforms shaders,
        LoadedModel prop,
        Vector3 centerPosition,
        float rotationDegrees,
        Color tint,
        float outlineWidth)
    {
        float radians = rotationDegrees * HeistConstants.DegToRad;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        float rotatedX = (prop.CenterOffsetXZ.X * cos) + (prop.CenterOffsetXZ.Y * sin);
        float rotatedZ = (-prop.CenterOffsetXZ.X * sin) + (prop.CenterOffsetXZ.Y * cos);

        var groundedOrigin = new Vector3(centerPosition.X - rotatedX, centerPosition.Y, centerPosition.Z - rotatedZ);
        DrawOutlined(shaders, prop.Model, groundedOrigin, rotationDegrees, prop.Scale, tint, outlineWidth);
    }

    public static void DrawOutlined(
        ToonShaderUniforms shaders,
        Model model,
        Vector3 position,
        float rotationDegrees,
        float scale,
        Color tint,
        float outlineWidth)
    {
        if (shaders.IsOutlineLoaded && shaders.IsToonLoaded && outlineWidth > 0f)
        {
            DrawInvertedHullOutline(shaders, model, position, rotationDegrees, scale, outlineWidth);
        }

        Raylib.DrawModelEx(model, position, Vector3.UnitY, rotationDegrees, new Vector3(scale), tint);
    }

    public static unsafe void SetModelShader(Model model, Shader shader)
    {
        for (int i = 0; i < model.MaterialCount; i++)
        {
            model.Materials[i].Shader = shader;
        }
    }

    private static void DrawInvertedHullOutline(
        ToonShaderUniforms shaders,
        Model model,
        Vector3 position,
        float rotationDegrees,
        float scale,
        float outlineWidth)
    {
        shaders.SetOutlineWidth(outlineWidth);
        SetModelShader(model, shaders.OutlineShader);
        Rlgl.SetCullFace(0);
        Raylib.DrawModelEx(model, position, Vector3.UnitY, rotationDegrees, new Vector3(scale), Color.Black);
        Rlgl.SetCullFace(1);
        SetModelShader(model, shaders.ToonShader);
    }
}
