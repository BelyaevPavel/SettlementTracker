using System;
using System.Collections.Generic;
using System.Linq;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Services
{
    public class EfficiencyCalculator
    {
        private readonly SettlementState _settlement;

        public EfficiencyCalculator(SettlementState settlement)
        {
            _settlement = settlement;
        }

        public float CalculateCitizenEfficiency(Citizen citizen, JobDefinition? jobDefinition = null)
        {
            var efficiency = 1.0f;

            // Базовая эффективность по возрасту/полу
            if (jobDefinition != null &&
                jobDefinition.BaseEfficiency.TryGetValue(citizen.GetEfficiencyKey(), out float baseEff))
                efficiency = baseEff;

            // Модификаторы для детей и рабов без присмотра
            if ((citizen.AgeCategory == AgeCategory.Child || citizen.IsSlave) && citizen.GuardianId == null)
                efficiency *= 0.5f; // Эффективность падает на 50% без присмотра

            // Модификатор для стариков
            if (citizen.AgeCategory == AgeCategory.Elder) efficiency *= 0.7f; // Старики работают менее эффективно

            return Math.Max(0.1f, efficiency); // Минимальная эффективность 10%
        }

        public Dictionary<Guid, float> CalculateWorkGroupEfficiencies(List<Guid> citizenIds,
            JobDefinition? jobDefinition = null)
        {
            var efficiencies = new Dictionary<Guid, float>();

            // Определяем, есть ли взрослые/старики для присмотра
            bool hasSupervisor = citizenIds.Any(id =>
            {
                Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == id);
                return citizen != null &&
                       (citizen.AgeCategory == AgeCategory.Adult || citizen.AgeCategory == AgeCategory.Elder) &&
                       !citizen.IsSlave;
            });

            foreach (Guid citizenId in citizenIds)
            {
                Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
                if (citizen == null) continue;

                // Если есть присмотр, назначаем опекуна детям и рабам
                if (hasSupervisor && (citizen.AgeCategory == AgeCategory.Child || citizen.IsSlave))
                {
                    // Назначаем первого подходящего опекуна
                    Citizen guardian = citizenIds
                        .Select(id => _settlement.Citizens.FirstOrDefault(c => c.Id == id))
                        .FirstOrDefault(c => c != null &&
                                             (c.AgeCategory == AgeCategory.Adult ||
                                              c.AgeCategory == AgeCategory.Elder) &&
                                             !c.IsSlave);

                    if (guardian != null) citizen.GuardianId = guardian.Id;
                }

                efficiencies[citizenId] = CalculateCitizenEfficiency(citizen, jobDefinition);
            }

            return efficiencies;
        }

        public float CalculateTotalEfficiency(List<Guid> citizenIds, JobDefinition? jobDefinition = null)
        {
            Dictionary<Guid, float> efficiencies = CalculateWorkGroupEfficiencies(citizenIds, jobDefinition);
            return efficiencies.Values.Sum();
        }
    }

    public class DayAdvancedEventArgs : EventArgs
    {
        public DayAdvancedEventArgs(int day, DailySummary summary)
        {
            Day = day;
            Summary = summary;
        }

        public int Day { get; }
        public DailySummary Summary { get; }
    }
}