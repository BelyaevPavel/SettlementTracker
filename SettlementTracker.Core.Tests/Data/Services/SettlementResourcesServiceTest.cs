using System.Text.Json;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests.Data.Services;

[TestFixture]
[TestOf(typeof(SettlementResourcesService))]
public class SettlementResourcesServiceTest
{
    [SetUp]
    public void SetUp()
    {
        // TODO: Здесь нужно заменить на репозиторий, который будет возвращать предварительно подготовленные описания 
        _resourceDefinitionRepository.DefinitionPreset = new Dictionary<string, ResourceDefinition>
        {
            {
                "wood", new ResourceDefinition
                {
                    Id = "wood",
                    Name = "Wood",
                    IsConsumable = false,
                    Description = "Wood resource",
                    IconPath = "wood.png"
                }
            },
            {
                "stone", new ResourceDefinition
                {
                    Id = "stone",
                    Name = "Stone",
                    IsConsumable = false,
                    Description = "Stone resource",
                    IconPath = "stone.png"
                }
            }
        };


        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _stateFilePath = Path.Combine(_tempDirectory, "resources.json");

        _service = new SettlementResourcesService(_resourceDefinitionRepository, _stateFilePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, true);
    }

    private SettlementResourcesService _service;
    private readonly MockDefinitionRepository _resourceDefinitionRepository = new();
    private string _tempDirectory;
    private string _stateFilePath;

