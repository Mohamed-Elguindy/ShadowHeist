using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;

namespace ShadowHeist.HeistWorld.Interactables;

public sealed class KeypadTerminal : WorldObject, IHoldInteractable
{
    public const float HackDurationSeconds = 3f;

    public string TargetId { get; }
    public float HoldProgress { get; private set; }
    public bool IsHacked { get; private set; }

    public string PromptText => HoldProgress > 0f
        ? $"Hacking... {HoldProgress:P0}"
        : "[Hold E] Hack Keypad";

    public KeypadTerminal(string id, Vector2 position, string targetId, float interactRadius = 55f)
        : base(id, position, interactRadius)
    {
        TargetId = targetId;
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsHacked && IsPlayerInRange(playerPosition);

    public void Hold(float deltaTime, GameContext context, GameEventBus eventBus)
    {
        if (IsHacked) return;

        AdvanceHoldProgress(deltaTime);

        if (IsHoldComplete)
        {
            OnInteract(context, eventBus);
        }
    }

    public void Release()
    {
        if (!IsHacked)
        {
            HoldProgress = 0f;
        }
    }

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        IsHacked = true;
        FindTargetDoor(context)?.Unlock();
    }

    private bool IsHoldComplete => HoldProgress >= 1f;

    private void AdvanceHoldProgress(float deltaTime) =>
        HoldProgress = Math.Min(1f, HoldProgress + deltaTime / HackDurationSeconds);

    private VaultDoor? FindTargetDoor(GameContext context) =>
        context.Interactables.OfType<VaultDoor>().FirstOrDefault(door => door.Id == TargetId);
}
