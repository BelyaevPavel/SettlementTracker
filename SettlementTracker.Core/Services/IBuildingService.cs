using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Services
{
    public interface IBuildingService
    {
        Task<IEnumerable<BuildingDefinition>> GetAvailableDefinitionsAsync();
        Task<BuildingDefinition?> GetDefinitionByIdAsync(string definitionId);

        Task<IEnumerable<Building>> GetBuiltBuildingsAsync();
        Task<Building?> GetBuildingAsync(Guid id);
        Task<bool> IsBuildingBuiltAsync(string definitionId);

        Task<Building> BuildAsync(string definitionId, (int X, int Y) position);
        Task<bool> DemolishAsync(Guid buildingId);
        Task<bool> ToggleActiveAsync(Guid buildingId);

        Task<bool> AssignCitizenAsync(Guid buildingId, Guid citizenId);
        Task<bool> UnassignCitizenAsync(Guid buildingId, Guid citizenId);

        Task SaveChangesAsync();
        Task LoadFromFileAsync();

        Task<bool> CanAffordBuildAsync(string definitionId);
        Task<bool> TrySpendResourcesForBuildAsync(string definitionId);

        public event EventHandler? BuildingChange;
    }
}