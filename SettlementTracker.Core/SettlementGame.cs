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
        private SettlementSimulator _simulator;
        private JsonDefinitionRepository _definitionRepository;
        private JsonGameStateRepository _gameStateRepository;

        public SettlementSimulator Simulator => _simulator;
        public SettlementState Settlement => _simulator.GetSettlementState();

        public event EventHandler<DayAdvancedEventArgs> DayAdvanced
        {
            add => _simulator.DayAdvanced += value;
            remove => _simulator.DayAdvanced -= value;
        }

        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged
        {
            add => _simulator.ResourcesChanged += value;
            remove => _simulator.ResourcesChanged -= value;
        }

        public event EventHandler<PopulationChangedEventArgs> PopulationChanged
        {
            add => _simulator.PopulationChanged += value;
            remove => _simulator.PopulationChanged -= value;
        }

        public SettlementGame(string dataPath, string savePath)
        {
            _definitionRepository = new JsonDefinitionRepository(dataPath);
            _gameStateRepository = new JsonGameStateRepository(savePath);
        }

        public void StartNewGame(string settlementName)
        {
            var settlement = _gameStateRepository.CreateNewGame(settlementName);
            InitializeSimulator(settlement);
        }

        public bool LoadGame(string fileName = "save.json")
        {
            var settlement = _gameStateRepository.LoadGameState(fileName);
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
            _simulator.AdvanceToNextDay();
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
            _simulator = new SettlementSimulator(settlement);

            // Загружаем определения
            var resources = _definitionRepository.LoadResourceDefinitions();
            var buildings = _definitionRepository.LoadBuildingDefinitions();
            var jobs = _definitionRepository.LoadJobDefinitions();
            var populationDefinition = _definitionRepository.LoadPopulationDefinition();

            // Инициализируем симулятор
            _simulator.Initialize(resources, buildings, jobs, populationDefinition);
        }

        public void Dispose()
        {
            // Очистка ресурсов, если необходимо
        }
    }
}