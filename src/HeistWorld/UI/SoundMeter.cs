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
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        float activeHoldDelta = ConsumeHoldTime(deltaTime);
        MoveTowardsTarget(activeHoldDelta);

        if (_holdTimer <= 0f)
        {
            _targetLevel = 0f;
            MoveTowardsTarget(deltaTime - activeHoldDelta);
        }
    }

    private float ConsumeHoldTime(float deltaTime)
    {
        if (_holdTimer <= 0f)
        {
            return 0f;
        }

        float consumed = Math.Min(_holdTimer, deltaTime);
        _holdTimer -= consumed;
        return consumed;
    }

    private void MoveTowardsTarget(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (Level < _targetLevel)
        {
            Level = Math.Min(_targetLevel, Level + (AttackPerSecond * deltaTime));
        }
        else if (Level > _targetLevel)
        {
            Level = Math.Max(_targetLevel, Level - (DecayPerSecond * deltaTime));
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
            _holdTimer = PeakHoldDuration;
        }
    }

    private static bool IsRelevantNoise(NoiseEmittedEvent noise) =>
        noise.Category == NoiseCategory.Player && noise.Radius > 0f;
}
