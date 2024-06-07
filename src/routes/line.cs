using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

using NetTopologySuite.Geometries;

using LineStringTools;
using RoadNetworkLinearReferencing;

namespace Routes;

public partial class AddRoute{
    public static void line(WebApplication app, RoadNetworkLinearReferencingService road_asset_service) {
        
        

        app.MapGet("/line", async context => {
            Console.WriteLine("LINE QUERY PROCESSING");
            Console.WriteLine(context.Request.Query.ContainsKey("road"));
            Console.WriteLine(context.Request.Query.ContainsKey("slk_from"));
            Console.WriteLine(context.Request.Query.ContainsKey("slk_to"));
            if (
                !context.Request.Query.ContainsKey("road")
                && !context.Request.Query.ContainsKey("slk_from")
                && !context.Request.Query.ContainsKey("slk_to")
            ) {
                Console.WriteLine("BUT FAIL");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid parameters.");
            }
            Console.WriteLine("LINE QUERY PROCESSING: IN");
            var road = context.Request.Query["road"].ToString();
            var cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";
            if (double.TryParse(context.Request.Query["slk_from"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slkFrom) &&
                double.TryParse(context.Request.Query["slk_to"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slkTo)) {
                
                Console.WriteLine($"GOT PARAMS: {road} {cwy} {slkFrom} {slkTo}");

                var matching_features = road_asset_service.GetRoadAssets(road)
                    .Where(f => f.Attributes.Start_slk <= slkTo && f.Attributes.End_slk >= slkFrom)
                    .Where(f => cwy == "LRS" || (cwy == "LS" && (f.Attributes.Cwy == "Left" || f.Attributes.Cwy == "Single")) || (cwy == "RS" && (f.Attributes.Cwy == "Right" || f.Attributes.Cwy == "Single")))
                    .ToList();
                Console.WriteLine($"Matching Features: {matching_features} COUNT: {matching_features.Count}");
                if (matching_features.Count > 0) {
                    Console.WriteLine($"Matching Features Count: {matching_features.Count}");
                    var lineStrings = new List<List<double[]>>();
                    foreach (var feature in matching_features) {
                        var paths = feature.Geometry.Paths;

                        // Since paths is (1,n,2), we need to access the first element
                        var lineStringCoordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                        var lineString = new LineString(lineStringCoordinates);
                        var customLineString = new LineStringMeasured(lineString);

                        var itemLenKm = feature.Attributes.End_slk - feature.Attributes.Start_slk;
                        var fractionStart = (slkFrom - feature.Attributes.Start_slk) / itemLenKm;
                        var fractionEnd = (slkTo - feature.Attributes.Start_slk) / itemLenKm;

                        var (beforeStart, between, afterEnd) = customLineString.CutTwice(fractionStart, fractionEnd);
                        Console.WriteLine($"{between}");
                        if(between != null){
                            var betweenCoordinates = between.ToCoordinateList();
                            lineStrings.Add(betweenCoordinates);
                        }
                    }
                    Console.WriteLine("Got Result");
                    Console.WriteLine(lineStrings);
                    await context.Response.WriteAsJsonAsync(lineStrings);
                    return;
                }else{
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("No matching features");
                }
            }
        });
    }
}