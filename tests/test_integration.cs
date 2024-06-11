using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public partial class TestIntegration : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;


    /// <summary>A constant URL that will produce a successful query result;</summary>
    private const string TEST_URL = "/?road=H001&slk=1.4";

    public TestIntegration(WebApplicationFactory<Program> factory) {
        this.factory = factory;
        client = this.factory.CreateClient();
    }
}