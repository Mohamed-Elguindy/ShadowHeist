using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Spawns and animates 3D gold particle bursts when loot crates are collected.
/// </summary>
public sealed class LootSparkRenderer
{
    private const float SparkLifetimeSeconds = 0.90f;
    private const int SparksPerBurst = 14;

    private readonly List<GoldSpark> _sparks = new();
    private readonly HashSet<string> _collectedCrates = new();

    public void Reset()
    {
        _sparks.Clear();
        _collectedCrates.Clear();
    }

    public void Update(float deltaTime, GameContext context)
    {
        UpdateSparks(deltaTime);

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

    public void Draw()
    {
        foreach (GoldSpark spark in _sparks)
        {
            float progress = spark.Age / SparkLifetimeSeconds;
            byte alpha = (byte)(255f * (1f - progress));
            Raylib.DrawCube(spark.Position, 5f, 5f, 5f, new Color((byte)248, (byte)196, (byte)44, alpha));
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

            if (updated.Age >= SparkLifetimeSeconds)
            {
                _sparks.RemoveAt(i);
            }
            else
            {
                _sparks[i] = updated;
            }
        }
    }

    private void SpawnLootBurst(Vector2 origin)
    {
        for (int i = 0; i < SparksPerBurst; i++)
        {
            float angle = (i / (float)SparksPerBurst) * MathF.PI * 2f;
            var velocity = new Vector3(MathF.Cos(angle) * 55f, 95f + ((i % 3) * 28f), MathF.Sin(angle) * 55f);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, 24f, origin.Y), velocity, Age: 0f));
        }
    }

    private readonly record struct GoldSpark(Vector3 Position, Vector3 Velocity, float Age);
}
