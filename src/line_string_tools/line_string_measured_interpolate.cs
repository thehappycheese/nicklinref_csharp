using NetTopologySuite.Geometries;

namespace LineStringTools;

public partial class LineStringMeasured {
    public Coordinate? Interpolate(double fractionOfLength) {
        if (Segments.Count == 0) return null;
        if (fractionOfLength <= 0) return Segments[0].A;

        double deNormalisedDistanceAlong = TotalLength * fractionOfLength;
        double lenSoFar = 0;

        foreach (var segment in Segments) {
            lenSoFar += segment.Length;
            if (lenSoFar >= deNormalisedDistanceAlong) {
                double remainingDistance = lenSoFar - deNormalisedDistanceAlong;
                double ratio = remainingDistance / segment.Length;
                double x = segment.B.X - (segment.B.X - segment.A.X) * ratio;
                double y = segment.B.Y - (segment.B.Y - segment.A.Y) * ratio;
                return new Coordinate(x, y);
            }
        }

        return Segments[Segments.Count - 1].B;
    } 
}
