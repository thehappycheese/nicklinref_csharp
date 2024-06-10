namespace Helpers;
/// Used for the `offset=` query parameter.
/// Because offsets are very small (typically 10 metres, max 50),
/// and they are being used for visualization, not measurement,
// We can get away with some pretty scuffed approximations;
public static class ScuffedSpatialStuff { 
    public const double EARTH_RADIUS_METRES    	= 6.3781e+6;
    public const double EARTH_METRES_PER_RADIAN	= EARTH_RADIUS_METRES;
    public const double EARTH_METRES_PER_DEGREE	= EARTH_METRES_PER_RADIAN * Math.PI / 180.0;

    /// Use a simplistic conversion from metres to degrees to convert cartesian
    /// distances in metres into geocentric coordinate distances in degrees.
    /// It is not accurate but produces acceptable results in
    /// Western Australia which is close enough to the equator.
    public static double convert_metres_to_degrees(double metres) {
        return metres / EARTH_METRES_PER_DEGREE;
    }
}
