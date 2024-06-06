// using NetTopologySuite.Geometries;
// using LineStringTools;


// var line = new LineString([
//     new Coordinate(0, 0),
//     new Coordinate(10, 0),
//     new Coordinate(20, 0)
// ]);

// var customLineString = new LineStringMeasured(line);
// var interpolatedPoint = customLineString.Interpolate(0.8);

// Console.WriteLine($"Interpolated Point: {interpolatedPoint}");

using System.Globalization;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using LineStringTools;
using RoadAssetData;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient(); // Register the HTTP client factory
builder.Services.AddSingleton<RoadAssetsService>();
builder.Services.AddHostedService(provider => provider.GetService<RoadAssetsService>());

var app = builder.Build();

var roadAssetsService = app.Services.GetRequiredService<RoadAssetsService>();

app.MapGet("/", async context =>
{
    if (context.Request.Query.ContainsKey("geom") && context.Request.Query.ContainsKey("fraction"))
    {
        var geomWKT = context.Request.Query["geom"].ToString();
        if (double.TryParse(context.Request.Query["fraction"], NumberStyles.Any, CultureInfo.InvariantCulture, out double fraction))
        {
            var reader = new WKTReader();
            var geom = reader.Read(geomWKT);

            if (geom is LineString lineString)
            {
                var customLineString = new LineStringMeasured(lineString);
                var interpolatedPoint = customLineString.Interpolate(fraction);

                var result = $"{interpolatedPoint.Y},{interpolatedPoint.X}";
                await context.Response.WriteAsync(result);
                return;
            }
        }
    }

    context.Response.StatusCode = 400;
    await context.Response.WriteAsync("Invalid parameters.");
});

app.MapGet("/road-assets", context =>
{
    var roadAssets = roadAssetsService.GetRoadAssets();
    return context.Response.WriteAsJsonAsync(roadAssets);
});


app.MapGet("/point", async context =>
{
    if (context.Request.Query.ContainsKey("road") && context.Request.Query.ContainsKey("slk"))
    {
        var road = context.Request.Query["road"].ToString();
        if (double.TryParse(context.Request.Query["slk"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk))
        {
            var matchingFeatures = roadAssetsService.GetRoadAssets()
                .Where(f => f.Attributes.Road == road && f.Attributes.Start_slk <= slk && f.Attributes.End_slk >= slk)
                .ToList();

            if (matchingFeatures.Count > 0)
            {
                var points = new List<double[]>();

                foreach (var feature in matchingFeatures)
                {
                    var paths = feature.Geometry.Paths;

                    // Since paths is (1,n,2), we need to access the first element
                    var lineStringCoordinates = paths[0].Select(coord => new Coordinate(coord[0], coord[1])).ToArray();
                    var lineString = new LineString(lineStringCoordinates);
                    var customLineString = new LineStringMeasured(lineString);

                    var totalLength = customLineString.TotalLength;
                    var fraction = (slk - feature.Attributes.Start_slk) / (feature.Attributes.End_slk - feature.Attributes.Start_slk);

                    var interpolatedPoint = customLineString.Interpolate(fraction);

                    points.Add(new double[] { interpolatedPoint.X, interpolatedPoint.Y });
                }

                await context.Response.WriteAsJsonAsync(points);
                return;
            }
        }
    }

    context.Response.StatusCode = 400;
    await context.Response.WriteAsync("Invalid parameters.");
});


app.Run();
