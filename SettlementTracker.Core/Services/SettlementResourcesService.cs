using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Repositories;
using SettlementTracker.WebInterface.Data.Services;

namespace SettlementTracker.Core.Services
{
    public class SettlementResourcesService : ISettlementResourcesService
    {
        private readonly IResourceDefinitionRepository _definitionRepository;
        private readonly object _lock = new();
        private readonly string _stateFilePath;
        private Dictionary<string, ResourceDefinition> _definitions;
        private Dictionary<string, float> _resources;
        private readonly string _directoryName;

        public SettlementResourcesService(IResourceDefinitionRepository definitionRepository,
            string stateFilePath = "State\\resources.json")
        {
            _definitionRepository = definitionRepository;
            _stateFilePath = stateFilePath;
            _directoryName = Path.GetDirectoryName(Path.GetFullPath(_stateFilePath));
            _resources = new Dictionary<string, float>();
            _definitions = new Dictionary<string, ResourceDefinition>();
            LoadDefinitionsAsync().Wait();
        }

        public async Task LoadDefinitionsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _definitions = await _definitionRepository.LoadResourceDefinitionsAsync(cancellationToken);


            lock (_lock)
            {
                _resources = new Dictionary<string, float>();
                foreach (ResourceDefinition resourceDef in _definitions.Values)
                    _resources[resourceDef.Id] = 0;
            }
        }

        public IReadOnlyDictionary<string, ResourceDefinition> GetResourceDefinitions()
        {
            lock (_lock)
            {
                return _definitions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public IReadOnlyDictionary<string, float> GetCurrentBalance()
        {
            lock (_lock)
            {
                return _resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public async Task AddResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            lock (_lock)
            {
                if (!_resources.ContainsKey(resourceId))
                    throw new ArgumentException(null, nameof(resourceId));

                _resources[resourceId] += amount;
            }

            await OnResourcesChanged(cancellationToken);
        }

        public async Task<bool> TrySpendResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_lock)
            {
                if (amount >= 0 && _resources.TryGetValue(resourceId, out float currentAmount) &&
                    currentAmount >= amount)
                {
                    _resources[resourceId] -= amount;
                }
                else
                {
                    return false;
                }
            }

            await OnResourcesChanged(cancellationToken);

            return true;
        }

        public bool CanSpend(string resourceId, float amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            lock (_lock)
            {
                if (!_resources.TryGetValue(resourceId, out float resource))
                    throw new ArgumentException(null, nameof(resourceId));

                return resource >= amount;
            }
        }

        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        public Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task SaveStateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json;

            lock (_lock)
            {
                json = JsonSerializer.Serialize(_resources, options);
            }

            Directory.CreateDirectory(_directoryName!);

            await File.WriteAllTextAsync(_stateFilePath, json, cancellationToken);
        }

        public async Task LoadStateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (File.Exists(_stateFilePath))
            {
                string json = await File.ReadAllTextAsync(_stateFilePath, cancellationToken);

                lock (_lock)
                {
                    var deserializeResources = JsonSerializer.Deserialize<Dictionary<string, float>>(json);
                    if (deserializeResources != null)
                    {
                        foreach (KeyValuePair<string, float> deserializeResource in deserializeResources)
                        {
                            if (_definitions.ContainsKey(deserializeResource.Key))
                            {
                                if (!_resources.TryAdd(deserializeResource.Key, 0))
                                {
                                    _resources[deserializeResource.Key] = deserializeResource.Value;
                                }
                            }
                            else
                            {
                                throw new ArgumentException("Unknown resource Id is loaded");
                            }
                        }
                    }
                    else
                    {
                        _resources = new Dictionary<string, float>();
                        foreach (KeyValuePair<string, ResourceDefinition> deserializeResource in _definitions)
                        {
                            _resources[deserializeResource.Key] = 0;
                        }
                    }
                }
            }
        }


        public async Task SetResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            lock (_lock)
            {
                if (!_resources.ContainsKey(resourceId))
                    throw new ArgumentException(null, nameof(resourceId));

                _resources[resourceId] = amount;
            }

            await OnResourcesChanged(cancellationToken);
        }

        private async Task OnResourcesChanged(CancellationToken cancellationToken = default(CancellationToken))
        {
            await SaveStateAsync(cancellationToken);
            lock (_lock)
            {
                ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_resources.ToDictionary()));
            }
        }
    }
}