namespace ShadowHeist.Core.Events;

/// <summary>
/// Published when a FuseBox or LaserSwitch changes power state for a linked light or laser group.
/// </summary>
public readonly record struct PowerStateChangedEvent(string TargetGroupId, bool IsPowered);
