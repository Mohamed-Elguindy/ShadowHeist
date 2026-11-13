using System.Numerics;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.Core;

public enum HeistOutcome
{
    Playing,
    Escaped,
    Caught
}

/// <summary>
/// Shared dependency container &amp; state context passed to all IGameSystem modules.
/// </summary>
public sealed class GameContext : IDisposable
{
    private readonly List<IGameSystem> _systems = new();

    public GameEventBus EventBus { get; } = new();
    public List<IInteractable> Interactables { get; } = new();
    public List<RectangleBounds> Walls { get; } = new();

    public Vector2 PlayerPosition { get; set; } = new(160f, 360f);
    public float PlayerRotationDegrees { get; set; }
    public bool IsPlayerSneaking { get; set; }
    public Vector2? AimWorldOverride { get; set; }
    public bool Is3DRenderingActive { get; set; }
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
        foreach (var system in _systems)
        {
            system.Init(this);
        }
    }

    public void UpdateAll(float deltaTime)
    {
        foreach (var system in _systems)
        {
            system.Update(deltaTime, this);
        }
    }

    public void DrawAll()
    {
        foreach (var system in _systems)
        {
            system.Draw(this);
        }
    }

    public void RestartMission()
    {
        Interactables.Clear();
        Walls.Clear();
        EventBus.ClearSubscribers();
        StolenCash = 0;
        IsAlarmTriggered = false;
        Outcome = HeistOutcome.Playing;
        CurrentInput = default;
        AimWorldOverride = null;
        IsPlayerSneaking = false;
        InitializeAll();
    }

    public void Dispose()
    {
        foreach (var system in _systems)
        {
            if (system is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

/// <summary>
/// Pure C# AABB rectangle struct so core collision logic can be unit-tested without Raylib window dependencies.
/// </summary>
public readonly record struct RectangleBounds(float X, float Y, float Width, float Height);
