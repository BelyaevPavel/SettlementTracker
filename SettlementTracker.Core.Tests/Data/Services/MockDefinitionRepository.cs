using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Repositories;

namespace SettlementTracker.Core.Tests.Data.Services;

public class MockDefinitionRepository : IResourceDefinitionRepository
{
    public Dictionary<string, ResourceDefinition> DefinitionPreset { get; set; } = new();

    public Dictionary<string, ResourceDefinition> LoadResourceDefinitions()
    {
        return DefinitionPreset;
    }

    public async Task<Dictionary<string, ResourceDefinition>> LoadResourceDefinitionsAsync()
    {
        return DefinitionPreset;
    }

    public void SaveResourceDefinitions(Dictionary<string, ResourceDefinition> definitions)
    {
    }

    public async Task SaveResourceDefinitionsAsync(Dictionary<string, ResourceDefinition> definitions)
    {
    }
}