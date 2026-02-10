using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Definitions
{
    public class JobDefinition
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

        [JsonPropertyName("dailyEffects")] public List<ResourceEffect> DailyEffects { get; set; } = new();

        [JsonPropertyName("workerRequirements")]
        public List<WorkerRequirement> WorkerRequirements { get; set; } = new();

        [JsonPropertyName("maxWorkers")] public int MaxWorkers { get; set; } = 1;

        [JsonPropertyName("baseEfficiency")] public Dictionary<string, float> BaseEfficiency { get; set; } = new();
        // Ключ: комбинация AgeCategory_Gender_IsSlave, например: "Adult_Male_False"
    }
}