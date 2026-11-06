using ShadowHeist.Core.Events;

namespace ShadowHeist.HeistWorld.UI;

public sealed class SoundMeter
{
    public const float FullMeterNoiseRadius = 220f;
    public const float DecayPerSecond = 0.75f;

    public float Level { get; private set; }

    public SoundMeter(GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        Level = Math.Max(0f, Level - (DecayPerSecond * deltaTime));
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noise)
    {
        if (!IsRelevantNoise(noise))
        {
            return;
        }

        float incomingLevel = Math.Clamp(noise.Radius / FullMeterNoiseRadius, 0f, 1f);
        Level = Math.Max(Level, incomingLevel);
    }

    private static bool IsRelevantNoise(NoiseEmittedEvent noise) => noise.Radius > 0f;
}
