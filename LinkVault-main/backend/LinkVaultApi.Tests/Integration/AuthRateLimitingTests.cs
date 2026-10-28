using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LinkVaultApi.Dtos;
using LinkVaultApi.Tests.Helpers;
using Xunit;

namespace LinkVaultApi.Tests.Integration;

public class AuthRateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthRateLimitingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginEndpoint_ShouldReturn429_WhenLimitExceeded()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequestDto
        {
            EmailOrUsername = "testuser",
            Password = "WrongPassword123!"
        };

        HttpResponseMessage? lastResponse = null;

        // Perform 10 requests (the limit)
        for (int i = 0; i < 10; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", request);
            lastResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        // The 11th request must be rate limited to 429 Too Many Requests
        var throttledResponse = await client.PostAsJsonAsync("/api/auth/login", request);
        throttledResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
