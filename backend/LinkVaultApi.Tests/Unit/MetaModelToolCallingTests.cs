using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
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
    private readonly Mock<ILogger<MetaLlamaClient>> _mockLogger = new();

    [Fact]
    public void GetWorkspaceToolsDefinition_ReturnsAllSixToolsWithCorrectSchemas()
    {
        // Act
        var tools = MetaLlamaClient.GetWorkspaceToolsDefinition();

        // Assert
        tools.Should().NotBeNull();
        tools.Count.Should().Be(6);

        var toolNames = new List<string>();
        foreach (var tool in tools)
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
            "create_directory",
            "move_resource",
            "create_share_link",
            "search",
            "list_directory",
            "delete_resource"
        });
    }

    [Fact]
    public async Task GenerateToolPlanAsync_SendsToolsArrayAndExtractsModelToolCalls()
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
                                ["content"] = "Plan synthesized via Meta Muse Spark tool calling.",
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

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateToolPlanAsync("Create Quarterly Reports and move file into it");

        // Assert: Request payload verification
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://inference.baseten.co/v1/chat/completions");
        capturedBody.Should().NotBeNull();

        var parsedBody = JsonNode.Parse(capturedBody!);
        parsedBody?["tools"].Should().NotBeNull();
        parsedBody?["tools"]?.AsArray().Count.Should().Be(6);
        parsedBody?["tool_choice"]?.ToString().Should().Be("auto");
        parsedBody?["model"]?.ToString().Should().Be("meta-models/muse-spark-open");

        // Assert: Plan result verification (extracted from model tool calls)
        result.Should().NotBeNull();
        result.Summary.Should().Contain("Meta Muse Spark");
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].ToolName.Should().Be("create_directory");
        result.ToolCalls[0].ArgumentsJson.Should().Contain("Quarterly Reports");
        result.ToolCalls[1].ToolName.Should().Be("move_resource");
    }

    [Fact]
    public async Task GenerateToolPlanAsync_WhenToolCallsReturned_HandlesNullContentAndIgnoresReasoningContent()
    {
        // Arrange: Verified fact: When tool calls are returned, message.content is null; reasoning_content must be ignored
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
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
                                ["reasoning_content"] = "Thinking: User wants to move file...",
                                ["tool_calls"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["id"] = "call_move_1",
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

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateToolPlanAsync("Move my document");

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Be("Synthesized plan containing 1 step(s).");
        result.Summary.Should().NotContain("Thinking");
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].ToolName.Should().Be("move_resource");
    }

    [Fact]
    public async Task GenerateToolPlanAsync_WhenRemoteApiFails_GracefullyFallsBackToLocalPlanner()
    {
        // Arrange: Mock handler returns HTTP 502 Bad Gateway
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://inference.baseten.co/v1",
            ModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateToolPlanAsync("Organize my invoices into an Invoices folder and share the latest invoice for 7 days");

        // Assert: Local fallback safely engaged
        result.Should().NotBeNull();
        result.ToolCalls.Should().NotBeEmpty();
        result.ToolCalls.Should().Contain(t => t.ToolName == "create_directory");
    }

    [Fact]
    public async Task GenerateToolPlanAsync_WithWorkspaceContext_InjectsRealIdsIntoLocalFallbackPlan()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var config = Options.Create(new MetaModelConfig
        {
            ApiKey = "", // Forces local planner fallback
            ModelName = "meta-models/muse-spark-open"
        });
        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        var realResourceId = Guid.NewGuid();
        var realDirId = Guid.NewGuid();
        var context = new WorkspaceContextDto
        {
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Directories = new List<WorkspaceDirectoryContext>
            {
                new() { Id = realDirId, Name = "Invoices" }
            },
            Resources = new List<WorkspaceResourceContext>
            {
                new() { Id = realResourceId, Name = "invoice_2026_Q3.pdf", Type = "File" }
            }
        };

        // Act
        var result = await client.GenerateToolPlanAsync("Move invoice to Invoices and share it", context);

        // Assert
        result.Should().NotBeNull();
        var moveCall = result.ToolCalls.FirstOrDefault(t => t.ToolName == "move_resource");
        moveCall.Should().NotBeNull();
        moveCall!.ArgumentsJson.Should().Contain(realResourceId.ToString());
        moveCall.ArgumentsJson.Should().Contain(realDirId.ToString());

        var shareCall = result.ToolCalls.FirstOrDefault(t => t.ToolName == "create_share_link");
        shareCall.Should().NotBeNull();
        shareCall!.ArgumentsJson.Should().Contain(realResourceId.ToString());
    }

    [Fact]
    public async Task ClassifyImageWithVisionAsync_SendsOpenAiImageUrlFormat_ParsesResponse()
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
            ModelName = "meta-models/muse-spark-open"
        });

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);
        var sampleBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic bytes

        // Act
        var result = await client.ClassifyImageWithVisionAsync(sampleBytes, "image/png");

        // Assert: Request payload verification
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://inference.baseten.co/v1/chat/completions");
        capturedBody.Should().NotBeNull();

        var parsedBody = JsonNode.Parse(capturedBody!);
        parsedBody?["model"]?.ToString().Should().Be("meta-models/muse-spark-open");

        var userMessage = parsedBody?["messages"]?[0];
        userMessage?["role"]?.ToString().Should().Be("user");

        var contentArray = userMessage?["content"]?.AsArray();
        contentArray.Should().NotBeNull();
        contentArray!.Count.Should().Be(2);
        contentArray[0]?["type"]?.ToString().Should().Be("text");
        contentArray[1]?["type"]?.ToString().Should().Be("image_url");

        var imageUrl = contentArray[1]?["image_url"]?["url"]?.ToString();
        imageUrl.Should().StartWith("data:image/png;base64,");

        // Assert: Parsed result
        result.Should().NotBeNull();
        result.Category.Should().Be("Architecture Diagrams");
        result.Description.Should().Be("Cloud infrastructure schematic");
        result.Confidence.Should().Be(0.96);
    }

    [Fact]
    public void MetaModelConfig_DefaultsToVerifiedBasetenEndpointAndMuseSpark()
    {
        // Arrange & Act
        var config = new MetaModelConfig();
        var llamaConfig = new MetaLlamaConfig();

        // Assert: BaseUrl and Model defaults
        config.BaseUrl.Should().Be("https://inference.baseten.co/v1");
        config.ModelName.Should().Be("meta-models/muse-spark-open");
        config.VisionModel.Should().Be("meta-models/muse-spark-open");
        config.MuseSparkModel.Should().Be("meta-models/muse-spark-open");
        config.MuseGlimmerModel.Should().Be("meta-models/Muse-Glimmer-30B");
        config.ApiKey.Should().BeEmpty();

        llamaConfig.BaseUrl.Should().Be("https://inference.baseten.co/v1");
        llamaConfig.ModelName.Should().Be("meta-models/muse-spark-open");
        llamaConfig.Llama33Model.Should().Be("meta-models/muse-spark-open");
        llamaConfig.Llama31Model.Should().Be("meta-models/Muse-Glimmer-30B");
    }
}
