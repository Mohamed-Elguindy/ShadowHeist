using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

public sealed class LootCrate : IInteractable
{
    private const float NoiseRadius = 60f;

    public string Id { get; }
    public Vector2 Position { get; }
    public int CashValue { get; }
    public float InteractRadius { get; }
    public bool IsCollected { get; private set; }
    public string PromptText => $"[E] Steal Cash (${CashValue:N0})";

    public LootCrate(string id, Vector2 position, int cashValue = 250_000, float interactRadius = 55f)
    {
        Id = id;
        Position = position;
        CashValue = cashValue;
        InteractRadius = interactRadius;
    }

    public bool CanInteract(Vector2 playerPosition) =>
        !IsCollected && Vector2.Distance(playerPosition, Position) <= InteractRadius;

    public void OnInteract(GameContext context, GameEventBus eventBus)
    {
        if (IsCollected) return;

        IsCollected = true;
        context.StolenCash += CashValue;
        eventBus.Publish(new NoiseEmittedEvent(Position, NoiseRadius, $"LootCrate:{Id}"));
    }
}
