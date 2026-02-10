using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Repositories
{
    public interface IResourceDefinitionRepository
    {
        Dictionary<string, ResourceDefinition> LoadResourceDefinitions();
        Task<Dictionary<string, ResourceDefinition>> LoadResourceDefinitionsAsync();
        void SaveResourceDefinitions(Dictionary<string, ResourceDefinition> definitions);
        Task SaveResourceDefinitionsAsync(Dictionary<string, ResourceDefinition> definitions);
    }
}