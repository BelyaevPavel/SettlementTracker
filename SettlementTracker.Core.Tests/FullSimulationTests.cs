using System.Text.Json;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Tests;

// [TestFixture]
public class FullSimulationTests
{
    private SettlementGame _game;
    private string _testDataPath;
    private string _testSavePath;

    [SetUp]
    public void Setup()
    {
        // Создаем временные директории для тестов
        _testDataPath = Path.Combine(Path.GetTempPath(), "SettlementTestData");
        _testSavePath = Path.Combine(Path.GetTempPath(), "SettlementTestSaves");

        Directory.CreateDirectory(_testDataPath);
        Directory.CreateDirectory(_testSavePath);

        // Создаем тестовые файлы определений
        CreateTestDefinitionFiles();

        _game = new SettlementGame(_testDataPath, _testSavePath);
    }

    [TearDown]
    public void TearDown()
    {
        _game?.Dispose();

        // Очищаем временные файлы
        try
        {
            Directory.Delete(_testDataPath, true);
            Directory.Delete(_testSavePath, true);
        }
        catch
        {
            /* Игнорируем ошибки очистки */
        }
    }

    // [Test]
    public void FullGameFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var settlementName = "Integration Test Settlement";

        // Act - Создаем новую игру
        _game.StartNewGame(settlementName);

        SettlementState? settlement = _game.Settlement;

        // Проверяем начальное состояние
        Assert.That(settlement.Name, Is.EqualTo(settlementName));
        Assert.That(settlement.Citizens.Count, Is.GreaterThan(0));
        Assert.That(settlement.Resources["food"], Is.GreaterThan(0));

        // Строим здание
        BuildingManager? buildingManager = _game.Simulator.GetBuildingManager();
        Building? building = buildingManager.BuildBuilding("house", (1, 1));

        Assert.That(building, Is.Not.Null);
        Assert.That(settlement.Buildings.Count, Is.EqualTo(1));

        // Создаем задание
        JobManager? jobManager = _game.Simulator.GetJobManager();
        ActiveJob? job = jobManager.CreateJob("hunting");

        Assert.That(job, Is.Not.Null);
        Assert.That(settlement.ActiveJobs.Count, Is.EqualTo(1));

        // Назначаем работников
        PopulationManager? populationManager = _game.Simulator.GetPopulationManager();
        Citizen? availableAdultMale = settlement.Citizens
            .First(c => c.AgeCategory == AgeCategory.Adult &&
                        c.Gender == Gender.Male &&
                        c.WorkStatus == WorkStatus.Idle);

        populationManager.AssignCitizenToJob(availableAdultMale.Id, job.Id);

        Assert.That(availableAdultMale.WorkStatus, Is.EqualTo(WorkStatus.AssignedToJob));
        Assert.That(job.AssignedCitizenIds.Contains(availableAdultMale.Id), Is.True);

        // Сохраняем игру
        _game.SaveGame("integration_test.json");
        Assert.That(File.Exists(Path.Combine(_testSavePath, "integration_test.json")), Is.True);

        // Прогрессируем день
        var dayEvents = 0;
        _game.DayAdvanced += (sender, args) => dayEvents++;

        int initialDay = settlement.CurrentDay;
        float initialFood = settlement.Resources["food"];

        _game.AdvanceDay();

        // Проверяем результаты
        Assert.Multiple(() =>
        {
            Assert.That(settlement.CurrentDay, Is.EqualTo(initialDay + 1));
            Assert.That(dayEvents, Is.EqualTo(1));
            // Еда должна измениться (потребление населением + производство охотой)
            Assert.That(settlement.Resources["food"], Is.Not.EqualTo(initialFood));
        });

        // Загружаем игру
        var newGame = new SettlementGame(_testDataPath, _testSavePath);
        bool loaded = newGame.LoadGame("integration_test.json");

