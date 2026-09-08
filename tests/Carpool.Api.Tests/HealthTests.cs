using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Carpool.Api.Tests;

public sealed class HealthTests
{
    private static WebApplicationFactory<Program> CreateFactory(string environment = "Testing") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1",
                    ["Database:ApplyMigrations"] = "false",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
                }));
        });

    [Fact]
    public async Task LivenessWorksWithoutDatabase()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Healthy", payload!["status"]);
    }

    [Fact]
    public async Task ReadinessFailsWhenDatabaseIsUnavailableWithoutExposingDetails()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unhealthy", body);
        Assert.DoesNotContain("127.0.0.1", body);
    }

    [Fact]
    public async Task ProductionDoesNotExposeOpenApi()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task ConfiguredFrontendOriginIsAllowed()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        var response = await client.GetAsync("/api/health");
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
