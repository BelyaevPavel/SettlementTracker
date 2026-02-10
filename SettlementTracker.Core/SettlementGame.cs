using System;
using System.Collections.Generic;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Repositories;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core
{
    public class SettlementGame : IDisposable
    {
        private readonly JsonDefinitionRepository _definitionRepository;
        private readonly JsonGameStateRepository _gameStateRepository;

        public SettlementGame(string dataPath, string savePath)
        {
            _definitionRepository = new JsonDefinitionRepository(dataPath);
            _gameStateRepository = new JsonGameStateRepository(savePath);
        }

        public SettlementSimulator Simulator { get; private set; }

        public SettlementState Settlement => Simulator.GetSettlementState();

        public void Dispose()
        {
            // Очистка ресурсов, если необходимо
        }

        public event EventHandler<DayAdvancedEventArgs> DayAdvanced
        {
            add => Simulator.DayAdvanced += value;
            remove => Simulator.DayAdvanced -= value;
        }

        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged
        {
            add => Simulator.ResourcesChanged += value;
            remove => Simulator.ResourcesChanged -= value;
        }

        public event EventHandler<PopulationChangedEventArgs> PopulationChanged
        {
            add => Simulator.PopulationChanged += value;
            remove => Simulator.PopulationChanged -= value;
        }

        public void StartNewGame(string settlementName)
        {
            SettlementState settlement = _gameStateRepository.CreateNewGame(settlementName);
            InitializeSimulator(settlement);
        }

        public bool LoadGame(string fileName = "save.json")
        {
            SettlementState settlement = _gameStateRepository.LoadGameState(fileName);
            if (settlement == null)
                return false;

            InitializeSimulator(settlement);
            return true;
        }

        public void SaveGame(string fileName = "save.json")
        {
            _gameStateRepository.SaveGameState(Settlement, fileName);
        }

        public void AdvanceDay()
        {
            Simulator.AdvanceToNextDay();
        }

        public Dictionary<string, ResourceDefinition> GetResourceDefinitions()
        {
            return _definitionRepository.LoadResourceDefinitions();
        }

        public Dictionary<string, BuildingDefinition> GetBuildingDefinitions()
        {
            return _definitionRepository.LoadBuildingDefinitions();
        }

        public Dictionary<string, JobDefinition> GetJobDefinitions()
        {
            return _definitionRepository.LoadJobDefinitions();
        }

        private void InitializeSimulator(SettlementState settlement)
        {
            Simulator = new SettlementSimulator(settlement);

            // Загружаем определения
            Dictionary<string, ResourceDefinition> resources = _definitionRepository.LoadResourceDefinitions();
            Dictionary<string, BuildingDefinition> buildings = _definitionRepository.LoadBuildingDefinitions();
            Dictionary<string, JobDefinition> jobs = _definitionRepository.LoadJobDefinitions();
            PopulationDefinition populationDefinition = _definitionRepository.LoadPopulationDefinition();

            // Инициализируем симулятор
            Simulator.Initialize(resources, buildings, jobs, populationDefinition);
        }
    }
}