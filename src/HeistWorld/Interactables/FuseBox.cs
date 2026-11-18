using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

/// <summary>
/// One-shot power breaker that cuts power to a linked target group when interacted with.
/// </summary>
public sealed class FuseBox : PowerDevice, IInteractable
{
    private const float DefaultInteractRadius = 55f;
    private const string CutPowerPrompt = "[E] Cut Power";

    public string PromptText => IsPowered ? CutPowerPrompt : string.Empty;

    public FuseBox(
        string id,
        Vector2 position,
        string targetGroupId,
        float interactRadius = DefaultInteractRadius)
        : base(id, position, targetGroupId, interactRadius)
    {
    }

    public bool CanInteract(Vector2 playerPosition) =>
        IsPowered && IsPlayerInRange(playerPosition);

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        if (!IsPowered)
        {
            return;
        }

        CutPower();
        PublishPowerState(eventBus);
    }

    private void CutPower() => IsPowered = false;
}
