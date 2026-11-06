using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld;

public static class InteractableQuery
{
    public static IInteractable? FindInPlayerRange(GameContext context) =>
        context.Interactables.FirstOrDefault(item => item.CanInteract(context.PlayerPosition));

    public static IHoldInteractable? FindHoldInProgress(GameContext context)
    {
        IInteractable? interactable = FindInPlayerRange(context);
        if (interactable is IHoldInteractable hold && hold.HoldProgress > 0f)
        {
            return hold;
        }

        return null;
    }
}