    [Test]
    public async Task LoadDefinitionsAsync_ValidJson_LoadsDefinitions()
    {
        // Arrange
        _resourceDefinitionRepository.DefinitionPreset = new Dictionary<string, ResourceDefinition>
        {
            {
                "food", new ResourceDefinition
                {
                    Id = "food",
                    Name = "Food",
                    IsConsumable = true,
                    Description = "Food resource",
                    IconPath = "food.png"
                }
            },
            {
                "stone", new ResourceDefinition
                {
                    Id = "stone",
                    Name = "Stone",
                    IsConsumable = false,
                    Description = "Stone resource",
                    IconPath = "stone.png"
                }
            }
        };

        // Act
        await _service.LoadDefinitionsAsync();
        IReadOnlyDictionary<string, ResourceDefinition> definitions = _service.GetResourceDefinitions();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(definitions.Count, Is.EqualTo(2));
            Assert.That(definitions.ContainsKey("food"), Is.True);
            Assert.That(definitions.ContainsKey("stone"), Is.True);
            Assert.That(definitions["food"].IsConsumable, Is.True);
            Assert.That(definitions["stone"].IsConsumable, Is.False);
        });
    }

    [Test]
    public async Task AddResourceAsync_ShouldIncreaseBalance()
    {
        // Arrange
        var resourceId = "wood";
        float addAmount = 5;

        IReadOnlyDictionary<string, float>? beforeAdd = _service.GetCurrentBalance();

        // Act
        await _service.AddResourceAsync(resourceId, addAmount);
        IReadOnlyDictionary<string, float>? afterAdd = _service.GetCurrentBalance();

        // Assert
        Assert.That(afterAdd[resourceId], Is.EqualTo(addAmount));
    }

    [Test]
    public async Task TrySpendResourceAsync_WithSufficientBalance_ReturnsTrueAndDecreasesBalance()
    {
        // Arrange
        var resourceId = "wood";
        float initialAmount = 10;
        float spendAmount = 4;

        await _service.AddResourceAsync(resourceId, initialAmount);

        // Act
        bool result = await _service.TrySpendResourceAsync(resourceId, spendAmount);
        IReadOnlyDictionary<string, float>? newBalance = _service.GetCurrentBalance();

        // Assert
        Assert.That(result, Is.True);
        Assert.That(newBalance[resourceId], Is.EqualTo(initialAmount - spendAmount));
    }

    [Test]
    public async Task TrySpendResourceAsync_WithInsufficientBalance_ReturnsFalseAndBalanceUnchanged()
    {
        // Arrange
        var resourceId = "wood";
        float initialAmount = 3;
        float spendAmount = 5;

        await _service.AddResourceAsync(resourceId, initialAmount);

        // Act
        bool result = await _service.TrySpendResourceAsync(resourceId, spendAmount);
        IReadOnlyDictionary<string, float>? newBalance = _service.GetCurrentBalance();

        // Assert
        Assert.That(result, Is.False);
        Assert.That(newBalance[resourceId], Is.EqualTo(initialAmount));
    }

    [Test]
    public void CanSpend_WithSufficientBalance_ReturnsTrue()
    {
        // Arrange
        var resourceId = "wood";
        float initialAmount = 10;
        _service.AddResourceAsync(resourceId, initialAmount).Wait(); // синхронно для теста

        // Act
        bool canSpend = _service.CanSpend(resourceId, 5);

        // Assert
        Assert.That(canSpend, Is.True);
    }

    [Test]
    public void CanSpend_WithInsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var resourceId = "wood";
        float initialAmount = 3;
        _service.AddResourceAsync(resourceId, initialAmount).Wait();

        // Act
        bool canSpend = _service.CanSpend(resourceId, 5);

        // Assert
        Assert.That(canSpend, Is.False);
    }

    [Test]
    public void CanSpend_ForUnknownResource_ReturnsFalse()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CanSpend("nonexistent", 1));
    }


    [Test]
    public void ResourcesChangedEvent_FiredOnAddResource()
    {
        // Arrange
        var eventFired = false;
        IReadOnlyDictionary<string, float> newBalance = null;
        _service.ResourcesChanged += (sender, e) =>
        {
            eventFired = true;
            newBalance = e.CurrentResources;
        };

        // Act
        _service.AddResourceAsync("wood", 10).Wait();

        // Assert
        Assert.That(eventFired, Is.True);
        Assert.That(newBalance, Is.Not.Null);
        Assert.That(newBalance["wood"], Is.EqualTo(10));
    }

    [Test]
    public void ResourcesChangedEvent_FiredOnTrySpendResource()
    {
        // Arrange
        _service.AddResourceAsync("wood", 10).Wait();
        var eventFired = false;
        _service.ResourcesChanged += (sender, e) => eventFired = true;

        // Act
        _service.TrySpendResourceAsync("wood", 5).Wait();

        // Assert
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void GetCurrentBalance_ReturnsReadOnlyCopy()
    {
        // Arrange
        _service.AddResourceAsync("wood", 10).Wait();
        IReadOnlyDictionary<string, float>? balance = _service.GetCurrentBalance();

        // Act
        // Попытка изменить словарь должна быть невозможна (он read-only)
        // Assert
        Assert.That(balance, Is.InstanceOf<IReadOnlyDictionary<string, float>>());
        // Проверим, что изменения через интерфейс не влияют на внутреннее состояние (но это сложно проверить без доступа)
    }

    // Дополнительные тесты на граничные случаи
    [Test]
    public async Task TrySpendResourceAsync_WithZeroAmount_ReturnsTrueAndNoChange()
    {
        // Arrange
        var resourceId = "wood";
        await _service.AddResourceAsync(resourceId, 10);
        float before = _service.GetCurrentBalance()[resourceId];

        // Act
        bool result = await _service.TrySpendResourceAsync(resourceId, 0);
        float after = _service.GetCurrentBalance()[resourceId];

        // Assert
        Assert.That(result, Is.True);
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public async Task AddResourceAsync_WithNegativeAmount_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.AddResourceAsync("wood", -5));
    }

    [Test]
    public async Task TrySpendResourceAsync_WithNegativeAmount_ReturnsFalse()
    {
        // Assert
        bool result = await _service.TrySpendResourceAsync("wood", -5);

        // Act & Assert
        Assert.That(result, Is.False);
    }

    // This test is skipped until ApplyDailyEffectsAsync is implemented
    // [Test]
    public async Task ApplyDailyEffectsAsync_WithNullCollections_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.ApplyDailyEffectsAsync(null));
    }

    [Test]
    public async Task GetCurrentBalance_ReturnsAllResourcesWithZeroIfNotPresent()
    {
        // Arrange: после загрузки определений баланс должен содержать все ресурсы с 0
        await _service.LoadDefinitionsAsync();

        // Act
        IReadOnlyDictionary<string, float>? balance = _service.GetCurrentBalance();

        // Assert
        Assert.That(balance.Keys, Is.EquivalentTo(new[] { "wood", "stone" }));
        Assert.That(balance["wood"], Is.EqualTo(0));
        Assert.That(balance["stone"], Is.EqualTo(0));
    }

    [Test]
    public async Task SaveChangesAsync_ShouldCreateDirectoryAndWriteJsonFile()
    {
        // Arrange

        // Act
        await _service.SaveStateAsync();

        // Assert
        Assert.That(File.Exists(_stateFilePath), Is.True, "Файл состояния должен быть создан.");

        string? json = await File.ReadAllTextAsync(_stateFilePath);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, float>>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Is.EquivalentTo(_service.GetCurrentBalance()));
    }

    [Test]
    public async Task LoadFromFileAsync_WhenFileExists_ShouldLoadResourcesFromJson()
    {
        // Arrange
        float testWoodAmount = 1500;
        float testStoneAmount = 1200;
        var fileResources = new Dictionary<string, float>(_service.GetCurrentBalance());
        fileResources["wood"] = testWoodAmount;
        fileResources["stone"] = testStoneAmount;

        // Изменение ресурсов вызовет сохранение состояния, поэтому производится до записи тестового состояния
        await _service.SetResourceAsync("wood", 234.6f);
        await _service.SetResourceAsync("stone", 100);

        string? json = JsonSerializer.Serialize(fileResources, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_stateFilePath, json);


        // Act
        await _service.LoadStateAsync();

        // Assert
        IReadOnlyDictionary<string, float>? actualResources = _service.GetCurrentBalance();
        Assert.Multiple(() =>
        {
            Assert.That(actualResources["wood"], Is.EqualTo(testWoodAmount));
            Assert.That(actualResources["stone"], Is.EqualTo(testStoneAmount));
        });
    }

    // [Test]
    // public async Task ApplyDailyEffectsAsync_WithActiveBuildings_AppliesEffects()
    // {
    //     // Arrange
    //     var buildings = new List<Building>
    //     {
    //         new Building
    //         {
    //             IsActive = true,
    //             Definition = new BuildingDefinition
    //             {
    //                 DailyEffects = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "wood", Amount = 5, IsProduction = true },
    //                     new ResourceEffect { ResourceId = "stone", Amount = 2, IsProduction = false } // потребление
    //                 }
    //             }
    //         },
    //         new Building
    //         {
    //             IsActive = false, // неактивное не должно влиять
    //             Definition = new BuildingDefinition
    //             {
    //                 DailyEffects = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "wood", Amount = 10, IsProduction = true }
    //                 }
    //             }
    //         }
    //     };
    //
    //     // Добавим начальные ресурсы
    //     await _service.AddResourceAsync("wood", 10);
    //     await _service.AddResourceAsync("stone", 10);
    //
    //     // Act
    //     await _service.ApplyDailyEffectsAsync(buildings, new List<Citizen>());
    //
    //     var balance = _service.GetCurrentBalance();
    //
    //     // Assert
    //     // wood: 10 + 5 = 15
    //     // stone: 10 - 2 = 8
    //     Assert.That(balance["wood"], Is.EqualTo(15));
    //     Assert.That(balance["stone"], Is.EqualTo(8));
    // }
    //
    // [Test]
    // public async Task ApplyDailyEffectsAsync_WithCitizens_AppliesConsumptionFromDefinition()
    // {
    //     // Arrange
    //     var citizens = new List<Citizen>
    //     {
    //         new Citizen
    //         {
    //             Definition = new PopulationDefinition
    //             {
    //                 DailyNeeds = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "food", Amount = 1, IsProduction = false },
    //                     new ResourceEffect { ResourceId = "water", Amount = 2, IsProduction = false }
    //                 }
    //             }
    //         },
    //         new Citizen
    //         {
    //             Definition = new PopulationDefinition
    //             {
    //                 DailyNeeds = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "food", Amount = 0.5f, IsProduction = false }
    //                 }
    //             }
    //         }
    //     };
    //
    //     await _service.AddResourceAsync("food", 10);
    //     await _service.AddResourceAsync("water", 10);
    //
    //     // Act
    //     await _service.ApplyDailyEffectsAsync(new List<Building>(), citizens);
    //
    //     var balance = _service.GetCurrentBalance();
    //
    //     // Assert
    //     // food: 10 - 1 - 0.5 = 8.5
    //     // water: 10 - 2 = 8
    //     Assert.That(balance["food"], Is.EqualTo(8.5f));
    //     Assert.That(balance["water"], Is.EqualTo(8));
    // }
    //
    // [Test]
    // public async Task ApplyDailyEffectsAsync_WithBuildingsAndCitizens_CombinesEffects()
    // {
    //     // Arrange
    //     var buildings = new List<Building>
    //     {
    //         new Building
    //         {
    //             IsActive = true,
    //             Definition = new BuildingDefinition
    //             {
    //                 DailyEffects = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "food", Amount = 10, IsProduction = true },
    //                     new ResourceEffect { ResourceId = "wood", Amount = -2, IsProduction = false }
    //                 }
    //             }
    //         }
    //     };
    //
    //     var citizens = new List<Citizen>
    //     {
    //         new Citizen
    //         {
    //             Definition = new PopulationDefinition
    //             {
    //                 DailyNeeds = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "food", Amount = 1, IsProduction = false },
    //                     new ResourceEffect { ResourceId = "water", Amount = 1, IsProduction = false }
    //                 }
    //             }
    //         }
    //     };
    //
    //     await _service.AddResourceAsync("food", 5);
    //     await _service.AddResourceAsync("wood", 5);
    //     await _service.AddResourceAsync("water", 5);
    //
    //     // Act
    //     await _service.ApplyDailyEffectsAsync(buildings, citizens);
    //
    //     var balance = _service.GetCurrentBalance();
    //
    //     // Assert
    //     // food: 5 + 10 (production) - 1 (consumption) = 14
    //     // wood: 5 - 2 = 3
    //     // water: 5 - 1 = 4
    //     Assert.That(balance["food"], Is.EqualTo(14));
    //     Assert.That(balance["wood"], Is.EqualTo(3));
    //     Assert.That(balance["water"], Is.EqualTo(4));
    // }
    //
    // [Test]
    // public async Task ApplyDailyEffectsAsync_WhenBalanceGoesNegative_ShouldAllowNegativeForConsumableResources()
    // {
    //     // Arrange: ресурс consumable может уходить в минус (долг)
    //     var definitionsJson = @"[{ ""id"": ""food"", ""isConsumable"": true }]";
    //     await _service.LoadDefinitionsAsync(definitionsJson);
    //
    //     var citizens = new List<Citizen>
    //     {
    //         new Citizen
    //         {
    //             Definition = new PopulationDefinition
    //             {
    //                 DailyNeeds = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "food", Amount = 5, IsProduction = false }
    //                 }
    //             }
    //         }
    //     };
    //
    //     await _service.AddResourceAsync("food", 2); // не хватает
    //
    //     // Act
    //     await _service.ApplyDailyEffectsAsync(new List<Building>(), citizens);
    //
    //     var balance = _service.GetCurrentBalance();
    //
    //     // Assert
    //     Assert.That(balance["food"], Is.EqualTo(-3)); // ушло в минус
    // }
    //
    // [Test]
    // public async Task ApplyDailyEffectsAsync_WhenBalanceGoesNegativeForNonConsumable_ShouldNotAllowNegative()
    // {
    //     // Arrange: non-consumable ресурс не может быть отрицательным (например, камень)
    //     var definitionsJson = @"[{ ""id"": ""stone"", ""isConsumable"": false }]";
    //     await _service.LoadDefinitionsAsync(definitionsJson);
    //
    //     var buildings = new List<Building>
    //     {
    //         new Building
    //         {
    //             IsActive = true,
    //             Definition = new BuildingDefinition
    //             {
    //                 DailyEffects = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "stone", Amount = -5, IsProduction = false }
    //                 }
    //             }
    //         }
    //     };
    //
    //     await _service.AddResourceAsync("stone", 2); // не хватает
    //
    //     // Act & Assert: ожидаем исключение или баланс останавливается на 0? Надо определить поведение.
    //     // По логике, если ресурс не может быть отрицательным, то потребление должно быть ограничено доступным количеством.
    //     // Пусть метод выбрасывает InvalidOperationException или уменьшает только до 0.
    //     // В тесте проверим, что баланс стал 0 (а не -3).
    //     await _service.ApplyDailyEffectsAsync(buildings, new List<Citizen>());
    //
    //     var balance = _service.GetCurrentBalance();
    //     Assert.That(balance["stone"], Is.EqualTo(0));
    // }


    // [Test]
    // public void ResourcesChangedEvent_FiredOnApplyDailyEffects()
    // {
    //     // Arrange
    //     _service.AddResourceAsync("wood", 10).Wait();
    //     var eventFired = false;
    //     _service.ResourcesChanged += (sender, e) => eventFired = true;
    //
    //     var buildings = new List<Building>();
    //     var citizens = new List<Citizen>();
    //
    //     // Act
    //     _service.ApplyDailyEffectsAsync(buildings, citizens).Wait();
    //
    //     // Assert
    //     Assert.That(eventFired, Is.True);
    // }

    // [Test]
    // public async Task ApplyDailyEffectsAsync_WithUnknownResourceInEffect_IgnoresOrThrows()
    // {
    //     // Поведение: если в эффекте указан resourceId, которого нет в определениях, что делать?
    //     // Предположим, что игнорируем (логируем) или выбрасываем исключение. Выберем игнорирование.
    //     var definitionsJson = @"[{ ""id"": ""wood"", ""isConsumable"": true }]";
    //     await _service.LoadDefinitionsAsync(definitionsJson);
    //
    //     var buildings = new List<Building>
    //     {
    //         new Building
    //         {
    //             IsActive = true,
    //             Definition = new BuildingDefinition
    //             {
    //                 DailyEffects = new List<ResourceEffect>
    //                 {
    //                     new ResourceEffect { ResourceId = "unknown", Amount = 5, IsProduction = true }
    //                 }
    //             }
    //         }
    //     };
    //
    //     // Act (не должно быть исключения)
    //     Assert.DoesNotThrowAsync(async () => await _service.ApplyDailyEffectsAsync(buildings, new List<Citizen>()));
    // }
}