using System;
using SettlementTracker.Core.Managers;

namespace SettlementTracker.Core.Services
{
    public class PopulationChangedEventArgs : EventArgs
    {
        public PopulationChangedEventArgs(PopulationStatistics statistics)
        {
            Statistics = statistics;
        }

        public PopulationStatistics Statistics { get; }
    }
}