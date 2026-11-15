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
    private const float GravityY = -180f;
    private const float HorizontalSpeed = 55f;
    private const float BaseUpwardSpeed = 95f;
    private const float UpwardSpeedStep = 28f;
    private const float SpawnHeightY = 24f;
    private const float SparkCubeSize = 5f;
    private const int SparksPerBurst = 14;

    private static readonly Color SparkBaseColor = new(248, 196, 44, 255);

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
            var sparkColor = new Color(SparkBaseColor.R, SparkBaseColor.G, SparkBaseColor.B, alpha);
            Raylib.DrawCube(spark.Position, SparkCubeSize, SparkCubeSize, SparkCubeSize, sparkColor);
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        var gravityStep = new Vector3(0f, GravityY, 0f) * deltaTime;
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            GoldSpark current = _sparks[i];
            GoldSpark updated = new(
                current.Position + (current.Velocity * deltaTime),
                current.Velocity + gravityStep,
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
            var velocity = new Vector3(
                MathF.Cos(angle) * HorizontalSpeed,
                BaseUpwardSpeed + ((i % 3) * UpwardSpeedStep),
                MathF.Sin(angle) * HorizontalSpeed);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, SpawnHeightY, origin.Y), velocity, Age: 0f));
        }
    }

    private readonly record struct GoldSpark(Vector3 Position, Vector3 Velocity, float Age);
}
