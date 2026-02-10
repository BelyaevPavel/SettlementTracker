using System;
using System.Collections.Generic;

namespace SettlementTracker.Core.Managers
{
    public class ResourcesChangedEventArgs : EventArgs
    {
        public Dictionary<string, float> CurrentResources { get; }

        public ResourcesChangedEventArgs(Dictionary<string, float> currentResources)
        {
            CurrentResources = currentResources;
        }
    }
}