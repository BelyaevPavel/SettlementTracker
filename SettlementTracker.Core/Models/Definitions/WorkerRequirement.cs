using System.Text.Json.Serialization;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Models.Definitions
{
    public class WorkerRequirement
    {
        [JsonPropertyName("ageCategory")] public AgeCategory AgeCategory { get; set; }

        [JsonPropertyName("gender")] public Gender Gender { get; set; }

        [JsonPropertyName("count")] public int Count { get; set; }

        [JsonPropertyName("canBeSlave")] public bool CanBeSlave { get; set; }

        [JsonPropertyName("requiresSupervision")]
        public bool RequiresSupervision { get; set; } // Требует присмотра взрослого
    }
}