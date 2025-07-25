// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine.Parsing;
using AzureMcp.Core.Models.Command;
using AzureMcp.Sql.Commands.EntraAdmin;
using AzureMcp.Sql.Models;
using AzureMcp.Sql.Services;
using AzureMcp.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzureMcp.Sql.UnitTests.EntraAdmin;

public class EntraAdminListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlService _service;
    private readonly ILogger<EntraAdminListCommand> _logger;
    private readonly EntraAdminListCommand _command;
    private readonly CommandContext _context;
    private readonly Parser _parser;

    public EntraAdminListCommandTests()
    {
        _service = Substitute.For<ISqlService>();
        _logger = Substitute.For<ILogger<EntraAdminListCommand>>();

        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();

        _command = new(_logger);
        _context = new(_serviceProvider);
        _parser = new(_command.GetCommand());
    }

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = _command.GetCommand();
        Assert.Equal("list", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Theory]
    [InlineData("--subscription sub --resource-group rg --server server", true)]
    [InlineData("--subscription sub --resource-group rg", false)] // Missing server
    [InlineData("--subscription sub --server server", false)] // Missing resource group
    [InlineData("--resource-group rg --server server", false)] // Missing subscription
    [InlineData("", false)] // Missing all required parameters
    public async Task ExecuteAsync_ValidatesInputCorrectly(string args, bool shouldSucceed)
    {
        // Arrange
        if (shouldSucceed)
        {
            _service.GetEntraAdministratorsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
                Arg.Any<CancellationToken>())
                .Returns(new List<SqlServerEntraAdministrator>());
        }

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse(args);

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(shouldSucceed ? 200 : 400, response.Status);
        if (shouldSucceed)
        {
            Assert.Equal("Success", response.Message);
        }
        else
        {
            Assert.Contains("required", response.Message.ToLower());
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAdministratorsSuccessfully()
    {
        // Arrange
        var administrators = new List<SqlServerEntraAdministrator>
        {
            new("ActiveDirectory", "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Sql/servers/server/administrators/ActiveDirectory",
                "Microsoft.Sql/servers/administrators", "ActiveDirectory", "admin@domain.com", "12345678-1234-1234-1234-123456789012",
                "87654321-4321-4321-4321-210987654321", false)
        };

        _service.GetEntraAdministratorsAsync(
            "testserver",
            "testrg",
            "testsub",
            Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(administrators);

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse("--subscription testsub --resource-group testrg --server testserver");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Results);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmptyListWhenNoAdministrators()
    {
        // Arrange
        _service.GetEntraAdministratorsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<SqlServerEntraAdministrator>());

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse("--subscription testsub --resource-group testrg --server testserver");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.Null(response.Results);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesServiceErrors()
    {
        // Arrange
        _service.GetEntraAdministratorsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<SqlServerEntraAdministrator>>(new Exception("Test error")));

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse("--subscription testsub --resource-group testrg --server testserver");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(500, response.Status);
        Assert.Contains("Test error", response.Message);
        Assert.Contains("troubleshooting", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Handles404Error()
    {
        // Arrange
        var requestException = new Azure.RequestFailedException(404, "Server not found");
        _service.GetEntraAdministratorsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<SqlServerEntraAdministrator>>(requestException));

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse("--subscription testsub --resource-group testrg --server testserver");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(404, response.Status);
        Assert.Contains("SQL server not found", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Handles403Error()
    {
        // Arrange
        var requestException = new Azure.RequestFailedException(403, "Access denied");
        _service.GetEntraAdministratorsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<AzureMcp.Core.Options.RetryPolicyOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<SqlServerEntraAdministrator>>(requestException));

        var context = new CommandContext(_serviceProvider);
        var parseResult = _parser.Parse("--subscription testsub --resource-group testrg --server testserver");

        // Act
        var response = await _command.ExecuteAsync(context, parseResult);

        // Assert
        Assert.Equal(403, response.Status);
        Assert.Contains("Authorization failed", response.Message);
    }
}
