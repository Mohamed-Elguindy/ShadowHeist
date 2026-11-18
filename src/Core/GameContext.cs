using System.Numerics;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.Core;

/// <summary>
/// Shared dependency container, 3D graphics state, and game loop coordinator for all IGameSystem modules.
/// </summary>
public sealed class GameContext : IDisposable
{
    private const string LobbyLightsPowerGroupId = "lobby_lights";
    private const string AllPowerGroupId = "all";
    private static readonly Vector2 DefaultPlayerPosition = new(160f, 360f);

    private readonly List<IGameSystem> _systems = new();
    private bool _isPowerSubscribed;

    public GameEventBus EventBus { get; } = new();
    public List<IInteractable> Interactables { get; } = new();
    public List<RectangleBounds> Walls { get; } = new();
    public TacticalCameraController Camera { get; } = new();
    public ToonModelLibrary Models { get; } = new();

    public Vector2 PlayerPosition { get; set; } = DefaultPlayerPosition;
    public float PlayerRotationDegrees { get; set; }
    public bool IsPlayerSneaking { get; set; }
    public Vector2? AimWorldOverride { get; set; }
    public float ElapsedSeconds { get; private set; }
    public int StolenCash { get; set; }
    public bool IsAlarmTriggered { get; set; }
    public bool AreLobbyLightsPowered { get; set; } = true;
    public HeistOutcome Outcome { get; set; } = HeistOutcome.Playing;
    public PlayerInputCommand CurrentInput { get; set; }

    public void RegisterSystem(IGameSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
    }

    public void InitializeAll()
    {
        SubscribeToPowerEvents();
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
        _isPowerSubscribed = false;
        ElapsedSeconds = 0f;
        StolenCash = 0;
        IsAlarmTriggered = false;
        AreLobbyLightsPowered = true;
        Outcome = HeistOutcome.Playing;
        CurrentInput = default;
        AimWorldOverride = null;
        PlayerPosition = DefaultPlayerPosition;
        PlayerRotationDegrees = 0f;
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

    private void SubscribeToPowerEvents()
    {
        if (_isPowerSubscribed)
        {
            return;
        }

        EventBus.Subscribe<PowerStateChangedEvent>(OnPowerStateChanged);
        _isPowerSubscribed = true;
    }

    private void OnPowerStateChanged(PowerStateChangedEvent evt)
    {
        if (MatchesLobbyLightsGroup(evt.TargetGroupId))
        {
            AreLobbyLightsPowered = evt.IsPowered;
        }
    }

    private static bool MatchesLobbyLightsGroup(string targetGroupId) =>
        string.Equals(targetGroupId, LobbyLightsPowerGroupId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(targetGroupId, AllPowerGroupId, StringComparison.OrdinalIgnoreCase);
}
