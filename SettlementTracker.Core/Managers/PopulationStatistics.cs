using System.Collections.Generic;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Managers
{
    public class PopulationStatistics
    {
        public Dictionary<(AgeCategory Age, Gender Gender, bool IsSlave), int> Counts { get; } = new();
        public Dictionary<(AgeCategory Age, Gender Gender, bool IsSlave), int> WorkingCounts { get; } = new();
        public int TotalPopulation { get; set; }
        public int TotalWorking { get; set; }
    }
}