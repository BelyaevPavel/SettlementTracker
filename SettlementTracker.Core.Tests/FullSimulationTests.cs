using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Tests;

[TestFixture]
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

    [Test]
    public void FullGameFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var settlementName = "Integration Test Settlement";

        // Act - Создаем новую игру
        _game.StartNewGame(settlementName);

        var settlement = _game.Settlement;

        // Проверяем начальное состояние
        Assert.That(settlement.Name, Is.EqualTo(settlementName));
        Assert.That(settlement.Citizens.Count, Is.GreaterThan(0));
        Assert.That(settlement.Resources["food"], Is.GreaterThan(0));

        // Строим здание
        var buildingManager = _game.Simulator.GetBuildingManager();
        var building = buildingManager.BuildBuilding("house", (1, 1));

        Assert.That(building, Is.Not.Null);
        Assert.That(settlement.Buildings.Count, Is.EqualTo(1));

        // Создаем задание
        var jobManager = _game.Simulator.GetJobManager();
        var job = jobManager.CreateJob("hunting");

        Assert.That(job, Is.Not.Null);
        Assert.That(settlement.ActiveJobs.Count, Is.EqualTo(1));

        // Назначаем работников
        var populationManager = _game.Simulator.GetPopulationManager();
        var availableAdultMale = settlement.Citizens
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

        var initialDay = settlement.CurrentDay;
        var initialFood = settlement.Resources["food"];

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
        var loaded = newGame.LoadGame("integration_test.json");

        Assert.That(loaded, Is.True);
        Assert.That(newGame.Settlement.Name, Is.EqualTo(settlementName));
        Assert.That(newGame.Settlement.CurrentDay, Is.EqualTo(settlement.CurrentDay));
    }

    [Test]
    public void EfficiencyCalculation_IntegrationTest()
    {
        // Arrange
        _game.StartNewGame("Efficiency Test");
        var settlement = _game.Settlement;

        // Добавляем разнообразное население
        var populationManager = _game.Simulator.GetPopulationManager();

        var adultMale = populationManager.AddCitizen("Worker1", Gender.Male, 25);
        var adultFemale = populationManager.AddCitizen("Worker2", Gender.Female, 30);
        var child = populationManager.AddCitizen("Child", Gender.Male, 10);
        var slave = populationManager.AddCitizen("Slave", Gender.Male, 35, true);
        var elder = populationManager.AddCitizen("Elder", Gender.Female, 70);

        // Создаем ферму
        var buildingManager = _game.Simulator.GetBuildingManager();
        var farm = buildingManager.BuildBuilding("farm", (2, 2));

        Assert.That(farm, Is.Not.Null);

        // Назначаем работников на ферму (требуется 1 мужчина и 1 женщина)
        populationManager.AssignCitizenToBuilding(adultMale.Id, farm.Id);
        populationManager.AssignCitizenToBuilding(adultFemale.Id, farm.Id);

        // Добавляем еще работников для теста эффективности группы
        populationManager.AssignCitizenToBuilding(child.Id, farm.Id);
        populationManager.AssignCitizenToBuilding(slave.Id, farm.Id);

        var initialFood = settlement.Resources["food"];
        var initialWater = settlement.Resources["water"];

        // Act - Прогрессируем день
        _game.AdvanceDay();

        // Assert
        // Проверяем, что эффективность была рассчитана и применена
        var dailyChanges = _game.Simulator.GetResourceManager().GetDailyResourceChanges();

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

    [Test]
    public void ResourceShortage_ShouldAffectSimulation()
    {
        // Arrange
        _game.StartNewGame("Shortage Test");
        var settlement = _game.Settlement;

        // Создаем ситуацию с нехваткой ресурсов
        settlement.Resources["food"] = 1; // Очень мало еды
        settlement.Resources["water"] = 1; // Очень мало воды

        var populationCount = settlement.Citizens.Count;

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
        var resources = TestDataGenerator.CreateResourceDefinitions();
        var resourcesJson = System.Text.Json.JsonSerializer.Serialize(resources.Values,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "resources.json"), resourcesJson);

        // Здания
        var buildings = TestDataGenerator.CreateBuildingDefinitions();
        var buildingsJson = System.Text.Json.JsonSerializer.Serialize(buildings.Values,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "buildings.json"), buildingsJson);

        // Задания
        var jobs = TestDataGenerator.CreateJobDefinitions();
        var jobsJson = System.Text.Json.JsonSerializer.Serialize(jobs.Values,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_testDataPath, "jobs.json"), jobsJson);
    }
}