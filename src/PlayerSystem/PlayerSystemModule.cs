using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Input;
using ShadowHeist.PlayerSystem.Rendering;

namespace ShadowHeist.PlayerSystem;

/// <summary>
/// Owned by: Mahmoud (ShadowHeist.PlayerSystem)
/// Week 1: Ticket #3 (KeyboardInputProvider) & Ticket #4 (Player Movement + PlayerRenderer)
/// Week 2: Ticket #11 (SneakStance, WalkStance, SprintStance) & Ticket #12 (NoiseEmittedEvent)
/// Week 3: Ticket #19 (PlayerInventory + LINQ) & Ticket #20 (CoinGadget, EmpGadget)
/// Week 4: Ticket #27 (SuppressedPistol) & Ticket #28 (xUnit Unit Tests)
/// </summary>
public sealed class PlayerSystemModule : IGameSystem, IDisposable
{
    private readonly IInputProvider _input;
    private Player _player = null!;
    private PlayerRenderer? _renderer;

    public PlayerSystemModule(IInputProvider? input = null)
    {
        _input = input ?? new KeyboardInputProvider();
    }

    public void Init(GameContext context)
    {
        _player = new Player(context.PlayerPosition);
        _renderer = new PlayerRenderer();
    }

    public void Update(float deltaTime, GameContext context)
    {
        context.CurrentInput = _input.ReadInput();
        _player.Update(context.CurrentInput, deltaTime, context.Walls, context.EventBus);
        context.PlayerPosition = _player.Position;
    }

    public void Draw(GameContext context) => _renderer?.Draw(_player);

    public void Dispose() => _renderer?.Dispose();
}
