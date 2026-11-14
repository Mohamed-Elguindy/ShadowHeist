using System.Numerics;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.Core;

public enum HeistOutcome
{
    Playing,
    Escaped,
    Caught
}

/// <summary>
/// Shared dependency container, 3D graphics state &amp; state context passed to all IGameSystem modules.
/// </summary>
public sealed class GameContext : IDisposable
{
    private readonly List<IGameSystem> _systems = new();

    public GameEventBus EventBus { get; } = new();
    public List<IInteractable> Interactables { get; } = new();
    public List<RectangleBounds> Walls { get; } = new();
    public TacticalCameraController Camera { get; } = new();
    public ToonModelLibrary Models { get; } = new();

    public Vector2 PlayerPosition { get; set; } = new(160f, 360f);
    public float PlayerRotationDegrees { get; set; }
    public bool IsPlayerSneaking { get; set; }
    public Vector2? AimWorldOverride { get; set; }
    public float ElapsedSeconds { get; private set; }
    public int StolenCash { get; set; }
    public bool IsAlarmTriggered { get; set; }
    public HeistOutcome Outcome { get; set; } = HeistOutcome.Playing;
    public PlayerInputCommand CurrentInput { get; set; }

    public void RegisterSystem(IGameSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
    }

    public void InitializeAll()
    {
        foreach (IGameSystem system in _systems)
        {
            system.Init(this);
        }
    }

    public void UpdateAll(float deltaTime)
    {
        ElapsedSeconds += deltaTime;
        foreach (IGameSystem system in _systems)
        {
            system.Update(deltaTime, this);
        }
    }

    public void DrawAll()
    {
        foreach (IGameSystem system in _systems)
        {
            system.Draw(this);
        }
    }

    public void DrawHudAll()
    {
        foreach (IGameSystem system in _systems)
        {
            system.DrawHud(this);
        }
    }

    public void RestartMission()
    {
        Interactables.Clear();
        Walls.Clear();
        EventBus.ClearSubscribers();
        ElapsedSeconds = 0f;
        StolenCash = 0;
        IsAlarmTriggered = false;
        Outcome = HeistOutcome.Playing;
        CurrentInput = default;
        AimWorldOverride = null;
        IsPlayerSneaking = false;
        InitializeAll();
        Camera.Reset(PlayerPosition);
    }

    public void Dispose()
    {
        foreach (IGameSystem system in _systems)
        {
            if (system is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        Models.Dispose();
    }
}

/// <summary>
/// Pure C# AABB rectangle struct so core collision logic can be unit-tested without Raylib window dependencies.
/// </summary>
public readonly record struct RectangleBounds(float X, float Y, float Width, float Height);
