using System;
using System.Collections.Generic;
using System.Linq;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Managers
{
    public class PopulationManager
    {
        private readonly Random _random = new();
        private readonly SettlementState _settlement;

        public PopulationManager(SettlementState settlement)
        {
            _settlement = settlement;
        }

        // Вспомогательные свойства
        public PopulationDefinition Definition { get; set; }

        public Citizen AddCitizen(string name, Gender gender, int age, bool isSlave = false)
        {
            var citizen = new Citizen(Guid.NewGuid(), name, gender, age, isSlave);
            _settlement.Citizens.Add(citizen);
            return citizen;
        }

        public void AddRandomCitizen(bool isSlave = false)
        {
            Gender gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female;
            int age = isSlave
                ? _random.Next(15, 50)
                : // Рабы обычно взрослые
                GetRandomAgeByDistribution();

            string name = GenerateName(gender);
            AddCitizen(name, gender, age, isSlave);
        }

        public void AddFamily(int adultCount = 2, int childrenCount = 0)
        {
            // Добавляем взрослых
            for (var i = 0; i < adultCount; i++)
            {
                Gender gender = i % 2 == 0 ? Gender.Male : Gender.Female;
                int age = _random.Next(20, 50);
                AddCitizen(GenerateName(gender), gender, age);
            }

            // Добавляем детей
            for (var i = 0; i < childrenCount; i++)
            {
                Gender gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female;
                int age = _random.Next(0, 14);
                AddCitizen(GenerateName(gender), gender, age);
            }
        }

        public void RemoveCitizen(Guid citizenId)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            if (citizen != null)
            {
                // Снимаем с работы
                UnassignCitizen(citizenId);
                _settlement.Citizens.Remove(citizen);
            }
        }

        public void AgePopulation()
        {
            foreach (Citizen citizen in _settlement.Citizens)
            {
                citizen.AgeOneDay();

                // Случайная смерть (упрощенная модель)
                if (ShouldDieThisYear(citizen)) citizen.WorkStatus = WorkStatus.Idle; // Помечаем для удаления
            }

            // Удаляем умерших
            _settlement.Citizens.RemoveAll(c => c.WorkStatus == WorkStatus.Idle && ShouldRemove(c));
        }

        public List<ResourceEffect> CalculateDailyNeeds()
        {
            var needs = new List<ResourceEffect>();
            int livingCitizens = _settlement.Citizens.Count;

            // Рассчитываем общее потребление ресурсов
            // TODO: Нужен рефакторинг. PopulationDefinition нужно хранить в каком-то другом месте. Сейчас и здесь, и в Citizen. Можно забрать все методы по манипуляции возрастом из Citizen сюда, но, как-будто нарушает SRP.
            List<ResourceEffect> definitionDailyNeeds = Definition.DailyNeeds;
            if (definitionDailyNeeds != null)
                foreach (ResourceEffect need in definitionDailyNeeds)
                {
                    float totalAmount = need.Amount * livingCitizens;
                    needs.Add(new ResourceEffect
                    {
                        ResourceId = need.ResourceId,
                        Amount = totalAmount,
                        IsProduction = false
                    });
                }

            return needs;
        }

        public List<Citizen> FindAvailableWorkers(WorkerRequirement requirement)
        {
            return _settlement.Citizens.Where(c =>
                c.AgeCategory == requirement.AgeCategory &&
                c.Gender == requirement.Gender &&
                c.WorkStatus == WorkStatus.Idle &&
                (!requirement.CanBeSlave || c.IsSlave) &&
                (!c.IsSlave || requirement.CanBeSlave)
            ).ToList();
        }

        public void AssignCitizenToBuilding(Guid citizenId, Guid buildingId)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            Building building = _settlement.Buildings.FirstOrDefault(b => b.Id == buildingId);

            if (citizen != null && building != null)
            {
                // Снимаем с предыдущего задания
                UnassignCitizen(citizenId);

                citizen.WorkStatus = WorkStatus.AssignedToBuilding;
                citizen.AssignedToId = buildingId;
                citizen.AssignedToType = "Building";

                building.AssignCitizen(citizenId);
            }
        }

        public void AssignCitizenToJob(Guid citizenId, Guid jobId)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            ActiveJob job = _settlement.ActiveJobs.FirstOrDefault(j => j.Id == jobId);

            if (citizen != null && job != null)
            {
                // Снимаем с предыдущего задания
                UnassignCitizen(citizenId);

                citizen.WorkStatus = WorkStatus.AssignedToJob;
                citizen.AssignedToId = jobId;
                citizen.AssignedToType = "Job";

                job.AssignCitizen(citizenId);
            }
        }

        public void UnassignCitizen(Guid citizenId)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            if (citizen == null) return;

            switch (citizen.WorkStatus)
            {
                case WorkStatus.AssignedToBuilding:
                    Building building = _settlement.Buildings.FirstOrDefault(b => b.Id == citizen.AssignedToId);
                    building?.UnassignCitizen(citizenId);
                    break;

                case WorkStatus.AssignedToJob:
                    ActiveJob job = _settlement.ActiveJobs.FirstOrDefault(j => j.Id == citizen.AssignedToId);
                    job?.UnassignCitizen(citizenId);
                    break;
            }

            citizen.WorkStatus = WorkStatus.Idle;
            citizen.AssignedToId = null;
            citizen.AssignedToType = string.Empty;
            citizen.GuardianId = null;
        }

        public void AssignCitizenToJob(Guid citizenId, Guid jobId, Guid? guardianId = null)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            ActiveJob job = _settlement.ActiveJobs.FirstOrDefault(j => j.Id == jobId);

            if (citizen == null) return;
            if (job == null) return;

            citizen.WorkStatus = WorkStatus.AssignedToJob;

            citizen.AssignedToId = jobId;
            citizen.AssignedToType = WorkStatus.AssignedToJob.ToString();
            citizen.GuardianId = guardianId;

            job.AssignCitizen(citizenId);
        }

        public void AssignCitizenToBuilding(Guid citizenId, Guid buildingId, Guid? guardianId = null)
        {
            Citizen citizen = _settlement.Citizens.FirstOrDefault(c => c.Id == citizenId);
            Building building = _settlement.Buildings.FirstOrDefault(j => j.Id == buildingId);

            if (citizen == null) return;
            if (building == null) return;

            citizen.WorkStatus = WorkStatus.AssignedToJob;

            citizen.AssignedToId = buildingId;
            citizen.AssignedToType = WorkStatus.AssignedToJob.ToString();
            citizen.GuardianId = guardianId;

            building.AssignCitizen(citizenId);
        }

        public PopulationStatistics GetPopulationStatistics()
        {
            var stats = new PopulationStatistics();

            foreach (Citizen citizen in _settlement.Citizens)
            {
                (AgeCategory AgeCategory, Gender Gender, bool IsSlave) key = (citizen.AgeCategory, citizen.Gender,
                    citizen.IsSlave);

                if (!stats.Counts.ContainsKey(key)) stats.Counts[key] = 0;

                stats.Counts[key]++;

                // Считаем работающих
                if (citizen.WorkStatus != WorkStatus.Idle)
                {
                    if (!stats.WorkingCounts.ContainsKey(key)) stats.WorkingCounts[key] = 0;

                    stats.WorkingCounts[key]++;
                }
            }

            stats.TotalPopulation = _settlement.Citizens.Count;
            stats.TotalWorking = _settlement.Citizens.Count(c => c.WorkStatus != WorkStatus.Idle);

            return stats;
        }

        private int GetRandomAgeByDistribution()
        {
            // Упрощенное распределение возрастов
            double r = _random.NextDouble();
            if (r < 0.25) return _random.Next(0, 14); // 25% дети
            if (r < 0.85) return _random.Next(15, 59); // 60% взрослые
            return _random.Next(60, 90); // 15% старики
        }

        private string GenerateName(Gender gender)
        {
            var maleNames = new[] { "Иван", "Петр", "Алексей", "Михаил", "Дмитрий" };
            var femaleNames = new[] { "Мария", "Анна", "Екатерина", "Ольга", "Наталья" };

            string[] names = gender == Gender.Male ? maleNames : femaleNames;
            return names[_random.Next(names.Length)];
        }

        private bool ShouldDieThisYear(Citizen citizen)
        {
            return citizen.Age > citizen.Definition.ElderMaxAge;
        }

        private bool ShouldRemove(Citizen citizen)
        {
            return citizen.Age > 90 || _random.NextDouble() < 0.1;
        }

        public void InitializeDefinitions(PopulationDefinition populationDefinition)
        {
            Definition = populationDefinition;
        }
    }
}