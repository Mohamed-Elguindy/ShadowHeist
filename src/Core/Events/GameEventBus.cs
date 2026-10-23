namespace ShadowHeist.Core.Events;

/// <summary>
/// Lightweight publish/subscribe Event Bus to decouple Player, GuardAI, WorldLighting, and HeistWorld.
/// </summary>
public sealed class GameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        ArgumentNullException.ThrowIfNull(handler);
        var eventType = typeof(TEvent);
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            _subscribers[eventType] = handlers;
        }
        handlers.Add(handler);
    }

    public void Publish<TEvent>(TEvent gameEvent) where TEvent : struct
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        foreach (var subscriber in handlers.ToArray())
        {
            ((Action<TEvent>)subscriber).Invoke(gameEvent);
        }
    }
}
