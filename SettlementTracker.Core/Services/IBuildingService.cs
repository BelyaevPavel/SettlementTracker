using System;
using System.Collections.Generic;
using System.Threading;
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

        Task<bool> TryBuildAsync(string definitionId, (int X, int Y) position,
            CancellationToken cancellationToken = default);

        Task<bool> TryDemolishAsync(Guid buildingId,
            CancellationToken cancellationToken = default);

        Task<bool> ToggleActiveAsync(Guid buildingId);

        Task<bool> AssignCitizenAsync(Guid buildingId, Guid citizenId);
        Task<bool> UnassignCitizenAsync(Guid buildingId, Guid citizenId);

        Task SaveChangesAsync();
        Task LoadFromFileAsync();

        bool CanAffordBuild(string definitionId);

        public event EventHandler? BuildingChange;
    }
}