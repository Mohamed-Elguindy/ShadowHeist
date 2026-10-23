using System.Numerics;

namespace ShadowHeist.Core.Events;

/// <summary>
/// Published when the player sprints, walks on broken glass, or throws a distraction coin.
/// </summary>
public readonly record struct NoiseEmittedEvent(Vector2 Origin, float Radius, string SourceName);
