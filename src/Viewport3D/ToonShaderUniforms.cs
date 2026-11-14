using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Compiles cel-shading & outline GLSL programs, caches uniform locations, and pushes per-frame lighting state.
/// </summary>
public sealed class ToonShaderUniforms : IDisposable
{
    private const string ToonVsPath = "assets/shaders/toon.vs";
    private const string ToonFsPath = "assets/shaders/toon.fs";
    private const string OutlineVsPath = "assets/shaders/outline.vs";
    private const string OutlineFsPath = "assets/shaders/outline.fs";
    private const int MaxShaderGuards = 2;

    private static readonly Vector3 DefaultLightDirection = Vector3.Normalize(new Vector3(-0.48f, -1f, -0.52f));
    private static readonly Vector3 DefaultLightColor = new(0.56f, 0.60f, 0.72f);
    private static readonly Vector3 DefaultAmbient = new(0.24f, 0.28f, 0.39f);
    private static readonly Vector3 DefaultRim = new(0.22f, 0.48f, 0.68f);
    private static readonly Vector3 AlarmAmbient = new(0.46f, 0.16f, 0.20f);
    private static readonly Vector3 AlarmRim = new(0.95f, 0.22f, 0.24f);
    private static readonly Vector4 InkOutlineColor = new(0.04f, 0.05f, 0.09f, 1f);

    private readonly int[] _guardPosLocations = { -1, -1 };
    private readonly int[] _guardDirLocations = { -1, -1 };

    private Shader _toonShader;
    private Shader _outlineShader;
    private int _outlineWidthLocation = -1;
    private int _viewPosLocation = -1;
    private int _ambientLocation = -1;
    private int _rimLocation = -1;
    private int _playerPosLocation = -1;
    private int _alarmLocation = -1;

    public bool IsToonLoaded { get; private set; }
    public bool IsOutlineLoaded { get; private set; }
    public Shader ToonShader => _toonShader;
    public Shader OutlineShader => _outlineShader;

    public void Load()
    {
        LoadToonShader();
        LoadOutlineShader();
    }

    public void UpdateFrameUniforms(Vector3 cameraPosition, GameContext context, IReadOnlyList<Guard> guards, float timeSeconds)
    {
        if (!IsToonLoaded)
        {
            return;
        }

        float alarmPulse = context.IsAlarmTriggered ? 0.5f + 0.5f * MathF.Sin(timeSeconds * 8f) : 0f;
        SetVec3(_viewPosLocation, cameraPosition);
        SetVec3(_ambientLocation, Vector3.Lerp(DefaultAmbient, AlarmAmbient, alarmPulse));
        SetVec3(_rimLocation, Vector3.Lerp(DefaultRim, AlarmRim, alarmPulse));
        SetVec3(_playerPosLocation, new Vector3(context.PlayerPosition.X, 36f, context.PlayerPosition.Y));

        if (_alarmLocation >= 0)
        {
            Raylib.SetShaderValue(_toonShader, _alarmLocation, context.IsAlarmTriggered ? 1f : 0f, ShaderUniformDataType.Float);
        }

        PushGuardFlashlightUniforms(guards);
    }

    public void SetOutlineWidth(float width)
    {
        if (IsOutlineLoaded && _outlineWidthLocation >= 0)
        {
            Raylib.SetShaderValue(_outlineShader, _outlineWidthLocation, width, ShaderUniformDataType.Float);
        }
    }

    public void Dispose()
    {
        if (IsToonLoaded)
        {
            Raylib.UnloadShader(_toonShader);
            IsToonLoaded = false;
        }

        if (IsOutlineLoaded)
        {
            Raylib.UnloadShader(_outlineShader);
            IsOutlineLoaded = false;
        }
    }

    private void LoadToonShader()
    {
        if (!File.Exists(ToonVsPath) || !File.Exists(ToonFsPath))
        {
            return;
        }

        _toonShader = Raylib.LoadShader(ToonVsPath, ToonFsPath);
        IsToonLoaded = _toonShader.Id > 0;
        if (!IsToonLoaded)
        {
            return;
        }

        CacheUniformLocations();
        PushStaticLightUniforms();
    }

    private void LoadOutlineShader()
    {
        if (!File.Exists(OutlineVsPath) || !File.Exists(OutlineFsPath))
        {
            return;
        }

        _outlineShader = Raylib.LoadShader(OutlineVsPath, OutlineFsPath);
        IsOutlineLoaded = _outlineShader.Id > 0;
        if (!IsOutlineLoaded)
        {
            return;
        }

        _outlineWidthLocation = Raylib.GetShaderLocation(_outlineShader, "outlineWidth");
        int colorLoc = Raylib.GetShaderLocation(_outlineShader, "outlineColor");
        if (colorLoc >= 0)
        {
            Raylib.SetShaderValue(_outlineShader, colorLoc, InkOutlineColor, ShaderUniformDataType.Vec4);
        }
    }

    private void CacheUniformLocations()
    {
        _viewPosLocation = Raylib.GetShaderLocation(_toonShader, "viewPos");
        _ambientLocation = Raylib.GetShaderLocation(_toonShader, "ambientColor");
        _rimLocation = Raylib.GetShaderLocation(_toonShader, "rimColor");
        _playerPosLocation = Raylib.GetShaderLocation(_toonShader, "playerPos");
        _alarmLocation = Raylib.GetShaderLocation(_toonShader, "alarmActive");

        for (int i = 0; i < MaxShaderGuards; i++)
        {
            _guardPosLocations[i] = Raylib.GetShaderLocation(_toonShader, $"guardPos{i}");
            _guardDirLocations[i] = Raylib.GetShaderLocation(_toonShader, $"guardDir{i}");
        }
    }

    private void PushStaticLightUniforms()
    {
        SetVec3(Raylib.GetShaderLocation(_toonShader, "lightDirection"), DefaultLightDirection);
        SetVec3(Raylib.GetShaderLocation(_toonShader, "lightColor"), DefaultLightColor);

        FurnitureSpec[] warmLamps = BankFurnitureLayout.LobbyFurniture
            .Where(spec => spec.EmitsWarmGlow)
            .Take(MaxShaderGuards)
            .ToArray();

        for (int i = 0; i < warmLamps.Length; i++)
        {
            int loc = Raylib.GetShaderLocation(_toonShader, $"lampPos{i}");
            SetVec3(loc, new Vector3(warmLamps[i].Position.X, 64f, warmLamps[i].Position.Y));
        }
    }

    private void PushGuardFlashlightUniforms(IReadOnlyList<Guard> guards)
    {
        for (int i = 0; i < MaxShaderGuards; i++)
        {
            Vector3 position = i < guards.Count
                ? new Vector3(guards[i].Position.X, 36f, guards[i].Position.Y)
                : new Vector3(-9999f, 0f, -9999f);

            float radians = i < guards.Count ? guards[i].RotationDegrees * HeistConstants.DegToRad : 0f;
            SetVec3(_guardPosLocations[i], position);
            SetVec3(_guardDirLocations[i], new Vector3(MathF.Cos(radians), 0f, MathF.Sin(radians)));
        }
    }

    private void SetVec3(int location, Vector3 value)
    {
        if (location >= 0)
        {
            Raylib.SetShaderValue(_toonShader, location, value, ShaderUniformDataType.Vec3);
        }
    }
}
