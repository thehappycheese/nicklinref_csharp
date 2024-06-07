using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RoadNetworkLinearReferencing;
using Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<RoadNetworkLinearReferencingService>();
builder.Services.AddHostedService(
    provider => provider.GetService<RoadNetworkLinearReferencingService>()
        ?? throw new InvalidOperationException("Unable to start Linear Referencing Service")
);

var app = builder.Build();

var road_asset_service = app.Services.GetRequiredService<RoadNetworkLinearReferencingService>();

AddRoute.point(app, road_asset_service);
AddRoute.line(app, road_asset_service);

app.MapGet("/data", async context => {
    if (!context.Request.Query.ContainsKey("road")) {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("bad request, missing query param road");
    }
    var road = context.Request.Query["road"].ToString();
    var result = road_asset_service.GetRoadAssets(road);
    if (result != null) {
        await context.Response.WriteAsJsonAsync(result);
    } else {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Road not found");
    }
});

app.Run();