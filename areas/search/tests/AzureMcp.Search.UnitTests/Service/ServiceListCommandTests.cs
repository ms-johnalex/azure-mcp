// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureMcp.Core.Models.Command;
using AzureMcp.Core.Options;
using AzureMcp.Search.Commands.Service;
using AzureMcp.Search.Services;
using AzureMcp.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AzureMcp.Search.UnitTests.Service;

public class ServiceListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISearchService _searchService;
    private readonly ILogger<ServiceListCommand> _logger;

    public ServiceListCommandTests()
    {
        _searchService = Substitute.For<ISearchService>();
        _logger = Substitute.For<ILogger<ServiceListCommand>>();

        var collection = new ServiceCollection();
        collection.AddSingleton(_searchService);

        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsServices_WhenServicesExist()
    {
        // Arrange
        var expectedServices = new List<string> { "service1", "service2" };
        _searchService.ListServices(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RetryPolicyOptions>())
            .Returns(expectedServices);

        var command = new ServiceListCommand(_logger);
        var parser = new Parser(command.GetCommand());
        var args = parser.Parse("--subscription sub123");
        var context = new CommandContext(_serviceProvider);

        // Act
        var response = await command.ExecuteAsync(context, args);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize<ServiceListResult>(json);

        Assert.NotNull(result);
        Assert.Equal(expectedServices, result.Services);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenNoServices()
    {
        // Arrange
        _searchService.ListServices(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RetryPolicyOptions>())
            .Returns(new List<string>());

        var command = new ServiceListCommand(_logger);
        var parser = new Parser(command.GetCommand());
        var args = parser.Parse("--subscription sub123");
        var context = new CommandContext(_serviceProvider);

        // Act
        var response = await command.ExecuteAsync(context, args);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.Results);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        // Arrange
        var expectedError = "Test error";
        var subscriptionId = "sub123";

        _searchService.ListServices(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RetryPolicyOptions>())
            .ThrowsAsync(new Exception(expectedError));

        var command = new ServiceListCommand(_logger);
        var parser = new Parser(command.GetCommand());
        var args = parser.Parse($"--subscription {subscriptionId}");
        var context = new CommandContext(_serviceProvider);

        // Act
        var response = await command.ExecuteAsync(context, args);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(500, response.Status);
        Assert.StartsWith(expectedError, response.Message);
    }

    private class ServiceListResult
    {
        [JsonPropertyName("services")]
        public List<string> Services { get; set; } = [];
    }
}
