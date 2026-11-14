using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders environmental and event-driven 3D VFX: lamp glows, vault security seal beams, sound ripples, and loot sparks.
/// </summary>
public sealed class StealthVfxRenderer : ISceneRenderPass
{
    private const int MaxRipples = 16;
    private const float RippleLifetimeSeconds = 0.85f;
    private const float SparkLifetimeSeconds = 0.90f;
    private const float LoudSprintNoiseThreshold = 180f;

    private readonly List<SoundRipple> _ripples = new();
    private readonly List<GoldSpark> _sparks = new();
    private readonly HashSet<string> _collectedCrates = new();

    public void SubscribeEvents(GameEventBus eventBus)
    {
        _ripples.Clear();
        _sparks.Clear();
        _collectedCrates.Clear();
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime, GameContext context)
    {
        UpdateRipples(deltaTime);
        UpdateSparks(deltaTime);
        UpdateLootBursts(context);
    }

    public void Draw(in Scene3DRenderFrame frame)
    {
        DrawLampGlows();
        DrawVaultSecuritySeal(frame.Context, frame.ElapsedSeconds);
        DrawAcousticRipples();
        DrawLootSparks();
    }

    private void UpdateRipples(float deltaTime)
    {
        for (int i = _ripples.Count - 1; i >= 0; i--)
        {
            SoundRipple updated = _ripples[i] with { Age = _ripples[i].Age + deltaTime };
            if (updated.Age >= RippleLifetimeSeconds) _ripples.RemoveAt(i);
            else _ripples[i] = updated;
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            GoldSpark current = _sparks[i];
            GoldSpark updated = new(
                current.Position + (current.Velocity * deltaTime),
                current.Velocity + (new Vector3(0f, -180f, 0f) * deltaTime),
                current.Age + deltaTime);

            if (updated.Age >= SparkLifetimeSeconds) _sparks.RemoveAt(i);
            else _sparks[i] = updated;
        }
    }

    private void UpdateLootBursts(GameContext context)
    {
        if (_collectedCrates.Count > 0 && context.StolenCash == 0)
        {
            _collectedCrates.Clear();
        }

        foreach (LootCrate crate in context.Interactables.OfType<LootCrate>())
        {
            if (crate.IsCollected && _collectedCrates.Add(crate.Id))
            {
                SpawnLootBurst(crate.Position);
            }
        }
    }

    private void SpawnLootBurst(Vector2 origin)
    {
        const int sparkCount = 14;
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = (i / (float)sparkCount) * MathF.PI * 2f;
            var velocity = new Vector3(MathF.Cos(angle) * 55f, 95f + ((i % 3) * 28f), MathF.Sin(angle) * 55f);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, 24f, origin.Y), velocity, Age: 0f));
        }
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noiseEvent)
    {
        if (noiseEvent.Radius <= 0f) return;
        if (_ripples.Count >= MaxRipples) _ripples.RemoveAt(0);

        _ripples.Add(new SoundRipple(
            noiseEvent.Origin,
            MathF.Max(noiseEvent.Radius, 40f),
            ResolveRippleColor(noiseEvent),
            Age: 0f));
    }

    private static Color ResolveRippleColor(NoiseEmittedEvent noiseEvent) => noiseEvent.Category switch
    {
        NoiseCategory.Distraction => new Color(216, 180, 254, 255),
        NoiseCategory.Environment => new Color(244, 63, 94, 255),
        _ when noiseEvent.Radius >= LoudSprintNoiseThreshold => new Color(251, 146, 60, 255),
        _ => new Color(56, 225, 255, 255)
    };

    private static void DrawLampGlows()
    {
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow) continue;

            var center = new Vector3(spec.Position.X, 0.6f, spec.Position.Y);
            Raylib.DrawCylinder(center, 95f, 95f, 0.4f, 20, new Color(255, 214, 120, 34));
            Raylib.DrawCylinder(center, 46f, 46f, 0.6f, 16, new Color(255, 224, 150, 56));
            Raylib.DrawSphere(new Vector3(spec.Position.X, 66f, spec.Position.Y), 7f, new Color(255, 236, 168, 225));
        }
    }

    private static void DrawVaultSecuritySeal(GameContext context, float timeSeconds)
    {
        VaultDoor? door = context.Interactables.OfType<VaultDoor>().FirstOrDefault();
        if (door is not { IsLocked: true }) return;

        byte glowAlpha = (byte)(95f + (50f * MathF.Sin(timeSeconds * 4.5f)));
        byte coreAlpha = (byte)(190f + (60f * MathF.Sin(timeSeconds * 4.5f)));
        var glow = new Color((byte)244, (byte)63, (byte)94, glowAlpha);
        var core = new Color((byte)255, (byte)130, (byte)150, coreAlpha);

        for (int i = 0; i < 3; i++)
        {
            float beamHeight = 24f + (i * 18f);
            var start = new Vector3(door.Position.X - 16f, beamHeight, door.Position.Y - 40f);
            var end = new Vector3(door.Position.X - 16f, beamHeight, door.Position.Y + 40f);
            Raylib3DPrimitives.DrawVolumetricBeam(start, end, glowRadius: 1.4f, coreRadius: 0.45f, glow, core);
        }
    }

    private void DrawAcousticRipples()
    {
        foreach (SoundRipple ripple in _ripples)
        {
            float progress = ripple.Age / RippleLifetimeSeconds;
            float radius = 14f + ((ripple.MaxRadius - 14f) * progress);
            byte alpha = (byte)(215f * (1f - progress));
            var ringColor = new Color(ripple.BaseColor.R, ripple.BaseColor.G, ripple.BaseColor.B, alpha);

            Raylib.DrawCylinderWires(new Vector3(ripple.Origin.X, 2f, ripple.Origin.Y), radius, radius, 0.6f, 28, ringColor);
        }
    }

    private void DrawLootSparks()
    {
        foreach (GoldSpark spark in _sparks)
        {
            float progress = spark.Age / SparkLifetimeSeconds;
            byte alpha = (byte)(255f * (1f - progress));
            Raylib.DrawCube(spark.Position, 5f, 5f, 5f, new Color((byte)248, (byte)196, (byte)44, alpha));
        }
    }

    private readonly record struct SoundRipple(Vector2 Origin, float MaxRadius, Color BaseColor, float Age);
    private readonly record struct GoldSpark(Vector3 Position, Vector3 Velocity, float Age);
}
