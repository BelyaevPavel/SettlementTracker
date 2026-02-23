using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Repositories
{
    public interface IJobDefinitionRepository
    {
        Dictionary<string, JobDefinition> LoadJobDefinitions();
        Task<Dictionary<string, JobDefinition>> LoadJobDefinitionsAsync();
        void SaveJobDefinitions(Dictionary<string, JobDefinition> definitions);
        Task SaveJobDefinitionsAsync(Dictionary<string, JobDefinition> definitions);
    }
}