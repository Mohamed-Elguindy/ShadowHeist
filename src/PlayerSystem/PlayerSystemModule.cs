using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Owned by: Mahmoud (ShadowHeist.PlayerSystem)
/// Week 1: Ticket #3 (KeyboardInputProvider) &amp; Ticket #4 (Player Movement + PlayerRenderer)
/// Week 2: Ticket #11 (SneakStance, WalkStance, SprintStance) &amp; Ticket #12 (NoiseEmittedEvent)
/// Week 3: Ticket #19 (PlayerInventory + LINQ) &amp; Ticket #20 (CoinGadget, EmpGadget)
/// Week 4: Ticket #27 (SuppressedPistol) &amp; Ticket #28 (xUnit Unit Tests)
/// </summary>
public sealed class PlayerSystemModule : IGameSystem
{
    public void Init(GameContext context)
    {
        // TODO (Ticket #3 & #4 - Mahmoud): Initialize KeyboardInputProvider, Player, and PlayerRenderer
    }

    public void Update(float deltaTime, GameContext context)
    {
        // TODO (Ticket #4 - Mahmoud): Read PlayerInputCommand and update Player position & stance
    }

    public void Draw(GameContext context)
    {
        // Starter placeholder until Ticket #4 loads "assets/sprites/player_sneak.png"
        Raylib.DrawCircleV(context.PlayerPosition, 14f, Color.SkyBlue);
    }
}
