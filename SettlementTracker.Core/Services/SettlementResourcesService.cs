using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Repositories;

namespace SettlementTracker.Core.Services
{
    public class SettlementResourcesService : ISettlementResourcesService
    {
        private readonly IResourceDefinitionRepository _definitionRepository;
        private readonly string _directoryName;
        private readonly object _lock = new();
        private readonly string _stateFilePath;
        private Dictionary<string, ResourceDefinition> _definitions;
        private Dictionary<string, float> _resources;

        public SettlementResourcesService(IResourceDefinitionRepository definitionRepository,
            string stateFilePath = "State\\resources.json")
        {
            _definitionRepository = definitionRepository;
            _stateFilePath = stateFilePath;
            _directoryName = Path.GetDirectoryName(Path.GetFullPath(_stateFilePath));
            _resources = new Dictionary<string, float>();
            _definitions = new Dictionary<string, ResourceDefinition>();
            LoadDefinitions();
        }

        public ILogger<SettlementResourcesService>? Logger { get; set; }

        public async Task LoadDefinitionsAsync(CancellationToken cancellationToken = default)
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
            CancellationToken cancellationToken = default)
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
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (amount >= 0 && _resources.TryGetValue(resourceId, out float currentAmount) &&
                    currentAmount >= amount)
                    _resources[resourceId] -= amount;
                else
                    return false;
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

        /// <summary>
        ///     Пытается применить эффект ресурса (производство или потребление) к текущему балансу.
        /// </summary>
        /// <param name="resourceEffect">
        ///     Эффект ресурса, содержащий идентификатор ресурса, величину и признак
        ///     производства/потребления.
        /// </param>
        /// <param name="cancellationToken">Токен отмены для асинхронной операции сохранения.</param>
        /// <returns>
        ///     true, если эффект успешно применён; false, если ресурс не найден (отсутствует определение или запись о балансе)
        ///     либо для нерасходуемого ресурса (IsConsumable = false) недостаточно средств.
        /// </returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="resourceEffect" /> равен null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Выбрасывается, если <paramref name="resourceEffect.Amount" /> неположителен.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     Выбрасывается, если операция отменена через
        ///     <paramref name="cancellationToken" /> во время сохранения состояния.
        /// </exception>
        /// <remarks>
        ///     Метод учитывает признак <see cref="ResourceEffect.IsProduction" />:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>Если <c>true</c>, величина добавляется к балансу (производство).</description>
        ///         </item>
        ///         <item>
        ///             <description>Если <c>false</c>, величина вычитается из баланса (потребление).</description>
        ///         </item>
        ///     </list>
        ///     Для ресурсов с флагом <see cref="ResourceDefinition.IsConsumable" />, равным <c>false</c>, проверяется,
        ///     что итоговый баланс не станет отрицательным.
        ///     <para>
        ///         При успешном изменении вызывается событие <see cref="ResourcesChanged" /> и состояние асинхронно сохраняется в
        ///         файл с помощью <see cref="SaveStateAsync" />.
        ///     </para>
        ///     <para>
        ///         Все операции с внутренним словарём ресурсов выполняются под блокировкой для обеспечения потокобезопасности.
        ///     </para>
        /// </remarks>
        public async Task<bool> TryApplyResourceEffectAsync(ResourceEffect resourceEffect,
            CancellationToken cancellationToken = default)
        {
            if (resourceEffect == null) throw new ArgumentNullException(nameof(resourceEffect));
            if (resourceEffect.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(resourceEffect));
            if (!_definitions.ContainsKey(resourceEffect.ResourceId))
            {
                Logger?.LogWarning("Resource definition for resourceId {resourceId} not found",
                    resourceEffect.ResourceId);
                return false;
            }


            float amount = resourceEffect.IsProduction ? resourceEffect.Amount : -resourceEffect.Amount;
            string resourceId = resourceEffect.ResourceId;

            lock (_lock)
            {
                if (_resources.TryGetValue(resourceId, out float currentAmount))
                {
                    if (currentAmount + amount >= 0)
                        _resources[resourceId] += amount;
                    else
                        return false;
                }
                else
                {
                    Logger?.LogWarning("Resource for resourceId {resourceId} not found", resourceEffect.ResourceId);
                    return false;
                }
            }

            await OnResourcesChanged(cancellationToken);
            return true;
        }

        public async Task SaveStateAsync(CancellationToken cancellationToken = default)
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

        public async Task LoadStateAsync(CancellationToken cancellationToken = default)
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
                            if (_definitions.ContainsKey(deserializeResource.Key))
                            {
                                if (!_resources.TryAdd(deserializeResource.Key, 0))
                                    _resources[deserializeResource.Key] = deserializeResource.Value;
                            }
                            else
                            {
                                throw new ArgumentException("Unknown resource Id is loaded");
                            }
                    }
                    else
                    {
                        _resources = new Dictionary<string, float>();
                        foreach (KeyValuePair<string, ResourceDefinition> deserializeResource in _definitions)
                            _resources[deserializeResource.Key] = 0;
                    }
                }
            }
        }


        public async Task SetResourceAsync(string resourceId, float amount,
            CancellationToken cancellationToken = default)
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

        private void LoadDefinitions()
        {
            _definitions = _definitionRepository.LoadResourceDefinitions();

            lock (_lock)
            {
                _resources = new Dictionary<string, float>();
                foreach (ResourceDefinition resourceDef in _definitions.Values)
                    _resources[resourceDef.Id] = 0;
            }
        }

        private async Task OnResourcesChanged(CancellationToken cancellationToken = default)
        {
            await SaveStateAsync(cancellationToken);
            lock (_lock)
            {
                ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_resources.ToDictionary()));
            }
        }
    }
}