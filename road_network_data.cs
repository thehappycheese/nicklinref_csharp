using System.Net.Http.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace RoadAssetData
{
    public class Geometry
    {
        public List<List<List<double>>> Paths { get; set; }
    }

    public class Feature
    {
        public Attributes Attributes { get; set; }
        public Geometry Geometry { get; set; }
    }

    public class Attributes
    {
        public string Road { get; set; }
        public double Start_slk { get; set; }
        public double End_slk { get; set; }
        public string Cwy { get; set; }
    }

    public class RoadAssetsResponse
    {
        public List<Feature> Features { get; set; }
        public bool ExceededTransferLimit { get; set; }
    }

    public class RoadAssetsService : IHostedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private List<Feature> _roadAssets;
        private const string CacheFilePath = "roadAssetsCache.json";

        public RoadAssetsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(CacheFilePath))
            {
                // Load from cache
                var cachedData = await File.ReadAllTextAsync(CacheFilePath, cancellationToken);
                _roadAssets = JsonSerializer.Deserialize<List<Feature>>(cachedData);
            }
            else
            {
                // Download and cache
                await DownloadAndCacheData(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public List<Feature> GetRoadAssets() => _roadAssets;

        private async Task DownloadAndCacheData(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();
            var allFeatures = new List<Feature>();
            bool exceededTransferLimit;
            int offset = 0;

            do
            {
                var url = $"https://mrgis.mainroads.wa.gov.au/arcgis/rest/services/OpenData/RoadAssets_DataPortal/MapServer/17/query?where=1%3D1&outFields=ROAD,START_SLK,END_SLK,CWY&outSR=4326&f=json&resultOffset={offset}";
                var response = await client.GetFromJsonAsync<RoadAssetsResponse>(url, cancellationToken);

                if (response != null && response.Features != null)
                {
                    allFeatures.AddRange(response.Features);
                    exceededTransferLimit = response.ExceededTransferLimit;
                    offset += response.Features.Count;
                }
                else
                {
                    exceededTransferLimit = false;
                }
            } while (exceededTransferLimit);

            _roadAssets = allFeatures;

            // Cache the result
            var jsonData = JsonSerializer.Serialize(_roadAssets);
            await File.WriteAllTextAsync(CacheFilePath, jsonData, cancellationToken);
        }
    }
}
