namespace AdventureChat.Server.Models;

public readonly record struct Point2D(double X, double Y)
{
    public double DistanceTo(Point2D other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public bool IsFinite() => double.IsFinite(X) && double.IsFinite(Y);
}
