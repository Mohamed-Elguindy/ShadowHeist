using System.Numerics;
using Raylib_cs;
using ShadowHeist.PlayerSystem.Gadgets;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders 3D parabolic throw trajectories for coins and EMP grenades plus EMP shockwave domes.
/// </summary>
public sealed class Gadget3DVfxRenderer
{
    private const float ThrowFlightDurationSeconds = 0.30f;
    private const float CoinLingerDurationSeconds = 2.0f;
    private const float EmpWaveDurationSeconds = 0.65f;
    private const float ThrowArcPeakHeightY = 40f;
    private const float ThrowStartHeightY = 26f;
    private const float GroundElevationY = 2.4f;
    private const float CoinRadius = 4.2f;
    private const float CoinPulseAmplitude = 0.6f;
    private const float CoinThickness = 1.6f;
    private const float CoinSpinSpeed = 14f;
    private const int CoinSides = 12;
    private const float EmpGrenadeRadius = 5.2f;
    private const float EmpWaveElevationY = 4f;
    private const float EmpWaveHeight = 2.5f;
    private const int EmpWaveSides = 32;
    private const float EmpActiveRingElevationY = 2.6f;
    private const float EmpPulseSpeed = 6f;
    private const float EmpActiveBaseAlpha = 95f;
    private const float EmpActivePulseAlpha = 55f;
    private const float EmpCoreAlphaScale = 0.45f;

    private static readonly Color GoldCoinColor = new(250, 204, 21, 255);
    private static readonly Color CoinTrailColor = new(216, 180, 254, 150);
    private static readonly Color EmpShockwaveColor = new(56, 225, 255, 210);
    private static readonly Color EmpCoreColor = new(167, 139, 250, 140);

    private readonly List<ThrownEffect> _coinEffects = new();
    private readonly List<ThrownEffect> _empThrows = new();

    public void Reset()
    {
        _coinEffects.Clear();
        _empThrows.Clear();
    }

    public void RecordCoinThrow(Vector2 origin, Vector2 landingPoint) =>
        _coinEffects.Add(new ThrownEffect(origin, landingPoint, 0f, Age: 0f));

    public void RecordEmpThrow(Vector2 origin, Vector2 landingPoint, float maxRadius) =>
        _empThrows.Add(new ThrownEffect(origin, landingPoint, maxRadius, Age: 0f));

    public void Update(float deltaTime)
    {
        float safeDelta = MathF.Max(0f, deltaTime);
        AdvanceEffects(_coinEffects, safeDelta, ThrowFlightDurationSeconds + CoinLingerDurationSeconds);
        AdvanceEffects(_empThrows, safeDelta, ThrowFlightDurationSeconds + EmpWaveDurationSeconds);
    }

    public void Draw(EmpGadget emp, float elapsedSeconds)
    {
        Rlgl.DisableDepthMask();
        DrawCoinEffects(elapsedSeconds);
        DrawEmpThrows();
        DrawActiveEmpAura(emp, elapsedSeconds);
        Rlgl.EnableDepthMask();
    }

    private static void AdvanceEffects(List<ThrownEffect> effects, float deltaTime, float maxAge)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            ThrownEffect updated = effects[i] with { Age = effects[i].Age + deltaTime };
            if (updated.Age >= maxAge) effects.RemoveAt(i);
            else effects[i] = updated;
        }
    }

    private void DrawCoinEffects(float elapsedSeconds)
    {
        foreach (ThrownEffect coin in _coinEffects)
        {
            Vector3 worldPos = ComputeArcPosition(coin, out bool isInFlight);
            float lingerProgress = Math.Clamp((coin.Age - ThrowFlightDurationSeconds) / CoinLingerDurationSeconds, 0f, 1f);
            byte alpha = (byte)(GoldCoinColor.A * (1f - lingerProgress));
            var coinColor = new Color(GoldCoinColor.R, GoldCoinColor.G, GoldCoinColor.B, alpha);

            if (isInFlight)
            {
                DrawThrowTrail(coin.Origin, worldPos, CoinTrailColor);
            }

            float pulseRadius = CoinRadius + (MathF.Sin(elapsedSeconds * CoinSpinSpeed) * CoinPulseAmplitude);
            Raylib.DrawCylinder(worldPos, pulseRadius, pulseRadius, CoinThickness, CoinSides, coinColor);
        }
    }

    private void DrawEmpThrows()
    {
        foreach (ThrownEffect emp in _empThrows)
        {
            Vector3 worldPos = ComputeArcPosition(emp, out bool isInFlight);
            if (isInFlight)
            {
                DrawThrowTrail(emp.Origin, worldPos, EmpShockwaveColor);
                Raylib.DrawSphere(worldPos, EmpGrenadeRadius, EmpShockwaveColor);
                continue;
            }

            float waveProgress = Math.Clamp((emp.Age - ThrowFlightDurationSeconds) / EmpWaveDurationSeconds, 0f, 1f);
            float radius = emp.MaxRadius * waveProgress;
            byte alpha = (byte)(EmpShockwaveColor.A * (1f - waveProgress));
            var ringColor = new Color(EmpShockwaveColor.R, EmpShockwaveColor.G, EmpShockwaveColor.B, alpha);
            var fillColor = new Color(EmpCoreColor.R, EmpCoreColor.G, EmpCoreColor.B, (byte)(alpha * EmpCoreAlphaScale));

            var blastCenter = new Vector3(emp.LandingPoint.X, EmpWaveElevationY, emp.LandingPoint.Y);
            Raylib.DrawCylinder(blastCenter, radius, radius, EmpWaveHeight, EmpWaveSides, fillColor);
            Raylib.DrawCylinderWires(blastCenter, radius, radius, EmpWaveHeight, EmpWaveSides, ringColor);
        }
    }

    private static void DrawActiveEmpAura(EmpGadget emp, float elapsedSeconds)
    {
        if (!emp.IsActivePulse)
        {
            return;
        }

        byte alpha = (byte)(EmpActiveBaseAlpha + (EmpActivePulseAlpha * MathF.Sin(elapsedSeconds * EmpPulseSpeed)));
        var auraColor = new Color(EmpShockwaveColor.R, EmpShockwaveColor.G, EmpShockwaveColor.B, alpha);
        var center = new Vector3(emp.LastLandingPoint.X, EmpActiveRingElevationY, emp.LastLandingPoint.Y);
        Raylib.DrawCylinderWires(center, emp.EmpRadius, emp.EmpRadius, EmpWaveHeight, EmpWaveSides, auraColor);
    }

    private static Vector3 ComputeArcPosition(ThrownEffect effect, out bool isInFlight)
    {
        float flightT = Math.Clamp(effect.Age / ThrowFlightDurationSeconds, 0f, 1f);
        isInFlight = flightT < 1f;
        Vector2 pos2D = Vector2.Lerp(effect.Origin, effect.LandingPoint, flightT);
        float heightY = isInFlight
            ? (ThrowStartHeightY * (1f - flightT)) + (ThrowArcPeakHeightY * MathF.Sin(flightT * MathF.PI))
            : GroundElevationY;
        return new Vector3(pos2D.X, heightY, pos2D.Y);
    }

    private static void DrawThrowTrail(Vector2 origin, Vector3 currentPos, Color color) =>
        Raylib.DrawLine3D(new Vector3(origin.X, ThrowStartHeightY, origin.Y), currentPos, color);

    private readonly record struct ThrownEffect(Vector2 Origin, Vector2 LandingPoint, float MaxRadius, float Age);
}
