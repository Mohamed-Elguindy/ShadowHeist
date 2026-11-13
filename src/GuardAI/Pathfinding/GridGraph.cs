using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.GuardAI.Pathfinding;

/// <summary>
/// Immutable 2D grid coordinate (Column, Row) for tile-based pathfinding.
/// </summary>
public readonly record struct GridCoord(int Col, int Row);

/// <summary>
/// 2D walkability grid constructed from world-space wall bounds (Ticket #14 — Hatem).
/// </summary>
public sealed class GridGraph
{
    public const float DefaultCellSize = 32f;
    public const float DefaultMapWidth = 1280f;
    public const float DefaultMapHeight = 768f;
    public const float DefaultClearance = 12f;

    private const int MaxSearchRadius = 6;
    private const float CellCenterOffset = 0.5f;

    private static readonly GridCoord[] CardinalDirections =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    };

    private readonly bool[,] _walkable;

    public int Columns { get; }
    public int Rows { get; }
    public float CellSize { get; }

    public GridGraph(
        IReadOnlyList<RectangleBounds> walls,
        float cellSize = DefaultCellSize,
        float mapWidth = DefaultMapWidth,
        float mapHeight = DefaultMapHeight,
        float clearance = DefaultClearance)
    {
        ArgumentNullException.ThrowIfNull(walls);
        if (cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
        }

        CellSize = cellSize;
        Columns = Math.Max(1, (int)MathF.Ceiling(mapWidth / cellSize));
        Rows = Math.Max(1, (int)MathF.Ceiling(mapHeight / cellSize));
        _walkable = new bool[Columns, Rows];

        BuildWalkabilityGrid(walls, clearance);
    }

    public bool IsInBounds(GridCoord coord) =>
        coord.Col >= 0 && coord.Col < Columns && coord.Row >= 0 && coord.Row < Rows;

    public bool IsWalkable(GridCoord coord) =>
        IsInBounds(coord) && _walkable[coord.Col, coord.Row];

    public GridCoord WorldToGrid(Vector2 worldPosition)
    {
        int col = Math.Clamp((int)(worldPosition.X / CellSize), 0, Columns - 1);
        int row = Math.Clamp((int)(worldPosition.Y / CellSize), 0, Rows - 1);
        return new GridCoord(col, row);
    }

    public Vector2 GridToWorld(GridCoord coord) =>
        new((coord.Col + CellCenterOffset) * CellSize, (coord.Row + CellCenterOffset) * CellSize);

    public GridCoord FindNearestWalkable(GridCoord origin)
    {
        if (IsWalkable(origin))
        {
            return origin;
        }

        for (int radius = 1; radius <= MaxSearchRadius; radius++)
        {
            if (TryFindWalkableAtRadius(origin, radius, out var found))
            {
                return found;
            }
        }

        return origin;
    }

    public IEnumerable<GridCoord> GetWalkableNeighbors(GridCoord coord)
    {
        foreach (var dir in CardinalDirections)
        {
            var neighbor = new GridCoord(coord.Col + dir.Col, coord.Row + dir.Row);
            if (IsWalkable(neighbor))
            {
                yield return neighbor;
            }
        }
    }

    private void BuildWalkabilityGrid(IReadOnlyList<RectangleBounds> walls, float clearance)
    {
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                Vector2 center = GridToWorld(new GridCoord(col, row));
                _walkable[col, row] = !OverlapsAnyWall(center, walls, clearance);
            }
        }
    }

    private static bool OverlapsAnyWall(Vector2 point, IReadOnlyList<RectangleBounds> walls, float clearance)
    {
        foreach (var wall in walls)
        {
            if (point.X >= wall.X - clearance &&
                point.X <= wall.X + wall.Width + clearance &&
                point.Y >= wall.Y - clearance &&
                point.Y <= wall.Y + wall.Height + clearance)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindWalkableAtRadius(GridCoord origin, int radius, out GridCoord result)
    {
        for (int dCol = -radius; dCol <= radius; dCol++)
        {
            for (int dRow = -radius; dRow <= radius; dRow++)
            {
                if (Math.Abs(dCol) != radius && Math.Abs(dRow) != radius)
                {
                    continue;
                }

                var candidate = new GridCoord(origin.Col + dCol, origin.Row + dRow);
                if (IsWalkable(candidate))
                {
                    result = candidate;
                    return true;
                }
            }
        }

        result = origin;
        return false;
    }
}

