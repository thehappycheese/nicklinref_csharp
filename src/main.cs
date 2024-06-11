using System.Globalization;
using System.IO.Compression;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using CustomServicesAndMiddlewares;

public class Program {

    public static void Main() {

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddHttpClient();

        // === Road Network Download / Cache / Lookup Service ===
        builder.Services.AddSingleton<RoadNetworkService>();
        builder.Services.AddHostedService(provider => provider.GetService<RoadNetworkService>()
            ?? throw new InvalidOperationException("Unable to start Road Network Data Service")
        );

        // === Linear Referencing Service ===
        builder.Services.AddSingleton<LinearReferencingService>();
        builder.Services.AddHostedService(
            provider => provider.GetService<LinearReferencingService>()
                ?? throw new InvalidOperationException("Unable to start Linear Referencing Service")
        );

        // === GZip Compression ===
        builder.Services.AddResponseCompression(options => {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<GzipCompressionProviderOptions>(options => {
            options.Level = CompressionLevel.Fastest;
        });

        var app = builder.Build();

        // === Use Static Files Middleware ===
        app.Use(async (context, next) => {
            if (context.Request.Path == "/show" || context.Request.Path == "/show/") {
                context.Request.Path = "/show/index.html";
            }
            await next();
        });
        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "static")),
            RequestPath = "/show"
        });

        // === GZip Compression ===
        app.UseResponseCompression();

        // === Custom CORS (Cross Origin Resource Sharing) Middleware ===
        app.UseMiddleware<PermissiveCORSMiddleware>();

        // === Custom Echo X-Request-Id Header Middleware ===
        app.UseMiddleware<EchoXRequestIdMiddleware>();

        // === Routes: ===

        // GET latitude longitude points from road number and slk, OR get line string from road and slk_from/slk_to
        app.MapGet("/", async context => {
            var linear_referencing_service = app.Services.GetRequiredService<LinearReferencingService>();
            if (linear_referencing_service is null) {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Linear Referencing Service is Unavailable.");
                return;
            }

            // === ROAD NUMBER FILTER ===
            if (!context.Request.Query.ContainsKey("road")) {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid parameters. Must specify road= parameter.");
                return;
            }
            string road = context.Request.Query["road"].ToString();
            // === OFFSET ===
            double offset_metres = 0;
            if (context.Request.Query.TryGetValue("offset", out var offsetValue) && double.TryParse(offsetValue, out double result)) {
                offset_metres = result;
            }
            // === CARRIAGEWAY FILTER ===
            string cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";

            if (
                context.Request.Query.ContainsKey("slk")
            ) {
                // POINT QUERY
                if (double.TryParse(context.Request.Query["slk"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk)) {
                    List<double[]> points = linear_referencing_service.get_point(road, cwy, slk, offset_metres);

                    if (points.Count < 1) {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("No points found");
                        return;
                    }

                    // for compatibility with the NickMap-BI PowerBI visual, it is important that we support
                    // at least one of the alternate formats; `f=latlon` which returns a single comma separated coordinate
                    // pair rather than a list of point results. Rather than support all formats of the original rust version,
                    // I will just patch in support for this one alternate query type here.
                    if (context.Request.Query.ContainsKey("f")) {
                        string format = context.Request.Query["f"].ToString();
                        if (format == "latlon") {
                            // take the average position of all points that we found
                            // Note: we should never receive so many points that this averaging method will overflow.
                            double[] average = points
                                .Aggregate(
                                    new double[] { 0.0, 0.0 },
                                    (acc, point) => [acc[0] + point[0], acc[1] + point[1]]
                                )
                                .Select(sum => sum / points.Count)
                                .ToArray();
                            await context.Response.WriteAsync(
                                $"{average[1]},{average[0]}" // note swapped order is required.
                            );
                            return;
                        } else if (format == "json") {
                            // this is the only other supported type from the old rust version
                            // allow fall through to default return.
                        } else {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync(
                                "Specified format is unavailable in this version of the server. please only use f=latlon or omit the f= parameter."
                            );
                            return;
                        }
                    }
                    await context.Response.WriteAsJsonAsync(points);
                    return;
                }
            } else if (
                   context.Request.Query.ContainsKey("slk_from")
                && context.Request.Query.ContainsKey("slk_to")
            ) {
                // LINE QUERY
                if (
                       double.TryParse(context.Request.Query["slk_from"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk_from)
                    && double.TryParse(context.Request.Query["slk_to"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk_to)
                ) {
                    var lineStrings = linear_referencing_service.get_line(road, cwy, slk_from, slk_to, offset_metres);
                    await context.Response.WriteAsJsonAsync(lineStrings);
                    return;
                }
            } else {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid parameters. Must specify either slk= or both slk_from= and slk_to=.");
                return;
            }
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid parameters.");
        });

        // POST: many line strings at once using a binary request format and a plain JSON response.
        app.MapPost("/batch", async context => {
            // Note: it would be even better if we could respond in a binary format as upload/download
            // times are a significant component of the latency experienced by the user.
            //
            // But once I had binary encoded the request, the performance became acceptable
            // and I chose to just respond in JSON to keep things simple. As long as the response is gzipped its not too bad.
            // it is a shame that BSON does not have wider usage/standardization for this use-case.

            var linearReferencingService = app.Services.GetRequiredService<LinearReferencingService>();
            using var memory_stream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memory_stream);
            var batch = memory_stream.ToArray();

            var results = linearReferencingService.line_batch(batch);

            await context.Response.WriteAsJsonAsync(results);
        });

        app.Run();
    }
}
