using System.Globalization;
using System.IO.Compression;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using CustomServicesAndMiddlewares;
using RequestTypes;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

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

        // Swagger did not work for me... i think i didnt define the routes properly.
        // builder.Services.AddEndpointsApiExplorer();
        // builder.Services.AddSwaggerGen();

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

        // === Swagger ===
        // didn't work for me. don't think i defined the routes in a way that it can introspect the parameters properly :()

        //app.UseSwagger();
        //app.UseSwaggerUI();
        //app.MapSwagger();


        // === Routes: ===
        app.Map("/", async context => {
            LinearReferencingService linear_referencing_service = app.Services.GetRequiredService<LinearReferencingService>();
            if (context.Request.Method == "GET") {
                if (LineRequest.try_from_query(context.Request.Query, out LineRequest line_request)) {
                    var lineStrings = linear_referencing_service.get_line(
                        line_request.road,
                        line_request.cwy.GetValueOrDefault().ToString(),
                        line_request.slk_from,
                        line_request.slk_to,
                        line_request.offset.GetValueOrDefault()
                    );
                    await context.Response.WriteAsJsonAsync(lineStrings);
                    return;
                } else if (PointRequest.try_from_query(context.Request.Query, out PointRequest point_request)) {
                    var points = linear_referencing_service.get_point(
                        point_request.road,
                        point_request.cwy.GetValueOrDefault().ToString(),
                        point_request.slk,
                        point_request.offset.GetValueOrDefault()
                    );
                    if (point_request.f == FormatPointResponse.latlon) {
                        // take the average and return `<lat>,<lon>`
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
                    }
                    await context.Response.WriteAsJsonAsync(points);
                    return;
                } else {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(
                        "Invalid Query Parameters"
                    );
                    return;
                }
            } else if (context.Request.Method == "POST") {
                using (var reader = new StreamReader(context.Request.Body)) {
                    string requestBody = await reader.ReadToEndAsync();
                    if (requestBody is null) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(
                            "Unable to read POST body"
                        );
                        return;
                    }
                    LineRequest? line_request = null;
                    try{
                        line_request = JsonSerializer.Deserialize<LineRequest>(requestBody);
                    }catch{}
                    if (line_request != null) {
                        var lineStrings = linear_referencing_service.get_line(
                            line_request.road,
                            line_request.cwy.GetValueOrDefault().ToString(),
                            line_request.slk_from,
                            line_request.slk_to,
                            line_request.offset.GetValueOrDefault()
                        );
                        await context.Response.WriteAsJsonAsync(lineStrings);
                        return;
                    }
                    
                    PointRequest? point_request = null;
                    try{
                        point_request = JsonSerializer.Deserialize<PointRequest>(requestBody);
                    }catch{}
                    if (point_request != null) {
                        var points = linear_referencing_service.get_point(
                            point_request.road,
                            point_request.cwy.GetValueOrDefault().ToString(),
                            point_request.slk,
                            point_request.offset.GetValueOrDefault()
                        );
                        if (point_request.f == FormatPointResponse.latlon) {
                            // take the average and return `<lat>,<lon>`
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
                        }
                        await context.Response.WriteAsJsonAsync(points);
                        return;
                    }
                }
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
