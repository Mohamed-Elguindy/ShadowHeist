using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.HeistWorld.UI;

namespace ShadowHeist.HeistWorld;

public sealed class HeistWorldModule : IGameSystem, IDisposable
{
    private const string CrateTexturePath = "assets/sprites/prop_loot_crate.png";
    private const float CrateSpriteSize = 64f;

    private readonly HudRenderer _hudRenderer = new();
    private readonly List<LootCrate> _crates = new();
    private Texture2D _crateTexture;
    private bool _texturesLoaded;

    public void Init(GameContext context)
    {
        LoadTextures();
        SpawnInitialCrates(context);
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.E))
        {
            TryInteractWithNearbyObject(context);
        }
    }

    public void Draw(GameContext context)
    {
        DrawCrates();
        _hudRenderer.Draw(context);
    }

    public void Dispose()
    {
        if (_texturesLoaded)
        {
            Raylib.UnloadTexture(_crateTexture);
            _texturesLoaded = false;
        }
    }

    private void SpawnInitialCrates(GameContext context)
    {
        var crate1 = new LootCrate("loot_vault_1", new Vector2(480f, 280f));
        var crate2 = new LootCrate("loot_vault_2", new Vector2(720f, 280f));

        _crates.Add(crate1);
        _crates.Add(crate2);

        context.Interactables.Add(crate1);
        context.Interactables.Add(crate2);
    }

    private static void TryInteractWithNearbyObject(GameContext context)
    {
        foreach (var item in context.Interactables)
        {
            if (item.CanInteract(context.PlayerPosition))
            {
                item.OnInteract(context, context.EventBus);
                break;
            }
        }
    }

    private void DrawCrates()
    {
        foreach (var crate in _crates)
        {
            if (crate.IsCollected) continue;

            if (_texturesLoaded)
            {
                DrawCrateSprite(crate.Position);
            }
            else
            {
                DrawFallbackCrate(crate.Position);
            }
        }
    }

    private void DrawCrateSprite(Vector2 position)
    {
        var sourceRect = new Rectangle(0, 0, _crateTexture.Width, _crateTexture.Height);
        var destRect = new Rectangle(
            position.X - CrateSpriteSize / 2f,
            position.Y - CrateSpriteSize / 2f,
            CrateSpriteSize,
            CrateSpriteSize);

        Raylib.DrawTexturePro(_crateTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
    }

    private static void DrawFallbackCrate(Vector2 position)
    {
        Raylib.DrawRectangleV(position - new Vector2(20f, 20f), new Vector2(40f, 40f), Color.Gold);
    }

    private void LoadTextures()
    {
        if (File.Exists(CrateTexturePath))
        {
            _crateTexture = Raylib.LoadTexture(CrateTexturePath);
            _texturesLoaded = true;
        }
    }
}
