using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class KeypadTerminalTests
{
    private const string DoorId = "vault_door";
    private static readonly Vector2 KeypadPosition = new(200f, 200f);

    [Fact]
    public void Hold_FillsProgressProportionallyToTime()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);

        keypad.Hold(KeypadTerminal.HackDurationSeconds / 2f, context, context.EventBus);

        Assert.Equal(0.5f, keypad.HoldProgress, 0.001f);
        Assert.False(keypad.IsHacked);
    }

    [Fact]
    public void Hold_WhenProgressCompletes_UnlocksTheTargetDoor()
    {
        using var context = new GameContext();
        var door = new VaultDoor(DoorId, new Vector2(500f, 200f));
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);
        context.Interactables.Add(door);

        keypad.Hold(KeypadTerminal.HackDurationSeconds, context, context.EventBus);

        Assert.True(keypad.IsHacked);
        Assert.False(door.IsLocked);
    }

    [Fact]
    public void Hold_WhenProgressIsIncomplete_KeepsTheDoorLocked()
    {
        using var context = new GameContext();
        var door = new VaultDoor(DoorId, new Vector2(500f, 200f));
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);
        context.Interactables.Add(door);

        keypad.Hold(KeypadTerminal.HackDurationSeconds - 0.5f, context, context.EventBus);

        Assert.True(door.IsLocked);
    }

    [Fact]
    public void Hold_WhenTargetDoorDoesNotExist_StillCompletesWithoutError()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("keypad", KeypadPosition, "missing_door");

        keypad.Hold(KeypadTerminal.HackDurationSeconds, context, context.EventBus);

        Assert.True(keypad.IsHacked);
    }

    [Fact]
    public void Release_BeforeCompletion_ResetsProgress()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);
        keypad.Hold(1f, context, context.EventBus);

        keypad.Release();

        Assert.Equal(0f, keypad.HoldProgress);
    }

    [Fact]
    public void Release_AfterCompletion_KeepsTheHackedState()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);
        keypad.Hold(KeypadTerminal.HackDurationSeconds, context, context.EventBus);

        keypad.Release();

        Assert.True(keypad.IsHacked);
        Assert.Equal(1f, keypad.HoldProgress);
    }

    [Fact]
    public void CanInteract_AfterHacked_ReturnsFalse()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId);
        keypad.Hold(KeypadTerminal.HackDurationSeconds, context, context.EventBus);

        Assert.False(keypad.CanInteract(KeypadPosition));
    }

    [Fact]
    public void CanInteract_WhenPlayerIsFar_ReturnsFalse()
    {
        var keypad = new KeypadTerminal("keypad", KeypadPosition, DoorId, interactRadius: 50f);

        Assert.False(keypad.CanInteract(new Vector2(600f, 200f)));
    }
}
