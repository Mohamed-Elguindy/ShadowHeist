using ShadowHeist.Core.Events;

namespace ShadowHeist.HeistWorld.UI;

public sealed class SoundMeter
{
    public const float FullMeterNoiseRadius = 300f;
    public const float AttackPerSecond = 1.8f;
    public const float DecayPerSecond = 0.75f;
    public const float PeakHoldDuration = 0.35f;

    private float _targetLevel;
    private float _holdTimer;

    public float Level { get; private set; }
    public float TargetLevel => _targetLevel;

    public SoundMeter(GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        if (_holdTimer > 0f)
        {
            float holdTimeUsed = Math.Min(_holdTimer, deltaTime);
            _holdTimer -= holdTimeUsed;
            float remainingDelta = deltaTime - holdTimeUsed;

            if (Level < _targetLevel)
            {
                Level = Math.Min(_targetLevel, Level + (AttackPerSecond * holdTimeUsed));
            }
            else if (Level > _targetLevel)
            {
                Level = Math.Max(_targetLevel, Level - (DecayPerSecond * holdTimeUsed));
            }

            if (_holdTimer <= 0f)
            {
                _targetLevel = 0f;
                Level = Math.Max(0f, Level - (DecayPerSecond * remainingDelta));
            }
        }
        else
        {
            _targetLevel = 0f;
            Level = Math.Max(0f, Level - (DecayPerSecond * deltaTime));
        }
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noise)
    {
        if (!IsRelevantNoise(noise))
        {
            return;
        }

        float incomingTarget = Math.Clamp(noise.Radius / FullMeterNoiseRadius, 0f, 1f);
        if (incomingTarget >= _targetLevel || _holdTimer <= 0f)
        {
            _targetLevel = incomingTarget;
        }

        _holdTimer = PeakHoldDuration;
    }

    private static bool IsRelevantNoise(NoiseEmittedEvent noise) => noise.Radius > 0f;
}
