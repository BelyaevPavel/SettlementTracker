using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Entities
{
    public class SettlementState
    {
        public SettlementState(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

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
        [JsonIgnore] public Dictionary<string, float> DailyResourceChanges { get; set; } = new();
    }
}