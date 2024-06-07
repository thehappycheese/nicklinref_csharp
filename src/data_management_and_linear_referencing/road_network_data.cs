using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace RoadNetworkLinearReferencing;

public class RoadNetworkLinearReferencingService : IHostedService {
    
    private readonly IHttpClientFactory                http_client_factory;
    private          Dictionary<string, List<Feature>> road_assets_by_road;
    private const    string                            CACHE_FILE_PATH = "road_assets_cache.json";

    public RoadNetworkLinearReferencingService(IHttpClientFactory httpClientFactory) {
        http_client_factory = httpClientFactory;
        road_assets_by_road = new Dictionary<string, List<Feature>>();
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        if (File.Exists(CACHE_FILE_PATH)) {
            // Load from cache
            var cached_data = await File.ReadAllTextAsync(CACHE_FILE_PATH, cancellationToken);
            
            var cached_features = JsonSerializer.Deserialize<List<Feature>>(cached_data);

            if (cached_features == null){
                throw new InvalidOperationException("Deserialization failed: cached_data is null or cannot be deserialized into a List<Feature>.");
            }
            
            organize_features_by_road(cached_features);
        } else {
            // Download and cache
            await download_and_cache_data(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public List<Feature> GetRoadAssets(string road) =>
        road_assets_by_road.TryGetValue(road, out var features) ? features : new List<Feature>();

    private void organize_features_by_road(List<Feature> features) {
        foreach (var feature in features) {
            if (!road_assets_by_road.ContainsKey(feature.Attributes.Road)) {
                road_assets_by_road[feature.Attributes.Road] = new List<Feature>();
            }
            road_assets_by_road[feature.Attributes.Road].Add(feature);
        }
    }

    private async Task download_and_cache_data(CancellationToken cancellationToken) {
        var  client = http_client_factory.CreateClient();
        var  all_features = new List<Feature>();
        bool exceeded_transfer_limit;
        int  offset = 0;

        do {
            string url = $"https://mrgis.mainroads.wa.gov.au/arcgis/rest/services/OpenData/RoadAssets_DataPortal/MapServer/17/query?where=1%3D1&outFields=ROAD,START_SLK,END_SLK,CWY&outSR=4326&f=json&resultOffset={offset}";
            var response = await client.GetFromJsonAsync<RoadAssetsResponse>(url, cancellationToken);

            if (response != null && response.Features != null) {
                all_features.AddRange(response.Features);
                exceeded_transfer_limit = response.ExceededTransferLimit;
                offset += response.Features.Count;
            } else {
                exceeded_transfer_limit = false;
            }
        } while (exceeded_transfer_limit);

        organize_features_by_road(all_features);

        // Cache the result
        string json_data = JsonSerializer.Serialize(all_features);
        await File.WriteAllTextAsync(CACHE_FILE_PATH, json_data, cancellationToken);
    }
}
