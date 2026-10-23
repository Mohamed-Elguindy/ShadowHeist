using System.Numerics;
using ShadowHeist.Core.Events;

namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Polymorphic contract for world objects: LootCrate, VaultDoor, KeypadTerminal, FuseBox (Tickets #2, #9, #17 — Guindy).
/// </summary>
public interface IInteractable
{
    string Id { get; }
    Vector2 Position { get; }
    float InteractRadius { get; }
    string PromptText { get; }
    bool CanInteract(Vector2 playerPosition);
    void OnInteract(GameContext context, GameEventBus eventBus);
}
