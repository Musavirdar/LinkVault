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
                                ["content"] = "Plan synthesized via Meta Llama 3.3 tool calling.",
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
        var config = Options.Create(new MetaLlamaConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://bridge.baseten.co/v1",
            Llama33Model = "meta-llama/Llama-3.3-70B-Instruct"
        });

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateToolPlanAsync("Create Quarterly Reports and move file into it");

        // Assert: Request payload verification
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://bridge.baseten.co/v1/chat/completions");
        capturedBody.Should().NotBeNull();

        var parsedBody = JsonNode.Parse(capturedBody!);
        parsedBody?["tools"].Should().NotBeNull();
        parsedBody?["tools"]?.AsArray().Count.Should().Be(6);
        parsedBody?["tool_choice"]?.ToString().Should().Be("auto");
        parsedBody?["model"]?.ToString().Should().Be("meta-llama/Llama-3.3-70B-Instruct");

        // Assert: Plan result verification (extracted from model tool calls)
        result.Should().NotBeNull();
        result.Summary.Should().Contain("Meta Llama 3.3");
        result.ToolCalls.Should().HaveCount(2);
        result.ToolCalls[0].ToolName.Should().Be("create_directory");
        result.ToolCalls[0].ArgumentsJson.Should().Contain("Quarterly Reports");
        result.ToolCalls[1].ToolName.Should().Be("move_resource");
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
        var config = Options.Create(new MetaLlamaConfig
        {
            ApiKey = "test-meta-key",
            BaseUrl = "https://bridge.baseten.co/v1",
            Llama33Model = "meta-llama/Llama-3.3-70B-Instruct"
        });

        var client = new MetaLlamaClient(config, httpClient, _mockLogger.Object);

        // Act
        var result = await client.GenerateToolPlanAsync("Organize my invoices into an Invoices folder and share the latest invoice for 7 days");

        // Assert: Local fallback safely engaged
        result.Should().NotBeNull();
        result.ToolCalls.Should().NotBeEmpty();
        result.ToolCalls.Should().Contain(t => t.ToolName == "create_directory");
    }
}
