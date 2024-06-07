using NetTopologySuite.Geometries;

namespace LineStringTools;

public static class CoordinateExtensions {
    public static Coordinate left(this Coordinate coord) {
        return new Coordinate(-coord.Y, coord.X);
    }

    public static Coordinate unit(this Coordinate coord) {
        double length = Math.Sqrt(coord.X * coord.X + coord.Y * coord.Y);
        return new Coordinate(coord.X / length, coord.Y / length);
    }

    public static Coordinate subtract(this Coordinate a, Coordinate b) {
        return new Coordinate(a.X - b.X, a.Y - b.Y);
    }

    public static Coordinate add(this Coordinate a, Coordinate b) {
        return new Coordinate(a.X + b.X, a.Y + b.Y);
    }

    public static Coordinate scale(this Coordinate a, double scalar) {
        return new Coordinate(a.X * scalar, a.Y * scalar);
    }

    public static double cross(this Coordinate a, Coordinate b) {
        return a.X * b.Y - a.Y * b.X;
    }
}