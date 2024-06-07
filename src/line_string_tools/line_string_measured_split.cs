using NetTopologySuite.Geometries;

namespace LineStringTools;

public partial class LineStringMeasured {
    public (LineStringMeasured?, LineStringMeasured?) split(double fractionOfLength) {
        double distanceAlong = TotalLength * fractionOfLength;

        if (distanceAlong <= 0) {
            return (null, this);
        } else if (distanceAlong >= TotalLength) {
            return (this, null);
        } else {
            double distanceSoFar = 0;
            double distanceRemaining = distanceAlong;
            for (int i = 0; i < Segments.Count; i++) {
                var segment = Segments[i];
                if (distanceRemaining <= 0) {
                    return (
                        new LineStringMeasured(CreateLineString(Segments.GetRange(0, i))),
                        new LineStringMeasured(CreateLineString(Segments.GetRange(i, Segments.Count - i)))
                    );
                } else if (distanceRemaining < segment.Length) {
                    double ratio = distanceRemaining / segment.Length;
                    double x = segment.a.X + (segment.b.X - segment.a.X) * ratio;
                    double y = segment.a.Y + (segment.b.Y - segment.a.Y) * ratio;
                    var intermediatePoint = new Coordinate(x, y);

                    var part1Segments = new List<LineSegmentMeasured>(Segments.GetRange(0, i))
                    {
                        new LineSegmentMeasured(segment.a, intermediatePoint)
                    };

                    var part2Segments = new List<LineSegmentMeasured>
                    {
                        new LineSegmentMeasured(intermediatePoint, segment.b)
                    };
                    part2Segments.AddRange(Segments.GetRange(i + 1, Segments.Count - i - 1));

                    return (
                        new LineStringMeasured(CreateLineString(part1Segments)),
                        new LineStringMeasured(CreateLineString(part2Segments))
                    );
                }
                distanceSoFar += segment.Length;
                distanceRemaining -= segment.Length;
            }
        }

        return (null, null);
    }

    public (LineStringMeasured?, LineStringMeasured?, LineStringMeasured?) split_twice(double fractionOfLengthStart, double fractionOfLengthEnd) {
        var (a, bc) = split(fractionOfLengthStart);
        if (bc == null) {
            return (a, null, null);
        }
        var (b, c) = bc.split((fractionOfLengthEnd - fractionOfLengthStart) / (1 - fractionOfLengthStart));
        return (a, b, c);
    }
}