using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Loads cel + outline shaders, articulated rigs, palette-baked models, and dynamic lighting uniforms.
/// </summary>
public sealed unsafe class ToonModelLibrary : IDisposable
{
    private const string ToonVs = "assets/shaders/toon.vs";
    private const string ToonFs = "assets/shaders/toon.fs";
    private const string OutlineVs = "assets/shaders/outline.vs";
    private const string OutlineFs = "assets/shaders/outline.fs";

    private static readonly Vector3 DefaultLightDir = Vector3.Normalize(new Vector3(-0.48f, -1f, -0.52f));
    private static readonly Vector3 DefaultLightColor = new(0.56f, 0.60f, 0.72f);
    private static readonly Vector3 DefaultAmbient = new(0.24f, 0.28f, 0.39f);
    private static readonly Vector3 DefaultRim = new(0.22f, 0.48f, 0.68f);
    private static readonly Vector3 AlarmAmbient = new(0.46f, 0.16f, 0.20f);
    private static readonly Vector3 AlarmRim = new(0.95f, 0.22f, 0.24f);
    private static readonly Vector4 InkOutlineColor = new(0.04f, 0.05f, 0.09f, 1f);

    private readonly List<Model> _models = new();
    private readonly List<Texture2D> _textures = new();
    private Shader _shader;
    private Shader _outlineShader;
    private int _outlineWidthLoc = -1;

    public bool ShaderLoaded { get; private set; }
    public bool OutlineLoaded { get; private set; }

    public void LoadShader()
    {
        if (File.Exists(ToonVs) && File.Exists(ToonFs))
        {
            _shader = Raylib.LoadShader(ToonVs, ToonFs);
            ShaderLoaded = _shader.Id > 0;
            if (ShaderLoaded)
            {
                SetVec3("lightDirection", DefaultLightDir);
                SetVec3("lightColor", DefaultLightColor);
                SetVec3("lampPos0", new Vector3(84f, 64f, 345f));
                SetVec3("lampPos1", new Vector3(896f, 64f, 632f));
            }
        }

        if (!File.Exists(OutlineVs) || !File.Exists(OutlineFs)) return;
        _outlineShader = Raylib.LoadShader(OutlineVs, OutlineFs);
        OutlineLoaded = _outlineShader.Id > 0;
        if (!OutlineLoaded) return;
        _outlineWidthLoc = Raylib.GetShaderLocation(_outlineShader, "outlineWidth");
        Raylib.SetShaderValue(_outlineShader, Raylib.GetShaderLocation(_outlineShader, "outlineColor"), InkOutlineColor, ShaderUniformDataType.Vec4);
    }

    public void UpdateFrameUniforms(Vector3 cameraPos, GameContext context, IReadOnlyList<Guard> guards, float time)
    {
        if (!ShaderLoaded) return;
        float pulse = context.IsAlarmTriggered ? 0.5f + 0.5f * MathF.Sin(time * 8f) : 0f;
        SetVec3("viewPos", cameraPos);
        SetVec3("ambientColor", Vector3.Lerp(DefaultAmbient, AlarmAmbient, pulse));
        SetVec3("rimColor", Vector3.Lerp(DefaultRim, AlarmRim, pulse));
        SetVec3("playerPos", new Vector3(context.PlayerPosition.X, 36f, context.PlayerPosition.Y));
        Raylib.SetShaderValue(_shader, Raylib.GetShaderLocation(_shader, "alarmActive"), context.IsAlarmTriggered ? 1f : 0f, ShaderUniformDataType.Float);
        PushGuardUniforms(guards);
    }

    public LoadedModel? LoadProp(string objPath, float targetMaxExtent)
    {
        if (!File.Exists(objPath)) return null;
        Model model = Raylib.LoadModel(objPath);
        if (model.MeshCount == 0) return null;
        for (int i = 0; i < model.MaterialCount; i++) if (ShaderLoaded) model.Materials[i].Shader = _shader;
        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 size = bounds.Max - bounds.Min;
        float scale = targetMaxExtent / MathF.Max(MathF.Max(size.X, MathF.Max(size.Y, size.Z)), 0.0001f);
        _models.Add(model);
        return new LoadedModel(model, scale, -bounds.Min.Y * scale);
    }

