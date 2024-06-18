using System.Text.Json;
using Xunit;

public partial class TestIntegration {

    [Fact]
    public async Task TestPointQuery() {
        // TODO: The values used in this test will likely stop working when the road network is updated

        var url = "/?road=H001&slk=0.5";
        var response = await client.GetAsync(url);


        response.EnsureSuccessStatusCode();
        var response_content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(response_content);
        // >> [[115.88299536359695,-31.96484516020695]]
        // >> reference rust value [[115.88299536369249,-31.964845160263945]]
        var result = JsonSerializer.Deserialize<List<List<double>>>(response_content);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(115.88299536369249, result[0][0], 6);
    }

    [Fact]
    public async Task TestPointQueryWithLatLon() {
        // TODO: The values used in this test will likely stop working when the road network is updated

        var url = "/?road=H001&slk=0.5&f=latlon";
        var response = await client.GetAsync(url);


        response.EnsureSuccessStatusCode();
        var response_content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(response_content);
        // >> [[115.88299536359695,-31.96484516020695]]
        // >> reference rust value [[115.88299536369249,-31.964845160263945]]

        //Note in f=latlon mode, the latitude is returned first, which is the opposite ordering to in geojson
        var swapped_response = string.Join(",", response_content.Split(",").Reverse());
        var result = JsonSerializer.Deserialize<List<List<double>>>($"[[{swapped_response}]]");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(115.88299536369249, result[0][0], 6);
    }

    [Fact]
    public async Task TestPointPOST() {
        // TODO: The values used in this test will likely stop working when the road network is updated

        var url = "/";
        var request_body = new StringContent("""{"road":"H001","slk":0.5}""");
        var response = await client.PostAsync(url, request_body);


        response.EnsureSuccessStatusCode();
        var response_content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(response_content);
        // >> [[115.88299536359695,-31.96484516020695]]
        // >> reference rust value [[115.88299536369249,-31.964845160263945]]
        var result = JsonSerializer.Deserialize<List<List<double>>>(response_content);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(115.88299536369249, result[0][0], 6);
    }
}