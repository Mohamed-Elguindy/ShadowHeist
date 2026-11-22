using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.PlayerSystem.Stances;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the articulated 3D thief character rig, ground stance ring, and suppressed pistol.
/// </summary>
public sealed class PlayerRenderer : IDisposable
{
    private const string CharacterAssetDir = "assets/models/characters";
    private const string ThiefSkinPath = "assets/models/characters/skin_thief.png";

    private readonly CharacterPoseAnimator _poseAnimator = new();
    private CharacterRig? _thiefRig;

    public void Load(ToonModelLibrary models)
    {
        _poseAnimator.Reset();
        if (_thiefRig is not null || !Raylib.IsWindowReady())
        {
            return;
        }

        models.EnsureLoaded();
        _thiefRig = models.LoadCharacterRig(CharacterAssetDir, ThiefSkinPath, Viewport3DConfig.CharacterUnitScale);
    }

    public void UpdateAnimation(Vector2 playerPosition, float deltaTime) =>
        _poseAnimator.UpdateActor("player", playerPosition, deltaTime);

    public void Draw(Player player, GameContext context)
    {
        if (_thiefRig is not CharacterRig thiefRig || !context.Cutscene.IsPlayerVisible())
        {
            return;
        }

        Vector2 drawPosition = context.Cutscene.ResolvePlayerPosition(player.Position);
        float drawRotation = context.Cutscene.ResolvePlayerRotation(player.RotationDegrees);
        Color ringColor = player.CurrentStance is SprintStance
            ? Color.Orange
            : new Color(56, 225, 255, 190);
        CharacterPose pose = _poseAnimator.ComputePose(
            "player",
            thiefRig.Scale,
            context.IsPlayerSneaking && !context.Cutscene.IsActive,
            isGuard: false,
            context.ElapsedSeconds);

        Raylib3DPrimitives.DrawGroundRing(drawPosition, ringColor);
        CharacterMeshDrawer.DrawCharacterRig(
            context.Models,
            thiefRig,
            drawPosition,
            drawRotation,
            pose,
            DrawPlayerPistol);
    }

    public void Dispose() => _thiefRig = null;

    private static void DrawPlayerPistol(Vector3 handPosition) =>
        Raylib.DrawCube(handPosition + new Vector3(0f, -4f, 4f), 2.5f, 3.5f, 11f, new Color(22, 26, 34, 255));
}
