using NetTopologySuite.Geometries;

namespace LineStringTools;

public class LineSegmentMeasured {
    public Coordinate A { get; }
    public Coordinate B { get; }
    public double Length { get; }

    public LineSegmentMeasured(Coordinate a, Coordinate b) {
        A = a;
        B = b;
        Length = a.Distance(b);
    }
}
