using Xunit;

public partial class TestIntegration {
    [Fact]
    public async Task TestRequestWithValidXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", "12345678901234567890");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("x-request-id"));
        Assert.Equal("12345678901234567890", response.Headers.GetValues("x-request-id").First());
    }

    [Fact]
    public async Task TestRequestWithoutXRequestId() {
        var response = await client.GetAsync(TEST_URL);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("x-request-id"));
    }

    [Fact]
    public async Task TestRequestWithMalformedXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", "invalid_u64_value");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("x-request-id"));
    }

    [Fact]
    public async Task TestRequestWithEmptyXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", "");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("x-request-id"));
    }

    [Fact]
    public async Task TestRequestWithMaximumValueXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", ulong.MaxValue.ToString());

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("x-request-id"));
        Assert.Equal(ulong.MaxValue.ToString(), response.Headers.GetValues("x-request-id").First());
    }

    [Fact]
    public async Task TestRequestWithMinimumValueXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", "0");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("x-request-id"));
        Assert.Equal("0", response.Headers.GetValues("x-request-id").First());
    }

    [Fact]
    public async Task TestRequestWithNegativeValueXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", "-1");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("x-request-id"));
    }
    [Fact]
    public async Task TestRequestWithTooBigValueXRequestId() {
        var request = new HttpRequestMessage(HttpMethod.Get, TEST_URL);
        request.Headers.Add("x-request-id", ulong.MaxValue.ToString()+"9");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("x-request-id"));
    }
}