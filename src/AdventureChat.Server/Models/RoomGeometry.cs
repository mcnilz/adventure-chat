namespace AdventureChat.Server.Models;

public sealed record RoomGeometry(Polygon OuterBoundary, IReadOnlyList<Polygon> Obstacles)
{
    public bool IsWalkable(Point2D p)
    {
        if (!p.IsFinite())
        {
            return false;
        }

        if (!OuterBoundary.ContainsPoint(p))
        {
            return false;
        }

        for (int i = 0; i < Obstacles.Count; i++)
        {
            if (Obstacles[i].ContainsPoint(p))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasLineOfSight(Point2D a, Point2D b)
    {
        if (!IsWalkable(a) || !IsWalkable(b))
        {
            return false;
        }

        var dist = a.DistanceTo(b);
        if (dist <= 1e-4)
        {
            return true;
        }

        // Check intersection with all obstacle edges
        for (int o = 0; o < Obstacles.Count; o++)
        {
            var edges = Obstacles[o].GetEdges();
            for (int e = 0; e < edges.Count; e++)
            {
                if (SegmentsIntersectStrict(a, b, edges[e].A, edges[e].B))
                {
                    return false;
                }
            }
        }

        // Check intersection with outer boundary edges
        var outerEdges = OuterBoundary.GetEdges();
        for (int e = 0; e < outerEdges.Count; e++)
        {
            if (SegmentsIntersectStrict(a, b, outerEdges[e].A, outerEdges[e].B))
            {
                return false;
            }
        }

        // Sample points along segment to ensure line doesn't cross outside or inside an obstacle
        int samples = Math.Max(3, (int)Math.Ceiling(dist / 15.0));
        for (int i = 1; i < samples; i++)
        {
            double t = (double)i / samples;
            var sample = new Point2D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
            if (!IsWalkable(sample))
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<Point2D> GetNavigationNodes(double offset = 6.0)
    {
        var nodes = new List<Point2D>();

        // Expand obstacles outwards
        for (int i = 0; i < Obstacles.Count; i++)
        {
            var expanded = Obstacles[i].GetExpandedVertices(offset);
            for (int j = 0; j < expanded.Count; j++)
            {
                var pt = expanded[j];
                if (IsWalkable(pt))
                {
                    nodes.Add(pt);
                }
            }
        }

        // Contract outer boundary inwards for corners
        var contractedOuter = OuterBoundary.GetExpandedVertices(-offset);
        for (int j = 0; j < contractedOuter.Count; j++)
        {
            var pt = contractedOuter[j];
            if (IsWalkable(pt))
            {
                nodes.Add(pt);
            }
        }

        return nodes;
    }

    private static bool SegmentsIntersectStrict(Point2D p1, Point2D p2, Point2D p3, Point2D p4)
    {
        double d1 = Direction(p3, p4, p1);
        double d2 = Direction(p3, p4, p2);
        double d3 = Direction(p1, p2, p3);
        double d4 = Direction(p1, p2, p4);

        if (((d1 > 1e-6 && d2 < -1e-6) || (d1 < -1e-6 && d2 > 1e-6)) &&
            ((d3 > 1e-6 && d4 < -1e-6) || (d3 < -1e-6 && d4 > 1e-6)))
        {
            return true;
        }

        return false;
    }

    private static double Direction(Point2D pi, Point2D pj, Point2D pk)
    {
        return (pk.X - pi.X) * (pj.Y - pi.Y) - (pj.X - pi.X) * (pk.Y - pi.Y);
    }
}
