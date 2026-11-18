using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

/// <summary>
/// Reusable wall/pedestal switch that toggles power for a linked laser tripwire group.
/// </summary>
public sealed class LaserSwitch : PowerDevice, IInteractable
{
    private const float DefaultInteractRadius = 55f;
    private const string DisableLasersPrompt = "[E] Disable Lasers";
    private const string EnableLasersPrompt = "[E] Enable Lasers";

    public string PromptText => IsPowered ? DisableLasersPrompt : EnableLasersPrompt;

    public LaserSwitch(
        string id,
        Vector2 position,
        string targetGroupId,
        float interactRadius = DefaultInteractRadius)
        : base(id, position, targetGroupId, interactRadius)
    {
    }

    public bool CanInteract(Vector2 playerPosition) =>
        IsPlayerInRange(playerPosition);

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        TogglePower();
        PublishPowerState(eventBus);
    }

    private void TogglePower() => IsPowered = !IsPowered;
}
