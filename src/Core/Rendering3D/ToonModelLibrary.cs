using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Manages GPU model, texture, and character rig resources with centered bounding-box alignment and safe disposal.
/// </summary>
public sealed unsafe class ToonModelLibrary : IDisposable
{
    private const float MinDimensionEpsilon = 0.0001f;

    private readonly ToonShaderUniforms _shaders = new();
    private readonly List<Model> _models = new();
    private readonly List<Texture2D> _textures = new();
    private readonly HashSet<uint> _ownedTextureIds = new();
    private bool _shaderInitialized;

    public void EnsureLoaded()
    {
        if (_shaderInitialized || !Raylib.IsWindowReady())
        {
            return;
        }

        _shaders.Load();
        _shaderInitialized = true;
    }

    public void UpdateFrameLighting(Vector3 cameraPosition, GameContext context) =>
        _shaders.UpdateFrameLighting(cameraPosition, context);

    public void SetGuardFlashlight(int slotIndex, Vector2 position, float rotationDegrees) =>
        _shaders.SetGuardFlashlight(slotIndex, position, rotationDegrees);

    public void ClearUnusedGuardFlashlights(int activeGuardCount) =>
        _shaders.ClearUnusedGuardFlashlights(activeGuardCount);

    public LoadedModel? LoadProp(string objPath, float targetMaxExtent)
    {
        if (!TryLoadValidModel(objPath, out Model model))
        {
            return null;
        }

        if (_shaders.IsToonLoaded)
        {
            ToonOutlineDrawer.SetModelShader(model, _shaders.ToonShader);
        }

        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 size = bounds.Max - bounds.Min;
        float maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        float scale = targetMaxExtent / MathF.Max(maxDimension, MinDimensionEpsilon);
        var centerOffsetXZ = new Vector2((bounds.Min.X + bounds.Max.X) * 0.5f * scale, (bounds.Min.Z + bounds.Max.Z) * 0.5f * scale);

        _models.Add(model);
        return new LoadedModel(model, scale, -bounds.Min.Y * scale, centerOffsetXZ);
    }

    public CharacterRig? LoadCharacterRig(string directory, string skinPath, float uniformScale)
    {
        if (!File.Exists(skinPath))
        {
            return null;
        }

        Texture2D skin = LoadTrackedTexture(skinPath);
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

        return new CharacterRig(body.Value, head.Value, armLeft.Value, armRight.Value, legLeft.Value, legRight.Value, uniformScale);
    }

    public Model? LoadTexturedBox(string texturePath, float width, float height, float length)
    {
        if (!File.Exists(texturePath))
        {
            return null;
        }

        Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(width, height, length));
        ApplyTexturedMaterial(model, LoadTrackedTexture(texturePath));
        _models.Add(model);
        return model;
    }

    public void DrawCenteredProp(LoadedModel prop, Vector3 centerPosition, float rotationDegrees, Color tint, float outlineWidth) =>
        ToonOutlineDrawer.DrawCenteredProp(_shaders, prop, centerPosition, rotationDegrees, tint, outlineWidth);

    public void DrawOutlined(Model model, Vector3 position, float rotationDegrees, float scale, Color tint, float outlineWidth) =>
        ToonOutlineDrawer.DrawOutlined(_shaders, model, position, rotationDegrees, scale, tint, outlineWidth);

    public void Dispose()
    {
        foreach (Model model in _models)
        {
            DetachOwnedTextures(model);
            Raylib.UnloadModel(model);
        }

        foreach (Texture2D texture in _textures)
        {
            Raylib.UnloadTexture(texture);
        }

        _shaders.Dispose();
        _models.Clear();
        _textures.Clear();
        _ownedTextureIds.Clear();
        _shaderInitialized = false;
    }

    private Texture2D LoadTrackedTexture(string texturePath)
    {
        Texture2D texture = Raylib.LoadTexture(texturePath);
        _textures.Add(texture);
        _ownedTextureIds.Add(texture.Id);
        return texture;
    }

    private Model? LoadCharacterPart(string partPath, Texture2D skin)
    {
        if (!TryLoadValidModel(partPath, out Model model))
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

    private void DetachOwnedTextures(Model model)
    {
        int albedoIndex = (int)MaterialMapIndex.Albedo;
        for (int i = 0; i < model.MaterialCount; i++)
        {
            Texture2D albedoTexture = model.Materials[i].Maps[albedoIndex].Texture;
            if (_ownedTextureIds.Contains(albedoTexture.Id))
            {
                model.Materials[i].Maps[albedoIndex].Texture = default;
            }
        }
    }

    private static bool TryLoadValidModel(string objPath, out Model model)
    {
        model = default;
        if (!File.Exists(objPath))
        {
            return false;
        }

        model = Raylib.LoadModel(objPath);
        if (model.MeshCount > 0)
        {
            return true;
        }

        Raylib.UnloadModel(model);
        model = default;
        return false;
    }
}
