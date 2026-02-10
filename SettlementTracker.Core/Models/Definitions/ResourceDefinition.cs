using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Definitions
{
    public class ResourceDefinition
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

        [JsonPropertyName("iconPath")] public string IconPath { get; set; } = string.Empty;

        [JsonPropertyName("isConsumable")] public bool IsConsumable { get; set; } = true;
    }
}