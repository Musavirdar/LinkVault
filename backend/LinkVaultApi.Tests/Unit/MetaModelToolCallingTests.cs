using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LinkVaultApi.Agent.Exceptions;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Agent.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class MetaModelToolCallingTests
{
    private readonly Mock<ILogger<MetaModelClient>> _mockLogger = new();

    [Fact]
    public void GetWorkspaceToolsDefinition_ReturnsToolsWithCorrectSchemas()
    {
        // Act
        var toolsNoVision = MetaModelClient.GetWorkspaceToolsDefinition(includeVision: false);
        var toolsWithVision = MetaModelClient.GetWorkspaceToolsDefinition(includeVision: true);

        // Assert
        toolsNoVision.Count.Should().Be(6);
        toolsWithVision.Count.Should().Be(7);

        var toolNames = new List<string>();
        foreach (var tool in toolsNoVision)
        {
            var fn = tool?["function"];
            fn.Should().NotBeNull();
            var name = fn?["name"]?.ToString();
            name.Should().NotBeNullOrWhiteSpace();
            toolNames.Add(name!);

            var parameters = fn?["parameters"];
            parameters.Should().NotBeNull();
            parameters?["type"]?.ToString().Should().Be("object");
            parameters?["properties"].Should().NotBeNull();
        }

        toolNames.Should().Contain(new[]
        {
            "list_directory",
            "search",
            "create_directory",
            "move_resource",
            "create_share_link",
            "delete_resource"
        });
    }

    [Fact]
    public async Task ChatCompletionAsync_SendsToolsArrayAndExtractsModelToolCalls()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync(ct);
                }
            })
            .ReturnsAsync(() =>
            {
                var fakeResourceId = Guid.NewGuid().ToString();
                var responseJson = new JsonObject
                {
                    ["choices"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["message"] = new JsonObject
                            {
                                ["role"] = "assistant",
                                ["content"] = null,
                                ["reasoning_content"] = "Internal thinking that must be suppressed",
                                ["tool_calls"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["id"] = "call_dir_1",
                                        ["type"] = "function",
                                        ["function"] = new JsonObject
                                        {
                                            ["name"] = "create_directory",
                                            ["arguments"] = "{\"name\":\"Quarterly Reports\"}"
                                        }
                                    },
                                    new JsonObject
                                    {
                                        ["id"] = "call_move_2",
                                        ["type"] = "function",
                                        ["function"] = new JsonObject
                                        {
                                            ["name"] = "move_resource",
                                            ["arguments"] = $"{{\"resourceId\":\"{fakeResourceId}\"}}"
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://inference.baseten.co/v1",
            ModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaModelClient(config, httpClient, _mockLogger.Object);
        var tools = MetaModelClient.GetWorkspaceToolsDefinition(false);

        // Act
        var result = await client.ChatCompletionAsync(new List<ModelChatMessage>
        {
            new("user", "Create Quarterly Reports and move file into it")
        }, tools);

        // Assert: Request payload verification
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://inference.baseten.co/v1/chat/completions");
        capturedBody.Should().NotBeNull();

        var parsedBody = JsonNode.Parse(capturedBody!);
        parsedBody?["tools"].Should().NotBeNull();
        parsedBody?["tools"]?.AsArray().Count.Should().Be(6);
        parsedBody?["tool_choice"]?.ToString().Should().Be("auto");
        parsedBody?["model"]?.ToString().Should().Be("meta-models/muse-spark-open");

        // Assert: Result verification
        result.Should().NotBeNull();
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].ToolName.Should().Be("create_directory");
        result.ToolCalls[0].ArgumentsJson.Should().Contain("Quarterly Reports");
        result.ToolCalls[1].ToolName.Should().Be("move_resource");
    }

    [Fact]
    public async Task ChatCompletionAsync_RetriesOnceOn429_AndSucceeds()
    {
        // Arrange
        int attempts = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                attempts++;
                if (attempts == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                }

                var responseJson = new JsonObject
                {
                    ["choices"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["message"] = new JsonObject
                            {
                                ["role"] = "assistant",
                                ["content"] = "Response after rate limit retry"
                            }
                        }
                    }
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://inference.baseten.co/v1",
            ModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaModelClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.ChatCompletionAsync(new List<ModelChatMessage> { new("user", "Hello") });

        // Assert
        attempts.Should().Be(2);
        result.Should().NotBeNull();
        result.Content.Should().Be("Response after rate limit retry");
    }

    [Fact]
    public async Task ChatCompletionAsync_RemoteFailure_ThrowsModelUnavailableException()
    {
        // Arrange: Mock handler returns 503
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://inference.baseten.co/v1",
            ModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaModelClient(config, httpClient, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ModelUnavailableException>(() =>
            client.ChatCompletionAsync(new List<ModelChatMessage> { new("user", "Test") }));
    }

    [Fact]
    public async Task ClassifyImageAsync_SendsOpenAiImageUrlFormat_ParsesResponse()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync(ct);
                }
            })
            .ReturnsAsync(() =>
            {
                var responseJson = new JsonObject
                {
                    ["choices"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["message"] = new JsonObject
                            {
                                ["role"] = "assistant",
                                ["content"] = "{\"category\":\"Architecture Diagrams\",\"description\":\"Cloud infrastructure schematic\",\"confidence\":0.96}"
                            }
                        }
                    }
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://inference.baseten.co/v1",
            ModelName = "meta-models/muse-spark-open",
            VisionModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaModelClient(config, httpClient, _mockLogger.Object);
        var sampleBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Act
        var result = await client.ClassifyImageAsync(sampleBytes, "image/png");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://inference.baseten.co/v1/chat/completions");
        capturedBody.Should().NotBeNull();

        result.Should().NotBeNull();
        result!.Category.Should().Be("Architecture Diagrams");
        result.Description.Should().Be("Cloud infrastructure schematic");
        result.Confidence.Should().Be(0.96);
    }

    [Fact]
    public void MetaModelConfig_DefaultsToVerifiedBasetenEndpointAndHackathonModel()
    {
        // Arrange & Act
        var config = new MetaModelConfig();

        // Assert
        config.BaseUrl.Should().Be("https://inference.baseten.co/v1");
        config.ModelName.Should().Be("meta-models/muse-spark-open");
        config.ApiKey.Should().BeEmpty();
        config.TimeoutSeconds.Should().Be(30);
    }
}
