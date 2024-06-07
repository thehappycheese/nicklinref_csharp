using NetTopologySuite.Geometries;

namespace LineStringTools;

public partial class LineStringMeasured {
    public List<LineSegmentMeasured> Segments { get; }
    public double TotalLength { get; }

    public LineStringMeasured(LineString lineString) {
        Segments = new List<LineSegmentMeasured>();
        for (int i = 0; i < lineString.NumPoints - 1; i++) {
            var a = lineString.GetCoordinateN(i);
            var b = lineString.GetCoordinateN(i + 1);
            Segments.Add(new LineSegmentMeasured(a, b));
        }
        TotalLength = lineString.Length;
    }

    public List<double[]> ToCoordinateList() {
        var coordinates = new List<double[]>();
        foreach (var segment in Segments) {
            coordinates.Add([segment.a.X, segment.a.Y]);
        }
        // Add the last point
        if (Segments.Count > 0) {
            coordinates.Add([Segments[Segments.Count - 1].b.X, Segments[Segments.Count - 1].b.Y]);
        }
        return coordinates;
    }

    private LineString CreateLineString(List<LineSegmentMeasured> segments) {
        var coordinates = new List<Coordinate>();
        foreach (var segment in segments) {
            coordinates.Add(segment.a);
        }
        coordinates.Add(segments[segments.Count - 1].b);
        return new LineString(coordinates.ToArray());
    }
}
