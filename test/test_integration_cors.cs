using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

public partial class TestIntegration {
    
    private static readonly HttpRequestMessage OptionsRequestMessage = new HttpRequestMessage(HttpMethod.Options, "/point?road=H001&slk=1.4") {
        Headers = {
            { "Access-Control-Request-Headers", "Date, X-Request-Id, and, any, other, random, headers, requested, by, the browser" },
            { "Access-Control-Request-Method", "GET" }, // Does not affect the result, server should always respond with "GET, POST, OPTIONS"
            { "Origin", "https://example.com" }
        }
    };

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://another.example.com")]
    [InlineData("null")]
    [InlineData("any other rubbish text or malformed domain value")]
    public async Task TestCorsPreflightBasic(string origin) {
        var request = new HttpRequestMessage(HttpMethod.Options, test_basic_point_url) {
            Headers =
            {
                { "Access-Control-Request-Headers", "Date, X-Request-Id, and, any, other, random, headers, requested, by, the browser" },
                { "Access-Control-Request-Method", "GET" },
                { "Origin", origin }
            }
        };

        var response = await client.SendAsync(request);
        Console.WriteLine($"============== REQUEST WITH ORIGIN {origin}");
        Console.WriteLine(response.Headers);
        Assert.Equal(204, (int)response.StatusCode);
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
    }

    [Fact]
    public async Task TestCorsPreflightExposeHeaders() {
        var response = await client.SendAsync(OptionsRequestMessage);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Expose-Headers").FirstOrDefault());
    }

    [Fact]
    public async Task TestCorsPreflightAllowRequestedHeaders() {
        var response = await client.SendAsync(OptionsRequestMessage);
        var expectedHeaders = new HashSet<string> { "DATE", "X-REQUEST-ID", "AND", "ANY", "OTHER", "RANDOM", "HEADERS", "REQUESTED", "BY", "THE", "BROWSER" };
        var xx = response.Headers.GetValues("Access-Control-Allow-Headers").FirstOrDefault()?.ToUpper().Split(',').Select(h => h.Trim());
        Assert.NotNull(xx);
        var actualHeaders = new HashSet<string>(xx);
        Assert.Equal(expectedHeaders, actualHeaders);
    }

    [Fact]
    public async Task TestCorsPreflightAllowMethods() {
        var response = await client.SendAsync(OptionsRequestMessage);
        var expectedMethods = new HashSet<string> { "GET", "POST", "OPTIONS" };
        var xx = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault()?.ToUpper().Split(',').Select(m => m.Trim());
        Assert.NotNull(xx);
        var actualMethods = new HashSet<string>(xx);
        Assert.Equal(expectedMethods, actualMethods);
    }

    [Fact]
    public async Task TestCorsPreflightMaxAge() {
        var response = await client.SendAsync(OptionsRequestMessage);
        var xx = response.Headers.GetValues("Access-Control-Max-Age").FirstOrDefault();
        Assert.NotNull(xx);
        Assert.True(int.Parse(xx) >= 5 * 60);
    }

    [Fact]
    public async Task TestCorsGet() {
        var origin = "https://get.example.com";
        var request = new HttpRequestMessage(HttpMethod.Get, test_basic_point_url) {
            Headers = { { "Origin", origin } }
        };
        var response = await client.SendAsync(request);
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault());
    }

}
