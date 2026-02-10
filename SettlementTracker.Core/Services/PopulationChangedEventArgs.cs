using System;
using SettlementTracker.Core.Managers;

namespace SettlementTracker.Core.Services
{
    public class PopulationChangedEventArgs : EventArgs
    {
        public PopulationStatistics Statistics { get; }

        public PopulationChangedEventArgs(PopulationStatistics statistics)
        {
            Statistics = statistics;
        }
    }
}