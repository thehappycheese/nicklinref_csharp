using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NetTopologySuite.Geometries;

using LineStringTools;
using RoadNetworkLinearReferencing;

namespace Routes;

public partial class AddRoute{
    public static void point(WebApplication app, RoadNetworkLinearReferencingService road_asset_service) {
        app.MapGet("/point", async context => {
            if (context.Request.Query.ContainsKey("road") && context.Request.Query.ContainsKey("slk")) {
                var road = context.Request.Query["road"].ToString();
                var cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";

                if (double.TryParse(context.Request.Query["slk"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk)) {
                    var matchingFeatures = road_asset_service.GetRoadAssets(road)
                        .Where(f => f.Attributes.Start_slk <= slk && f.Attributes.End_slk >= slk)
                        .Where(f => cwy == "LRS" || (cwy == "LS" && (f.Attributes.Cwy == "Left" || f.Attributes.Cwy == "Single")) || (cwy == "RS" && (f.Attributes.Cwy == "Right" || f.Attributes.Cwy == "Single")))
                        .ToList();

                    if (matchingFeatures.Count > 0) {
                        var points = new List<double[]>();

                        foreach (var feature in matchingFeatures) {
                            var paths = feature.Geometry.Paths;

                            // Since paths is (1,n,2), we need to access the first element
                            var lineStringCoordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                            var lineString = new LineString(lineStringCoordinates);
                            var customLineString = new LineStringMeasured(lineString);

                            var totalLength = customLineString.TotalLength;
                            var fraction = (slk - feature.Attributes.Start_slk) / (feature.Attributes.End_slk - feature.Attributes.Start_slk);

                            var interpolated_point = customLineString.Interpolate(fraction);
                            if (interpolated_point!=null){
                                points.Add([interpolated_point.X, interpolated_point.Y]);
                            }
                        }

                        await context.Response.WriteAsJsonAsync(points);
                        return;
                    }
                }
            }

            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid parameters.");
        });
    }
}