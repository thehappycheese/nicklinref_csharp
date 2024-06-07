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

public class LineStringMeasured {
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

    public (LineStringMeasured?, LineStringMeasured?) Cut(double fractionOfLength) {
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
                    double x = segment.A.X + (segment.B.X - segment.A.X) * ratio;
                    double y = segment.A.Y + (segment.B.Y - segment.A.Y) * ratio;
                    var intermediatePoint = new Coordinate(x, y);

                    var part1Segments = new List<LineSegmentMeasured>(Segments.GetRange(0, i))
                    {
                        new LineSegmentMeasured(segment.A, intermediatePoint)
                    };

                    var part2Segments = new List<LineSegmentMeasured>
                    {
                        new LineSegmentMeasured(intermediatePoint, segment.B)
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

    public (LineStringMeasured?, LineStringMeasured?, LineStringMeasured?) CutTwice(double fractionOfLengthStart, double fractionOfLengthEnd) {
        var (a, bc) = Cut(fractionOfLengthStart);
        if (bc == null) {
            return (a, null, null);
        }
        var (b, c) = bc.Cut((fractionOfLengthEnd - fractionOfLengthStart) / (1 - fractionOfLengthStart));
        return (a, b, c);
    }
    private LineString CreateLineString(List<LineSegmentMeasured> segments) {
        var coordinates = new List<Coordinate>();
        foreach (var segment in segments) {
            coordinates.Add(segment.A);
        }
        coordinates.Add(segments[segments.Count - 1].B);
        return new LineString(coordinates.ToArray());
    }

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

    public List<double[]> ToCoordinateList() {
        var coordinates = new List<double[]>();
        foreach (var segment in Segments) {
            coordinates.Add([segment.A.X, segment.A.Y]);
        }
        // Add the last point
        if (Segments.Count > 0) {
            coordinates.Add([Segments[Segments.Count - 1].B.X, Segments[Segments.Count - 1].B.Y]);
        }
        return coordinates;
    }
}