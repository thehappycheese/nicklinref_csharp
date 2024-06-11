using Microsoft.Extensions.Hosting;

using LineStringTools;
using NetTopologySuite.Geometries;
using Helpers;

namespace CustomServices;

public class LinearReferencingService : IHostedService {
    private readonly RoadNetworkService road_network_service;

    public LinearReferencingService(RoadNetworkService roadNetworkService) {
        road_network_service = roadNetworkService;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public List<double[]> get_point(string road, string cwy, double slk, double offset_metres) {
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
                if (customLineString is null) {
                    continue;
                }

                if (offset_metres > 0.0) {
                    double offset_degrees = ScuffedSpatialStuff.convert_metres_to_degrees(offset_metres);
                    customLineString = customLineString.offset_basic(offset_degrees);
                    if (customLineString is null) {
                        continue;
                    }
                }

                var totalLength = customLineString.TotalLength;
                var fraction = (slk - feature.Attributes.Start_slk) / (feature.Attributes.End_slk - feature.Attributes.Start_slk);

                var interpolated_point = customLineString.interpolate(fraction);
                if (interpolated_point != null) {
                    points.Add([interpolated_point.X, interpolated_point.Y]);
                }
            }
        }
        return points;
    }

    public List<List<List<double>>> get_line(string road, string cwy, double slk_from, double slk_to, double offset_metres) {
        var matching_features = road_network_service.GetRoadAssets(road)
            .Where(f => f.Attributes.Start_slk <= slk_to && f.Attributes.End_slk >= slk_from)
            .Where(f => cwy == "LRS" || (cwy == "LS" && (f.Attributes.Cwy == "Left" || f.Attributes.Cwy == "Single")) || (cwy == "RS" && (f.Attributes.Cwy == "Right" || f.Attributes.Cwy == "Single")))
            .ToList();

        var line_strings = new List<List<List<double>>>();
        if (matching_features.Count > 0) {
            foreach (var feature in matching_features) {
                var paths = feature.Geometry.Paths;

                // Since paths is (1,n,2), we need to access the first element
                var line_string_coordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                var line_string = new LineString(line_string_coordinates);
                LineStringMeasured line_string_measured = new LineStringMeasured(line_string);
                if (offset_metres > 0.0) {
                    double offset_degrees = ScuffedSpatialStuff.convert_metres_to_degrees(offset_metres);
                    var offset_line_string = line_string_measured.offset_basic(offset_degrees);
                    if (offset_line_string is null) {
                        continue;
                    }
                    line_string_measured = offset_line_string;
                }
                var itemLenKm = feature.Attributes.End_slk - feature.Attributes.Start_slk;
                var fractionStart = (slk_from - feature.Attributes.Start_slk) / itemLenKm;
                var fractionEnd = (slk_to - feature.Attributes.Start_slk) / itemLenKm;

                var (beforeStart, between, afterEnd) = line_string_measured.split_twice(fractionStart, fractionEnd);
                if (between != null) {
                    var between_coordinates = between.ToCoordinateList();
                    if (between_coordinates is null) continue;
                    line_strings.Add(between_coordinates);
                }
            }
        }
        return line_strings;
    }

    public List<List<List<List<double>>>> line_batch(byte[] batch) {
        var results = new List<List<List<List<double>>>>(); // lol, too deeply nested. fix later.
        var decoded_requests = BatchRequestDecoder.DecodeRequests(batch);

        foreach (var (road, slk_from, slk_to, offset_metres, cwy) in decoded_requests) {
            Console.WriteLine($"{road}, {slk_from}, {slk_to}, {offset_metres}, {cwy}");
            var line_strings = get_line(road, cwy, slk_from, slk_to, offset_metres);
            results.Add(line_strings);
        }

        return results;
    }

}