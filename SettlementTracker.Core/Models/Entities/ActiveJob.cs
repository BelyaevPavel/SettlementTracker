using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Models.Entities
{
    public class ActiveJob
    {
        public ActiveJob(Guid id, string definitionId)
        {
            Id = id;
            DefinitionId = definitionId;
        }

        public Guid Id { get; private set; }
        public string DefinitionId { get; private set; }
        public string Name { get; set; } = string.Empty;

        // Назначенные работники
        public List<Guid> AssignedCitizenIds { get; } = new();

        [JsonIgnore] public JobDefinition? Definition { get; set; }

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