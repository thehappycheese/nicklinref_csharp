using NetTopologySuite.Geometries;

namespace LineStringTools;

public class LineSegmentMeasured {
    public Coordinate a { get; }
    public Coordinate b { get; }
    public double Length { get; }

    public LineSegmentMeasured(Coordinate a, Coordinate b) {
        this.a = a;
        this.b = b;
        Length = a.Distance(b);
    }

    
	public bool intersect(LineSegmentMeasured other, out Coordinate coordinate, out double time_ab, out double time_cd) { 
		var ab = b.subtract(a);
        var c = other.a;
        var d = other.b;
		var cd = d.subtract(c);

		var ab_cross_cd = ab.cross(cd);

		if (ab_cross_cd == 0.0) {
            // this is SO stupid.
            // but try returning a nullable tuple instead!!?
            // Doesn't work. The typechecker seems to be broken and if null-guards do not work on nullable tuples. 
            time_ab = 0;
            time_cd = 0;
            coordinate = new Coordinate();
			return false;
		}

		var ac = c.subtract(a);
		time_ab = ac.cross(cd) / ab_cross_cd;
		time_cd = -ab.cross(ac) / ab_cross_cd;
        coordinate = a.add(ab.scale(time_ab));
		return true;
	}
}
