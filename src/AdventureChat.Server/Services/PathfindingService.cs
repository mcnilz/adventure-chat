using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public sealed class PathfindingService : IPathfindingService
{
    public PathResult CalculatePath(RoomGeometry geometry, Point2D start, Point2D target)
    {
        if (!start.IsFinite() || !target.IsFinite())
        {
            return PathResult.Fail("Coordinates must be finite numbers.");
        }

        if (!geometry.IsWalkable(start))
        {
            return PathResult.Fail("Start point is not walkable.");
        }

        if (!geometry.IsWalkable(target))
        {
            return PathResult.Fail("Target point is not walkable.");
        }

        if (start.DistanceTo(target) < 1e-4)
        {
            return PathResult.Ok([start]);
        }

        // Direct line of sight
        if (geometry.HasLineOfSight(start, target))
        {
            return PathResult.Ok([start, target]);
        }

        // Build visibility graph nodes
        var navNodes = geometry.GetNavigationNodes();
        var allNodes = new List<Point2D>(navNodes.Count + 2)
        {
            start,  // Index 0
            target  // Index 1
        };
        allNodes.AddRange(navNodes);

        var nodeCount = allNodes.Count;
        var adj = new List<(int Neighbor, double Distance)>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            adj[i] = [];
        }

        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (geometry.HasLineOfSight(allNodes[i], allNodes[j]))
                {
                    var d = allNodes[i].DistanceTo(allNodes[j]);
                    adj[i].Add((j, d));
                    adj[j].Add((i, d));
                }
            }
        }

        // A* Algorithm from 0 (start) to 1 (target)
        var gScore = new double[nodeCount];
        var fScore = new double[nodeCount];
        var parent = new int[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            gScore[i] = double.PositiveInfinity;
            fScore[i] = double.PositiveInfinity;
            parent[i] = -1;
        }

        gScore[0] = 0;
        fScore[0] = allNodes[0].DistanceTo(target);

        var openSet = new PriorityQueue<int, double>();
        openSet.Enqueue(0, fScore[0]);
        var closedSet = new bool[nodeCount];

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == 1)
            {
                // Reconstruct path
                var path = new List<Point2D>();
                int currIdx = 1;
                while (currIdx != -1)
                {
                    path.Add(allNodes[currIdx]);
                    currIdx = parent[currIdx];
                }
                path.Reverse();

                // Post-process path smoothing (string pulling)
                var smoothed = SmoothPath(geometry, path);
                return PathResult.Ok(smoothed);
            }

            if (closedSet[current])
            {
                continue;
            }
            closedSet[current] = true;

            foreach (var (neighbor, dist) in adj[current])
            {
                if (closedSet[neighbor])
                {
                    continue;
                }

                var tentativeG = gScore[current] + dist;
                if (tentativeG < gScore[neighbor] - 1e-6)
                {
                    parent[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + allNodes[neighbor].DistanceTo(target);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return PathResult.Fail("No valid path found to target.");
    }

    private static IReadOnlyList<Point2D> SmoothPath(RoomGeometry geometry, List<Point2D> path)
    {
        if (path.Count <= 2)
        {
            return path;
        }

        var smoothed = new List<Point2D> { path[0] };
        int currentIdx = 0;

        while (currentIdx < path.Count - 1)
        {
            int nextIdx = path.Count - 1;
            while (nextIdx > currentIdx + 1)
            {
                if (geometry.HasLineOfSight(path[currentIdx], path[nextIdx]))
                {
                    break;
                }
                nextIdx--;
            }

            smoothed.Add(path[nextIdx]);
            currentIdx = nextIdx;
        }

        return smoothed;
    }
}
