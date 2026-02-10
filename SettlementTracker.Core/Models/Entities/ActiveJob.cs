using System;
using System.Collections.Generic;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Models.Entities
{
    public class ActiveJob
    {
        public Guid Id { get; private set; }
        public string DefinitionId { get; private set; }
        public string Name { get; set; } = string.Empty;

        // Назначенные работники
        public List<Guid> AssignedCitizenIds { get; private set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public JobDefinition? Definition { get; set; }

        public ActiveJob(Guid id, string definitionId)
        {
            Id = id;
            DefinitionId = definitionId;
        }

        public void AssignCitizen(Guid citizenId)
        {
            if (!AssignedCitizenIds.Contains(citizenId)) AssignedCitizenIds.Add(citizenId);
        }

        public void UnassignCitizen(Guid citizenId)
        {
            AssignedCitizenIds.Remove(citizenId);
        }
    }
}