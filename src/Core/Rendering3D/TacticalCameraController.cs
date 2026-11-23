using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Manages isometric 3D camera smoothing, tactical peek, alarm shake, zoom clamping, and ground raycasting.
/// </summary>
public sealed class TacticalCameraController
{
    private const float MaxGroundRayDistance = 5000f;
    private const float MinDownwardRaySlope = -0.001f;
    private const float MinWheelDelta = 0.01f;
    private const float MenuOrbitSpeedRadians = 0.16f;
    private const float MenuOrbitRadiusX = 90f;
    private const float MenuOrbitRadiusZ = 55f;

    private readonly CameraShakeEffect _shake = new();
    private Camera3D _camera = new()
    {
        Up = Vector3.UnitY,
        FovY = Viewport3DConfig.FollowFovDegrees,
        Projection = CameraProjection.Perspective
    };

    private Vector3 _focusPoint;
    private Vector3 _unpeekedFocusPoint;
    private Vector3 _desiredPeekOffset;
    private float _zoom = 1f;
    private bool _hasInitialFocus;

    public Camera3D Camera => _camera;
    public Vector3 FocusPoint => _focusPoint;
    public Vector3 CurrentPeekOffset => _desiredPeekOffset;
    public Vector3 CurrentShakeOffset => _shake.CurrentOffset;
    public float ShakeTrauma => _shake.Trauma;
    public bool IsOverviewMode { get; private set; }
    public float CutsceneZoomMultiplier { get; set; } = 1f;

    public void Reset(Vector2 playerPosition)
    {
        _hasInitialFocus = false;
        _desiredPeekOffset = Vector3.Zero;
        CutsceneZoomMultiplier = 1f;
        _shake.Reset();
        Update(0f, playerPosition);
    }

    public void ToggleOverviewMode() => IsOverviewMode = !IsOverviewMode;

    public void ApplyZoomWheel(float wheelDelta)
    {
        if (MathF.Abs(wheelDelta) <= MinWheelDelta)
        {
            return;
        }

        _zoom = Math.Clamp(
            _zoom - (wheelDelta * Viewport3DConfig.ZoomStep),
            Viewport3DConfig.MinZoom,
            Viewport3DConfig.MaxZoom);
    }

    public void SetTacticalPeek(bool isPeeking, Vector2 playerPosition, Vector2 aimWorldPosition)
    {
        _desiredPeekOffset = isPeeking && !IsOverviewMode
            ? ComputePeekOffset(playerPosition, aimWorldPosition)
            : Vector3.Zero;
    }

    public void UpdatePeekFromScreen(bool isPeeking, Vector2 mouseScreenPosition, Vector2 playerPosition)
    {
        if (!isPeeking || IsOverviewMode)
        {
            _desiredPeekOffset = Vector3.Zero;
            return;
        }

        Vector2? groundPoint = ProjectScreenToGround(mouseScreenPosition, BuildUnpeekedCamera(playerPosition));
        if (groundPoint.HasValue)
        {
            SetTacticalPeek(true, playerPosition, groundPoint.Value);
        }
    }

    public void TriggerAlarmShake(float intensity = 1f) => _shake.Trigger(intensity);

    public void NotifyAlarmState(bool isAlarmActive) => _shake.NotifyAlarmState(isAlarmActive);

    public void Update(float deltaTime, Vector2 playerPosition)
    {
        UpdateFocusPoints(deltaTime, playerPosition);
        _shake.Update(deltaTime);
        ApplyCameraTransform();
    }

    public void UpdateMenuOrbit(float elapsedSeconds)
    {
        float angle = elapsedSeconds * MenuOrbitSpeedRadians;
        Vector3 orbitTarget = Viewport3DConfig.OverviewTarget + new Vector3(
            MathF.Sin(angle) * MenuOrbitRadiusX,
            0f,
            MathF.Cos(angle) * MenuOrbitRadiusZ);

        _camera.FovY = Viewport3DConfig.OverviewFovDegrees;
        _camera.Target = orbitTarget;
        _camera.Position = orbitTarget + Viewport3DConfig.OverviewCameraOffset;
    }

