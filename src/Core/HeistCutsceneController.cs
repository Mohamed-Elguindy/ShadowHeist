using System.Numerics;

namespace ShadowHeist.Core;

public enum CutsceneMode
{
    None,
    EntryArrival,
    EscapeGetaway,
    CaughtBusted
}

/// <summary>
/// Coordinates the mission-entry van drop-off and mission-escape getaway drive-off cutscenes.
/// </summary>
public sealed class HeistCutsceneController
{
    public const float EntryDriveDurationSeconds = CutscenePathMath.EntryDriveDurationSeconds;
    public const float EntryWalkDurationSeconds = CutscenePathMath.EntryWalkDurationSeconds;
    public const float EntryTotalDurationSeconds = CutscenePathMath.EntryTotalDurationSeconds;
    public const float EscapeBoardDurationSeconds = CutscenePathMath.EscapeBoardDurationSeconds;
    public const float EscapeDriveDurationSeconds = CutscenePathMath.EscapeDriveDurationSeconds;
    public const float EscapeSummaryDelaySeconds = CutscenePathMath.EscapeSummaryDelaySeconds;
    public const float EscapeTotalDurationSeconds = CutscenePathMath.EscapeTotalDurationSeconds;
    public const float CaughtDurationSeconds = CutscenePathMath.CaughtDurationSeconds;

    private const float EntryCameraVanBlend = 0.45f;
    private const float EscapeCameraVanBlend = 0.72f;

    private Vector2 _entrySpawnPosition;
    private Vector2 _escapeStartPosition;
    private Vector2 _caughtPosition;

    public bool IsEnabled { get; set; }
    public CutsceneMode Mode { get; private set; } = CutsceneMode.None;
    public float ElapsedSeconds { get; private set; }
    public bool WasEntrySkipped { get; private set; }
    public bool IsActive => Mode != CutsceneMode.None;
    public bool IsEntryPlaying => Mode == CutsceneMode.EntryArrival;
    public bool IsEscapePlaying => Mode == CutsceneMode.EscapeGetaway;
    public bool IsCaughtPlaying => Mode == CutsceneMode.CaughtBusted;
    public bool IsBlockingPlayerControl => Mode != CutsceneMode.None;
    public bool IsReadyForSummary => Mode switch
    {
        CutsceneMode.EscapeGetaway => ElapsedSeconds >= EscapeSummaryDelaySeconds,
        CutsceneMode.CaughtBusted => ElapsedSeconds >= CaughtDurationSeconds,
        _ => true
    };

    public void Reset()
    {
        Mode = CutsceneMode.None;
        ElapsedSeconds = 0f;
        WasEntrySkipped = false;
    }

    public void BeginEntry(Vector2 playerSpawnPosition)
    {
        if (!IsEnabled)
        {
            Reset();
            return;
        }

        _entrySpawnPosition = playerSpawnPosition;
        ElapsedSeconds = 0f;
        WasEntrySkipped = false;
        Mode = CutsceneMode.EntryArrival;
    }

    public void BeginEscape(Vector2 playerPosition)
    {
        if (!IsEnabled)
        {
            return;
        }

        _escapeStartPosition = playerPosition;
        ElapsedSeconds = 0f;
        Mode = CutsceneMode.EscapeGetaway;
    }

    public void BeginCaught(Vector2 playerPosition)
    {
        if (!IsEnabled)
        {
            return;
        }

        _caughtPosition = playerPosition;
        ElapsedSeconds = 0f;
        Mode = CutsceneMode.CaughtBusted;
    }

    public void SkipEntry()
    {
        if (Mode == CutsceneMode.EntryArrival)
        {
            Mode = CutsceneMode.None;
            ElapsedSeconds = 0f;
            WasEntrySkipped = true;
        }
    }

