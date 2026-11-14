using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.WorldLighting;

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
        if (context.Walls.Count == 0) return true;
        if (item is not VaultDoor door)
        {
            return CollisionHelper.HasLineOfSight(context.PlayerPosition, item.Position, context.Walls);
        }

        // Ignore the closed door's own AABB so the player can open it from outside the threshold.
        List<RectangleBounds> otherWalls = context.Walls.Where(w => w != door.DoorBounds).ToList();
        return CollisionHelper.HasLineOfSight(context.PlayerPosition, item.Position, otherWalls);
    }
}
