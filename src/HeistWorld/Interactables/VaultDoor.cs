using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

public sealed class VaultDoor : WorldObject, IInteractable
{
    private const float DoorThickness = 32f;
    private const float DoorSpan = 96f;

    public bool IsLocked { get; private set; } = true;
    public bool IsOpen { get; private set; }
    public RectangleBounds DoorBounds { get; }
    public string PromptText => "[E] Open Vault";

    public VaultDoor(string id, Vector2 position, float interactRadius = 75f)
        : base(id, position, interactRadius)
    {
        DoorBounds = new RectangleBounds(
            position.X - DoorThickness * 0.5f,
            position.Y - DoorSpan * 0.5f,
            DoorThickness,
            DoorSpan);
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsLocked && !IsOpen && IsPlayerInRange(playerPosition);

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        if (IsLocked) return;

        IsOpen = true;
        context.Walls.Remove(DoorBounds);
    }

    public void Unlock() => IsLocked = false;
}
