using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders declarative 3D furniture models and their ground shadows across the bank lobby.
/// </summary>
public sealed class Furniture3DRenderer
{
    private const string FurnitureFolder = "assets/models/furniture";
    private const string RugAssetKey = "rugRectangle";
    private const float RugLiftY = 1.2f;
    private const float PropOutlineWidth = 0.012f;

    private readonly Dictionary<string, LoadedModel> _furnitureModels = new();
    private ToonModelLibrary? _library;

    public void Load(ToonModelLibrary library)
    {
        _library = library;
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (_furnitureModels.ContainsKey(spec.AssetKey))
            {
                continue;
            }

            string modelPath = Path.Combine(FurnitureFolder, $"{spec.AssetKey}.obj");
            if (library.LoadProp(modelPath, spec.TargetMaxExtent) is LoadedModel loaded)
            {
                _furnitureModels[spec.AssetKey] = loaded;
            }
        }
    }

    public void Draw()
    {
        if (_library is null)
        {
            return;
        }

        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            DrawFurniturePiece(spec);
        }
    }

    private void DrawFurniturePiece(FurnitureSpec spec)
    {
        if (!_furnitureModels.TryGetValue(spec.AssetKey, out LoadedModel model))
        {
            return;
        }

        bool isRug = spec.AssetKey == RugAssetKey;
        if (!isRug)
        {
            Raylib3DPrimitives.DrawGroundShadow(spec.Position, spec.TargetMaxExtent * 0.42f);
        }

        float verticalOffset = model.BaseOffsetY + (isRug ? RugLiftY : 0f);
        var worldPosition = new Vector3(spec.Position.X, verticalOffset, spec.Position.Y);
        float outlineWidth = isRug ? 0f : PropOutlineWidth;

        _library!.DrawCenteredProp(model, worldPosition, spec.RotationDegrees, Color.White, outlineWidth);
    }
}
