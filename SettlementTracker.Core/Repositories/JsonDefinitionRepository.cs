using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SettlementTracker.Core.Models.Definitions;

namespace SettlementTracker.Core.Repositories
{
    public class JsonDefinitionRepository
    {
        private readonly string _basePath;

        public JsonDefinitionRepository(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
        }

        public Dictionary<string, ResourceDefinition> LoadResourceDefinitions()
        {
            var filePath = Path.Combine(_basePath, "resources.json");
            if (!File.Exists(filePath))
                // Создаем базовые ресурсы по умолчанию
                return CreateDefaultResourceDefinitions();

            var json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<ResourceDefinition>>(json);

            var result = new Dictionary<string, ResourceDefinition>();
            if (definitions != null)
                foreach (var def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public Dictionary<string, BuildingDefinition> LoadBuildingDefinitions()
        {
            var filePath = Path.Combine(_basePath, "buildings.json");
            if (!File.Exists(filePath))
                // Создаем базовые здания по умолчанию
                return CreateDefaultBuildingDefinitions();

            var json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);

            var result = new Dictionary<string, BuildingDefinition>();
            if (definitions != null)
                foreach (var def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public Dictionary<string, JobDefinition> LoadJobDefinitions()
        {
            var filePath = Path.Combine(_basePath, "jobs.json");
            if (!File.Exists(filePath))
                // Создаем базовые задания по умолчанию
                return CreateDefaultJobDefinitions();

            var json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<JobDefinition>>(json);

            var result = new Dictionary<string, JobDefinition>();
            if (definitions != null)
                foreach (var def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public PopulationDefinition LoadPopulationDefinition()
        {
            var filePath = Path.Combine(_basePath, "population.json");
            if (!File.Exists(filePath))
                // Создаем настройки по умолчанию
                return CreateDefaultPopulationDefinition();

            var json = File.ReadAllText(filePath);
            var definition = JsonSerializer.Deserialize<PopulationDefinition>(json);

            return definition ?? CreateDefaultPopulationDefinition();
        }

        public void SaveResourceDefinitions(Dictionary<string, ResourceDefinition> definitions)
        {
            var filePath = Path.Combine(_basePath, "resources.json");
            var json = JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void SaveBuildingDefinitions(Dictionary<string, BuildingDefinition> definitions)
        {
            var filePath = Path.Combine(_basePath, "buildings.json");
            var json = JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void SaveJobDefinitions(Dictionary<string, JobDefinition> definitions)
        {
            var filePath = Path.Combine(_basePath, "jobs.json");
            var json = JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void SavePopulationDefinition(PopulationDefinition definition)
        {
            var filePath = Path.Combine(_basePath, "population.json");
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private Dictionary<string, ResourceDefinition> CreateDefaultResourceDefinitions()
        {
            var resources = new Dictionary<string, ResourceDefinition>
            {
                ["food"] = new()
                {
                    Id = "food",
                    Name = "Еда",
                    Description = "Основной пищевой ресурс",
                    IconPath = "Icons/food.png",
                    IsConsumable = true
                },
                ["wood"] = new()
                {
                    Id = "wood",
                    Name = "Древесина",
                    Description = "Строительный материал",
                    IconPath = "Icons/wood.png",
                    IsConsumable = false
                },
                ["stone"] = new()
                {
                    Id = "stone",
                    Name = "Камень",
                    Description = "Строительный материал",
                    IconPath = "Icons/stone.png",
                    IsConsumable = false
                },
                ["water"] = new()
                {
                    Id = "water",
                    Name = "Вода",
                    Description = "Питьевая вода",
                    IconPath = "Icons/water.png",
                    IsConsumable = true
                }
            };

            SaveResourceDefinitions(resources);
            return resources;
        }

        private Dictionary<string, BuildingDefinition> CreateDefaultBuildingDefinitions()
        {
            var buildings = new Dictionary<string, BuildingDefinition>
            {
                ["house"] = new()
                {
                    Id = "house",
                    Name = "Дом",
                    Description = "Жилое здание для 5 человек",
                    Size = (2, 2),
                    BuildCost = new List<ResourceEffect>
                    {
                        new() { ResourceId = "wood", Amount = 20, IsProduction = false }
                    },
                    DailyEffects = new List<ResourceEffect>(),
                    WorkerRequirements = new List<WorkerRequirement>(),
                    ProvidesHousing = 5
                },
                ["lumberjack"] = new()
                {
                    Id = "lumberjack",
                    Name = "Лесопилка",
                    Description = "Производит древесину",
                    Size = (3, 2),
                    BuildCost = new List<ResourceEffect>
                    {
                        new() { ResourceId = "wood", Amount = 10, IsProduction = false },
                        new() { ResourceId = "stone", Amount = 5, IsProduction = false }
                    },
                    DailyEffects = new List<ResourceEffect>
                    {
                        new() { ResourceId = "wood", Amount = 5, IsProduction = true }
                    },
                    WorkerRequirements = new List<WorkerRequirement>
                    {
                        new()
                        {
                            AgeCategory = Models.Enums.AgeCategory.Adult,
                            Gender = Models.Enums.Gender.Male,
                            Count = 2,
                            CanBeSlave = true,
                            RequiresSupervision = false
                        }
                    },
                    MaxWorkers = 3
                },
                ["farm"] = new()
                {
                    Id = "farm",
                    Name = "Ферма",
                    Description = "Производит еду",
                    Size = (4, 4),
                    BuildCost = new List<ResourceEffect>
                    {
                        new() { ResourceId = "wood", Amount = 15, IsProduction = false }
                    },
                    DailyEffects = new List<ResourceEffect>
                    {
                        new() { ResourceId = "food", Amount = 10, IsProduction = true }
                    },
                    WorkerRequirements = new List<WorkerRequirement>
                    {
                        new()
                        {
                            AgeCategory = Models.Enums.AgeCategory.Adult,
                            Gender = Models.Enums.Gender.Male,
                            Count = 1,
                            CanBeSlave = true,
                            RequiresSupervision = true
                        },
                        new()
                        {
                            AgeCategory = Models.Enums.AgeCategory.Adult,
                            Gender = Models.Enums.Gender.Female,
                            Count = 1,
                            CanBeSlave = true,
                            RequiresSupervision = false
                        }
                    },
                    MaxWorkers = 4
                }
            };

            SaveBuildingDefinitions(buildings);
            return buildings;
        }

        private Dictionary<string, JobDefinition> CreateDefaultJobDefinitions()
        {
            var jobs = new Dictionary<string, JobDefinition>
            {
                ["hunting"] = new()
                {
                    Id = "hunting",
                    Name = "Охота",
                    Description = "Добыча пищи охотой",
                    DailyEffects = new List<ResourceEffect>
                    {
                        new() { ResourceId = "food", Amount = 3, IsProduction = true }
                    },
                    WorkerRequirements = new List<WorkerRequirement>
                    {
                        new()
                        {
                            AgeCategory = Models.Enums.AgeCategory.Adult,
                            Gender = Models.Enums.Gender.Male,
                            Count = 1,
                            CanBeSlave = false,
                            RequiresSupervision = false
                        }
                    },
                    MaxWorkers = 2,
                    BaseEfficiency = new Dictionary<string, float>
                    {
                        ["Adult_Male_False"] = 1.0f,
                        ["Adult_Female_False"] = 0.7f,
                        ["Adult_Male_True"] = 0.8f,
                        ["Elder_Male_False"] = 0.6f
                    }
                },
                ["water_carrying"] = new()
                {
                    Id = "water_carrying",
                    Name = "Доставка воды",
                    Description = "Принесение воды из источника",
                    DailyEffects = new List<ResourceEffect>
                    {
                        new() { ResourceId = "water", Amount = 5, IsProduction = true }
                    },
                    WorkerRequirements = new List<WorkerRequirement>
                    {
                        new()
                        {
                            AgeCategory = Models.Enums.AgeCategory.Adult,
                            Gender = Models.Enums.Gender.Female,
                            Count = 1,
                            CanBeSlave = true,
                            RequiresSupervision = true
                        }
                    },
                    MaxWorkers = 3,
                    BaseEfficiency = new Dictionary<string, float>
                    {
                        ["Adult_Female_False"] = 1.0f,
                        ["Adult_Male_False"] = 0.9f,
                        ["Adult_Female_True"] = 0.7f,
                        ["Child_Female_False"] = 0.4f
                    }
                }
            };

            SaveJobDefinitions(jobs);
            return jobs;
        }

        private PopulationDefinition CreateDefaultPopulationDefinition()
        {
            var populationDef = new PopulationDefinition
            {
                Id = "default",
                Name = "Стандартное население",
                Description = "Стандартные настройки для людей",
                ChildMaxAge = 14,
                AdultMaxAge = 59,
                ElderMaxAge = 80,
                AgePerDay = 1.0f / 365,
                DailyNeeds = new List<ResourceEffect>
                {
                    new() { ResourceId = "food", Amount = 1.0f, IsProduction = false },
                    new() { ResourceId = "water", Amount = 0.5f, IsProduction = false }
                },
                StarvationDeathChance = 0.1f,
                SlaveEfficiencyPenalty = 0.5f,
                ChildEfficiencyPenalty = 0.5f,
                ElderEfficiencyPenalty = 0.7f
            };

            SavePopulationDefinition(populationDef);
            return populationDef;
        }
    }
}