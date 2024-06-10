using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public partial class MyIntegrationTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public MyIntegrationTests(WebApplicationFactory<Program> factory) {
        this.factory = factory;
        client = this.factory.CreateClient();
    }

    [Fact]
    public async Task TestGetEndpoint() {
        // TODO: The values used in this test will likely stop working when the road network is updated

        var url = "/point?road=H001&slk=0.5";
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
}