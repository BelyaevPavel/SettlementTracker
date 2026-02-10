using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class ResourceManagerTests
{
    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _resourceManager = new ResourceManager(_settlement);

        Dictionary<string, ResourceDefinition>? resourceDefinitions = TestDataGenerator.CreateResourceDefinitions();
        _resourceManager.InitializeResources(resourceDefinitions);
    }

    private SettlementState _settlement;
    private ResourceManager _resourceManager;

    [Test]
    public void InitializeResources_ShouldCreateMissingResourcesWithZero()
    {
        // Arrange
        var newDefinitions = new Dictionary<string, ResourceDefinition>
        {
            ["new_resource"] = new() { Id = "new_resource", Name = "New Resource" }
        };

        // Act
        _resourceManager.InitializeResources(newDefinitions);

        // Assert
        Assert.That(_settlement.Resources.ContainsKey("new_resource"), Is.True);
        Assert.That(_settlement.Resources["new_resource"], Is.EqualTo(0));
    }

    [Test]
    public void HasEnoughResources_ShouldReturnTrue_WhenResourcesAreSufficient()
    {
        // Arrange
        var requirements = new List<ResourceEffect>
        {
            new() { ResourceId = "food", Amount = 50, IsProduction = false },
            new() { ResourceId = "wood", Amount = 10, IsProduction = false }
        };

        // Act
        bool result = _resourceManager.HasEnoughResources(requirements);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasEnoughResources_ShouldReturnFalse_WhenResourcesAreInsufficient()
    {
        // Arrange
        var requirements = new List<ResourceEffect>
        {
            new() { ResourceId = "food", Amount = 200, IsProduction = false }
        };

        // Act
        bool result = _resourceManager.HasEnoughResources(requirements);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryConsumeResources_ShouldConsumeResources_WhenEnoughAvailable()
    {
        // Arrange
        float initialFood = _settlement.Resources["food"];
        var requirements = new List<ResourceEffect>
        {
            new() { ResourceId = "food", Amount = 30, IsProduction = false }
        };

        // Act
        bool result = _resourceManager.TryConsumeResources(requirements);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood - 30));
    }

    [Test]
    public void TryConsumeResources_ShouldNotConsumeResources_WhenNotEnoughAvailable()
    {
        // Arrange
        float initialFood = _settlement.Resources["food"];
        var requirements = new List<ResourceEffect>
        {
            new() { ResourceId = "food", Amount = 150, IsProduction = false }
        };

        // Act
        bool result = _resourceManager.TryConsumeResources(requirements);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood));
    }

    [Test]
    public void AddResources_ShouldIncreaseResourceAmounts()
    {
        // Arrange
        float initialFood = _settlement.Resources["food"];
        var resourcesToAdd = new List<ResourceEffect>
        {
            new() { ResourceId = "food", Amount = 25, IsProduction = true }
        };

        // Act
        _resourceManager.AddResources(resourcesToAdd);

        // Assert
        Assert.That(_settlement.Resources["food"], Is.EqualTo(initialFood + 25));
    }

    [Test]
    public void AddResource_ShouldCreateResourceIfNotExists()
    {
        // Arrange
        const string newResourceId = "gold";

        // Act
        _resourceManager.AddResource(newResourceId, 100);

        // Assert
        Assert.That(_settlement.Resources.ContainsKey(newResourceId), Is.True);
        Assert.That(_settlement.Resources[newResourceId], Is.EqualTo(100));
    }

    [Test]
    public void GetDaysUntilResourceDepletion_ShouldReturnCorrectValues()
    {
        // Arrange
        // food: 100, wood: 50, water: 50
        // Добавляем дневные изменения: food потребляется 20 в день
        var dailyChanges = new Dictionary<string, float>
        {
            ["food"] = -20,
            ["wood"] = 10, // производится
            ["water"] = -5
        };

        foreach (KeyValuePair<string, float> change in dailyChanges)
            _settlement.DailyResourceChanges[change.Key] = change.Value;

        // Act
        Dictionary<string, int>? result = _resourceManager.GetDaysUntilResourceDepletion();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result["food"], Is.EqualTo(5)); // 100 / 20 = 5 дней
            Assert.That(result["wood"], Is.EqualTo(-1)); // ресурс производится
            Assert.That(result["water"], Is.EqualTo(10)); // 50 / 5 = 10 дней
        });
    }

    [Test]
    public void GetResourceForecast_ShouldIncludeCurrentAndDailyChange()
    {
        // Arrange
        _settlement.Resources["food"] = 100;
        _settlement.DailyResourceChanges["food"] = -10;

        // Act
        Dictionary<string, (float Current, float DailyChange)>? forecast = _resourceManager.GetResourceForecast();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(forecast.ContainsKey("food"), Is.True);
            Assert.That(forecast["food"].Current, Is.EqualTo(100));
            Assert.That(forecast["food"].DailyChange, Is.EqualTo(-10));
        });
    }

    [Test]
    public void ClearDailyChanges_ShouldResetDailyChanges()
    {
        // Arrange
        _settlement.DailyResourceChanges["food"] = -10;
        _settlement.DailyResourceChanges["wood"] = 5;

        // Act
        _resourceManager.ClearDailyChanges();

        // Assert
        Assert.That(_settlement.DailyResourceChanges, Is.Empty);
    }

    [Test]
    public void ResourcesChangedEvent_ShouldFire_WhenResourcesChange()
    {
        // Arrange
        var eventFired = false;
        Dictionary<string, float> eventResources = null;

        _resourceManager.ResourcesChanged += (sender, args) =>
        {
            eventFired = true;
            eventResources = args.CurrentResources;
        };

        // Act
        _resourceManager.AddResource("food", 10);

        // Assert
        Assert.That(eventFired, Is.True);
        Assert.That(eventResources, Is.Not.Null);
        Assert.That(eventResources["food"], Is.EqualTo(110));
    }

    [Test]
    public void AddResource_ShouldHandleNegativeAmounts()
    {
        // Arrange
        _settlement.Resources["food"] = 100;

        // Act
        _resourceManager.AddResource("food", -30);

        // Assert
        Assert.That(_settlement.Resources["food"], Is.EqualTo(70));
    }
}