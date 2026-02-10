using System;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Enums;

namespace SettlementTracker.Core.Models.Entities
{
    public class Citizen
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public float Age { get; set; } // В годах
        public AgeCategory AgeCategory => CalculateAgeCategory();
        public bool IsSlave { get; set; }
        public WorkStatus WorkStatus { get; set; } = WorkStatus.Idle;
        public Guid? AssignedToId { get; set; } // ID здания или задания
        public string AssignedToType { get; set; } = string.Empty; // "Building" или "Job"
        public Guid? GuardianId { get; set; } // ID опекуна (для детей и рабов без присмотра)


        // Вспомогательные свойства (будут заполняться из Definition)
        [System.Text.Json.Serialization.JsonIgnore]
        public PopulationDefinition Definition { get; set; }

        // Эффективность работы (рассчитывается каждый день)
        public float CurrentEfficiency { get; set; } = 1.0f;

        public Citizen(Guid id, string name, Gender gender, float age, bool isSlave = false)
        {
            Id = id;
            Name = name;
            Gender = gender;
            Age = age;
            IsSlave = isSlave;
        }

        public void AgeOneDay()
        {
            Age += Definition.AgePerDay;
        }

        public AgeCategory CalculateAgeCategory()
        {
            if (Age < Definition.ChildMaxAge)
                return AgeCategory.Child;
            else if (Age < Definition.AdultMaxAge)
                return AgeCategory.Adult;
            else
                return AgeCategory.Elder;
        }

        public string GetEfficiencyKey()
        {
            return $"{AgeCategory}_{Gender}_{IsSlave}";
        }
    }
}