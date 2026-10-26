using System.Numerics;

namespace ShadowHeist.HeistWorld.Interactables;

public abstract class WorldObject
{
    public string Id { get; }
    public Vector2 Position { get; }
    public float InteractRadius { get; }

    protected WorldObject(string id, Vector2 position, float interactRadius)
    {
        Id = id;
        Position = position;
        InteractRadius = interactRadius;
    }

    protected bool IsPlayerInRange(Vector2 playerPosition) =>
        Vector2.Distance(playerPosition, Position) <= InteractRadius;
}
