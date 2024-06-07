using NetTopologySuite.Geometries;

namespace LineStringTools;

public partial class LineStringMeasured {
    public Coordinate? interpolate(double fractionOfLength) {
        if (Segments.Count == 0) return null;
        if (fractionOfLength <= 0) return Segments[0].a;

        double de_normalized_distance_along = TotalLength * fractionOfLength;
        double len_so_far = 0;

        foreach (var segment in Segments) {
            len_so_far += segment.Length;
            if (len_so_far >= de_normalized_distance_along) {
                double remainingDistance = len_so_far - de_normalized_distance_along;
                double ratio = remainingDistance / segment.Length;
                double x = segment.b.X - (segment.b.X - segment.a.X) * ratio;
                double y = segment.b.Y - (segment.b.Y - segment.a.Y) * ratio;
                return new Coordinate(x, y);
            }
        }

        return Segments[Segments.Count - 1].b;
    } 
}
