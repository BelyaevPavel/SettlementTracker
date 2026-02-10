using System.Text.Json.Serialization;

namespace SettlementTracker.Core.Models.Definitions
{
    public class ResourceEffect
    {
        [JsonPropertyName("resourceId")] public string ResourceId { get; set; } = string.Empty;

        [JsonPropertyName("amount")] public float Amount { get; set; }

        [JsonPropertyName("isProduction")]
        public bool IsProduction { get; set; } // true = производит, false = потребляет
    }
}