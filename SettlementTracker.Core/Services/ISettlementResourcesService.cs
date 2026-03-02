using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Services
{
    public interface ISettlementResourcesService
    {
        /// <summary>
        ///     Loads JSON from standard file path.
        /// </summary>
        /// <returns></returns>
        Task LoadDefinitionsAsync(CancellationToken cancellationToken = default);

        Task SaveStateAsync(CancellationToken cancellationToken = default);
        Task LoadStateAsync(CancellationToken cancellationToken = default);

        IReadOnlyDictionary<string, ResourceDefinition> GetResourceDefinitions();
        IReadOnlyDictionary<string, float> GetCurrentBalance();


        Task SetResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default);

        Task AddResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default);

        Task<bool> TrySpendResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default);

        bool CanSpend(string resourceId, float amount);
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        Task<bool> TryApplyResourceEffectAsync(ResourceEffect dailyEffects,
            CancellationToken cancellationToken = default);
    }
}