using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

public sealed class VaultDoor : WorldObject, IInteractable
{
    public bool IsLocked { get; private set; } = true;
    public bool IsOpen { get; private set; }
    public string PromptText => "[E] Open Vault";

    public VaultDoor(string id, Vector2 position, float interactRadius = 70f)
        : base(id, position, interactRadius)
    {
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsLocked && !IsOpen && IsPlayerInRange(playerPosition);

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        if (IsLocked) return;

        IsOpen = true;
    }

    public void Unlock() => IsLocked = false;
}
