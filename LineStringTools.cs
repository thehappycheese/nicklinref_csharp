using NetTopologySuite.Geometries;

namespace LineStringTools
{
    public class LineSegmentMeasured
    {
        public Coordinate A { get; }
        public Coordinate B { get; }
        public double Length { get; }

        public LineSegmentMeasured(Coordinate a, Coordinate b)
        {
            A = a;
            B = b;
            Length = a.Distance(b);
        }
    }

    public class LineStringMeasured
    {
        public List<LineSegmentMeasured> Segments { get; }
        public double TotalLength { get; }

        public LineStringMeasured(LineString lineString)
        {
            Segments = new List<LineSegmentMeasured>();
            for (int i = 0; i < lineString.NumPoints - 1; i++)
            {
                var a = lineString.GetCoordinateN(i);
                var b = lineString.GetCoordinateN(i + 1);
                Segments.Add(new LineSegmentMeasured(a, b));
            }
            TotalLength = lineString.Length;
        }

        public Coordinate? Interpolate(double fractionOfLength)
        {
            if (Segments.Count == 0) return null;
            if (fractionOfLength <= 0) return Segments[0].A;

            double deNormalisedDistanceAlong = TotalLength * fractionOfLength;
            double lenSoFar = 0;

            foreach (var segment in Segments)
            {
                lenSoFar += segment.Length;
                if (lenSoFar >= deNormalisedDistanceAlong)
                {
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
}