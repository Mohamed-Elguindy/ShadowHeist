namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Implemented by the 4 top-level team subsystem modules.
/// </summary>
public interface IGameSystem
{
    void Init(GameContext context);
    void Update(float deltaTime, GameContext context);
    void Draw(GameContext context);
}
