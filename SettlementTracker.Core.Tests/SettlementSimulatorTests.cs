using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class SettlementSimulatorTests
{
    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _simulator = new SettlementSimulator(_settlement);

        Dictionary<string, ResourceDefinition>? resourceDefinitions = TestDataGenerator.CreateResourceDefinitions();
        Dictionary<string, BuildingDefinition>? buildingDefinitions = TestDataGenerator.CreateBuildingDefinitions();
        Dictionary<string, JobDefinition>? jobDefinitions = TestDataGenerator.CreateJobDefinitions();
        PopulationDefinition? populationDefinition = TestDataGenerator.CreateDefaultPopulationDefinition();

        _simulator.Initialize(resourceDefinitions, buildingDefinitions, jobDefinitions, populationDefinition);
    }

    private SettlementState _settlement;
    private SettlementSimulator _simulator;

    [Test]
    public void AdvanceToNextDay_ShouldIncrementDayCounter()
    {
        // Arrange
        int initialDay = _settlement.CurrentDay;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        Assert.That(_settlement.CurrentDay, Is.EqualTo(initialDay + 1));
    }

    [Test]
    public void AdvanceToNextDay_ShouldAgePopulation()
    {
        // Arrange
        Dictionary<Guid, float>? initialAges = _settlement.Citizens.ToDictionary(c => c.Id, c => c.Age);

        float agePerDay = TestDataGenerator.CreateDefaultPopulationDefinition().AgePerDay;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        foreach (Citizen? citizen in _settlement.Citizens)
            Assert.That(citizen.Age, Is.EqualTo(initialAges[citizen.Id] + agePerDay));
    }

    // [Test]
    public void AdvanceToNextDay_ShouldProcessBasicNeeds()
    {
        // Arrange
        float initialFood = _settlement.Resources["food"];
        int populationCount = _settlement.Citizens.Count;

        List<ResourceEffect>? dailyNeeds = TestDataGenerator.CreateDefaultPopulationDefinition().DailyNeeds;

        foreach (ResourceEffect? need in dailyNeeds)
        {
        }

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Каждый гражданин потребляет 1 еду в день
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - populationCount));
    }

    // [Test]
    public void AdvanceToNextDay_ShouldProcessBuildingsWithWorkers()
    {
        // Arrange
        // Создаем ферму и назначаем работников
        BuildingDefinition? farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем работников, удовлетворяющих требованиям
        Citizen? adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == AgeCategory.Adult &&
            c.Gender == Gender.Male);

        Citizen? adultFemale = _settlement.Citizens.First(c =>
            c.AgeCategory == AgeCategory.Adult &&
            c.Gender == Gender.Female);

        farm.AssignCitizen(adultMale.Id);
        farm.AssignCitizen(adultFemale.Id);

        adultMale.WorkStatus = WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        adultFemale.WorkStatus = WorkStatus.AssignedToBuilding;
        adultFemale.AssignedToId = farm.Id;
        adultFemale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        float initialFood = _settlement.Resources["food"];
        float initialWater = _settlement.Resources["water"];

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Ферма производит 10 еды и потребляет 5 воды (с эффективностью 2.0 = 20 еды и -10 воды)
        Assert.Multiple(() =>
        {
            Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - _settlement.Citizens.Count + 20));
            Assert.That(_settlement.Resources["water"], Is.EqualTo(initialWater - 10));
        });
    }

    [Test]
    public void AdvanceToNextDay_ShouldNotProcessBuildingsWithoutRequiredWorkers()
    {
        // Arrange
        BuildingDefinition? farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем только одного работника (требуется 2)
        Citizen? adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == AgeCategory.Adult &&
            c.Gender == Gender.Male);

        farm.AssignCitizen(adultMale.Id);
        adultMale.WorkStatus = WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        float initialFood = _settlement.Resources["food"];

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Ферма не должна производить, так как требования к работникам не выполнены
        // Только базовое потребление еды населением
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - _settlement.Citizens.Count));
    }

    [Test]
    public void AdvanceToNextDay_ShouldFireDayAdvancedEvent()
    {
        // Arrange
        var eventFired = false;
        _simulator.DayAdvanced += (sender, args) => eventFired = true;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void AdvanceToNextDay_ShouldFireResourcesChangedEvent()
    {
        // Arrange
        var eventFired = false;
        _simulator.ResourcesChanged += (sender, args) => eventFired = true;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void AdvanceToNextDay_ShouldClearDailyChangesBeforeProcessing()
    {
        // Arrange
        // Добавляем старые дневные изменения
        _settlement.DailyResourceChanges["food"] = 100;
        _settlement.DailyResourceChanges["wood"] = 50;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Дневные изменения должны быть очищены перед обработкой нового дня
        // Проверяем, что изменения не накапливаются от предыдущего дня
        Dictionary<string, float>? dailyChanges = _simulator.GetResourceManager().GetDailyResourceChanges();

        // Изменения должны быть только от текущего дня
        Assert.That(dailyChanges["food"], Is.Not.EqualTo(100));
    }

    [Test]
    public void AdvanceToNextDay_ShouldHandleInsufficientResourcesForBuildingConsumption()
    {
        // Arrange
        BuildingDefinition? farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем работников
        Citizen? adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == AgeCategory.Adult &&
            c.Gender == Gender.Male);

        Citizen? adultFemale = _settlement.Citizens.First(c =>
            c.AgeCategory == AgeCategory.Adult &&
            c.Gender == Gender.Female);

        farm.AssignCitizen(adultMale.Id);
        farm.AssignCitizen(adultFemale.Id);

        adultMale.WorkStatus = WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        adultFemale.WorkStatus = WorkStatus.AssignedToBuilding;
        adultFemale.AssignedToId = farm.Id;
        adultFemale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        // Устанавливаем недостаточно воды для потребления зданием
        _settlement.Resources["water"] = 1;
        float initialFood = _settlement.Resources["food"];

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Ферма не должна производить еду, так как недостаточно воды для потребления
        // Только базовое потребление еды населением
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - _settlement.Citizens.Count));
    }

    [Test]
    public void GetManagers_ShouldReturnCorrectInstances()
    {
        // Act
        ResourceManager? resourceManager = _simulator.GetResourceManager();
        PopulationManager? populationManager = _simulator.GetPopulationManager();
        BuildingManager? buildingManager = _simulator.GetBuildingManager();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resourceManager, Is.Not.Null);
            Assert.That(populationManager, Is.Not.Null);
            Assert.That(buildingManager, Is.Not.Null);
            Assert.That(resourceManager, Is.TypeOf<ResourceManager>());
        });
    }
}