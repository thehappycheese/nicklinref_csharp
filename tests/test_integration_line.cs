using System.Text.Json;
using Xunit;

public partial class TestIntegration {

    [Fact]
    public async Task TestLineQuery() {
        // TODO: The values used in this test will likely stop working when the road network is updated

        var url = "/?road=H001&slk_from=0.5&slk_to=0.6";
        var response = await client.GetAsync(url);


        response.EnsureSuccessStatusCode();
        var response_content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(response_content);
        // >> [[115.88299536359695,-31.96484516020695]]
        // >> reference rust value [[115.88299536369249,-31.964845160263945]]
        var result = JsonSerializer.Deserialize<List<List<List<double>>>>(response_content);
        Assert.NotNull(result);
        Assert.Equal(115.88299536369249, result[0][0][0], 6);
    }

}