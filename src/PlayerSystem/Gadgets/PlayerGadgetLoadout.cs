using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Gadgets;

/// <summary>
/// Manages the player's equipped IGadget list, active gadget selection, and per-frame gadget timers.
/// </summary>
public sealed class PlayerGadgetLoadout
{
    private readonly List<IGadget> _gadgets = new();

    public CoinGadget Coin { get; private set; } = new();
    public EmpGadget Emp { get; private set; } = new();
    public IReadOnlyList<IGadget> Gadgets => _gadgets;
    public int SelectedIndex { get; private set; }
    public IGadget ActiveGadget => _gadgets[SelectedIndex];

    public PlayerGadgetLoadout() => Reset();

    public void Reset()
    {
        Coin = new CoinGadget();
        Emp = new EmpGadget();
        _gadgets.Clear();
        _gadgets.Add(Coin);
        _gadgets.Add(Emp);
        SelectedIndex = 0;
    }

    public void SelectIndex(int index)
    {
        if (_gadgets.Count == 0)
        {
            return;
        }

        SelectedIndex = Math.Clamp(index, 0, _gadgets.Count - 1);
    }

    public void CycleNext()
    {
        if (_gadgets.Count == 0)
        {
            return;
        }

        SelectedIndex = (SelectedIndex + 1) % _gadgets.Count;
    }

    public void HandleKeyboardSelection()
    {
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            SelectIndex(0);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            SelectIndex(1);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            CycleNext();
        }
    }

    public IGadget? TryUseActiveGadget(
        Vector2 origin,
        Vector2 aimTarget,
        IReadOnlyList<RectangleBounds> walls,
        GameEventBus eventBus)
    {
        Coin.SetWalls(walls);
        Emp.SetWalls(walls);
        IGadget active = ActiveGadget;
        return active.TryUse(origin, aimTarget, eventBus) ? active : null;
    }

    public void Update(float deltaTime, GameEventBus eventBus) =>
        Emp.Update(deltaTime, eventBus);

    public void OnPowerStateChanged(PowerStateChangedEvent evt) =>
        Emp.ObserveExternalPowerEvent(evt);
}
