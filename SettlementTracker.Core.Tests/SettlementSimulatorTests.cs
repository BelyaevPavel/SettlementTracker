using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class SettlementSimulatorTests
{
    private SettlementState _settlement;
    private SettlementSimulator _simulator;

    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _simulator = new SettlementSimulator(_settlement);

        var resourceDefinitions = TestDataGenerator.CreateResourceDefinitions();
        var buildingDefinitions = TestDataGenerator.CreateBuildingDefinitions();
        var jobDefinitions = TestDataGenerator.CreateJobDefinitions();
        var populationDefinition = TestDataGenerator.CreateDefaultPopulationDefinition();

        _simulator.Initialize(resourceDefinitions, buildingDefinitions, jobDefinitions, populationDefinition);
    }

    [Test]
    public void AdvanceToNextDay_ShouldIncrementDayCounter()
    {
        // Arrange
        var initialDay = _settlement.CurrentDay;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        Assert.That(_settlement.CurrentDay, Is.EqualTo(initialDay + 1));
    }

    [Test]
    public void AdvanceToNextDay_ShouldAgePopulation()
    {
        // Arrange
        var initialAges = _settlement.Citizens.ToDictionary(c => c.Id, c => c.Age);

        var agePerDay = TestDataGenerator.CreateDefaultPopulationDefinition().AgePerDay;

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        foreach (var citizen in _settlement.Citizens)
            Assert.That(citizen.Age, Is.EqualTo(initialAges[citizen.Id] + agePerDay));
    }

    [Test]
    public void AdvanceToNextDay_ShouldProcessBasicNeeds()
    {
        // Arrange
        var initialFood = _settlement.Resources["food"];
        var populationCount = _settlement.Citizens.Count;

        var dailyNeeds = TestDataGenerator.CreateDefaultPopulationDefinition().DailyNeeds;

        foreach (var need in dailyNeeds)
        {
            
        }

        // Act
        _simulator.AdvanceToNextDay();

        // Assert
        // Каждый гражданин потребляет 1 еду в день
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - populationCount));
    }

    [Test]
    public void AdvanceToNextDay_ShouldProcessBuildingsWithWorkers()
    {
        // Arrange
        // Создаем ферму и назначаем работников
        var farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем работников, удовлетворяющих требованиям
        var adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == Models.Enums.AgeCategory.Adult &&
            c.Gender == Models.Enums.Gender.Male);

        var adultFemale = _settlement.Citizens.First(c =>
            c.AgeCategory == Models.Enums.AgeCategory.Adult &&
            c.Gender == Models.Enums.Gender.Female);

        farm.AssignCitizen(adultMale.Id);
        farm.AssignCitizen(adultFemale.Id);

        adultMale.WorkStatus = Models.Enums.WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        adultFemale.WorkStatus = Models.Enums.WorkStatus.AssignedToBuilding;
        adultFemale.AssignedToId = farm.Id;
        adultFemale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        var initialFood = _settlement.Resources["food"];
        var initialWater = _settlement.Resources["water"];

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
        var farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем только одного работника (требуется 2)
        var adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == Models.Enums.AgeCategory.Adult &&
            c.Gender == Models.Enums.Gender.Male);

        farm.AssignCitizen(adultMale.Id);
        adultMale.WorkStatus = Models.Enums.WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        var initialFood = _settlement.Resources["food"];

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
        var dailyChanges = _simulator.GetResourceManager().GetDailyResourceChanges();

        // Изменения должны быть только от текущего дня
        Assert.That(dailyChanges["food"], Is.Not.EqualTo(100));
    }

    [Test]
    public void AdvanceToNextDay_ShouldHandleInsufficientResourcesForBuildingConsumption()
    {
        // Arrange
        var farmDefinition = TestDataGenerator.CreateBuildingDefinitions()["farm"];
        var farm = new Building(Guid.NewGuid(), "farm", (0, 0))
        {
            Definition = farmDefinition,
            IsActive = true
        };

        // Назначаем работников
        var adultMale = _settlement.Citizens.First(c =>
            c.AgeCategory == Models.Enums.AgeCategory.Adult &&
            c.Gender == Models.Enums.Gender.Male);

        var adultFemale = _settlement.Citizens.First(c =>
            c.AgeCategory == Models.Enums.AgeCategory.Adult &&
            c.Gender == Models.Enums.Gender.Female);

        farm.AssignCitizen(adultMale.Id);
        farm.AssignCitizen(adultFemale.Id);

        adultMale.WorkStatus = Models.Enums.WorkStatus.AssignedToBuilding;
        adultMale.AssignedToId = farm.Id;
        adultMale.AssignedToType = "Building";

        adultFemale.WorkStatus = Models.Enums.WorkStatus.AssignedToBuilding;
        adultFemale.AssignedToId = farm.Id;
        adultFemale.AssignedToType = "Building";

        _settlement.Buildings.Add(farm);

        // Устанавливаем недостаточно воды для потребления зданием
        _settlement.Resources["water"] = 1;
        var initialFood = _settlement.Resources["food"];

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
        var resourceManager = _simulator.GetResourceManager();
        var populationManager = _simulator.GetPopulationManager();
        var buildingManager = _simulator.GetBuildingManager();

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