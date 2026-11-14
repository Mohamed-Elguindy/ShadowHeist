using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Manages GPU model, texture, and character rig resources with centered bounding-box alignment and comic outlines.
/// </summary>
public sealed unsafe class ToonModelLibrary : IDisposable
{
    private readonly ToonShaderUniforms _shaders = new();
    private readonly List<Model> _models = new();
    private readonly List<Texture2D> _textures = new();

    public void LoadShader() => _shaders.Load();

    public void UpdateFrameUniforms(Vector3 cameraPosition, GameContext context, IReadOnlyList<Guard> guards, float timeSeconds) =>
        _shaders.UpdateFrameUniforms(cameraPosition, context, guards, timeSeconds);

    public LoadedModel? LoadProp(string objPath, float targetMaxExtent)
    {
        if (!File.Exists(objPath))
        {
            return null;
        }

        Model model = Raylib.LoadModel(objPath);
        if (model.MeshCount == 0)
        {
            return null;
        }

        ApplyToonShaderToMaterials(model);
        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 size = bounds.Max - bounds.Min;
        float maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        float scale = targetMaxExtent / MathF.Max(maxDimension, 0.0001f);
        var centerOffsetXZ = new Vector2(
            (bounds.Min.X + bounds.Max.X) * 0.5f * scale,
            (bounds.Min.Z + bounds.Max.Z) * 0.5f * scale);

        _models.Add(model);
        return new LoadedModel(model, scale, -bounds.Min.Y * scale, centerOffsetXZ);
    }

    public void DrawCenteredProp(LoadedModel prop, Vector3 centerPosition, float rotationDegrees, Color tint, float outlineWidth)
    {
        float radians = rotationDegrees * HeistConstants.DegToRad;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        float rotatedX = (prop.CenterOffsetXZ.X * cos) + (prop.CenterOffsetXZ.Y * sin);
        float rotatedZ = (-prop.CenterOffsetXZ.X * sin) + (prop.CenterOffsetXZ.Y * cos);

        var groundedOrigin = new Vector3(centerPosition.X - rotatedX, centerPosition.Y, centerPosition.Z - rotatedZ);
        DrawOutlined(prop.Model, groundedOrigin, rotationDegrees, prop.Scale, tint, outlineWidth);
    }

    public CharacterRig? LoadCharacterRig(string directory, string skinPath, float uniformScale)
    {
        if (!File.Exists(skinPath))
        {
            return null;
        }

        Texture2D skin = Raylib.LoadTexture(skinPath);
        _textures.Add(skin);

        Model? body = LoadCharacterPart(Path.Combine(directory, "part_body.obj"), skin);
        Model? head = LoadCharacterPart(Path.Combine(directory, "part_head.obj"), skin);
        Model? armLeft = LoadCharacterPart(Path.Combine(directory, "part_arm_left.obj"), skin);
        Model? armRight = LoadCharacterPart(Path.Combine(directory, "part_arm_right.obj"), skin);
        Model? legLeft = LoadCharacterPart(Path.Combine(directory, "part_leg_left.obj"), skin);
        Model? legRight = LoadCharacterPart(Path.Combine(directory, "part_leg_right.obj"), skin);

        if (body is null || head is null || armLeft is null || armRight is null || legLeft is null || legRight is null)
        {
            return null;
        }

        return new CharacterRig(
            body.Value,
            head.Value,
            armLeft.Value,
            armRight.Value,
            legLeft.Value,
            legRight.Value,
            uniformScale);
    }

    public Model? LoadTexturedBox(string texturePath, float width, float height, float length)
    {
        if (!File.Exists(texturePath))
        {
            return null;
        }

        Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(width, height, length));
        Texture2D texture = Raylib.LoadTexture(texturePath);
        _textures.Add(texture);
        ApplyTexturedMaterial(model, texture);
        _models.Add(model);
        return model;
    }

    public void DrawOutlined(Model model, Vector3 position, float rotationDegrees, float scale, Color tint, float outlineWidth)
    {
        if (_shaders.IsOutlineLoaded && _shaders.IsToonLoaded && outlineWidth > 0f)
        {
            DrawInvertedHullOutline(model, position, rotationDegrees, scale, outlineWidth);
        }

        Raylib.DrawModelEx(model, position, Vector3.UnitY, rotationDegrees, new Vector3(scale), tint);
    }

    public void Dispose()
    {
        foreach (Model model in _models)
        {
            Raylib.UnloadModel(model);
        }

        foreach (Texture2D texture in _textures)
        {
            Raylib.UnloadTexture(texture);
        }

        _shaders.Dispose();
        _models.Clear();
        _textures.Clear();
    }

    private void DrawInvertedHullOutline(Model model, Vector3 position, float rotationDegrees, float scale, float outlineWidth)
    {
        _shaders.SetOutlineWidth(outlineWidth);
        SetModelShader(model, _shaders.OutlineShader);
        Rlgl.SetCullFace(0);
        Raylib.DrawModelEx(model, position, Vector3.UnitY, rotationDegrees, new Vector3(scale), Color.Black);
        Rlgl.SetCullFace(1);
        SetModelShader(model, _shaders.ToonShader);
    }

    private void ApplyToonShaderToMaterials(Model model)
    {
        if (!_shaders.IsToonLoaded)
        {
            return;
        }

        SetModelShader(model, _shaders.ToonShader);
    }

    private Model? LoadCharacterPart(string partPath, Texture2D skin)
    {
        if (!File.Exists(partPath))
        {
            return null;
        }

        Model model = Raylib.LoadModel(partPath);
        if (model.MeshCount == 0)
        {
            return null;
        }

        ApplyTexturedMaterial(model, skin);
        _models.Add(model);
        return model;
    }

    private void ApplyTexturedMaterial(Model model, Texture2D texture)
    {
        if (_shaders.IsToonLoaded)
        {
            model.Materials[0].Shader = _shaders.ToonShader;
        }

        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = texture;
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Color = Color.White;
    }

    private static void SetModelShader(Model model, Shader shader)
    {
        for (int i = 0; i < model.MaterialCount; i++)
        {
            model.Materials[i].Shader = shader;
        }
    }
}
