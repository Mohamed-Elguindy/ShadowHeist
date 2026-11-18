using ShadowHeist.PlayerSystem;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class SprintStaminaTests
{
    [Fact]
    public void Stamina_DepletesAndEntersCooldown_WhenSprintingUntilEmpty()
    {
        var stamina = new SprintStamina();

        stamina.Update(SprintStamina.DrainSeconds, isSprinting: true);

        Assert.Equal(0f, stamina.Ratio);
        Assert.True(stamina.IsOnCooldown);
        Assert.False(stamina.CanSprint);
    }

    [Fact]
    public void Stamina_RegeneratesAfterCooldownFinishes()
    {
        var stamina = new SprintStamina();
        stamina.Update(SprintStamina.DrainSeconds, isSprinting: true);

        stamina.Update(SprintStamina.CooldownSeconds, isSprinting: false);
        Assert.False(stamina.IsOnCooldown);

        stamina.Update(SprintStamina.RegenSeconds * 0.5f, isSprinting: false);
        Assert.True(stamina.Ratio > 0.4f);
        Assert.True(stamina.CanSprint);
    }
}

