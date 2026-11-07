using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld;

public sealed class InteractionController
{
    private IHoldInteractable? _activeHold;

    public void HandlePlayerInteraction(float deltaTime, GameContext context)
    {
        IInteractable? nearby = InteractableQuery.FindInPlayerRange(context);

        if (nearby is IHoldInteractable holdable && context.CurrentInput.IsInteractHeld)
        {
            HoldNearbyObject(holdable, deltaTime, context);
            return;
        }

        CancelActiveHold();

        if (context.CurrentInput.IsInteractPressed)
        {
            PressNearbyObject(nearby, context);
        }
    }

    private void HoldNearbyObject(IHoldInteractable holdable, float deltaTime, GameContext context)
    {
        if (!ReferenceEquals(_activeHold, holdable))
        {
            CancelActiveHold();
            _activeHold = holdable;
        }

        holdable.Hold(deltaTime, context, context.EventBus);
    }

    private void CancelActiveHold()
    {
        _activeHold?.Release();
        _activeHold = null;
    }

    private static void PressNearbyObject(IInteractable? nearby, GameContext context)
    {
        if (nearby is null or IHoldInteractable) return;

        nearby.OnInteract(context, context.EventBus);
    }
}
