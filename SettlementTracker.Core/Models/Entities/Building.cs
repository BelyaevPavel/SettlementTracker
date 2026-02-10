using System;
using System.Collections.Generic;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Models.Entities
{
    public class Building
    {
        public Guid Id { get; private set; }
        public string DefinitionId { get; private set; }
        public string Name { get; set; } = string.Empty;
        public (int X, int Y) Position { get; set; }
        public bool IsActive { get; set; } = true;

        // Назначенные работники
        public List<Guid> AssignedCitizenIds { get; private set; } = new();

        // Вспомогательные свойства (будут заполняться из Definition)
        [System.Text.Json.Serialization.JsonIgnore]
        public BuildingDefinition? Definition { get; set; }

        public Building(Guid id, string definitionId, (int X, int Y) position)
        {
            Id = id;
            DefinitionId = definitionId;
            Position = position;
        }

        public void AssignCitizen(Guid citizenId)
        {
            if (!AssignedCitizenIds.Contains(citizenId)) AssignedCitizenIds.Add(citizenId);
        }

        public void UnassignCitizen(Guid citizenId)
        {
            AssignedCitizenIds.Remove(citizenId);
        }

        public void UnassignAllCitizens()
        {
            AssignedCitizenIds.Clear();
        }
    }
}