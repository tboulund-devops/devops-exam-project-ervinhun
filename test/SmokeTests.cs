using System.Net;
using FluentAssertions;

namespace test;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SmokeTests(CustomWebApplicationFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task App_starts_and_serves_swagger()
    {
        var res = await _client.GetAsync("/swagger");
        res.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }
}