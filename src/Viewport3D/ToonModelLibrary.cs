using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Loads cel-shading shaders, articulated character rigs, palette-baked furniture, and textured boxes.
/// </summary>
public sealed unsafe class ToonModelLibrary : IDisposable
{
    private const string VertexShaderPath = "assets/shaders/toon.vs";
    private const string FragmentShaderPath = "assets/shaders/toon.fs";

    private static readonly Vector3 DefaultLightDir = Vector3.Normalize(new Vector3(-0.48f, -1f, -0.52f));
    private static readonly Vector3 DefaultLightColor = new(0.92f, 0.88f, 0.82f);
    private static readonly Vector3 DefaultAmbient = new(0.38f, 0.43f, 0.56f);
    private static readonly Vector3 DefaultRim = new(0.24f, 0.52f, 0.72f);
    private static readonly Vector3 AlarmAmbient = new(0.54f, 0.24f, 0.28f);
    private static readonly Vector3 AlarmRim = new(0.90f, 0.20f, 0.22f);

    private readonly List<Model> _models = new();
    private readonly List<Texture2D> _textures = new();
    private Shader _shader;
    private int _viewPosLoc = -1;
    private int _ambientLoc = -1;
    private int _rimLoc = -1;

    public bool ShaderLoaded { get; private set; }

    public void LoadShader()
    {
        if (!File.Exists(VertexShaderPath) || !File.Exists(FragmentShaderPath)) return;
        _shader = Raylib.LoadShader(VertexShaderPath, FragmentShaderPath);
        ShaderLoaded = _shader.Id > 0;
        if (!ShaderLoaded) return;

        _viewPosLoc = Raylib.GetShaderLocation(_shader, "viewPos");
        _ambientLoc = Raylib.GetShaderLocation(_shader, "ambientColor");
        _rimLoc = Raylib.GetShaderLocation(_shader, "rimColor");
        SetVec3(Raylib.GetShaderLocation(_shader, "lightDirection"), DefaultLightDir);
        SetVec3(Raylib.GetShaderLocation(_shader, "lightColor"), DefaultLightColor);
        SetVec3(_ambientLoc, DefaultAmbient);
        SetVec3(_rimLoc, DefaultRim);
    }

    public void UpdateFrameUniforms(Vector3 cameraPosition, bool isAlarmTriggered, float timeSeconds)
    {
        if (!ShaderLoaded) return;
        SetVec3(_viewPosLoc, cameraPosition);
        float pulse = isAlarmTriggered ? 0.5f + 0.5f * MathF.Sin(timeSeconds * 8f) : 0f;
        SetVec3(_ambientLoc, Vector3.Lerp(DefaultAmbient, AlarmAmbient, pulse));
        SetVec3(_rimLoc, Vector3.Lerp(DefaultRim, AlarmRim, pulse));
    }

    public LoadedModel? LoadProp(string objPath, float targetMaxExtent)
    {
        if (!File.Exists(objPath)) return null;
        Model model = Raylib.LoadModel(objPath);
        if (model.MeshCount == 0) return null;

        for (int i = 0; i < model.MaterialCount; i++)
        {
            if (ShaderLoaded) model.Materials[i].Shader = _shader;
        }

        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 size = bounds.Max - bounds.Min;
        float maxExtent = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        float scale = targetMaxExtent / MathF.Max(maxExtent, 0.0001f);
        _models.Add(model);
        return new LoadedModel(model, scale, -bounds.Min.Y * scale);
    }

    public CharacterRig? LoadCharacterRig(string directory, string skinPath, float uniformScale)
    {
        if (!File.Exists(skinPath)) return null;
        Texture2D skin = Raylib.LoadTexture(skinPath);
        _textures.Add(skin);

        Model? body = LoadRigPart(Path.Combine(directory, "part_body.obj"), skin);
        Model? head = LoadRigPart(Path.Combine(directory, "part_head.obj"), skin);
        Model? armL = LoadRigPart(Path.Combine(directory, "part_arm_left.obj"), skin);
        Model? armR = LoadRigPart(Path.Combine(directory, "part_arm_right.obj"), skin);
        Model? legL = LoadRigPart(Path.Combine(directory, "part_leg_left.obj"), skin);
        Model? legR = LoadRigPart(Path.Combine(directory, "part_leg_right.obj"), skin);
        if (body is null || head is null || armL is null || armR is null || legL is null || legR is null) return null;

        return new CharacterRig(body.Value, head.Value, armL.Value, armR.Value, legL.Value, legR.Value, uniformScale);
    }

    public Model? LoadTexturedBox(string texturePath, float width, float height, float length)
    {
        if (!File.Exists(texturePath)) return null;
        Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(width, height, length));
        Texture2D texture = Raylib.LoadTexture(texturePath);
        _textures.Add(texture);
        ApplyMaterial(model, texture);
        _models.Add(model);
        return model;
    }

    public void Dispose()
    {
        foreach (Model model in _models) Raylib.UnloadModel(model);
        foreach (Texture2D texture in _textures) Raylib.UnloadTexture(texture);
        if (ShaderLoaded) Raylib.UnloadShader(_shader);
        ShaderLoaded = false;
        _models.Clear();
        _textures.Clear();
    }

    private Model? LoadRigPart(string objPath, Texture2D sharedSkin)
    {
        if (!File.Exists(objPath)) return null;
        Model model = Raylib.LoadModel(objPath);
        if (model.MeshCount == 0) return null;
        ApplyMaterial(model, sharedSkin);
        _models.Add(model);
        return model;
    }

    private void ApplyMaterial(Model model, Texture2D texture)
    {
        if (ShaderLoaded) model.Materials[0].Shader = _shader;
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = texture;
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Color = Color.White;
    }

    private void SetVec3(int location, Vector3 value)
    {
        if (location >= 0) Raylib.SetShaderValue(_shader, location, value, ShaderUniformDataType.Vec3);
    }
}
