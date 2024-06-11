using System.Text.Json;
using Xunit;

using LinrefTestHelpers;

public partial class TestIntegration {

    [Fact]
    public async Task TestBatchEndpoint() {
        var request1 = BatchRequestHelper.BinaryEncodeRequest("H001", 1.0f, 1.1f, 0f, "LRS");
        var request2 = BatchRequestHelper.BinaryEncodeRequest("H001", 3.0f, 3.2f, 0f, "LS");
        var request3 = BatchRequestHelper.BinaryEncodeRequest("H002", 4.1f, 4.2f, 20f, "LS");

        var requestBody = BatchRequestHelper.CombineRequests(request1, request2, request3);

        var requestContent = new ByteArrayContent(requestBody);
        requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PostAsync("/batch", requestContent);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);

        var result = JsonSerializer.Deserialize<List<List<List<List<double>>>>>(responseContent);
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
}