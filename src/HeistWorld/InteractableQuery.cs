using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld;

public static class InteractableQuery
{
    public static IInteractable? FindInPlayerRange(GameContext context) =>
        InteractablesInRange(context).FirstOrDefault();

    public static IHoldInteractable? FindHoldInRange(GameContext context) =>
        HoldablesInRange(context).FirstOrDefault();

    public static IHoldInteractable? FindHoldInProgress(GameContext context) =>
        HoldablesInRange(context).FirstOrDefault(hold => hold.HoldProgress > 0f);

    private static IEnumerable<IInteractable> InteractablesInRange(GameContext context) =>
        context.Interactables
            .Where(item => item.CanInteract(context.PlayerPosition) && HasUnobstructedAccess(context, item));

    private static IEnumerable<IHoldInteractable> HoldablesInRange(GameContext context) =>
        InteractablesInRange(context).OfType<IHoldInteractable>();

    private static bool HasUnobstructedAccess(GameContext context, IInteractable item)
    {
        if (context.Walls.Count == 0)
        {
            return true;
        }

        RectangleBounds? ignoredBounds = item is VaultDoor door ? door.DoorBounds : null;
        return Geometry2D.HasLineOfSight(context.PlayerPosition, item.Position, context.Walls, ignoredBounds);
    }
}
