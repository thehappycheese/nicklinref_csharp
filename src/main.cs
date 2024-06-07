﻿using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using CustomServices;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// GET latitude longitude points from road number and slk
app.MapGet("/point", async context => {
    var linear_referencing_service = app.Services.GetRequiredService<LinearReferencingService>();
    if (context.Request.Query.ContainsKey("road") && context.Request.Query.ContainsKey("slk")) {
        var road = context.Request.Query["road"].ToString();
        var cwy = context.Request.Query.ContainsKey("cwy") ? context.Request.Query["cwy"].ToString() : "LRS";
        if (double.TryParse(context.Request.Query["slk"], NumberStyles.Any, CultureInfo.InvariantCulture, out double slk)) {
            var points = linear_referencing_service.get_point(road, cwy, slk);
            await context.Response.WriteAsJsonAsync(points);
            return;
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
        var lineStrings = linear_referencing_service.get_line(road, cwy, slk_from, slk_to);
        await context.Response.WriteAsJsonAsync(lineStrings);
        return;
    }else{
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("No matching features");
    }
});

app.Run();