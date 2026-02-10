using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Repositories
{
    public interface IBuildingDefinitionRepository
    {
        Dictionary<string, BuildingDefinition> LoadBuildingDefinitions();
        Task<Dictionary<string, BuildingDefinition>> LoadBuildingDefinitionsAsync();
        void SaveBuildingDefinitions(Dictionary<string, BuildingDefinition> definitions);
        Task SaveBuildingDefinitionsAsync(Dictionary<string, BuildingDefinition> definitions);
    }
}