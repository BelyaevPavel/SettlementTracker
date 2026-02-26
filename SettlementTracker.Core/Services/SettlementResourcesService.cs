using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        public SettlementResourcesService(IResourceDefinitionRepository definitionRepository,
            string stateFilePath = "State\\resources.json")
        {
            _definitionRepository = definitionRepository;
            _stateFilePath = stateFilePath;
            _resources = new Dictionary<string, float>();
            _definitions = new Dictionary<string, ResourceDefinition>();
            LoadDefinitionsAsync();
        }

        public async Task LoadDefinitionsAsync()
        {
            _definitions = await _definitionRepository.LoadResourceDefinitionsAsync();


            lock (_lock)
            {
                _resources = new Dictionary<string, float>();
                foreach (ResourceDefinition resourceDef in _definitions.Values)
                    _resources[resourceDef.Id] = 0;
            }
        }

        public IReadOnlyList<ResourceDefinition> GetResourceDefinitions()
        {
            return _definitions.Values.ToList();
        }

        public IReadOnlyDictionary<string, float> GetCurrentBalance()
        {
            return _resources;
        }

        public async Task AddResourceAsync(string resourceId, float amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            lock (_lock)
            {
                if (!_resources.ContainsKey(resourceId))
                    throw new ArgumentException(null, nameof(resourceId));

                _resources[resourceId] += amount;
            }

            await OnResourcesChanged();
        }

        public async Task<bool> TrySpendResourceAsync(string resourceId, float amount)
        {
            if (!CanSpend(resourceId, amount))
                return false;

            lock (_lock)
            {
                _resources[resourceId] -= amount;
            }

            await OnResourcesChanged();

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

        public Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects)
        {
            throw new NotImplementedException();
        }

        private async Task OnResourcesChanged()
        {
            await SaveChangesAsync();
            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_resources));
        }

        public async Task SaveChangesAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json;

            lock (_lock)
            {
                json = JsonSerializer.Serialize(_resources, options);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_stateFilePath))!);
            await File.WriteAllTextAsync(_stateFilePath, json);
        }

        public async Task LoadFromFileAsync()
        {
            if (File.Exists(_stateFilePath))
            {
                string json = await File.ReadAllTextAsync(_stateFilePath);

                lock (_lock)
                {
                    _resources = JsonSerializer.Deserialize<Dictionary<string, float>>(json) ??
                                 new Dictionary<string, float>();
                }
            }
        }

        public async Task SetResourceAsync(string resourceId, float amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            lock (_lock)
            {
                if (!_resources.ContainsKey(resourceId))
                    throw new ArgumentException(null, nameof(resourceId));

                _resources[resourceId] = amount;
            }

            await OnResourcesChanged();
        }
    }
}