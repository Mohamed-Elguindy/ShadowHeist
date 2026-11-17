using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.GuardAI.Pathfinding;

/// <summary>
/// 2D A* shortest-path solver over a GridGraph so guards navigate cleanly around walls and rooms (Ticket #14 — Hatem).
/// </summary>
public static class AStarPathfinder
{
    private const float StepCost = 1f;

    private static IReadOnlyList<RectangleBounds>? _cachedWalls;
    private static int _cachedWallCount;
    private static float _cachedCellSize;
    private static GridGraph? _cachedGraph;

    public static IReadOnlyList<Vector2> FindPath(
        Vector2 startWorld,
        Vector2 targetWorld,
        IReadOnlyList<RectangleBounds> walls,
        float cellSize = GridGraph.DefaultCellSize)
    {
        if (walls.Count == 0)
        {
            return new List<Vector2> { targetWorld };
        }

        GridGraph graph = GetOrCreateGraph(walls, cellSize);
        return FindPath(graph, startWorld, targetWorld);
    }

    public static IReadOnlyList<Vector2> FindPath(GridGraph graph, Vector2 startWorld, Vector2 targetWorld)
    {
        GridCoord start = graph.FindNearestWalkable(graph.WorldToGrid(startWorld));
        GridCoord goal = graph.FindNearestWalkable(graph.WorldToGrid(targetWorld));

        if (!graph.IsWalkable(start) || !graph.IsWalkable(goal))
        {
            return Array.Empty<Vector2>();
        }

        if (start == goal)
        {
            Vector2 sameCellTarget = graph.IsWalkable(graph.WorldToGrid(targetWorld))
                ? targetWorld
                : graph.GridToWorld(goal);
            return new List<Vector2> { sameCellTarget };
        }

        var openSet = new PriorityQueue<GridCoord, float>();
        var cameFrom = new Dictionary<GridCoord, GridCoord>();
        var gScore = new Dictionary<GridCoord, float> { [start] = 0f };

        openSet.Enqueue(start, ComputeHeuristic(start, goal));

        while (openSet.Count > 0)
        {
            GridCoord current = openSet.Dequeue();
            if (current == goal)
            {
                return ReconstructWorldPath(graph, cameFrom, start, goal, targetWorld);
            }

            ExpandNeighbors(graph, current, goal, gScore, cameFrom, openSet);
        }

        return Array.Empty<Vector2>();
    }

    private static GridGraph GetOrCreateGraph(IReadOnlyList<RectangleBounds> walls, float cellSize)
    {
        if (_cachedGraph is not null &&
            ReferenceEquals(_cachedWalls, walls) &&
            _cachedWallCount == walls.Count &&
            MathF.Abs(_cachedCellSize - cellSize) < float.Epsilon)
        {
            return _cachedGraph;
        }

        _cachedWalls = walls;
        _cachedWallCount = walls.Count;
        _cachedCellSize = cellSize;
        _cachedGraph = new GridGraph(walls, cellSize);
        return _cachedGraph;
    }

    private static void ExpandNeighbors(
        GridGraph graph,
        GridCoord current,
        GridCoord goal,
        Dictionary<GridCoord, float> gScore,
        Dictionary<GridCoord, GridCoord> cameFrom,
        PriorityQueue<GridCoord, float> openSet)
    {
        float currentCost = gScore[current];

        foreach (GridCoord neighbor in graph.GetWalkableNeighbors(current))
        {
            float tentativeG = currentCost + StepCost;
            if (gScore.TryGetValue(neighbor, out float existingG) && tentativeG >= existingG)
            {
                continue;
            }

            cameFrom[neighbor] = current;
            gScore[neighbor] = tentativeG;
            openSet.Enqueue(neighbor, tentativeG + ComputeHeuristic(neighbor, goal));
        }
    }

    private static float ComputeHeuristic(GridCoord firstCoord, GridCoord secondCoord) =>
        Math.Abs(firstCoord.Col - secondCoord.Col) + Math.Abs(firstCoord.Row - secondCoord.Row);

    private static List<Vector2> ReconstructWorldPath(
        GridGraph graph,
        Dictionary<GridCoord, GridCoord> cameFrom,
        GridCoord start,
        GridCoord goal,
        Vector2 exactTargetWorld)
    {
        var gridNodes = new List<GridCoord>();
        GridCoord cursor = goal;

        while (cursor != start)
        {
            gridNodes.Add(cursor);
            cursor = cameFrom[cursor];
        }

        gridNodes.Reverse();

        var worldWaypoints = new List<Vector2>(gridNodes.Count);
        for (int nodeIndex = 0; nodeIndex < gridNodes.Count - 1; nodeIndex++)
        {
            worldWaypoints.Add(graph.GridToWorld(gridNodes[nodeIndex]));
        }

        Vector2 finalTarget = graph.IsWalkable(graph.WorldToGrid(exactTargetWorld))
            ? exactTargetWorld
            : graph.GridToWorld(goal);
        worldWaypoints.Add(finalTarget);
        return worldWaypoints;
    }
}
