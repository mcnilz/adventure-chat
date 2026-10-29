namespace AdventureChat.Server.Models;

public sealed record Polygon(IReadOnlyList<Point2D> Vertices)
{
    public bool ContainsPoint(Point2D p)
    {
        if (!p.IsFinite() || Vertices.Count < 3)
        {
            return false;
        }

        var inside = false;
        var count = Vertices.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            var pi = Vertices[i];
            var pj = Vertices[j];

            var intersects = ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                             (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y + double.Epsilon) + pi.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public IReadOnlyList<(Point2D A, Point2D B)> GetEdges()
    {
        var edges = new List<(Point2D, Point2D)>(Vertices.Count);
        for (int i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % Vertices.Count];
            edges.Add((a, b));
        }
        return edges;
    }

    public double GetSignedArea()
    {
        double area = 0.0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var p1 = Vertices[i];
            var p2 = Vertices[(i + 1) % Vertices.Count];
            area += (p1.X * p2.Y) - (p2.X * p1.Y);
        }
        return area * 0.5;
    }

    public IReadOnlyList<Point2D> GetExpandedVertices(double offset)
    {
        var count = Vertices.Count;
        if (count < 3) return Vertices;

        var result = new List<Point2D>(count);
        var isClockwise = GetSignedArea() < 0;

        for (int i = 0; i < count; i++)
        {
            var prev = Vertices[(i - 1 + count) % count];
            var curr = Vertices[i];
            var next = Vertices[(i + 1) % count];

            var v1x = curr.X - prev.X;
            var v1y = curr.Y - prev.Y;
            var len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
            if (len1 > 0) { v1x /= len1; v1y /= len1; }

            var v2x = next.X - curr.X;
            var v2y = next.Y - curr.Y;
            var len2 = Math.Sqrt(v2x * v2x + v2y * v2y);
            if (len2 > 0) { v2x /= len2; v2y /= len2; }

            // Normal vectors pointing outwards
            // For counter-clockwise polygon: outward normal to (dx, dy) is (dy, -dx)
            // For clockwise polygon: outward normal is (-dy, dx)
            double n1x = isClockwise ? -v1y : v1y;
            double n1y = isClockwise ? v1x : -v1x;

            double n2x = isClockwise ? -v2y : v2y;
            double n2y = isClockwise ? v2x : -v2x;

            var bisectorX = n1x + n2x;
            var bisectorY = n1y + n2y;
            var bisectorLen = Math.Sqrt(bisectorX * bisectorX + bisectorY * bisectorY);

            if (bisectorLen > 1e-4)
            {
                bisectorX /= bisectorLen;
                bisectorY /= bisectorLen;
                result.Add(new Point2D(curr.X + bisectorX * offset, curr.Y + bisectorY * offset));
            }
            else
            {
                result.Add(new Point2D(curr.X + n1x * offset, curr.Y + n1y * offset));
            }
        }

        return result;
    }
}
