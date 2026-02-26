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
        private Dictionary<string, ResourceDefinition> _definitions;
        private Dictionary<string, float> _resources;
        private readonly string _stateFilePath;

        public SettlementResourcesService(IResourceDefinitionRepository definitionRepository,
            string stateFilePath = "State\\resources.json")
        {
            _definitionRepository = definitionRepository;
            _stateFilePath = stateFilePath;
            _resources = new Dictionary<string, float>();
            _definitions = new Dictionary<string, ResourceDefinition>();
            LoadDefinitionsAsync();
        }

        private async Task OnResourcesChanged()
        {
            await SaveChangesAsync();
            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_resources));
        }

        public async Task LoadDefinitionsAsync()
        {
            _definitions = await _definitionRepository.LoadResourceDefinitionsAsync();

            foreach (var resourceDef in _definitions.Values)
                if (!_resources.ContainsKey(resourceDef.Id))
                    _resources[resourceDef.Id] = 0;
        }

        public async Task SaveChangesAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_resources, options);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_stateFilePath))!);
            await File.WriteAllTextAsync(_stateFilePath, json);
        }

        public async Task LoadFromFileAsync()
        {
            if (File.Exists(_stateFilePath))
            {
                var json = await File.ReadAllTextAsync(_stateFilePath);
                _resources = JsonSerializer.Deserialize<Dictionary<string, float>>(json) ??
                             new Dictionary<string, float>();
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
            if (_resources.ContainsKey(resourceId))
            {
                _resources[resourceId] += amount;
                await OnResourcesChanged();
            }
            else
            {
                throw new ArgumentException(null, nameof(resourceId));
            }
        }

        public async Task<bool> TrySpendResourceAsync(string resourceId, float amount)
        {
            if (!CanSpend(resourceId, amount))
                return false;

            _resources[resourceId] -= amount;

            await OnResourcesChanged();

            return true;
        }

        public bool CanSpend(string resourceId, float amount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                return resource >= amount;
            }
            else
            {
                throw new ArgumentException(null, nameof(resourceId));
            }
        }

        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        public Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects)
        {
            throw new NotImplementedException();
        }
    }
}