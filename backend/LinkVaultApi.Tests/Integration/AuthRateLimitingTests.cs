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

    [Fact]
    public async Task LoginEndpoint_WithForwardedFor_ShouldPartitionByClientIp()
    {
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.195");

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.42");

        var request = new LoginRequestDto
        {
            EmailOrUsername = "testuser",
            Password = "WrongPassword123!"
        };

        // Client A consumes its 10-request allowance
        for (int i = 0; i < 10; i++)
        {
            var res = await clientA.PostAsJsonAsync("/api/auth/login", request);
            res.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        // Client A's 11th request must be rate limited to 429
        var clientAThrottled = await clientA.PostAsJsonAsync("/api/auth/login", request);
        clientAThrottled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Client B (different X-Forwarded-For IP) must NOT be blocked by Client A's quota exhaustion
        var clientBResponse = await clientB.PostAsJsonAsync("/api/auth/login", request);
        clientBResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }
}
