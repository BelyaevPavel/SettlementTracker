using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class BuildingManagerTests
{
    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _resourceManager = new ResourceManager(_settlement);
        _buildingManager = new BuildingManager(_settlement, _resourceManager);

        Dictionary<string, ResourceDefinition>? resourceDefinitions = TestDataGenerator.CreateResourceDefinitions();
        _resourceManager.InitializeResources(resourceDefinitions);

        Dictionary<string, BuildingDefinition>? buildingDefinitions = TestDataGenerator.CreateBuildingDefinitions();
        _buildingManager.InitializeDefinitions(buildingDefinitions);
    }

    private SettlementState _settlement;
    private ResourceManager _resourceManager;
    private BuildingManager _buildingManager;

    [Test]
    public void CanBuildBuilding_ShouldReturnTrue_WhenResourcesAndPositionAvailable()
    {
        // Arrange
        const string buildingId = "house";
        var position = (1, 1);

        // Act
        bool canBuild = _buildingManager.CanBuildBuilding(buildingId, position);

        // Assert
        Assert.That(canBuild, Is.True);
    }

    [Test]
    public void CanBuildBuilding_ShouldReturnFalse_WhenInsufficientResources()
    {
        // Arrange
        const string buildingId = "house";
        var position = (1, 1);

        // Устанавливаем недостаточно древесины
        _settlement.Resources["wood"] = 5;

        // Act
        bool canBuild = _buildingManager.CanBuildBuilding(buildingId, position);

        // Assert
        Assert.That(canBuild, Is.False);
    }

    [Test]
    public void BuildBuilding_ShouldCreateBuilding_AndConsumeResources()
    {
        // Arrange
        const string buildingId = "house";
        var position = (1, 1);
        float initialWood = _settlement.Resources["wood"];

        // Act
        Building? building = _buildingManager.BuildBuilding(buildingId, position, "My House");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(building, Is.Not.Null);
            Assert.That(building.DefinitionId, Is.EqualTo(buildingId));
            Assert.That(building.Name, Is.EqualTo("My House"));
            Assert.That(building.Position, Is.EqualTo(position));
            Assert.That(_settlement.Buildings.Contains(building), Is.True);
            Assert.That(_settlement.Resources["wood"], Is.EqualTo(initialWood - 20));
        });
    }

    [Test]
    public void BuildBuilding_ShouldReturnNull_WhenCannotBuild()
    {
        // Arrange
        const string buildingId = "house";
        var position = (1, 1);

        // Устанавливаем недостаточно древесины
        _settlement.Resources["wood"] = 5;

        // Act
        Building? building = _buildingManager.BuildBuilding(buildingId, position);

        // Assert
        Assert.That(building, Is.Null);
    }

    [Test]
    public void DemolishBuilding_ShouldRemoveBuilding_AndUnassignWorkers()
    {
        // Arrange
        var building = new Building(Guid.NewGuid(), "house", (1, 1));
        Citizen? citizen = _settlement.Citizens[0];

        building.AssignCitizen(citizen.Id);
        citizen.WorkStatus = WorkStatus.AssignedToBuilding;
        citizen.AssignedToId = building.Id;
        citizen.AssignedToType = "Building";

        _settlement.Buildings.Add(building);

        // Act
        _buildingManager.DemolishBuilding(building.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_settlement.Buildings.Contains(building), Is.False);
            Assert.That(citizen.WorkStatus, Is.EqualTo(WorkStatus.Idle));
            Assert.That(citizen.AssignedToId, Is.Null);
        });
    }

    [Test]
    [TestCase(Gender.Male, Gender.Male)]
    [TestCase(Gender.Female, Gender.Female)]
    [TestCase(Gender.Any, Gender.Male)]
    [TestCase(Gender.Any, Gender.Female)]
    public void AreWorkerRequirementsMet_ShouldReturnTrue_WhenRequirementsSatisfied(Gender requiredGender,
        Gender actualGender)
    {
        // Arrange
        var buildingDef = new BuildingDefinition
        {
            Id = "test",
            Name = "Test Building",
            WorkerRequirements = new List<WorkerRequirement>
            {
                new()
                {
                    AgeCategory = AgeCategory.Adult,
                    Gender = requiredGender,
                    Count = 1,
                    CanBeSlave = false,
                    RequiresSupervision = false
                }
            }
        };

        var building = new Building(Guid.NewGuid(), "test", (0, 0))
        {
            Definition = buildingDef
        };
        Citizen? citizen = TestDataGenerator.CreateAdult(actualGender);

        building.AssignCitizen(citizen.Id);

        _settlement.Citizens.Add(citizen);
        _settlement.Buildings.Add(building);

        // Act
        bool requirementsMet = _buildingManager.AreWorkerRequirementsMet(building);

        // Assert
        Assert.That(requirementsMet, Is.True);
    }

    [Test]
    public void AreWorkerRequirementsMet_ShouldReturnFalse_WhenRequirementsNotSatisfied()
    {
        // Arrange
        var buildingDef = new BuildingDefinition
        {
            Id = "test",
            Name = "Test Building",
            WorkerRequirements = new List<WorkerRequirement>
            {
                new()
                {
                    AgeCategory = AgeCategory.Adult,
                    Gender = Gender.Male,
                    Count = 2, // Требуется 2, а есть только 1
                    CanBeSlave = false,
                    RequiresSupervision = false
                }
            }
        };

        var building = new Building(Guid.NewGuid(), "test", (0, 0))
        {
            Definition = buildingDef
        };

        Citizen? citizen = TestDataGenerator.CreateAdult();
        building.AssignCitizen(citizen.Id);

        _settlement.Buildings.Add(building);

        // Act
        bool requirementsMet = _buildingManager.AreWorkerRequirementsMet(building);

        // Assert
        Assert.That(requirementsMet, Is.False);
    }

    [Test]
    public void GetBuildingsByType_ShouldReturnCorrectBuildings()
    {
        // Arrange
        var house1 = new Building(Guid.NewGuid(), "house", (1, 1));
        var house2 = new Building(Guid.NewGuid(), "house", (2, 2));
        var farm = new Building(Guid.NewGuid(), "farm", (3, 3));

        _settlement.Buildings.AddRange(new[] { house1, house2, farm });

        // Act
        List<Building>? houses = _buildingManager.GetBuildingsByType("house");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(houses, Has.Count.EqualTo(2));
            Assert.That(houses, Has.All.Property("DefinitionId").EqualTo("house"));
        });
    }

    [Test]
    public void ToggleBuildingActivity_ShouldChangeIsActive()
    {
        // Arrange
        var building = new Building(Guid.NewGuid(), "house", (1, 1))
        {
            IsActive = true
        };
        _settlement.Buildings.Add(building);

        // Act
        _buildingManager.ToggleBuildingActivity(building.Id, false);

        // Assert
        Assert.That(building.IsActive, Is.False);
    }
}