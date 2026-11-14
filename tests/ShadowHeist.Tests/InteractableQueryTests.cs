using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class InteractableQueryTests
{
    private const string DoorId = "vault_door";
    private static readonly Vector2 TargetPosition = new(200f, 200f);

    [Fact]
    public void FindHoldInProgress_ReturnsHold_WhenInRangeAndProgressAboveZero()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        keypad.Hold(1f, context, context.EventBus);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Same(keypad, result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsNull_WhenProgressIsZero()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Null(result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsNull_WhenOutOfRange()
    {
        using var context = new GameContext();
        context.PlayerPosition = new Vector2(9999f, 9999f);
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        keypad.Hold(1f, context, context.EventBus);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Null(result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsNull_WhenTargetIsNotHold()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var crate = new LootCrate("crate", TargetPosition);
        context.Interactables.Add(crate);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Null(result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsNull_WhenCompleted()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        keypad.Hold(KeypadTerminal.HackDurationSeconds, context, context.EventBus);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Null(result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsHold_WhenNonHoldInteractableIsListedFirst()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var crate = new LootCrate("crate", TargetPosition);
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        keypad.Hold(1f, context, context.EventBus);
        context.Interactables.Add(crate);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Same(keypad, result);
    }

    [Fact]
    public void FindHoldInRange_ReturnsHoldable_WhenNonHoldInteractableIsListedFirst()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;
        var crate = new LootCrate("crate", TargetPosition);
        var keypad = new KeypadTerminal("keypad", TargetPosition, DoorId);
        context.Interactables.Add(crate);
        context.Interactables.Add(keypad);

        var result = InteractableQuery.FindHoldInRange(context);

        Assert.Same(keypad, result);
    }

    [Fact]
    public void FindHoldInProgress_ReturnsNull_WhenInteractablesListIsEmpty()
    {
        using var context = new GameContext();
        context.PlayerPosition = TargetPosition;

        var result = InteractableQuery.FindHoldInProgress(context);

        Assert.Null(result);
    }
}
