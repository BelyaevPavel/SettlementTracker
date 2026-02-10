using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Tests;

public static class TestDataGenerator
{
    public static SettlementState CreateTestSettlement()
    {
        var settlement = new SettlementState(Guid.NewGuid(), "Test Settlement");

        // Инициализируем ресурсы
        settlement.Resources["food"] = 100;
        settlement.Resources["wood"] = 50;
        settlement.Resources["water"] = 50;

        // Добавляем тестовых жителей
        var citizens = new List<Citizen>
        {
            CreateAdult(),
            CreateAdult(Gender.Female),
            CreateChild(),
            CreateAdult(Gender.Male, true),
            CreateElder()
        };

        settlement.Citizens.AddRange(citizens);

        return settlement;
    }

    public static Dictionary<string, ResourceDefinition> CreateResourceDefinitions()
    {
        return new Dictionary<string, ResourceDefinition>
        {
            ["food"] = new()
            {
                Id = "food",
                Name = "Food",
                Description = "Basic food resource",
                IconPath = "food.png",
                IsConsumable = true
            },
            ["wood"] = new()
            {
                Id = "wood",
                Name = "Wood",
                Description = "Building material",
                IconPath = "wood.png",
                IsConsumable = false
            },
            ["water"] = new()
            {
                Id = "water",
                Name = "Water",
                Description = "Drinking water",
                IconPath = "water.png",
                IsConsumable = true
            }
        };
    }

    public static Dictionary<string, BuildingDefinition> CreateBuildingDefinitions()
    {
        return new Dictionary<string, BuildingDefinition>
        {
            ["house"] = new()
            {
                Id = "house",
                Name = "House",
                Description = "Housing for 5 people",
                Size = (2, 2),
                BuildCost = new List<ResourceEffect>
                {
                    new() { ResourceId = "wood", Amount = 20, IsProduction = false }
                },
                DailyEffects = new List<ResourceEffect>(),
                WorkerRequirements = new List<WorkerRequirement>(),
                ProvidesHousing = 5
            },
            ["farm"] = new()
            {
                Id = "farm",
                Name = "Farm",
                Description = "Produces food",
                Size = (3, 3),
                BuildCost = new List<ResourceEffect>
                {
                    new() { ResourceId = "wood", Amount = 15, IsProduction = false }
                },
                DailyEffects = new List<ResourceEffect>
                {
                    new() { ResourceId = "food", Amount = 10, IsProduction = true },
                    new() { ResourceId = "water", Amount = 5, IsProduction = false }
                },
                WorkerRequirements = new List<WorkerRequirement>
                {
                    new()
                    {
                        AgeCategory = AgeCategory.Adult,
                        Gender = Gender.Male,
                        Count = 1,
                        CanBeSlave = true,
                        RequiresSupervision = false
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
    }

    public static Dictionary<string, JobDefinition> CreateJobDefinitions()
    {
        return new Dictionary<string, JobDefinition>
        {
            ["hunting"] = new()
            {
                Id = "hunting",
                Name = "Hunting",
                Description = "Hunting for food",
                DailyEffects = new List<ResourceEffect>
                {
                    new() { ResourceId = "food", Amount = 5, IsProduction = true }
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
            }
        };
    }

    public static PopulationDefinition CreateDefaultPopulationDefinition()
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

        return populationDef;
    }

    public static Citizen CreateAdult(Gender gender = Gender.Male, bool isSlave = false)
    {
        var adultFemale = new Citizen(Guid.NewGuid(), gender == Gender.Female ? "Test Female" : "Test Male",
            gender, 22, isSlave);
        adultFemale.Definition = CreateDefaultPopulationDefinition();
        return adultFemale;
    }

    public static Citizen CreateChild(Gender gender = Gender.Male, bool isSlave = false)
    {
        var child = new Citizen(Guid.NewGuid(), "Test Child", gender, 10, isSlave);
        child.Definition = CreateDefaultPopulationDefinition();
        return child;
    }

    public static Citizen CreateElder(Gender gender = Gender.Male, bool isSlave = false)
    {
        var elder = new Citizen(Guid.NewGuid(), "Test Elder", gender, 75, isSlave);
        elder.Definition = CreateDefaultPopulationDefinition();
        return elder;
    }
}