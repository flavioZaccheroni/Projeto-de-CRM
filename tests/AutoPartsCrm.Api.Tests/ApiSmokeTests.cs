using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoPartsCrm.Api.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(_ => { }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body);
    }

    [Fact]
    public async Task Dashboard_ReturnsCounters()
    {
        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("customers", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithDevelopmentUser_ReturnsSession()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@crm.local",
            password = "123456"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("dev-session", body);
    }

    [Theory]
    [InlineData("/api/customer-interactions")]
    [InlineData("/api/quotations")]
    [InlineData("/api/sales-orders")]
    [InlineData("/api/work-orders")]
    [InlineData("/api/stock-balances")]
    [InlineData("/api/stock-movements")]
    [InlineData("/api/purchase-orders")]
    public async Task CommercialEndpoints_ReturnOk(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
