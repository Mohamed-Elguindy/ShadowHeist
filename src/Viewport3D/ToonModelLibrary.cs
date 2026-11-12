using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Loads Kenney (CC0) OBJ models and applies the cel-shading shader and optional skin texture.
/// </summary>
public sealed unsafe class ToonModelLibrary : IDisposable
{
    private const string VertexShaderPath = "assets/shaders/toon.vs";
    private const string FragmentShaderPath = "assets/shaders/toon.fs";

    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.45f, -1f, -0.55f));
    private static readonly Vector3 LightColor = new(0.85f, 0.82f, 0.78f);
    private static readonly Vector3 AmbientColor = new(0.42f, 0.46f, 0.58f);

    private readonly List<Model> _models = new();
    private readonly List<Texture2D> _textures = new();
    private Shader _shader;

    public bool ShaderLoaded { get; private set; }

    public void LoadShader()
    {
        if (!File.Exists(VertexShaderPath) || !File.Exists(FragmentShaderPath))
        {
            return;
        }

        _shader = Raylib.LoadShader(VertexShaderPath, FragmentShaderPath);
        ShaderLoaded = _shader.Id > 0;
        if (!ShaderLoaded)
        {
            return;
        }

        SetVector3Uniform("lightDirection", LightDirection);
        SetVector3Uniform("lightColor", LightColor);
        SetVector3Uniform("ambientColor", AmbientColor);
    }

    /// <summary>Loads an OBJ and scales it so its height equals <paramref name="targetHeight"/>.</summary>
    public LoadedModel? Load(string objPath, float targetHeight, string? skinTexturePath = null)
    {
        if (!File.Exists(objPath))
        {
            return null;
        }

        // Console.Error.WriteLine("DBG before LoadModel");
        Console.WriteLine($"[DEBUG] Loading {objPath}..."); Console.WriteLine($"[DEBUG] Before Raylib.LoadModel({objPath})"); Model model = Raylib.LoadModel(objPath); Console.WriteLine($"[DEBUG] After Raylib.LoadModel({objPath})"); Console.WriteLine($"[DEBUG] Loaded {objPath}: Meshes={model.MeshCount}, Mats={model.MaterialCount}");
        // Console.Error.WriteLine("DBG after LoadModel");
        if (model.MeshCount == 0)
        {
            return null;
        }

        // Console.WriteLine($"DBG meshes={model.MeshCount} mats={model.MaterialCount} ptr={(long)model.Materials}");
        Console.WriteLine($"[DEBUG] Processing {model.MaterialCount} materials for {objPath}..."); for (int i = 0; i < model.MaterialCount; i++)
        {
            // Console.WriteLine($"DBG mat {i} shaderId={model.Materials[i].Shader.Id} maps={(long)model.Materials[i].Maps}");
            if (ShaderLoaded)
            {
                Console.WriteLine($"[DEBUG] Assigning shader to mat {i}..."); model.Materials[i].Shader = _shader; Console.WriteLine($"[DEBUG] Assigned shader to mat {i}");
            }

            if (skinTexturePath is not null && File.Exists(skinTexturePath))
            {
                Texture2D skin = Raylib.LoadTexture(skinTexturePath);
                _textures.Add(skin);
                model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture = skin;
                model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Color = Color.White;
            }
        }

        Console.WriteLine($"[DEBUG] Model loaded with {model.MeshCount} meshes. Getting bounding box..."); BoundingBox box = Raylib.GetModelBoundingBox(model); Console.WriteLine($"[DEBUG] BoundingBox: min={box.Min}, max={box.Max}");
        float height = MathF.Max(box.Max.Y - box.Min.Y, 0.0001f);
        float scale = targetHeight / height;
        _models.Add(model);
        return new LoadedModel(model, scale, -box.Min.Y * scale);
    }

    /// <summary>Builds a textured box (UVs 0..1 per face) with the toon shader, used for floor slabs.</summary>
    public Model? LoadTexturedBox(string texturePath, float width, float height, float length)
    {
        if (!File.Exists(texturePath))
        {
            return null;
        }

        Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(width, height, length));
        Texture2D texture = Raylib.LoadTexture(texturePath);
        _textures.Add(texture);
        model.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = texture;
        if (ShaderLoaded)
        {
            model.Materials[0].Shader = _shader;
        }

        _models.Add(model);
        return model;
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

        if (ShaderLoaded)
        {
            Raylib.UnloadShader(_shader);
            ShaderLoaded = false;
        }

        _models.Clear();
        _textures.Clear();
    }

    private void SetVector3Uniform(string name, Vector3 value)
    {
        int location = Raylib.GetShaderLocation(_shader, name);
        Raylib.SetShaderValue(_shader, location, value, ShaderUniformDataType.Vec3);
    }
}

/// <summary>A loaded model plus the uniform scale and Y offset that put its feet on the floor.</summary>
public readonly record struct LoadedModel(Model Model, float Scale, float FootOffsetY);
