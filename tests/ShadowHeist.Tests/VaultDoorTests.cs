using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class VaultDoorTests
{
    private static readonly Vector2 DoorPosition = new(100f, 100f);

    [Fact]
    public void CanInteract_WhenLocked_ReturnsFalseEvenIfPlayerIsClose()
    {
        var door = new VaultDoor("door", DoorPosition);

        Assert.False(door.CanInteract(DoorPosition));
    }

    [Fact]
    public void CanInteract_WhenUnlockedAndPlayerIsClose_ReturnsTrue()
    {
        var door = new VaultDoor("door", DoorPosition);
        door.Unlock();

        Assert.True(door.CanInteract(DoorPosition));
    }

    [Fact]
    public void CanInteract_WhenUnlockedButPlayerIsFar_ReturnsFalse()
    {
        var door = new VaultDoor("door", DoorPosition, interactRadius: 50f);
        door.Unlock();

        Assert.False(door.CanInteract(new Vector2(400f, 100f)));
    }

    [Fact]
    public void OnInteract_WhenUnlocked_OpensTheDoor()
    {
        using var context = new GameContext();
        var door = new VaultDoor("door", DoorPosition);
        door.Unlock();

        door.OnInteract(context, context.EventBus);

        Assert.True(door.IsOpen);
        Assert.False(door.CanInteract(DoorPosition));
    }

    [Fact]
    public void OnInteract_WhenLocked_KeepsTheDoorClosed()
    {
        using var context = new GameContext();
        var door = new VaultDoor("door", DoorPosition);

        door.OnInteract(context, context.EventBus);

        Assert.False(door.IsOpen);
    }
}
