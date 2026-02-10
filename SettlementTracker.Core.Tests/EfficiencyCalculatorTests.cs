using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests;

[TestFixture]
public class EfficiencyCalculatorTests
{
    private SettlementState _settlement;
    private EfficiencyCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _settlement = TestDataGenerator.CreateTestSettlement();
        _calculator = new EfficiencyCalculator(_settlement);
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldReturnBaseEfficiency_ForAdultMaleWithoutJobDefinition()
    {
        // Arrange
        var adultMale = TestDataGenerator.CreateAdult();

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(adultMale);

        // Assert
        Assert.That(efficiency, Is.EqualTo(1.0m));
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldApplyJobSpecificEfficiency()
    {
        // Arrange
        var adultMale = TestDataGenerator.CreateAdult();
        var jobDefinition = new JobDefinition
        {
            BaseEfficiency = new Dictionary<string, float>
            {
                ["Adult_Male_False"] = 1.2f,
                ["Adult_Female_False"] = 0.9f
            }
        };

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(adultMale, jobDefinition);

        // Assert
        Assert.That(efficiency, Is.EqualTo(1.2f));
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldReduceEfficiencyForUnsupervisedChild()
    {
        // Arrange
        var child = TestDataGenerator.CreateChild();

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(child);

        // Assert
        Assert.That(efficiency, Is.EqualTo(0.5m)); // 50% без присмотра
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldReduceEfficiencyForUnsupervisedSlave()
    {
        // Arrange
        var slave = TestDataGenerator.CreateAdult(isSlave: true);

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(slave);

        // Assert
        Assert.That(efficiency, Is.EqualTo(0.5m)); // 50% без присмотра
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldReduceEfficiencyForElder()
    {
        // Arrange
        var elder = TestDataGenerator.CreateElder();

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(elder);

        // Assert
        Assert.That(efficiency, Is.EqualTo(0.7m)); // 70% для стариков
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldApplyMultipleModifiers()
    {
        // Arrange
        var elderSlave = TestDataGenerator.CreateElder(isSlave: true);

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(elderSlave);

        // Assert
        // Старик (0.7) × без присмотра (0.5) = 0.35
        Assert.That(efficiency, Is.EqualTo(0.35m));
    }

    [Test]
    public void CalculateWorkGroupEfficiencies_ShouldAssignGuardianToChildren()
    {
        // Arrange
        var adult = TestDataGenerator.CreateAdult();
        var child = TestDataGenerator.CreateChild();

        _settlement.Citizens.Add(adult);
        _settlement.Citizens.Add(child);

        var citizenIds = new List<Guid> { adult.Id, child.Id };

        // Act
        var efficiencies = _calculator.CalculateWorkGroupEfficiencies(citizenIds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(child.GuardianId, Is.EqualTo(adult.Id));
            Assert.That(efficiencies[child.Id], Is.EqualTo(1.0m)); // 100% с присмотром
            Assert.That(efficiencies[adult.Id], Is.EqualTo(1.0m));
        });
    }

    [Test]
    public void CalculateWorkGroupEfficiencies_ShouldNotAssignGuardian_WhenNoSupervisor()
    {
        // Arrange
        var child1 = TestDataGenerator.CreateChild();
        var child2 = TestDataGenerator.CreateChild();

        _settlement.Citizens.Add(child1);
        _settlement.Citizens.Add(child2);

        var citizenIds = new List<Guid> { child1.Id, child2.Id };

        // Act
        var efficiencies = _calculator.CalculateWorkGroupEfficiencies(citizenIds);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(child1.GuardianId, Is.Null);
            Assert.That(child2.GuardianId, Is.Null);
            Assert.That(efficiencies[child1.Id], Is.EqualTo(0.5m)); // 50% без присмотра
            Assert.That(efficiencies[child2.Id], Is.EqualTo(0.5m));
        });
    }

    [Test]
    public void CalculateTotalEfficiency_ShouldSumAllEfficiencies()
    {
        // Arrange
        var adult1 = TestDataGenerator.CreateAdult(Gender.Male);
        var adult2 = TestDataGenerator.CreateAdult(Gender.Female);
        var child = TestDataGenerator.CreateChild();

        _settlement.Citizens.AddRange(new[] { adult1, adult2, child });

        var citizenIds = new List<Guid> { adult1.Id, adult2.Id, child.Id };

        // Act
        var totalEfficiency = _calculator.CalculateTotalEfficiency(citizenIds);

        // Assert
        // adult1: 1.0, adult2: 1.0, child: 1.0 (с присмотром) = 3.0
        Assert.That(totalEfficiency, Is.EqualTo(3.0m));
    }

    [Test]
    public void CalculateCitizenEfficiency_ShouldNotGoBelowMinimum()
    {
        // Arrange
        var childSlave = TestDataGenerator.CreateChild(isSlave: true);

        // Act
        var efficiency = _calculator.CalculateCitizenEfficiency(childSlave);

        // Assert
        // Ребенок (1.0) × раб без присмотра (0.5) = 0.5, но минимально 0.1
        Assert.That(efficiency, Is.EqualTo(0.5f)); // В данном случае 0.5 > 0.1
    }

    [Test]
    public void Citizen_GetEfficiencyKey_ShouldGenerateCorrectKey()
    {
        // Arrange
        var adultMale = TestDataGenerator.CreateAdult(Gender.Male);
        var adultFemaleSlave = TestDataGenerator.CreateAdult(Gender.Female, true);
        var elderFemale = TestDataGenerator.CreateElder(Gender.Female);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(adultMale.GetEfficiencyKey(), Is.EqualTo("Adult_Male_False"));
            Assert.That(adultFemaleSlave.GetEfficiencyKey(), Is.EqualTo("Adult_Female_True"));
            Assert.That(elderFemale.GetEfficiencyKey(), Is.EqualTo("Elder_Female_False"));
        });
    }
}