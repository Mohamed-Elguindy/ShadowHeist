using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.GuardAI.Pathfinding;

/// <summary>
/// 2D A* shortest-path solver over a GridGraph so guards navigate cleanly around walls and rooms (Ticket #14 — Hatem).
/// </summary>
public static class AStarPathfinder
{
    private const float StepCost = 1f;

    public static IReadOnlyList<Vector2> FindPath(
        Vector2 startWorld,
        Vector2 targetWorld,
        IReadOnlyList<RectangleBounds> walls,
        float cellSize = GridGraph.DefaultCellSize)
    {
        ArgumentNullException.ThrowIfNull(walls);

        if (walls.Count == 0)
        {
            return new List<Vector2> { targetWorld };
        }

        var graph = new GridGraph(walls, cellSize);
        return FindPath(graph, startWorld, targetWorld);
    }

    public static IReadOnlyList<Vector2> FindPath(GridGraph graph, Vector2 startWorld, Vector2 targetWorld)
    {
        ArgumentNullException.ThrowIfNull(graph);

        GridCoord start = graph.FindNearestWalkable(graph.WorldToGrid(startWorld));
        GridCoord goal = graph.FindNearestWalkable(graph.WorldToGrid(targetWorld));

        if (!graph.IsWalkable(start) || !graph.IsWalkable(goal))
        {
            return Array.Empty<Vector2>();
        }

        if (start == goal)
        {
            return new List<Vector2> { targetWorld };
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
            float fScore = tentativeG + ComputeHeuristic(neighbor, goal);
            openSet.Enqueue(neighbor, fScore);
        }
    }

    private static float ComputeHeuristic(GridCoord a, GridCoord b) =>
        Math.Abs(a.Col - b.Col) + Math.Abs(a.Row - b.Row);

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
        for (int i = 0; i < gridNodes.Count - 1; i++)
        {
            worldWaypoints.Add(graph.GridToWorld(gridNodes[i]));
        }

        worldWaypoints.Add(exactTargetWorld);
        return worldWaypoints;
    }
}

