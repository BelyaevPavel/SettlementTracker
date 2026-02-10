using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Repositories
{
    public interface IPopulationDefinitionRepository
    {
        PopulationDefinition LoadPopulationDefinition();
        Task<PopulationDefinition> LoadPopulationDefinitionAsync();
        void SavePopulationDefinition(PopulationDefinition definition);
        Task SavePopulationDefinitionAsync(PopulationDefinition definition);
    }
}