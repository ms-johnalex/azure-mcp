// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using AzureMcp.Tests;
using AzureMcp.Tests.Client;
using AzureMcp.Tests.Client.Helpers;
using Xunit;

namespace AzureMcp.AzureIsv.LiveTests;

public class AzureIsvCommandTests(LiveTestFixture liveTestFixture, ITestOutputHelper output)
    : CommandTestsBase(liveTestFixture, output),
    IClassFixture<LiveTestFixture>
{
    [Fact]
    public async Task Should_list_datadog_monitored_resources()
    {
        // Skipping test if Tenant is not 'Customer LED Tenant'
        if (Settings.TenantId != "888d76fa-54b2-4ced-8ee5-aac1585adee7")
        {
            Assert.Skip("Test skipped because Tenant is not 'Customer LED Tenant'.");
        }
        var result = await CallToolAsync(
            "azmcp_datadog_monitoredresources_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "datadog-resource", Settings.ResourceBaseName }
            });

        var resources = result.AssertProperty("resources");
        Assert.Equal(JsonValueKind.Array, resources.ValueKind);
    }
}
