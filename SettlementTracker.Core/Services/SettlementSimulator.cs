using System;
using System.Collections.Generic;
using System.Linq;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using ResourceManager = SettlementTracker.Core.Managers.ResourceManager;

namespace SettlementTracker.Core.Services
{
    public class SettlementSimulator
    {
        private readonly BuildingManager _buildingManager;
        private readonly EfficiencyCalculator _efficiencyCalculator;
        private readonly JobManager _jobManager;
        private readonly PopulationManager _populationManager;
        private readonly ResourceManager _resourceManager;
        private readonly SettlementState _settlement;

        public SettlementSimulator(SettlementState settlement)
        {
            _settlement = settlement;
            _resourceManager = new ResourceManager(settlement);
            _populationManager = new PopulationManager(settlement);
            _buildingManager = new BuildingManager(settlement, _resourceManager);
            _jobManager = new JobManager(settlement);
            _efficiencyCalculator = new EfficiencyCalculator(settlement);

            // Подписываемся на события
            _resourceManager.ResourcesChanged += (sender, e) => ResourcesChanged?.Invoke(sender, e);
        }

        public event EventHandler<DayAdvancedEventArgs>? DayAdvanced;
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;
        public event EventHandler<PopulationChangedEventArgs>? PopulationChanged;

        public void Initialize(
            Dictionary<string, ResourceDefinition> resourceDefinitions,
            Dictionary<string, BuildingDefinition> buildingDefinitions,
            Dictionary<string, JobDefinition> jobDefinitions,
            PopulationDefinition populationDefinition)
        {
            _resourceManager.InitializeResources(resourceDefinitions);
            _buildingManager.InitializeDefinitions(buildingDefinitions);
            _jobManager.InitializeDefinitions(jobDefinitions);
            _populationManager.InitializeDefinitions(populationDefinition);
        }

        public void AdvanceToNextDay()
        {
            // Очищаем дневные изменения ресурсов
            _resourceManager.ClearDailyChanges();

            // 1. Старение населения
            _populationManager.AgePopulation();

            // 2. Потребление ресурсов населением (базовые нужды)
            ProcessBasicNeeds();

            // 3. Обработка зданий
            ProcessBuildings();

            // 4. Обработка заданий
            ProcessJobs();

            // 5. Увеличение счетчика дней
            _settlement.CurrentDay++;

            // 6. Генерация событий
            DailySummary dailySummary = GenerateDailySummary();

            // 7. Вызываем событие
            DayAdvanced?.Invoke(this, new DayAdvancedEventArgs(
                _settlement.CurrentDay - 1, // Прошедший день
                dailySummary
            ));

            PopulationChanged?.Invoke(this, new PopulationChangedEventArgs(
                _populationManager.GetPopulationStatistics()
            ));
        }

        private void ProcessBasicNeeds()
        {
            List<ResourceEffect> needs = _populationManager.CalculateDailyNeeds();

            foreach (ResourceEffect need in needs)
            {
                float resourceAvailable = _resourceManager.GetResourceAmount(need.ResourceId);
                if (_resourceManager.HasEnoughResources(new List<ResourceEffect> { need }))
                    _resourceManager.AddResource(need.ResourceId, -need.Amount);
                else
                    _resourceManager.AddResource(need.ResourceId, -resourceAvailable);
                // TODO: Логика смертности от голода
            }
        }

        private void ProcessBuildings()
        {
            foreach (Building building in _settlement.Buildings.Where(b => b.IsActive))
            {
                if (building.Definition == null) continue;

                // Проверяем выполнены ли требования к работникам
                if (!_buildingManager.AreWorkerRequirementsMet(building))
                    continue;

                // Рассчитываем эффективность работы группы
                float totalEfficiency = _efficiencyCalculator.CalculateTotalEfficiency(
                    building.AssignedCitizenIds.ToList()
                );

                // Применяем дневные эффекты с учетом эффективности
                var accumulatedResourceEffects = new List<ResourceEffect>();
                foreach (ResourceEffect effect in building.Definition.DailyEffects)
                {
                    float adjustedAmount = effect.Amount * totalEfficiency;

                    var resourceEffect = new ResourceEffect
                    {
                        ResourceId = effect.ResourceId,
                        Amount = adjustedAmount,
                        IsProduction = effect.IsProduction
                    };

                    if (effect.IsProduction)
                    {
                        accumulatedResourceEffects.Add(resourceEffect);
                    }
                    else
                    {
                        // Потребление - проверяем достаточно ли ресурсов
                        if (!_resourceManager.HasEnoughResources(new List<ResourceEffect> { resourceEffect }))
                            // Недостаточно ресурсов - здание не работает
                            accumulatedResourceEffects.Clear();
                        break;
                    }
                }

                foreach (ResourceEffect resourceEffect in accumulatedResourceEffects)
                    if (resourceEffect.IsProduction)
                        _resourceManager.AddResources(new List<ResourceEffect> { resourceEffect });
                    else
                        _resourceManager.TryConsumeResources(new List<ResourceEffect> { resourceEffect });
            }
        }

        private void ProcessJobs()
        {
            foreach (ActiveJob job in _settlement.ActiveJobs.ToList())
            {
                if (job.Definition == null) continue;

                // Проверяем выполнены ли требования к работникам
                if (!_jobManager.AreWorkerRequirementsMet(job))
                    continue;

                // Рассчитываем эффективность работы группы
                Dictionary<Guid, float> efficiencies = _efficiencyCalculator.CalculateWorkGroupEfficiencies(
                    job.AssignedCitizenIds.ToList(),
                    job.Definition
                );

                float totalEfficiency = efficiencies.Values.Sum();

                // Применяем дневные эффекты с учетом эффективности
                foreach (ResourceEffect effect in job.Definition.DailyEffects)
                {
                    float adjustedAmount = effect.Amount * totalEfficiency;

                    var resourceEffect = new ResourceEffect
                    {
                        ResourceId = effect.ResourceId,
                        Amount = adjustedAmount,
                        IsProduction = effect.IsProduction
                    };

                    if (effect.IsProduction)
                    {
                        _resourceManager.AddResources(new List<ResourceEffect> { resourceEffect });
                    }
                    else
                    {
                        // Потребление - проверяем достаточно ли ресурсов
                        if (_resourceManager.HasEnoughResources(new List<ResourceEffect> { resourceEffect }))
                            _resourceManager.TryConsumeResources(new List<ResourceEffect> { resourceEffect });
                        else
                            // Недостаточно ресурсов - задание не выполняется
                            break;
                    }
                }
            }
        }

        private DailySummary GenerateDailySummary()
        {
            return new DailySummary
            {
                Day = _settlement.CurrentDay,
                ResourceChanges = _resourceManager.GetDailyResourceChanges(),
                PopulationStatistics = _populationManager.GetPopulationStatistics(),
                ActiveBuildings = _buildingManager.GetActiveBuildings().Count,
                ActiveJobs = _jobManager.GetActiveJobs().Count
            };
        }

        // Публичные методы для доступа к менеджерам
        public ResourceManager GetResourceManager()
        {
            return _resourceManager;
        }

        public PopulationManager GetPopulationManager()
        {
            return _populationManager;
        }

        public BuildingManager GetBuildingManager()
        {
            return _buildingManager;
        }

        public JobManager GetJobManager()
        {
            return _jobManager;
        }

        public EfficiencyCalculator GetEfficiencyCalculator()
        {
            return _efficiencyCalculator;
        }

        public SettlementState GetSettlementState()
        {
            return _settlement;
        }
    }
}