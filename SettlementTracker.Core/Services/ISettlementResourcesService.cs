using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.WebInterface.Data.Services
{
    public interface ISettlementResourcesService
    {
        /// <summary>
        ///     Loads JSON from standard file path.
        /// </summary>
        /// <returns></returns>
        Task LoadDefinitionsAsync();

        IReadOnlyList<ResourceDefinition> GetResourceDefinitions();
        IReadOnlyDictionary<string, float> GetCurrentBalance();


        Task AddResourceAsync(string resourceId, float amount);

        Task<bool> TrySpendResourceAsync(string resourceId, float amount);

        bool CanSpend(string resourceId, float amount);
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;
        Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects);
    }
}