        Assert.That(loaded, Is.True);
        Assert.That(newGame.Settlement.Name, Is.EqualTo(settlementName));
        Assert.That(newGame.Settlement.CurrentDay, Is.EqualTo(settlement.CurrentDay));
    }

    // [Test]
    public void EfficiencyCalculation_IntegrationTest()
    {
        // Arrange
        _game.StartNewGame("Efficiency Test");
        SettlementState? settlement = _game.Settlement;

        // Добавляем разнообразное население
        PopulationManager? populationManager = _game.Simulator.GetPopulationManager();

        Citizen? adultMale = populationManager.AddCitizen("Worker1", Gender.Male, 25);
        Citizen? adultFemale = populationManager.AddCitizen("Worker2", Gender.Female, 30);
        Citizen? child = populationManager.AddCitizen("Child", Gender.Male, 10);
        Citizen? slave = populationManager.AddCitizen("Slave", Gender.Male, 35, true);
        Citizen? elder = populationManager.AddCitizen("Elder", Gender.Female, 70);

        // Создаем ферму
        BuildingManager? buildingManager = _game.Simulator.GetBuildingManager();
        Building? farm = buildingManager.BuildBuilding("farm", (2, 2));

        Assert.That(farm, Is.Not.Null);

        // Назначаем работников на ферму (требуется 1 мужчина и 1 женщина)
        populationManager.AssignCitizenToBuilding(adultMale.Id, farm.Id);
        populationManager.AssignCitizenToBuilding(adultFemale.Id, farm.Id);

        // Добавляем еще работников для теста эффективности группы
        populationManager.AssignCitizenToBuilding(child.Id, farm.Id);
        populationManager.AssignCitizenToBuilding(slave.Id, farm.Id);

        float initialFood = settlement.Resources["food"];
        float initialWater = settlement.Resources["water"];

        // Act - Прогрессируем день
        _game.AdvanceDay();

        // Assert
        // Проверяем, что эффективность была рассчитана и применена
        Dictionary<string, float>? dailyChanges = _game.Simulator.GetResourceManager().GetDailyResourceChanges();

        Assert.Multiple(() =>
        {
            Assert.That(dailyChanges.ContainsKey("food"), Is.True);
            Assert.That(dailyChanges.ContainsKey("water"), Is.True);

            // Ферма производит еду и потребляет воду
            // С учетом эффективности работы группы
            Assert.That(dailyChanges["food"], Is.GreaterThan(0));
            Assert.That(dailyChanges["water"], Is.LessThan(0));
        });
    }

    //   [Test]
    public void ResourceShortage_ShouldAffectSimulation()
    {
        // Arrange
        _game.StartNewGame("Shortage Test");
        SettlementState? settlement = _game.Settlement;

        // Создаем ситуацию с нехваткой ресурсов
        settlement.Resources["food"] = 1; // Очень мало еды
        settlement.Resources["water"] = 1; // Очень мало воды

        int populationCount = settlement.Citizens.Count;

        // Act - Прогрессируем несколько дней
        for (var i = 0; i < 3; i++) _game.AdvanceDay();

        // Assert
        // Ресурсы должны быть исчерпаны или быть отрицательными
        // (в зависимости от реализации логики голода)
        Assert.That(settlement.Resources["food"], Is.LessThanOrEqualTo(0));
        Assert.That(settlement.Resources["water"], Is.LessThanOrEqualTo(0));
    }

    private void CreateTestDefinitionFiles()
    {
        // Ресурсы
        Dictionary<string, ResourceDefinition>? resources = TestDataGenerator.CreateResourceDefinitions();
        string? resourcesJson = JsonSerializer.Serialize(resources.Values,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "resources.json"), resourcesJson);

        // Здания
        Dictionary<string, BuildingDefinition>? buildings = TestDataGenerator.CreateBuildingDefinitions();
        string? buildingsJson = JsonSerializer.Serialize(buildings.Values,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "buildings.json"), buildingsJson);

        // Задания
        Dictionary<string, JobDefinition>? jobs = TestDataGenerator.CreateJobDefinitions();
        string? jobsJson = JsonSerializer.Serialize(jobs.Values,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "jobs.json"), jobsJson);
    }
}