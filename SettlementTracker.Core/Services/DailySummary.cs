using System.Collections.Generic;
using SettlementTracker.Core.Managers;

namespace SettlementTracker.Core.Services
{
    public class DailySummary
    {
        public int Day { get; set; }
        public Dictionary<string, float> ResourceChanges { get; set; } = new();
        public PopulationStatistics PopulationStatistics { get; set; } = new();
        public int ActiveBuildings { get; set; }
        public int ActiveJobs { get; set; }
    }
}