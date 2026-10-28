using Raylib_cs;
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

        // TODO (After Mahmoud's Week 1, Ticket #3 — feat/mahmoud-input-provider):
        // Replace direct Raylib.IsKeyDown / Raylib.IsKeyPressed calls with IInputProvider / PlayerInputCommand
        // (and add IsInteractHeld to PlayerInputCommand alongside IsInteractPressed).
        if (nearby is IHoldInteractable holdable && Raylib.IsKeyDown(KeyboardKey.E))
        {
            HoldNearbyObject(holdable, deltaTime, context);
            return;
        }

        CancelActiveHold();

        if (Raylib.IsKeyPressed(KeyboardKey.E))
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