    public CharacterRig? LoadCharacterRig(string directory, string skinPath, float uniformScale)
    {
        if (!File.Exists(skinPath)) return null;
        Texture2D skin = Raylib.LoadTexture(skinPath);
        _textures.Add(skin);
        Model? b = LoadPart(Path.Combine(directory, "part_body.obj"), skin);
        Model? h = LoadPart(Path.Combine(directory, "part_head.obj"), skin);
        Model? al = LoadPart(Path.Combine(directory, "part_arm_left.obj"), skin);
        Model? ar = LoadPart(Path.Combine(directory, "part_arm_right.obj"), skin);
        Model? ll = LoadPart(Path.Combine(directory, "part_leg_left.obj"), skin);
        Model? lr = LoadPart(Path.Combine(directory, "part_leg_right.obj"), skin);
        return (b is null || h is null || al is null || ar is null || ll is null || lr is null)
            ? null
            : new CharacterRig(b.Value, h.Value, al.Value, ar.Value, ll.Value, lr.Value, uniformScale);
    }

    public Model? LoadTexturedBox(string texturePath, float width, float height, float length)
    {
        if (!File.Exists(texturePath)) return null;
        Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(width, height, length));
        Texture2D tex = Raylib.LoadTexture(texturePath);
        _textures.Add(tex);
        ApplyMaterial(model, tex);
        _models.Add(model);
        return model;
    }

    public void DrawOutlined(Model model, Vector3 pos, float rotDeg, float scale, Color tint, float outlineWidth)
    {
        if (OutlineLoaded && ShaderLoaded)
        {
            Raylib.SetShaderValue(_outlineShader, _outlineWidthLoc, outlineWidth, ShaderUniformDataType.Float);
            for (int i = 0; i < model.MaterialCount; i++) model.Materials[i].Shader = _outlineShader;
            Rlgl.SetCullFace(0);
            Raylib.DrawModelEx(model, pos, Vector3.UnitY, rotDeg, new Vector3(scale), Color.Black);
            Rlgl.SetCullFace(1);
            for (int i = 0; i < model.MaterialCount; i++) model.Materials[i].Shader = _shader;
        }

        Raylib.DrawModelEx(model, pos, Vector3.UnitY, rotDeg, new Vector3(scale), tint);
    }

    public void Dispose()
    {
        foreach (Model m in _models) Raylib.UnloadModel(m);
        foreach (Texture2D t in _textures) Raylib.UnloadTexture(t);
        if (ShaderLoaded) Raylib.UnloadShader(_shader);
        if (OutlineLoaded) Raylib.UnloadShader(_outlineShader);
        ShaderLoaded = OutlineLoaded = false;
        _models.Clear();
        _textures.Clear();
    }

    private void PushGuardUniforms(IReadOnlyList<Guard> guards)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 pos = i < guards.Count ? new Vector3(guards[i].Position.X, 36f, guards[i].Position.Y) : new Vector3(-9999f, 0f, -9999f);
            float rad = i < guards.Count ? guards[i].RotationDegrees * Raylib.DEG2RAD : 0f;
            SetVec3($"guardPos{i}", pos);
            SetVec3($"guardDir{i}", new Vector3(MathF.Cos(rad), 0f, MathF.Sin(rad)));
        }
    }

    private Model? LoadPart(string path, Texture2D skin)
    {
        if (!File.Exists(path)) return null;
        Model model = Raylib.LoadModel(path);
        if (model.MeshCount == 0) return null;
        ApplyMaterial(model, skin);
        _models.Add(model);
        return model;
    }

    private void ApplyMaterial(Model model, Texture2D tex)
    {
        if (ShaderLoaded) model.Materials[0].Shader = _shader;
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = tex;
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Color = Color.White;
    }

    private void SetVec3(string name, Vector3 v)
    {
        int loc = Raylib.GetShaderLocation(_shader, name);
        if (loc >= 0) Raylib.SetShaderValue(_shader, loc, v, ShaderUniformDataType.Vec3);
    }
}
