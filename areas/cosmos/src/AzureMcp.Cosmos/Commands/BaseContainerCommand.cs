// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AzureMcp.Core.Commands;
using AzureMcp.Cosmos.Options;

namespace AzureMcp.Cosmos.Commands;

public abstract class BaseContainerCommand<
    [DynamicallyAccessedMembers(TrimAnnotations.CommandAnnotations)] TOptions>
    : BaseDatabaseCommand<TOptions> where TOptions : BaseContainerOptions, new()
{
    private readonly Option<string> _containerOption = CosmosOptionDefinitions.Container;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_containerOption);
    }

    protected override TOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Container = parseResult.GetValueForOption(_containerOption);
        return options;
    }
}
