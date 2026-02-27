using System;
using System.Collections.Generic;
using System.Threading;
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
        Task LoadDefinitionsAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task SaveStateAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task LoadStateAsync(CancellationToken cancellationToken = default(CancellationToken));

        IReadOnlyDictionary<string, ResourceDefinition> GetResourceDefinitions();
        IReadOnlyDictionary<string, float> GetCurrentBalance();


        Task SetResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken));

        Task AddResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> TrySpendResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken));

        bool CanSpend(string resourceId, float amount);
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}