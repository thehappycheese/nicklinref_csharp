using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public partial class TestIntegration : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    private const string test_basic_point_url = "/point?road=H001&slk=1.4";

    public TestIntegration(WebApplicationFactory<Program> factory) {
        this.factory = factory;
        client = this.factory.CreateClient();
    }
}