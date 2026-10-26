using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld;

internal static class InteractableQuery
{
    public static IInteractable? FindInPlayerRange(GameContext context) =>
        context.Interactables.FirstOrDefault(item => item.CanInteract(context.PlayerPosition));
}
