using ShadowHeist.Core.Events;

namespace ShadowHeist.HeistWorld.UI;

public sealed class SoundMeter
{
    public const float FullMeterNoiseRadius = 300f;
    public const float DecayPerSecond = 0.75f;
    public const float PeakHoldDuration = 0.35f;

    private float _holdTimer;

    public float Level { get; private set; }

    public SoundMeter(GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        if (_holdTimer > 0f)
        {
            _holdTimer -= deltaTime;
            if (_holdTimer > 0f)
            {
                return;
            }

            deltaTime = -_holdTimer;
            _holdTimer = 0f;
        }

        Level = Math.Max(0f, Level - (DecayPerSecond * deltaTime));
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noise)
    {
        if (!IsRelevantNoise(noise))
        {
            return;
        }

        float incomingLevel = Math.Clamp(noise.Radius / FullMeterNoiseRadius, 0f, 1f);
        if (incomingLevel >= Level)
        {
            Level = incomingLevel;
            _holdTimer = PeakHoldDuration;
        }
    }

    private static bool IsRelevantNoise(NoiseEmittedEvent noise) => noise.Radius > 0f;
}
