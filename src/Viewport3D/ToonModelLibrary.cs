using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Loads cel + outline shaders, articulated rigs, centered props, and cached uniform locations.
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
    private int _viewPosLoc = -1;
    private int _ambientLoc = -1;
    private int _rimLoc = -1;
    private int _playerPosLoc = -1;
    private int _alarmLoc = -1;
    private readonly int[] _guardPosLocs = { -1, -1 };
    private readonly int[] _guardDirLocs = { -1, -1 };

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
                CacheShaderLocations();
                PushStaticLights();
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
        SetVec3Loc(_viewPosLoc, cameraPos);
        SetVec3Loc(_ambientLoc, Vector3.Lerp(DefaultAmbient, AlarmAmbient, pulse));
        SetVec3Loc(_rimLoc, Vector3.Lerp(DefaultRim, AlarmRim, pulse));
        SetVec3Loc(_playerPosLoc, new Vector3(context.PlayerPosition.X, 36f, context.PlayerPosition.Y));
        if (_alarmLoc >= 0) Raylib.SetShaderValue(_shader, _alarmLoc, context.IsAlarmTriggered ? 1f : 0f, ShaderUniformDataType.Float);
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
        Vector2 centerXZ = new((bounds.Min.X + bounds.Max.X) * 0.5f * scale, (bounds.Min.Z + bounds.Max.Z) * 0.5f * scale);
        _models.Add(model);
        return new LoadedModel(model, scale, -bounds.Min.Y * scale, centerXZ);
    }

    public void DrawCenteredProp(LoadedModel prop, Vector3 centerPos, float rotDeg, Color tint, float outlineWidth)
    {
        float rad = rotDeg * Raylib.DEG2RAD;
        float rx = prop.CenterOffsetXZ.X * MathF.Cos(rad) + prop.CenterOffsetXZ.Y * MathF.Sin(rad);
        float rz = -prop.CenterOffsetXZ.X * MathF.Sin(rad) + prop.CenterOffsetXZ.Y * MathF.Cos(rad);
        DrawOutlined(prop.Model, new Vector3(centerPos.X - rx, centerPos.Y, centerPos.Z - rz), rotDeg, prop.Scale, tint, outlineWidth);
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
        if (OutlineLoaded && ShaderLoaded && outlineWidth > 0f)
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

    private void CacheShaderLocations()
    {
        _viewPosLoc = Raylib.GetShaderLocation(_shader, "viewPos");
        _ambientLoc = Raylib.GetShaderLocation(_shader, "ambientColor");
        _rimLoc = Raylib.GetShaderLocation(_shader, "rimColor");
        _playerPosLoc = Raylib.GetShaderLocation(_shader, "playerPos");
        _alarmLoc = Raylib.GetShaderLocation(_shader, "alarmActive");
        for (int i = 0; i < 2; i++)
        {
            _guardPosLocs[i] = Raylib.GetShaderLocation(_shader, $"guardPos{i}");
            _guardDirLocs[i] = Raylib.GetShaderLocation(_shader, $"guardDir{i}");
        }
    }

    private void PushStaticLights()
    {
        SetVec3Loc(Raylib.GetShaderLocation(_shader, "lightDirection"), DefaultLightDir);
        SetVec3Loc(Raylib.GetShaderLocation(_shader, "lightColor"), DefaultLightColor);
        FurnitureSpec[] lamps = Viewport3DConfig.LobbyFurniture.Where(f => f.EmitsWarmGlow).Take(2).ToArray();
        for (int i = 0; i < lamps.Length; i++)
        {
            SetVec3Loc(Raylib.GetShaderLocation(_shader, $"lampPos{i}"), new Vector3(lamps[i].Position.X, 64f, lamps[i].Position.Y));
        }
    }

    private void PushGuardUniforms(IReadOnlyList<Guard> guards)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 pos = i < guards.Count ? new Vector3(guards[i].Position.X, 36f, guards[i].Position.Y) : new Vector3(-9999f, 0f, -9999f);
            float rad = i < guards.Count ? guards[i].RotationDegrees * Raylib.DEG2RAD : 0f;
            SetVec3Loc(_guardPosLocs[i], pos);
            SetVec3Loc(_guardDirLocs[i], new Vector3(MathF.Cos(rad), 0f, MathF.Sin(rad)));
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

    private void SetVec3Loc(int loc, Vector3 v)
    {
        if (loc >= 0) Raylib.SetShaderValue(_shader, loc, v, ShaderUniformDataType.Vec3);
    }
}
