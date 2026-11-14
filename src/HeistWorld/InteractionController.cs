using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld;

public sealed class InteractionController
{
    private IHoldInteractable? _activeHold;

    public void HandlePlayerInteraction(float deltaTime, GameContext context)
    {
        IHoldInteractable? holdable = context.CurrentInput.IsInteractHeld
            ? ResolveHoldTarget(context)
            : null;

        if (holdable is not null)
        {
            HoldNearbyObject(holdable, deltaTime, context);
            return;
        }

        CancelActiveHold();

        if (context.CurrentInput.IsInteractPressed)
        {
            PressNearbyObject(InteractableQuery.FindInPlayerRange(context), context);
        }
    }

    private IHoldInteractable? ResolveHoldTarget(GameContext context)
    {
        if (_activeHold is not null && _activeHold.CanInteract(context.PlayerPosition))
        {
            return _activeHold;
        }

        return InteractableQuery.FindHoldInRange(context);
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
