using System.Numerics;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Capability interface for Guard AI states that navigate along an A* path toward an active target.
/// Keeps GuardRenderer Open/Closed across SuspiciousState and future AlertChaseState.
/// </summary>
public interface IGuardPathState : IGuardState
{
    Vector2 TargetPosition { get; }
    IReadOnlyList<Vector2> PathWaypoints { get; }
    int CurrentPathIndex { get; }
}

