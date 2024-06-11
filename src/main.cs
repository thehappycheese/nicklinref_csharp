using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;


using CustomServices;
using Microsoft.AspNetCore.Cors.Infrastructure;






public class Program {

    public static void Main() {

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<RoadNetworkService>();
        builder.Services.AddHostedService(provider => provider.GetService<RoadNetworkService>()
            ?? throw new InvalidOperationException("Unable to start Road Network Data Service")
        );
        builder.Services.AddSingleton<LinearReferencingService>();
        builder.Services.AddHostedService(
            provider => provider.GetService<LinearReferencingService>()
                ?? throw new InvalidOperationException("Unable to start Linear Referencing Service")
        );
        builder.Services.AddCors();

        var app = builder.Build();


        app.UseCors(policy => policy
            .WithOrigins("null")
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("*")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(60))
        );

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

        app.MapPost("/batch", async context => {
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
