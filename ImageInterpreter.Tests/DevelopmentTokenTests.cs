using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImageInterpreter.Tests;

[Collection(JwtHostCollection.Name)]
public class DevelopmentTokenTests
{
    private readonly JwtHostFixture _host;

    public DevelopmentTokenTests(JwtHostFixture host)
    {
        _host = host;
    }

    [Fact]
    public async Task Development_get_dev_token_returns_a_bearer_the_post_accepts()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

        using var tokenResponse = await client.GetAsync("/dev/token");
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        using var document = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        Assert.Equal("Bearer", document.RootElement.GetProperty("tokenType").GetString());
        var accessToken = document.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var request = InterpretationRequests.PostImage($"Bearer {accessToken}");
        using var response = await client.SendAsync(request);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Testing_get_dev_token_is_not_found()
    {
        using var client = _host.CreateClient();
        using var response = await client.GetAsync("/dev/token");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
