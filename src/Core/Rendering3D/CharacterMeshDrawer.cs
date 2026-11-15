using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Shared articulated 6-part character mesh drawer reused by both Player and Guard renderers.
/// </summary>
public static class CharacterMeshDrawer
{
    private const float FacingAngleOffsetDegrees = 90f;
    private const float TorsoHeightUnits = 6f;
    private const float ShoulderOffsetX = 2.0f;
    private const float ShoulderOffsetY = 5.5f;
    private const float HeadCounterLeanFactor = 0.6f;
    private const float ArmSwingFactor = 0.85f;
    private const float HandOffsetXUnits = -1f;
    private const float HandOffsetYUnits = -5.2f;
    private const float HandOffsetZUnits = 0.8f;
    private const float LimbOutlineWidth = 0.21f;

    public static void DrawCharacterRig(
        ToonModelLibrary library,
        CharacterRig rig,
        Vector2 position,
        float rotationDegrees,
        CharacterPose pose,
        Action<Vector3> drawHandTool)
    {
        float unitScale = rig.Scale;

        Rlgl.PushMatrix();
        Rlgl.Translatef(position.X, pose.HipHeight, position.Y);
        Rlgl.Rotatef(FacingAngleOffsetDegrees - rotationDegrees, 0f, 1f, 0f);

        DrawLimbPart(library, rig.LegLeft, new Vector3(unitScale, 0f, 0f), pose.LegSwingDegrees, unitScale);
        DrawLimbPart(library, rig.LegRight, new Vector3(-unitScale, 0f, 0f), -pose.LegSwingDegrees, unitScale);

        Rlgl.PushMatrix();
        Rlgl.Rotatef(pose.TorsoLeanDegrees, 1f, 0f, 0f);
        library.DrawOutlined(rig.Body, Vector3.Zero, 0f, unitScale, Color.White, LimbOutlineWidth);
        DrawLimbPart(library, rig.Head, new Vector3(0f, TorsoHeightUnits * unitScale, 0f), -pose.TorsoLeanDegrees * HeadCounterLeanFactor, unitScale);
        DrawLimbPart(library, rig.ArmLeft, new Vector3(ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), -pose.LegSwingDegrees * ArmSwingFactor, unitScale);
        DrawRightArmWithTool(library, rig.ArmRight, new Vector3(-ShoulderOffsetX * unitScale, ShoulderOffsetY * unitScale, 0f), pose.RightArmPitchDegrees, unitScale, drawHandTool);
        Rlgl.PopMatrix();

        Rlgl.PopMatrix();
    }

    private static void DrawLimbPart(ToonModelLibrary library, Model partModel, Vector3 pivot, float pitchDegrees, float scale)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        library.DrawOutlined(partModel, Vector3.Zero, 0f, scale, Color.White, LimbOutlineWidth);
        Rlgl.PopMatrix();
    }

    private static void DrawRightArmWithTool(
        ToonModelLibrary library,
        Model armRight,
        Vector3 pivot,
        float pitchDegrees,
        float scale,
        Action<Vector3> drawHandTool)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pivot.X, pivot.Y, pivot.Z);
        Rlgl.Rotatef(pitchDegrees, 1f, 0f, 0f);
        library.DrawOutlined(armRight, Vector3.Zero, 0f, scale, Color.White, LimbOutlineWidth);

        Vector3 handPosition = new(HandOffsetXUnits * scale, HandOffsetYUnits * scale, HandOffsetZUnits * scale);
        drawHandTool(handPosition);

        Rlgl.PopMatrix();
    }
}
