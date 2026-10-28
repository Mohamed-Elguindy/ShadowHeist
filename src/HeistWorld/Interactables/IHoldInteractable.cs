using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.Interactables;

public interface IHoldInteractable : IInteractable
{
    float HoldProgress { get; }

    void Hold(float deltaTime, GameContext context, GameEventBus eventBus);

    void Release();
}
