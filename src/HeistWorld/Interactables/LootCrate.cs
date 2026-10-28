using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

public sealed class LootCrate : WorldObject, IInteractable
{
    private const float NoiseRadius = 60f;

    public int CashValue { get; }
    public bool IsCollected { get; private set; }
    public string PromptText => $"[E] Steal Cash (${CashValue:N0})";

    public LootCrate(string id, Vector2 position, int cashValue = 250_000, float interactRadius = 55f)
        : base(id, position, interactRadius)
    {
        CashValue = cashValue;
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsCollected && IsPlayerInRange(playerPosition);

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        if (IsCollected) return;

        IsCollected = true;
        context.StolenCash += CashValue;
        eventBus.Publish(new NoiseEmittedEvent(Position, NoiseRadius, $"LootCrate:{Id}"));
    }
}