    public void Update(float deltaTime)
    {
        if (Mode == CutsceneMode.None || deltaTime <= 0f)
        {
            return;
        }

        ElapsedSeconds += deltaTime;
        if (Mode == CutsceneMode.EntryArrival && ElapsedSeconds >= EntryTotalDurationSeconds)
        {
            Mode = CutsceneMode.None;
            ElapsedSeconds = 0f;
        }
        else if (Mode == CutsceneMode.EscapeGetaway && ElapsedSeconds >= EscapeTotalDurationSeconds)
        {
            ElapsedSeconds = EscapeTotalDurationSeconds;
        }
        else if (Mode == CutsceneMode.CaughtBusted && ElapsedSeconds >= CaughtDurationSeconds)
        {
            ElapsedSeconds = CaughtDurationSeconds;
        }
    }

    public bool IsPlayerVisible() => Mode switch
    {
        CutsceneMode.EntryArrival => ElapsedSeconds >= EntryDriveDurationSeconds,
        CutsceneMode.EscapeGetaway => ElapsedSeconds < EscapeBoardDurationSeconds,
        _ => true
    };

    public Vector2 ResolveVanPosition(Vector2 defaultVanPosition) => Mode switch
    {
        CutsceneMode.EntryArrival => CutscenePathMath.EvaluateEntryVanPosition(ElapsedSeconds),
        CutsceneMode.EscapeGetaway => CutscenePathMath.EvaluateEscapeVanPosition(ElapsedSeconds),
        _ => defaultVanPosition
    };

    public float ResolveVanYaw(float defaultYawDegrees) => Mode switch
    {
        CutsceneMode.EntryArrival => CutscenePathMath.EvaluateEntryVanYaw(ElapsedSeconds),
        CutsceneMode.EscapeGetaway => CutscenePathMath.EvaluateEscapeVanYaw(ElapsedSeconds),
        _ => defaultYawDegrees
    };

    public Vector2 ResolvePlayerPosition(Vector2 simulationPosition) => Mode switch
    {
        CutsceneMode.EntryArrival => CutscenePathMath.EvaluateEntryPlayerPosition(ElapsedSeconds, _entrySpawnPosition),
        CutsceneMode.EscapeGetaway => CutscenePathMath.EvaluateEscapePlayerPosition(ElapsedSeconds, _escapeStartPosition),
        _ => simulationPosition
    };

    public float ResolvePlayerRotation(float simulationRotationDegrees) => Mode switch
    {
        CutsceneMode.EntryArrival => CutscenePathMath.ComputeFacingDegrees(CutscenePathMath.VanDoorPosition, _entrySpawnPosition, simulationRotationDegrees),
        CutsceneMode.EscapeGetaway => CutscenePathMath.ComputeFacingDegrees(_escapeStartPosition, CutscenePathMath.VanDoorPosition, simulationRotationDegrees),
        _ => simulationRotationDegrees
    };

    public Vector2 ResolveCameraFocus(Vector2 simulationPosition) => Mode switch
    {
        CutsceneMode.EntryArrival => Vector2.Lerp(
            ResolveVanPosition(CutscenePathMath.ParkedVanPosition),
            ResolvePlayerPosition(simulationPosition),
            ElapsedSeconds < EntryDriveDurationSeconds ? EntryCameraVanBlend : 1f),
        CutsceneMode.EscapeGetaway => Vector2.Lerp(
            ResolvePlayerPosition(simulationPosition),
            ResolveVanPosition(CutscenePathMath.ParkedVanPosition),
            ElapsedSeconds < EscapeBoardDurationSeconds ? EscapeCameraVanBlend : 1f),
        CutsceneMode.CaughtBusted => _caughtPosition,
        _ => simulationPosition
    };

    public float ResolveCameraZoomMultiplier() =>
        Mode == CutsceneMode.CaughtBusted ? CutscenePathMath.EvaluateCaughtZoomMultiplier(ElapsedSeconds) : 1f;

    public float ResolveCaughtRedPulseIntensity()
    {
        if (Mode != CutsceneMode.CaughtBusted)
        {
            return 0f;
        }

        float progress = Math.Clamp(ElapsedSeconds / CaughtDurationSeconds, 0f, 1f);
        return MathF.Sin(progress * MathF.PI);
    }
}
