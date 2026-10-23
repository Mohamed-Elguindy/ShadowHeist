using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld;

/// <summary>
/// Owned by: Guindy (ShadowHeist.HeistWorld)
/// Week 1: Ticket #1 (Core Skeleton) &amp; Ticket #2 (LootCrate + HudRenderer)
/// Week 2: Ticket #9 (VaultDoor + KeypadTerminal) &amp; Ticket #10 (Sound Meter HUD)
/// Week 3: Ticket #17 (FuseBox + LaserSwitch) &amp; Ticket #18 (TacticalCamera2D)
/// Week 4: Ticket #25 (GameStateManager + GetawayVanZone) &amp; Ticket #26 (Summary Screen)
/// </summary>
public sealed class HeistWorldModule : IGameSystem
{
    public void Init(GameContext context)
    {
        // TODO (Ticket #2 - Guindy): Register initial IInteractable objects and HUD
    }

    public void Update(float deltaTime, GameContext context)
    {
        // TODO (Ticket #2 & #9 - Guindy): Update active interactables and HUD animations
    }

    public void Draw(GameContext context)
    {
        Raylib.DrawText($"STOLEN CASH: ${context.StolenCash:N0}", 20, 20, 20, Color.Lime);
    }
}
