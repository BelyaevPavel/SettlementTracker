using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Definitions
{
    public class PopulationDefinition
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "default";

        [JsonPropertyName("name")] public string Name { get; set; } = "Население";

        [JsonPropertyName("description")] public string Description { get; set; } = "Настройки населения";

        [JsonPropertyName("childMaxAge")] public int ChildMaxAge { get; set; } = 14; // До какого возраста ребенок

        [JsonPropertyName("adultMaxAge")] public int AdultMaxAge { get; set; } = 59; // До какого возраста взрослый

        [JsonPropertyName("elderMaxAge")] public int ElderMaxAge { get; set; } = 80; // До какого живёт

        [JsonPropertyName("agePerDay")]
        public float AgePerDay { get; set; } = 1.0f / 365; // На сколько лет стареет житель за день

        [JsonPropertyName("dailyNeeds")] public List<ResourceEffect> DailyNeeds { get; set; } = new();

        [JsonPropertyName("starvationDeathChance")]
        public float StarvationDeathChance { get; set; } = 0.1f; // Шанс смерти от голода в день

        [JsonPropertyName("slaveEfficiencyPenalty")]
        public float SlaveEfficiencyPenalty { get; set; } = 0.5f; // Модификатор эффективности рабов без присмотра

        [JsonPropertyName("childEfficiencyPenalty")]
        public float ChildEfficiencyPenalty { get; set; } = 0.5f; // Модификатор эффективности детей без присмотра

        [JsonPropertyName("elderEfficiencyPenalty")]
        public float ElderEfficiencyPenalty { get; set; } = 0.7f; // Модификатор эффективности стариков
    }
}