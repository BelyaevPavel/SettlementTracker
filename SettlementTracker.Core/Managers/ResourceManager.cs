using System;
using System.Collections.Generic;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Managers
{
    public class ResourceManager
    {
        private readonly SettlementState _settlement;
        private Dictionary<string, ResourceDefinition> _resourceDefinitions = new();

        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        public ResourceManager(SettlementState settlement)
        {
            _settlement = settlement;
        }

        public void InitializeResources(Dictionary<string, ResourceDefinition> resourceDefinitions)
        {
            _resourceDefinitions = resourceDefinitions;

            // Инициализируем ресурсы нулями, если их еще нет
            foreach (var resourceDef in resourceDefinitions.Values)
                if (!_settlement.Resources.ContainsKey(resourceDef.Id))
                    _settlement.Resources[resourceDef.Id] = 0;
        }

        public bool HasEnoughResources(List<ResourceEffect> requiredResources)
        {
            foreach (var requirement in requiredResources)
                if (!_settlement.Resources.ContainsKey(requirement.ResourceId) ||
                    _settlement.Resources[requirement.ResourceId] < Math.Abs(requirement.Amount))
                    return false;
            return true;
        }

        public bool TryConsumeResources(List<ResourceEffect> resourcesToConsume)
        {
            if (!HasEnoughResources(resourcesToConsume))
                return false;

            foreach (var resource in resourcesToConsume)
            {
                _settlement.Resources[resource.ResourceId] -= Math.Abs(resource.Amount);
                TrackDailyChange(resource.ResourceId, -Math.Abs(resource.Amount));
            }

            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_settlement.Resources));
            return true;
        }

        public void AddResources(List<ResourceEffect> resourcesToAdd)
        {
            foreach (var resource in resourcesToAdd)
            {
                if (!_settlement.Resources.ContainsKey(resource.ResourceId))
                    _settlement.Resources[resource.ResourceId] = 0;

                _settlement.Resources[resource.ResourceId] += Math.Abs(resource.Amount);
                TrackDailyChange(resource.ResourceId, Math.Abs(resource.Amount));
            }

            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_settlement.Resources));
        }

        public void AddResource(string resourceId, float amount)
        {
            if (!_settlement.Resources.ContainsKey(resourceId)) _settlement.Resources[resourceId] = 0;

            _settlement.Resources[resourceId] += amount;
            TrackDailyChange(resourceId, amount);

            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_settlement.Resources));
        }

        public float GetResourceAmount(string resourceId)
        {
            return _settlement.Resources.TryGetValue(resourceId, out var amount) ? amount : 0;
        }

        public Dictionary<string, float> GetAllResources()
        {
            return new Dictionary<string, float>(_settlement.Resources);
        }

        public Dictionary<string, float> GetDailyResourceChanges()
        {
            return new Dictionary<string, float>(_settlement.DailyResourceChanges);
        }

        public void ClearDailyChanges()
        {
            _settlement.DailyResourceChanges.Clear();
        }

        public Dictionary<string, (float Current, float DailyChange)> GetResourceForecast()
        {
            var forecast = new Dictionary<string, (float Current, float DailyChange)>();

            foreach (var kvp in _settlement.Resources)
            {
                var dailyChange = _settlement.DailyResourceChanges.TryGetValue(kvp.Key, out var change) ? change : 0;
                forecast[kvp.Key] = (kvp.Value, dailyChange);
            }

            return forecast;
        }

        public Dictionary<string, int> GetDaysUntilResourceDepletion()
        {
            var daysUntilDepletion = new Dictionary<string, int>();

            foreach (var kvp in _settlement.Resources)
            {
                var dailyChange = _settlement.DailyResourceChanges.TryGetValue(kvp.Key, out var change) ? change : 0;

                if (dailyChange >= 0)
                {
                    daysUntilDepletion[kvp.Key] = -1; // Ресурс не истощается
                }
                else
                {
                    var days = (int)Math.Floor(kvp.Value / Math.Abs(dailyChange));
                    daysUntilDepletion[kvp.Key] = Math.Max(0, days);
                }
            }

            return daysUntilDepletion;
        }

        private void TrackDailyChange(string resourceId, float amount)
        {
            if (!_settlement.DailyResourceChanges.ContainsKey(resourceId))
                _settlement.DailyResourceChanges[resourceId] = 0;

            _settlement.DailyResourceChanges[resourceId] += amount;
        }
    }
}