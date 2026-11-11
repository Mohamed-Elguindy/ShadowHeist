using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld;

public static class InteractableQuery
{
    public static IInteractable? FindInPlayerRange(GameContext context) =>
        context.Interactables.FirstOrDefault(item => item.CanInteract(context.PlayerPosition));

    public static IHoldInteractable? FindHoldInRange(GameContext context) =>
        HoldablesInRange(context).FirstOrDefault();

    public static IHoldInteractable? FindHoldInProgress(GameContext context) =>
        HoldablesInRange(context).FirstOrDefault(hold => hold.HoldProgress > 0f);

    private static IEnumerable<IHoldInteractable> HoldablesInRange(GameContext context) =>
        context.Interactables
            .OfType<IHoldInteractable>()
            .Where(hold => hold.CanInteract(context.PlayerPosition));
}
