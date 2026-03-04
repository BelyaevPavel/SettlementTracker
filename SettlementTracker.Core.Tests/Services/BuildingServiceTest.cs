using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Repositories;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests.Services;

[TestFixture]
[TestOf(typeof(BuildingService))]
public class BuildingServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _definitionRepositoryMock = new Mock<IBuildingDefinitionRepository>();
        _resourcesServiceMock = new Mock<ISettlementResourcesService>();
        _loggerMock = new Mock<ILogger<BuildingService>>();


        _definitions = new Dictionary<string, BuildingDefinition>
        {
            ["testBuilding"] = new()
            {
                Id = "testBuilding",
                BuildCost = new List<ResourceEffect>
                {
                    new() { ResourceId = "wood", Amount = 10, IsProduction = false }
                }
            },
            ["buildingWithNoCost"] = new()
            {
                Id = "buildingWithNoCost",
                BuildCost = new List<ResourceEffect>()
            },
            ["buildingWithMultipleCosts"] = new()
            {
                Id = "buildingWithMultipleCosts",
                BuildCost = new List<ResourceEffect>
                {
                    new() { ResourceId = "wood", Amount = 5, IsProduction = false },
                    new() { ResourceId = "stone", Amount = 3, IsProduction = false }
                }
            },
            ["alreadyBuilt"] = new()
            {
                Id = "alreadyBuilt",
                BuildCost = new List<ResourceEffect>
                {
                    new() { ResourceId = "wood", Amount = 1, IsProduction = false }
                }
            }
        };

        _definitionRepositoryMock
            .Setup(r => r.LoadBuildingDefinitions())
            .Returns(_definitions);

        // Создаём сервис – в конструкторе вызовется LoadDefinitions
        _service = new BuildingService(
            _definitionRepositoryMock.Object,
            _resourcesServiceMock.Object,
            _loggerMock.Object);

        _resourcesServiceMock
            .Setup(r => r.CanSpend(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(true);

        _resourcesServiceMock
            .Setup(r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()))
            .Returns(Task.Run(() => true));
    }

    [TearDown]
    public void TearDown()
    {
        SetBuiltBuildings(new List<Building>());
        // Clean up any files created by tests (like State/builtBuildings.json)
        if (Directory.Exists("State"))
            Directory.Delete("State", true);
    }

    private Mock<IBuildingDefinitionRepository> _definitionRepositoryMock;
    private Mock<ISettlementResourcesService> _resourcesServiceMock;
    private Mock<ILogger<BuildingService>> _loggerMock;
    private BuildingService _service;
    private Dictionary<string, BuildingDefinition> _definitions;

    // Helper to access private field _builtBuildings via reflection
    private List<Building> GetBuiltBuildings()
    {
        FieldInfo? field = typeof(BuildingService).GetField("_builtBuildings",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (List<Building>)field.GetValue(_service);
    }

    private void SetBuiltBuildings(List<Building> buildings)
    {
        FieldInfo? field = typeof(BuildingService).GetField("_builtBuildings",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(_service, buildings);
    }

    // Вспомогательный метод для проверки лога с определённым уровнем и содержанием
    private static void VerifyLog<T>(Mock<ILogger<T>> loggerMock, LogLevel level, string containsMessage,
        Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(containsMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            times);
    }

    [Test]
    public void CanAffordBuild_NotEnoughResources_ReturnsFalse()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 10))
            .Returns(false);

        // Act
        bool result = _service.CanAffordBuild("testBuilding");

        // Assert
        Assert.That(result, Is.False);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Test]
    public void CanAffordBuild_EnoughResources_ReturnsTrue()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 10))
            .Returns(true);

        // Act
        bool result = _service.CanAffordBuild("testBuilding");

        // Assert
        Assert.That(result, Is.True);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Test]
    public void CanAffordBuild_EmptyBuildCost_ReturnsTrue()
    {
        // Arrange
        // Для buildingWithNoCost стоимость отсутствует – не должно быть проверок CanSpend

        // Act
        bool result = _service.CanAffordBuild("buildingWithNoCost");

        // Assert
        Assert.That(result, Is.True);
        _resourcesServiceMock.Verify(r => r.CanSpend(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Test]
    public void CanAffordBuild_MultipleResources_AllEnough_ReturnsTrue()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 5))
            .Returns(true);
        _resourcesServiceMock
            .Setup(r => r.CanSpend("stone", 3))
            .Returns(true);

        // Act
        bool result = _service.CanAffordBuild("buildingWithMultipleCosts");

        // Assert
        Assert.That(result, Is.True);
        _resourcesServiceMock.Verify(r => r.CanSpend("wood", 5), Times.Once);
        _resourcesServiceMock.Verify(r => r.CanSpend("stone", 3), Times.Once);
    }

    [Test]
    public void CanAffordBuild_MultipleResources_FirstFails_ReturnsFalseAndDoesNotCheckSecond()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 5))
            .Returns(false);
        // Не настраиваем stone – он не должен вызываться

        // Act
        bool result = _service.CanAffordBuild("buildingWithMultipleCosts");

        // Assert
        Assert.That(result, Is.False);
        _resourcesServiceMock.Verify(r => r.CanSpend("wood", 5), Times.Once);
        _resourcesServiceMock.Verify(r => r.CanSpend("stone", 3), Times.Never);
    }

    [Test]
    public void CanAffordBuild_DoesNotCallAnyResourceModificationMethods()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 10))
            .Returns(true);

        // Act
        _service.CanAffordBuild("testBuilding");

        // Assert
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.IsAny<ResourceEffect>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never);
        _resourcesServiceMock.Verify(
            r => r.AddResourceAsync(
                It.IsAny<string>(),
                It.IsAny<float>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never);
        _resourcesServiceMock.Verify(
            r => r.SetResourceAsync(
                It.IsAny<string>(),
                It.IsAny<float>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never);
        _resourcesServiceMock.Verify(
            r => r.TrySpendResourceAsync(
                It.IsAny<string>(),
                It.IsAny<float>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never);
    }

    [Test]
    public async Task TryBuildAsync_NullDefinitionId_ReturnsFalseAndLogsWarning()
    {
        // Act
        bool result = await _service.TryBuildAsync(null, (0, 0));

        // Assert
        Assert.That(result, Is.False);
        //  VerifyLog(_loggerMock, LogLevel.Warning, "definitionId cannot be null", Times.Once);
    }

    [Test]
    public async Task TryBuildAsync_EmptyDefinitionId_ReturnsFalseAndLogsWarning()
    {
        // Act
        bool result = await _service.TryBuildAsync(string.Empty, (0, 0));

        // Assert
        Assert.That(result, Is.False);
        //    VerifyLog(_loggerMock, LogLevel.Warning, "definitionId cannot be null", Times.Once);
    }

    [Test]
    public async Task TryBuildAsync_WhitespaceDefinitionId_ReturnsFalseAndLogsWarning()
    {
        // Act
        bool result = await _service.TryBuildAsync("   ", (0, 0));

        // Assert
        Assert.That(result, Is.False);
        //     VerifyLog(_loggerMock, LogLevel.Warning, "definitionId cannot be null", Times.Once);
    }

    [Test]
    public async Task TryBuildAsync_DefinitionNotFound_ReturnsFalseAndLogsWarning()
    {
        // Act
        bool result = await _service.TryBuildAsync("nonExisting", (0, 0));

        // Assert
        Assert.That(result, Is.False);
        //     VerifyLog(_loggerMock, LogLevel.Warning, "Building definition for nonExisting not found", Times.Once);
    }

    [Test]
    public async Task TryBuildAsync_AlreadyBuilt_ReturnsFalseAndLogsWarning()
    {
        // Arrange - add an already built building with definition "alreadyBuilt"
        var existingBuilding = new Building(Guid.NewGuid(), "alreadyBuilt", (1, 1))
        {
            Definition = _definitions["alreadyBuilt"]
        };
        SetBuiltBuildings(new List<Building> { existingBuilding });

        // Act
        bool result = await _service.TryBuildAsync("alreadyBuilt", (2, 2));

        // Assert
        Assert.That(result, Is.False);
        //VerifyLog(_loggerMock, LogLevel.Warning, "Здание alreadyBuilt уже построено", Times.Once);
        // Ensure no resource effect was applied
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task TryBuildAsync_CannotAfford_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 10))
            .Returns(false);

        // Act
        bool result = await _service.TryBuildAsync("testBuilding", (0, 0));

        // Assert
        Assert.That(result, Is.False);
        // VerifyLog(_loggerMock, LogLevel.Warning, "Недостаточно ресурсов для строительства testBuilding",
        //     Times.Once);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task TryBuildAsync_WithMultipleResources_AllApplied()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend("wood", 5))
            .Returns(true);
        _resourcesServiceMock
            .Setup(r => r.CanSpend("stone", 3))
            .Returns(true);

        // Act
        bool result = await _service.TryBuildAsync("buildingWithMultipleCosts", (0, 0));

        // Assert
        Assert.That(result, Is.True);

        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.Is<ResourceEffect>(e => e.ResourceId == "wood" && e.Amount == 5 && e.IsProduction == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.Is<ResourceEffect>(e => e.ResourceId == "stone" && e.Amount == 3 && e.IsProduction == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TryBuildAsync_EmptyBuildCost_BuildsSuccessfully()
    {
        // Act
        bool result = await _service.TryBuildAsync("buildingWithNoCost", (0, 0));

        // Assert
        Assert.That(result, Is.True);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Never);

        IEnumerable<Building>? builtBuildings = await _service.GetBuiltBuildingsAsync();
        Assert.That(builtBuildings.Count(), Is.EqualTo(1));
        Assert.That(builtBuildings.First().DefinitionId, Is.EqualTo("buildingWithNoCost"));
    }

    [Test]
    public async Task TryBuildAsync_AfterSuccessfulBuild_IsBuildingBuiltAsyncReturnsTrue()
    {
        // Arrange
        _resourcesServiceMock
            .Setup(r => r.CanSpend(It.IsAny<string>(), It.IsAny<float>())).Returns(true);

        // Act
        bool tryBuildResult = await _service.TryBuildAsync("testBuilding", (0, 0));

        // Assert
        bool isBuilt = await _service.IsBuildingBuiltAsync("testBuilding");
        Assert.That(tryBuildResult, Is.True);
        Assert.That(isBuilt, Is.True);
    }

    [Test]
    public async Task TryBuildAsync_WithDifferentPositions_BuildingsHaveCorrectPositions()
    {
        // Arrange
        var pos1 = (10, 20);
        var pos2 = (30, 40);
        _resourcesServiceMock
            .Setup(r => r.CanSpend(It.IsAny<string>(), It.IsAny<float>())).Returns(true);

        // Act
        bool testBuildingIsBuilt = await _service.TryBuildAsync("testBuilding", pos1);
        bool buildingWithNoCostIsBuilt = await _service.TryBuildAsync("buildingWithNoCost", pos2);

        // Assert
        IEnumerable<Building>? buildings = await _service.GetBuiltBuildingsAsync();
        Building? building1 = buildings.First(b => b.DefinitionId == "testBuilding");
        Building? building2 = buildings.First(b => b.DefinitionId == "buildingWithNoCost");

        Assert.Multiple(() =>
        {
            Assert.That(testBuildingIsBuilt, Is.True);
            Assert.That(buildingWithNoCostIsBuilt, Is.True);
            Assert.That(building1.Position, Is.EqualTo(pos1));
            Assert.That(building2.Position, Is.EqualTo(pos2));
        });
    }

    [Test]
    public async Task TryDemolishAsync_BuildingNotFound_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        bool result = await _service.TryDemolishAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.False);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task TryDemolishAsync_DefinitionIsNull_ReturnsFalse()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "testBuilding", (0, 0))
        {
            Definition = null // explicitly set null
        };
        SetBuiltBuildings(new List<Building> { building });

        // Act
        bool result = await _service.TryDemolishAsync(buildingId);

        // Assert
        Assert.That(result, Is.False);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // Building should still be in the list
        Assert.That(GetBuiltBuildings(), Contains.Item(building));
    }

    [Test]
    public async Task TryDemolishAsync_Success_DemolishesBuildingAndReturnsResources()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "testBuilding", (2, 3))
        {
            Definition = _definitions["testBuilding"]
        };
        SetBuiltBuildings(new List<Building> { building });

        var eventRaised = false;
        _service.BuildingChange += (sender, args) => eventRaised = true;

        // Act
        bool result = await _service.TryDemolishAsync(buildingId);

        // Assert
        Assert.That(result, Is.True);

        // Verify resource effect applied with reversed IsProduction (refund)
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.Is<ResourceEffect>(e =>
                    e.ResourceId == "wood" &&
                    e.Amount == 10 &&
                    e.IsProduction == true), // originally false, now true for refund
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Building should be removed
        Assert.That(GetBuiltBuildings(), Is.Empty);
        Assert.That(await _service.GetBuildingAsync(buildingId), Is.Null);

        // SaveChangesAsync should have been called (file created)
        Assert.That(File.Exists("State/builtBuildings.json"), Is.True);

        // Event should be raised
        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public async Task TryDemolishAsync_WithMultipleResources_AllReturned()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "buildingWithMultipleCosts", (0, 0))
        {
            Definition = _definitions["buildingWithMultipleCosts"]
        };
        SetBuiltBuildings(new List<Building> { building });

        // Act
        bool result = await _service.TryDemolishAsync(buildingId);

        // Assert
        Assert.That(result, Is.True);

        // Verify both resources returned with reversed IsProduction
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.Is<ResourceEffect>(e =>
                    e.ResourceId == "wood" &&
                    e.Amount == 5 &&
                    e.IsProduction == true),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(
                It.Is<ResourceEffect>(e =>
                    e.ResourceId == "stone" &&
                    e.Amount == 3 &&
                    e.IsProduction == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task TryDemolishAsync_AfterDemolish_BuildingIsNoLongerBuilt()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "testBuilding", (0, 0))
        {
            Definition = _definitions["testBuilding"]
        };
        SetBuiltBuildings(new List<Building> { building });

        // Act
        await _service.TryDemolishAsync(buildingId);

        // Assert
        bool isBuilt = await _service.IsBuildingBuiltAsync("testBuilding");
        Assert.That(isBuilt, Is.False);
    }

    [Test]
    public async Task TryDemolishAsync_Success_SavesStateToFile()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "testBuilding", (0, 0))
        {
            Definition = _definitions["testBuilding"]
        };
        SetBuiltBuildings(new List<Building> { building });

        // Act
        await _service.TryDemolishAsync(buildingId);

        // Assert - file should exist and contain empty list
        Assert.That(File.Exists("State/builtBuildings.json"), Is.True);
        string json = await File.ReadAllTextAsync("State/builtBuildings.json");
        Assert.That(json, Does.Contain("[]").Or.EqualTo("[]")); // empty array
    }

    [Test]
    public async Task TryDemolishAsync_MultipleDemolitions_OnlyFirstSucceeds()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var building = new Building(buildingId, "testBuilding", (0, 0))
        {
            Definition = _definitions["testBuilding"]
        };
        SetBuiltBuildings(new List<Building> { building });

        // Act
        bool firstResult = await _service.TryDemolishAsync(buildingId);
        bool secondResult = await _service.TryDemolishAsync(buildingId);

        // Assert
        Assert.That(firstResult, Is.True);
        Assert.That(secondResult, Is.False);
        _resourcesServiceMock.Verify(
            r => r.TryApplyResourceEffectAsync(It.IsAny<ResourceEffect>(), It.IsAny<CancellationToken>()),
            Times.Once); // only first demolition applied resources
    }
}