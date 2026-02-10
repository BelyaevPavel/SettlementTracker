using System;
using System.Collections.Generic;

namespace SettlementTracker.Core.Models.Entities
{
    public class SettlementState
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = "Новое поселение";
        public int CurrentDay { get; set; } = 1;

        // Население
        public List<Citizen> Citizens { get; set; } = new();

        // Здания
        public List<Building> Buildings { get; set; } = new();

        // Активные задания
        public List<ActiveJob> ActiveJobs { get; set; } = new();

        // Ресурсы
        public Dictionary<string, float> Resources { get; set; } = new();

        // История изменений ресурсов за день
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<string, float> DailyResourceChanges { get; set; } = new();

        public SettlementState(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}