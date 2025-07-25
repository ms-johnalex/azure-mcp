// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Core.Models.ResourceGroup;

public class ResourceGroupInfo(string name, string id, string location)
{
    public string Name { get; set; } = name;
    public string Id { get; set; } = id;
    public string Location { get; set; } = location;
}
