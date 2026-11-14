using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Manages isometric 3D camera smoothing, follow/overview framing, zoom clamping, and mouse-to-ground raycasting.
/// </summary>
public sealed class TacticalCameraController
{
    private const float MaxGroundRayDistance = 5000f;
    private const float MinDownwardRaySlope = -0.001f;

    private Camera3D _camera = new()
    {
        Up = Vector3.UnitY,
        FovY = Viewport3DConfig.FollowFovDegrees,
        Projection = CameraProjection.Perspective
    };

    private Vector3 _focusPoint;
    private bool _hasInitialFocus;
    private float _zoom = 1f;

    public Camera3D Camera => _camera;
    public bool IsOverviewMode { get; private set; }

    public void Reset(Vector2 playerPosition)
    {
        _hasInitialFocus = false;
        Update(0f, playerPosition);
    }

    public void ToggleOverviewMode() => IsOverviewMode = !IsOverviewMode;

    public void ApplyZoomWheel(float wheelDelta)
    {
        if (MathF.Abs(wheelDelta) <= 0.01f)
        {
            return;
        }

        _zoom = Math.Clamp(
            _zoom - (wheelDelta * Viewport3DConfig.ZoomStep),
            Viewport3DConfig.MinZoom,
            Viewport3DConfig.MaxZoom);
    }

    public void Update(float deltaTime, Vector2 playerPosition)
    {
        Vector3 followTarget = Vector3.Lerp(
            new Vector3(playerPosition.X, 0f, playerPosition.Y),
            Viewport3DConfig.OverviewTarget,
            Viewport3DConfig.FollowOverviewBlend);

        Vector3 desiredTarget = IsOverviewMode ? Viewport3DConfig.OverviewTarget : followTarget;
        Vector3 desiredOffset = (IsOverviewMode
            ? Viewport3DConfig.OverviewCameraOffset
            : Viewport3DConfig.FollowCameraOffset) * _zoom;

        float blendFactor = MathF.Min(1f, Viewport3DConfig.CameraFollowSpeed * deltaTime);
        _focusPoint = _hasInitialFocus
            ? Vector3.Lerp(_focusPoint, desiredTarget, blendFactor)
            : desiredTarget;
        _hasInitialFocus = true;

        _camera.FovY = IsOverviewMode ? Viewport3DConfig.OverviewFovDegrees : Viewport3DConfig.FollowFovDegrees;
        _camera.Target = _focusPoint;
        _camera.Position = _focusPoint + desiredOffset;
    }

    public Vector2? ScreenToGroundPoint(Vector2 screenPosition)
    {
        Ray ray = Raylib.GetMouseRay(screenPosition, _camera);
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
