using System.Numerics;
using ShadowHeist.Core;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class CoreLogicTests
{
    [Fact]
    public void HasLineOfSight_RespectsIgnoredObstacle()
    {
        var blocker = new RectangleBounds(150f, 80f, 20f, 80f);
        var walls = new List<RectangleBounds> { blocker };

        Assert.False(Geometry2D.HasLineOfSight(new Vector2(100f, 120f), new Vector2(220f, 120f), walls));
        Assert.True(Geometry2D.HasLineOfSight(new Vector2(100f, 120f), new Vector2(220f, 120f), walls, ignoredObstacle: blocker));
    }

    [Fact]
    public void RestartMission_ResetsPlayerPositionAndRotationWithoutWorldModule()
    {
        using var context = new GameContext
        {
            PlayerPosition = new Vector2(999f, 555f),
            PlayerRotationDegrees = 135f,
            StolenCash = 250_000,
            IsAlarmTriggered = true,
            Outcome = HeistOutcome.Caught
        };

        context.RestartMission();

        Assert.Equal(new Vector2(160f, 360f), context.PlayerPosition);
        Assert.Equal(0f, context.PlayerRotationDegrees);
        Assert.Equal(0, context.StolenCash);
        Assert.False(context.IsAlarmTriggered);
        Assert.Equal(HeistOutcome.Playing, context.Outcome);
    }
}
