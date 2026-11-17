using System.Numerics;
using ShadowHeist.Core.Events;

namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Strategy Pattern contract for heist gadgets.
/// </summary>
public interface IGadget
{
    string Name { get; }
    int RemainingCharges { get; }
    bool TryUse(Vector2 origin, Vector2 target, GameEventBus eventBus);
}
