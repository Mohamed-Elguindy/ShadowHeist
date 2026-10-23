using System.Numerics;

namespace ShadowHeist.Core.Events;

/// <summary>
/// Published when a laser tripwire is crossed or a guard's detection meter reaches 100%.
/// </summary>
public readonly record struct AlarmTriggeredEvent(Vector2 TriggerPosition, string Reason);
