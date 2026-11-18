using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Compiles cel-shading &amp; outline GLSL programs, caches uniform locations, and pushes per-frame lighting state.
/// </summary>
public sealed class ToonShaderUniforms : IDisposable
{
    private const string ToonVsPath = "assets/shaders/toon.vs";
    private const string ToonFsPath = "assets/shaders/toon.fs";
    private const string OutlineVsPath = "assets/shaders/outline.vs";
    private const string OutlineFsPath = "assets/shaders/outline.fs";
    private const int MaxShaderGuards = 2;

    private readonly int[] _guardPosLocations = { -1, -1 };
    private readonly int[] _guardDirLocations = { -1, -1 };
    private readonly int[] _lampPosLocations = { -1, -1 };

    private Shader _toonShader;
    private Shader _outlineShader;
    private int _outlineWidthLocation = -1;
    private int _viewPosLocation = -1;
    private int _lightColorLocation = -1;
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

    public void UpdateFrameLighting(Vector3 cameraPosition, GameContext context)
    {
        if (!IsToonLoaded) return;

        float alarmPulse = ToonLightingPalette.ComputeAlarmPulse(context.IsAlarmTriggered, context.ElapsedSeconds);
        UpdateRoomLightingUniforms(cameraPosition, context, alarmPulse);
        UpdateLobbyLampPositions(context.AreLobbyLightsPowered);
        UpdateAlarmUniform(context.IsAlarmTriggered);
    }

    public void SetGuardFlashlight(int slotIndex, Vector2 position, float rotationDegrees)
    {
        if (!IsToonLoaded || slotIndex < 0 || slotIndex >= MaxShaderGuards) return;

        float radians = rotationDegrees * HeistConstants.DegToRad;
        SetVec3(_guardPosLocations[slotIndex], new Vector3(position.X, ToonLightingPalette.ActorLightHeightY, position.Y));
        SetVec3(_guardDirLocations[slotIndex], new Vector3(MathF.Cos(radians), 0f, MathF.Sin(radians)));
    }

    public void ClearUnusedGuardFlashlights(int activeGuardCount)
    {
        if (!IsToonLoaded) return;

        for (int i = Math.Max(0, activeGuardCount); i < MaxShaderGuards; i++)
        {
            SetVec3(_guardPosLocations[i], ToonLightingPalette.OffstageLightPosition);
            SetVec3(_guardDirLocations[i], Vector3.UnitX);
        }
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
        UnloadShaderIfLoaded(ref _toonShader, () => IsToonLoaded = false, IsToonLoaded);
        UnloadShaderIfLoaded(ref _outlineShader, () => IsOutlineLoaded = false, IsOutlineLoaded);
    }

    private static void UnloadShaderIfLoaded(ref Shader shader, Action markUnloaded, bool isLoaded)
    {
        if (!isLoaded)
        {
            return;
        }

        Raylib.UnloadShader(shader);
        markUnloaded();
    }

    private void UpdateRoomLightingUniforms(Vector3 cameraPosition, GameContext context, float alarmPulse)
    {
        SetVec3(_viewPosLocation, cameraPosition);
        SetVec3(_lightColorLocation, ToonLightingPalette.ResolveLightColor(context.AreLobbyLightsPowered));
        SetVec3(_ambientLocation, ToonLightingPalette.ResolveAmbientColor(context.AreLobbyLightsPowered, alarmPulse));
        SetVec3(_rimLocation, ToonLightingPalette.ResolveRimColor(alarmPulse));
        SetVec3(_playerPosLocation, new Vector3(context.PlayerPosition.X, ToonLightingPalette.ActorLightHeightY, context.PlayerPosition.Y));
    }

    private void UpdateLobbyLampPositions(bool areLobbyLightsPowered)
    {
        int lampCount = Math.Min(MaxShaderGuards, Viewport3DConfig.WarmLampPositions.Length);
        for (int i = 0; i < lampCount; i++)
        {
            Vector3 lampPos = ToonLightingPalette.ResolveLampPosition(Viewport3DConfig.WarmLampPositions[i], areLobbyLightsPowered);
            SetVec3(_lampPosLocations[i], lampPos);
        }
    }

    private void UpdateAlarmUniform(bool isAlarmTriggered)
    {
        if (_alarmLocation >= 0)
        {
            Raylib.SetShaderValue(_toonShader, _alarmLocation, isAlarmTriggered ? 1f : 0f, ShaderUniformDataType.Float);
        }
    }

    private void LoadToonShader()
    {
        if (!TryLoadShaderProgram(ToonVsPath, ToonFsPath, out _toonShader)) return;

        IsToonLoaded = true;
        CacheUniformLocations();
        PushStaticLightUniforms();
    }

    private void LoadOutlineShader()
    {
        if (!TryLoadShaderProgram(OutlineVsPath, OutlineFsPath, out _outlineShader)) return;

        IsOutlineLoaded = true;
        _outlineWidthLocation = Raylib.GetShaderLocation(_outlineShader, "outlineWidth");
        int colorLoc = Raylib.GetShaderLocation(_outlineShader, "outlineColor");
        if (colorLoc >= 0)
        {
            Raylib.SetShaderValue(_outlineShader, colorLoc, ToonLightingPalette.InkOutlineColor, ShaderUniformDataType.Vec4);
        }
    }

    private static bool TryLoadShaderProgram(string vertexPath, string fragmentPath, out Shader shader)
    {
        shader = default;
        if (!File.Exists(vertexPath) || !File.Exists(fragmentPath)) return false;

        shader = Raylib.LoadShader(vertexPath, fragmentPath);
        return shader.Id > 0;
    }

    private void CacheUniformLocations()
    {
        _viewPosLocation = Raylib.GetShaderLocation(_toonShader, "viewPos");
        _lightColorLocation = Raylib.GetShaderLocation(_toonShader, "lightColor");
        _ambientLocation = Raylib.GetShaderLocation(_toonShader, "ambientColor");
        _rimLocation = Raylib.GetShaderLocation(_toonShader, "rimColor");
        _playerPosLocation = Raylib.GetShaderLocation(_toonShader, "playerPos");
        _alarmLocation = Raylib.GetShaderLocation(_toonShader, "alarmActive");

        for (int i = 0; i < MaxShaderGuards; i++)
        {
            _guardPosLocations[i] = Raylib.GetShaderLocation(_toonShader, $"guardPos{i}");
            _guardDirLocations[i] = Raylib.GetShaderLocation(_toonShader, $"guardDir{i}");
            _lampPosLocations[i] = Raylib.GetShaderLocation(_toonShader, $"lampPos{i}");
        }
    }

    private void PushStaticLightUniforms()
    {
        SetVec3(Raylib.GetShaderLocation(_toonShader, "lightDirection"), ToonLightingPalette.DefaultLightDirection);
        SetVec3(_lightColorLocation, ToonLightingPalette.DefaultLightColor);
        UpdateLobbyLampPositions(areLobbyLightsPowered: true);
    }

    private void SetVec3(int location, Vector3 value)
    {
        if (location >= 0)
        {
            Raylib.SetShaderValue(_toonShader, location, value, ShaderUniformDataType.Vec3);
        }
    }
}
