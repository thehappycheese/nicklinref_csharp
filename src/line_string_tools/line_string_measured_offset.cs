using NetTopologySuite.Geometries;

namespace LineStringTools;

public partial class LineStringMeasured {

    public List<LineSegmentMeasured> offset_segments(double distance) {
        return Segments.Select(segment => {
            var offsetVector = segment.b.subtract(segment.a).left().unit().scale(distance);
            return new LineSegmentMeasured(segment.a.add(offsetVector), segment.b.add(offsetVector));
        }).ToList();
    }

    public List<Coordinate> offset_basic(double distance) {
        if (Segments.Count == 0) {
            return new List<Coordinate>();
        }

        var offsetSegments = offset_segments(distance);

        var points = new List<Coordinate> { offsetSegments[0].a };

        for (int i = 0; i < offsetSegments.Count - 1; i++) {
            var mseg1 = offsetSegments[i];
            var mseg2 = offsetSegments[i + 1];

            var ab = mseg1.b.subtract(mseg1.a);
            var cd = mseg2.b.subtract(mseg2.a);

            if (Math.Abs(ab.cross(cd)) < 0.00000001) {
                points.Add(mseg1.b);
            } else {
                
                if (mseg1.intersect(mseg2, out Coordinate intersection, out double time_ab, out double time_cd)){
                        
                    bool tipAb = 0.0 <= time_ab && time_ab <= 1.0;
                    bool fipAb = !tipAb;
                    bool pfipAb = fipAb && time_ab > 0.0;
                    bool tipCd = 0.0 <= time_cd && time_cd <= 1.0;
                    bool fipCd = !tipCd;

                    if (tipAb && tipCd) {
                        points.Add(intersection);
                    } else if (fipAb && fipCd) {
                        if (pfipAb) {
                            points.Add(intersection);
                        } else {
                            points.Add(mseg1.b);
                            points.Add(mseg2.a);
                        }
                    } else {
                        points.Add(mseg1.b);
                        points.Add(mseg2.a);
                    }
                        
                }
            }
        }

        points.Add(offsetSegments[offsetSegments.Count - 1].b);
        return points;
    }
}
