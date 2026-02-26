using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Repositories
{
    public class JsonDefinitionRepository : IResourceDefinitionRepository, IBuildingDefinitionRepository,
        IPopulationDefinitionRepository, IJobDefinitionRepository
    {
        private readonly string _basePath;

        public JsonDefinitionRepository(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
        }

        #region Resources

        public Dictionary<string, ResourceDefinition> LoadResourceDefinitions()
        {
            string filePath = Path.Combine(_basePath, "resources.json");
            if (!File.Exists(filePath))
                // Создаем базовые ресурсы по умолчанию
                return CreateDefaultResourceDefinitions();

            string json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<ResourceDefinition>>(json);

            var result = new Dictionary<string, ResourceDefinition>();
            if (definitions != null)
                foreach (ResourceDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public async Task<Dictionary<string, ResourceDefinition>> LoadResourceDefinitionsAsync()
        {
            string filePath = Path.Combine(_basePath, "resources.json");
            if (!File.Exists(filePath))
                // Создаем базовые ресурсы по умолчанию
                return CreateDefaultResourceDefinitions();

            string json = await File.ReadAllTextAsync(filePath);
            var definitions = JsonSerializer.Deserialize<List<ResourceDefinition>>(json);

            var result = new Dictionary<string, ResourceDefinition>();
            if (definitions != null)
                foreach (ResourceDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public void SaveResourceDefinitions(Dictionary<string, ResourceDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "resources.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public async Task SaveResourceDefinitionsAsync(Dictionary<string, ResourceDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "resources.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
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

        #endregion

        #region Buildings

        public Dictionary<string, BuildingDefinition> LoadBuildingDefinitions()
        {
            string filePath = Path.Combine(_basePath, "buildings.json");
            if (!File.Exists(filePath))
                // Создаем базовые здания по умолчанию
                return CreateDefaultBuildingDefinitions();

            string json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);

            var result = new Dictionary<string, BuildingDefinition>();
            if (definitions != null)
                foreach (BuildingDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public async Task<Dictionary<string, BuildingDefinition>> LoadBuildingDefinitionsAsync()
        {
            string filePath = Path.Combine(_basePath, "buildings.json");
            if (!File.Exists(filePath))
                // Создаем базовые здания по умолчанию
                return CreateDefaultBuildingDefinitions();

            string json = await File.ReadAllTextAsync(filePath);
            var definitions = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);

            var result = new Dictionary<string, BuildingDefinition>();
            if (definitions != null)
                foreach (BuildingDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public void SaveBuildingDefinitions(Dictionary<string, BuildingDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "buildings.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public async Task SaveBuildingDefinitionsAsync(Dictionary<string, BuildingDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "buildings.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
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
                            AgeCategory = AgeCategory.Adult,
                            Gender = Gender.Male,
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
                            AgeCategory = AgeCategory.Adult,
                            Gender = Gender.Male,
                            Count = 1,
                            CanBeSlave = true,
                            RequiresSupervision = true
                        },
                        new()
                        {
                            AgeCategory = AgeCategory.Adult,
                            Gender = Gender.Female,
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

        #endregion

        #region Jobs

        public Dictionary<string, JobDefinition> LoadJobDefinitions()
        {
            string filePath = Path.Combine(_basePath, "jobs.json");
            if (!File.Exists(filePath))
                // Создаем базовые задания по умолчанию
                return CreateDefaultJobDefinitions();

            string json = File.ReadAllText(filePath);
            var definitions = JsonSerializer.Deserialize<List<JobDefinition>>(json);

            var result = new Dictionary<string, JobDefinition>();
            if (definitions != null)
                foreach (JobDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public async Task<Dictionary<string, JobDefinition>> LoadJobDefinitionsAsync()
        {
            string filePath = Path.Combine(_basePath, "jobs.json");
            if (!File.Exists(filePath))
                // Создаем базовые задания по умолчанию
                return CreateDefaultJobDefinitions();

            string json = await File.ReadAllTextAsync(filePath);
            var definitions = JsonSerializer.Deserialize<List<JobDefinition>>(json);

            var result = new Dictionary<string, JobDefinition>();
            if (definitions != null)
                foreach (JobDefinition def in definitions)
                    result[def.Id] = def;

            return result;
        }

        public void SaveJobDefinitions(Dictionary<string, JobDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "jobs.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public async Task SaveJobDefinitionsAsync(Dictionary<string, JobDefinition> definitions)
        {
            string filePath = Path.Combine(_basePath, "jobs.json");
            string json =
                JsonSerializer.Serialize(definitions.Values, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
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
                            AgeCategory = AgeCategory.Adult,
                            Gender = Gender.Male,
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
                            AgeCategory = AgeCategory.Adult,
                            Gender = Gender.Female,
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

        #endregion

        #region Population

        public PopulationDefinition LoadPopulationDefinition()
        {
            string filePath = Path.Combine(_basePath, "population.json");
            if (!File.Exists(filePath))
                // Создаем настройки по умолчанию
                return CreateDefaultPopulationDefinition();

            string json = File.ReadAllText(filePath);
            var definition = JsonSerializer.Deserialize<PopulationDefinition>(json);

            return definition ?? CreateDefaultPopulationDefinition();
        }

        public async Task<PopulationDefinition> LoadPopulationDefinitionAsync()
        {
            string filePath = Path.Combine(_basePath, "population.json");
            if (!File.Exists(filePath))
                // Создаем настройки по умолчанию
                return CreateDefaultPopulationDefinition();

            string json = await File.ReadAllTextAsync(filePath);
            var definition = JsonSerializer.Deserialize<PopulationDefinition>(json);

            return definition ?? CreateDefaultPopulationDefinition();
        }

        public void SavePopulationDefinition(PopulationDefinition definition)
        {
            string filePath = Path.Combine(_basePath, "population.json");
            string json = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public async Task SavePopulationDefinitionAsync(PopulationDefinition definition)
        {
            string filePath = Path.Combine(_basePath, "population.json");
            string json = JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
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

        #endregion
    }
}