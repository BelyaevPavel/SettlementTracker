using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Definitions
{
    public class BuildingDefinition
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

        [JsonPropertyName("size")] public (int Width, int Height) Size { get; set; } = (1, 1);

        [JsonPropertyName("buildCost")] public List<ResourceEffect> BuildCost { get; set; } = new();

        [JsonPropertyName("dailyEffects")] public List<ResourceEffect> DailyEffects { get; set; } = new();

        [JsonPropertyName("workerRequirements")]
        public List<WorkerRequirement> WorkerRequirements { get; set; } = new();

        [JsonPropertyName("maxWorkers")] public int MaxWorkers { get; set; } = 0;

        [JsonPropertyName("providesHousing")]
        public int ProvidesHousing { get; set; } = 0; // Сколько людей может разместить
    }
}