using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.HeistWorld;

internal static class InteractableQuery
{
    public static IInteractable? FindInPlayerRange(GameContext context)
    {
        foreach (IInteractable item in context.Interactables)
        {
            if (!item.CanInteract(context.PlayerPosition)) continue;
            if (HasUnobstructedAccess(context, item)) return item;
        }

        return null;
    }

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
