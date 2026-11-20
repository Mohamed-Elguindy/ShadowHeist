using System.Numerics;
using ShadowHeist.Core.Events;

namespace ShadowHeist.HeistWorld.Interactables;

/// <summary>
/// Shared base for world power-control interactables that target a named light or laser group.
/// </summary>
public abstract class PowerDevice : WorldObject
{
    public string TargetGroupId { get; }
    public bool IsPowered { get; protected set; } = true;

    protected PowerDevice(
        string id,
        Vector2 position,
        string targetGroupId,
        float interactRadius)
        : base(ValidateRequiredText(id, nameof(id)), position, interactRadius)
    {
        TargetGroupId = ValidateRequiredText(targetGroupId, nameof(targetGroupId));
    }

    protected void PublishPowerState(GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        eventBus.Publish(new PowerStateChangedEvent(TargetGroupId, IsPowered));
    }

    private static string ValidateRequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
