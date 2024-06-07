using Microsoft.Extensions.Hosting;

using LineStringTools;
using NetTopologySuite.Geometries;

namespace CustomServices;

public class LinearReferencingService : IHostedService {
    private readonly RoadNetworkService road_network_service;

    public LinearReferencingService(RoadNetworkService roadNetworkService) {
        road_network_service = roadNetworkService;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public List<double[]> get_point(string road, string cwy, double slk) {
        var matchingFeatures = road_network_service.GetRoadAssets(road)
            .Where(f => f.Attributes.Start_slk <= slk && f.Attributes.End_slk >= slk)
            .Where(f => cwy == "LRS" || (cwy == "LS" && (f.Attributes.Cwy == "Left" || f.Attributes.Cwy == "Single")) || (cwy == "RS" && (f.Attributes.Cwy == "Right" || f.Attributes.Cwy == "Single")))
            .ToList();

        var points = new List<double[]>();
        if (matchingFeatures.Count > 0) {
            foreach (var feature in matchingFeatures) {
                var paths = feature.Geometry.Paths;

                // Since paths is (1,n,2), we need to access the first element
                var lineStringCoordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                var lineString = new LineString(lineStringCoordinates);
                var customLineString = new LineStringMeasured(lineString);

                var totalLength = customLineString.TotalLength;
                var fraction = (slk - feature.Attributes.Start_slk) / (feature.Attributes.End_slk - feature.Attributes.Start_slk);

                var interpolated_point = customLineString.Interpolate(fraction);
                if (interpolated_point != null) {
                    points.Add([interpolated_point.X, interpolated_point.Y]);
                }
            }
        }
        return points;
    }

    public List<List<double[]>> get_line(string road, string cwy, double slk_from, double slk_to) {
        var matching_features = road_network_service.GetRoadAssets(road)
            .Where(f => f.Attributes.Start_slk <= slk_to && f.Attributes.End_slk >= slk_from)
            .Where(f => cwy == "LRS" || (cwy == "LS" && (f.Attributes.Cwy == "Left" || f.Attributes.Cwy == "Single")) || (cwy == "RS" && (f.Attributes.Cwy == "Right" || f.Attributes.Cwy == "Single")))
            .ToList();

        var line_strings = new List<List<double[]>>();
        if (matching_features.Count > 0) {
            foreach (var feature in matching_features) {
                var paths = feature.Geometry.Paths;

                // Since paths is (1,n,2), we need to access the first element
                var lineStringCoordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                var lineString = new LineString(lineStringCoordinates);
                var customLineString = new LineStringMeasured(lineString);

                var itemLenKm = feature.Attributes.End_slk - feature.Attributes.Start_slk;
                var fractionStart = (slk_from - feature.Attributes.Start_slk) / itemLenKm;
                var fractionEnd = (slk_to - feature.Attributes.Start_slk) / itemLenKm;

                var (beforeStart, between, afterEnd) = customLineString.CutTwice(fractionStart, fractionEnd);
                if (between != null) {
                    var betweenCoordinates = between.ToCoordinateList();
                    line_strings.Add(betweenCoordinates);
                }
            }
        }
        return line_strings;
    }
}