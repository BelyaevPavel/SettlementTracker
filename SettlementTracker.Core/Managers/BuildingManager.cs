using System;
using System.Collections.Generic;
using System.Linq;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Managers
{
    public class BuildingManager
    {
        private readonly ResourceManager _resourceManager;
        private readonly SettlementState _settlement;
        private Dictionary<string, BuildingDefinition> _buildingDefinitions = new();

        public BuildingManager(SettlementState settlement, ResourceManager resourceManager)
        {
            _settlement = settlement;
            _resourceManager = resourceManager;
        }

        public void InitializeDefinitions(Dictionary<string, BuildingDefinition> buildingDefinitions)
        {
            _buildingDefinitions = buildingDefinitions;

            // Связываем определения с существующими зданиями
            foreach (Building building in _settlement.Buildings)
                if (_buildingDefinitions.TryGetValue(building.DefinitionId, out BuildingDefinition definition))
                    building.Definition = definition;
        }

        public bool CanBuildBuilding(string definitionId, (int X, int Y) position)
        {
            if (!_buildingDefinitions.TryGetValue(definitionId, out BuildingDefinition definition))
                return false;

            // Проверяем достаточно ли ресурсов
            if (!_resourceManager.HasEnoughResources(definition.BuildCost))
                return false;

            // Проверяем нет ли коллизий (упрощенно)
            // В реальной реализации нужно учитывать размер здания
            if (IsPositionOccupied(position, definition.Size))
                return false;

            return true;
        }

        public Building? BuildBuilding(string definitionId, (int X, int Y) position, string customName = "")
        {
            if (!CanBuildBuilding(definitionId, position))
                return null;

            if (!_buildingDefinitions.TryGetValue(definitionId, out BuildingDefinition definition))
                return null;

            // Потребляем ресурсы
            if (!_resourceManager.TryConsumeResources(definition.BuildCost))
                return null;

            // Создаем здание
            var building = new Building(Guid.NewGuid(), definitionId, position)
            {
                Name = string.IsNullOrEmpty(customName) ? definition.Name : customName,
                Definition = definition
            };

            _settlement.Buildings.Add(building);
            return building;
        }

        public void DemolishBuilding(Guid buildingId)
        {
            Building building = _settlement.Buildings.FirstOrDefault(b => b.Id == buildingId);
            if (building == null) return;

            // Освобождаем работников
            foreach (Guid citizenId in building.AssignedCitizenIds.ToList())
            {
                var populationManager = new PopulationManager(_settlement);
                populationManager.UnassignCitizen(citizenId);
            }

            // Удаляем здание
            _settlement.Buildings.Remove(building);

            // TODO: Возвращаем часть ресурсов при сносе (опционально)
        }

        public List<Building> GetBuildingsByType(string definitionId)
        {
            return _settlement.Buildings
                .Where(b => b.DefinitionId == definitionId)
                .ToList();
        }

        public List<Building> GetActiveBuildings()
        {
            return _settlement.Buildings
                .Where(b => b.IsActive)
                .ToList();
        }

        public void ToggleBuildingActivity(Guid buildingId, bool isActive)
        {
            Building building = _settlement.Buildings.FirstOrDefault(b => b.Id == buildingId);
            if (building != null) building.IsActive = isActive;
        }

        public bool AreWorkerRequirementsMet(Building building)
        {
            if (building.Definition == null) return false;

            List<Citizen> assignedCitizens = building.AssignedCitizenIds
                .Select(id => _settlement.Citizens.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .ToList();

            foreach (WorkerRequirement requirement in building.Definition.WorkerRequirements)
            {
                int matchingWorkers = assignedCitizens.Count(c =>
                    c!.AgeCategory == requirement.AgeCategory &&
                    (c.Gender == requirement.Gender || requirement.Gender == Gender.Any) &&
                    (requirement.CanBeSlave || !c.IsSlave));

                if (matchingWorkers < requirement.Count)
                    return false;
            }

            return true;
        }

        private bool IsPositionOccupied((int X, int Y) position, (int Width, int Height) size)
        {
            foreach (Building building in _settlement.Buildings)
                // Простая проверка коллизий (можно улучшить)
                if (Math.Abs(building.Position.X - position.X) < size.Width &&
                    Math.Abs(building.Position.Y - position.Y) < size.Height)
                    return true;
            return false;
        }
    }
}