    public Vector2? ScreenToGroundPoint(Vector2 screenPosition) =>
        ProjectScreenToGround(screenPosition, BuildCameraAt(_focusPoint));

    private static Vector3 ComputePeekOffset(Vector2 playerPosition, Vector2 aimWorldPosition)
    {
        Vector2 delta = aimWorldPosition - playerPosition;
        float distance = delta.Length();
        if (distance <= Viewport3DConfig.PeekActivationDeadzone)
        {
            return Vector3.Zero;
        }

        float peekLength = MathF.Min(distance * Viewport3DConfig.PeekDistanceScale, Viewport3DConfig.MaxPeekDistance);
        Vector2 direction = delta / distance;
        return new Vector3(direction.X * peekLength, 0f, direction.Y * peekLength);
    }

    private void UpdateFocusPoints(float deltaTime, Vector2 playerPosition)
    {
        Vector3 baseTarget = ComputeBaseTarget(playerPosition);
        Vector3 peekedTarget = IsOverviewMode ? baseTarget : baseTarget + _desiredPeekOffset;
        float blendFactor = MathF.Min(1f, Viewport3DConfig.CameraFollowSpeed * deltaTime);

        _unpeekedFocusPoint = InterpolateFocus(_unpeekedFocusPoint, baseTarget, blendFactor);
        _focusPoint = InterpolateFocus(_focusPoint, peekedTarget, blendFactor);
        _hasInitialFocus = true;
    }

    private Vector3 InterpolateFocus(Vector3 current, Vector3 target, float blendFactor) =>
        _hasInitialFocus ? Vector3.Lerp(current, target, blendFactor) : target;

    private Vector3 ComputeBaseTarget(Vector2 playerPosition)
    {
        if (IsOverviewMode)
        {
            return Viewport3DConfig.OverviewTarget;
        }

        return Vector3.Lerp(
            new Vector3(playerPosition.X, 0f, playerPosition.Y),
            Viewport3DConfig.OverviewTarget,
            Viewport3DConfig.FollowOverviewBlend);
    }

    private void ApplyCameraTransform()
    {
        _camera.FovY = ResolveActiveFov();
        _camera.Target = _focusPoint + _shake.CurrentOffset;
        _camera.Position = _camera.Target + ComputeActiveOffset();
    }

    private float ResolveActiveFov() =>
        IsOverviewMode ? Viewport3DConfig.OverviewFovDegrees : Viewport3DConfig.FollowFovDegrees;

    private Vector3 ComputeActiveOffset() =>
        (IsOverviewMode ? Viewport3DConfig.OverviewCameraOffset : Viewport3DConfig.FollowCameraOffset) * (_zoom * CutsceneZoomMultiplier);

    private Camera3D BuildUnpeekedCamera(Vector2 playerPosition) =>
        BuildCameraAt(_hasInitialFocus ? _unpeekedFocusPoint : ComputeBaseTarget(playerPosition));

    private Camera3D BuildCameraAt(Vector3 target) =>
        new()
        {
            Up = Vector3.UnitY,
            Projection = CameraProjection.Perspective,
            FovY = ResolveActiveFov(),
            Target = target,
            Position = target + ComputeActiveOffset()
        };

    private static Vector2? ProjectScreenToGround(Vector2 screenPosition, Camera3D camera)
    {
        Ray ray = Raylib.GetMouseRay(screenPosition, camera);
        if (ray.Direction.Y >= MinDownwardRaySlope)
        {
            return null;
        }

        float hitDistance = -ray.Position.Y / ray.Direction.Y;
        if (hitDistance <= 0f || hitDistance >= MaxGroundRayDistance)
        {
            return null;
        }

        Vector3 hitPoint = ray.Position + (ray.Direction * hitDistance);
        return new Vector2(hitPoint.X, hitPoint.Z);
    }
}
