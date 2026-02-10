using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class PopulationManagerTests
{
    private SettlementState _settlement;
    private PopulationManager _populationManager;

    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _populationManager = new PopulationManager(_settlement);
    }

    [Test]
    public void AddCitizen_ShouldAddCitizenToSettlement()
    {
        // Arrange
        var initialCount = _settlement.Citizens.Count;

        // Act
        var citizen = _populationManager.AddCitizen("New Citizen", Gender.Male, 30);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_settlement.Citizens.Count, Is.EqualTo(initialCount + 1));
            Assert.That(_settlement.Citizens.Contains(citizen), Is.True);
            Assert.That(citizen.Name, Is.EqualTo("New Citizen"));
            Assert.That(citizen.Age, Is.EqualTo(30));
            Assert.That(citizen.WorkStatus, Is.EqualTo(WorkStatus.Idle));
        });
    }

    [Test]
    public void AddFamily_ShouldAddCorrectNumberOfCitizens()
    {
        // Arrange
        var initialCount = _settlement.Citizens.Count;
        const int adults = 2;
        const int children = 3;

        // Act
        _populationManager.AddFamily(adults, children);

        // Assert
        Assert.That(_settlement.Citizens.Count, Is.EqualTo(initialCount + adults + children));
    }

    [Test]
    public void RemoveCitizen_ShouldRemoveCitizen_AndUnassignFromWork()
    {
        // Arrange
        var citizen = _settlement.Citizens[0];
        citizen.WorkStatus = WorkStatus.AssignedToBuilding;
        ;

        var building = new Building(Guid.NewGuid(), "test", (0, 0));
        _settlement.Buildings.Add(building);
        _populationManager.AssignCitizenToBuilding(citizen.Id, building.Id);

        // Act
        _populationManager.RemoveCitizen(citizen.Id);

        // Assert
        Assert.That(_settlement.Citizens.Contains(citizen), Is.False);
        Assert.That(building.AssignedCitizenIds.Contains(citizen.Id), Is.False);
    }

    [Test]
    public void RemoveCitizen_ShouldRemoveCitizen_AndUnassignFromJob()
    {
        // Arrange
        var citizen = _settlement.Citizens[0];
        citizen.WorkStatus = WorkStatus.AssignedToBuilding;
        ;

        var job = new ActiveJob(Guid.NewGuid(), "test");
        _settlement.ActiveJobs.Add(job);
        _populationManager.AssignCitizenToJob(citizen.Id, job.Id);

        // Act
        _populationManager.RemoveCitizen(citizen.Id);

        // Assert
        Assert.That(_settlement.Citizens.Contains(citizen), Is.False);
        Assert.That(job.AssignedCitizenIds.Contains(citizen.Id), Is.False);
    }

    [Test]
    public void AgePopulation_ShouldIncreaseAgeOfAllCitizens()
    {
        // Arrange
        var initialAges = _settlement.Citizens.ToDictionary(c => c.Id, c => c.Age);

        var agePerDay = TestDataGenerator.CreateDefaultPopulationDefinition().AgePerDay;

        // Act
        _populationManager.AgePopulation();

        // Assert
        foreach (var citizen in _settlement.Citizens)
            Assert.That(citizen.Age, Is.EqualTo(initialAges[citizen.Id] + agePerDay));
    }

    [Test]
    public void FindAvailableWorkers_ShouldReturnOnlyMatchingCitizens()
    {
        // Arrange
        var requirement = new Models.Definitions.WorkerRequirement
        {
            AgeCategory = AgeCategory.Adult,
            Gender = Gender.Male,
            Count = 1,
            CanBeSlave = false,
            RequiresSupervision = false
        };

        // Act
        var availableWorkers = _populationManager.FindAvailableWorkers(requirement);

        // Assert
        Assert.That(availableWorkers, Has.All.Property("AgeCategory").EqualTo(AgeCategory.Adult));
        Assert.That(availableWorkers, Has.All.Property("Gender").EqualTo(Gender.Male));
        Assert.That(availableWorkers, Has.All.Property("WorkStatus").EqualTo(WorkStatus.Idle));
        Assert.That(availableWorkers, Has.All.Property("IsSlave").EqualTo(false));
    }

    [Test]
    public void AssignCitizenToBuilding_ShouldUpdateCitizenStatus_AndBuildingAssignment()
    {
        // Arrange
        var citizen = _settlement.Citizens[0];
        var building = new Building(Guid.NewGuid(), "test", (0, 0));
        _settlement.Buildings.Add(building);

        // Act
        _populationManager.AssignCitizenToBuilding(citizen.Id, building.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(citizen.WorkStatus, Is.EqualTo(WorkStatus.AssignedToBuilding));
            Assert.That(citizen.AssignedToId, Is.EqualTo(building.Id));
            Assert.That(citizen.AssignedToType, Is.EqualTo("Building"));
            Assert.That(building.AssignedCitizenIds.Contains(citizen.Id), Is.True);
        });
    }

    [Test]
    public void UnassignCitizen_ShouldResetCitizenStatus_AndRemoveFromAssignment()
    {
        // Arrange
        var citizen = _settlement.Citizens[0];
        var building = new Building(Guid.NewGuid(), "test", (0, 0));
        building.AssignCitizen(citizen.Id);
        _settlement.Buildings.Add(building);

        citizen.WorkStatus = WorkStatus.AssignedToBuilding;
        citizen.AssignedToId = building.Id;
        citizen.AssignedToType = "Building";

        // Act
        _populationManager.UnassignCitizen(citizen.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(citizen.WorkStatus, Is.EqualTo(WorkStatus.Idle));
            Assert.That(citizen.AssignedToId, Is.Null);
            Assert.That(citizen.AssignedToType, Is.Empty);
            Assert.That(building.AssignedCitizenIds.Contains(citizen.Id), Is.False);
        });
    }

    [Test]
    public void GetPopulationStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        // В тестовых данных есть: 1 взрослый мужчина, 1 взрослая женщина, 1 ребенок, 1 раб, 1 старик

        // Act
        var stats = _populationManager.GetPopulationStatistics();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(stats.TotalPopulation, Is.EqualTo(5));

            var adultMaleCount = stats.Counts.FirstOrDefault(kvp =>
                kvp.Key.Age == AgeCategory.Adult &&
                kvp.Key.Gender == Gender.Male &&
                kvp.Key.IsSlave == false).Value;

            var adultFemaleCount = stats.Counts.FirstOrDefault(kvp =>
                kvp.Key.Age == AgeCategory.Adult &&
                kvp.Key.Gender == Gender.Female &&
                kvp.Key.IsSlave == false).Value;

            Assert.That(adultMaleCount, Is.EqualTo(1));
            Assert.That(adultFemaleCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Citizen_AgeCategory_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var child = TestDataGenerator.CreateChild();
        var adult = TestDataGenerator.CreateAdult();
        var elder = TestDataGenerator.CreateElder();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(child.AgeCategory, Is.EqualTo(AgeCategory.Child));
            Assert.That(adult.AgeCategory, Is.EqualTo(AgeCategory.Adult));
            Assert.That(elder.AgeCategory, Is.EqualTo(AgeCategory.Elder));
        });
    }
}