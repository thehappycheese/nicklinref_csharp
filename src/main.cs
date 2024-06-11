using System.Globalization;
using System.IO.Compression;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        
        // === GZip Compression ===
        app.UseResponseCompression();
        // === Custom CORS (Cross Origin Resource Sharing) Middleware ===
        app.UseMiddleware<PermissiveCORSMiddleware>();
        // === Custom Echo X-Request-Id Header Middleware ===
        app.UseMiddleware<EchoXRequestIdMiddleware>();

        // === Routes: ===

        // GET latitude longitude points from road number and slk
        app.MapGet("/point", async context => {
            var linear_referencing_service = app.Services.GetRequiredService<LinearReferencingService>();
            if (linear_referencing_service is not null) {
                if (context.Request.Query.ContainsKey("road") && context.Request.Query.ContainsKey("slk")) {
                    string road = context.Request.Query["road"].ToString();
                    string cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";
                    double offset_metres = 0;
                    if (context.Request.Query.TryGetValue("offset", out var offsetValue) && double.TryParse(offsetValue, out double result)) {
                        offset_metres = result;
                    }
                    if (double.TryParse(context.Request.Query["slk"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk)) {
                        var points = linear_referencing_service.get_point(road, cwy, slk, offset_metres);
                        await context.Response.WriteAsJsonAsync(points);
                        return;
                    }
                }
            }
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid parameters.");
        });

        // GET linestring points from road number and slk range
        app.MapGet("/line", async context => {
            var linear_referencing_service = app.Services.GetRequiredService<LinearReferencingService>();
            if (
                !context.Request.Query.ContainsKey("road")
                && !context.Request.Query.ContainsKey("slk_from")
                && !context.Request.Query.ContainsKey("slk_to")
            ) {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid parameters.");
            }
            var road = context.Request.Query["road"].ToString();
            var cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";
            if (double.TryParse(context.Request.Query["slk_from"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk_from) &&
                double.TryParse(context.Request.Query["slk_to"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk_to)) {
                double offset_metres = 0;
                if (context.Request.Query.TryGetValue("offset", out var offsetValue) && double.TryParse(offsetValue, out double result)) {
                    offset_metres = result;
                }
                var lineStrings = linear_referencing_service.get_line(road, cwy, slk_from, slk_to, offset_metres);
                await context.Response.WriteAsJsonAsync(lineStrings);
                return;
            } else {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("No matching features");
            }
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
