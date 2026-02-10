using System;
using System.Collections.Generic;
using System.Linq;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Managers
{
    public class JobManager
    {
        private readonly SettlementState _settlement;
        private Dictionary<string, JobDefinition> _jobDefinitions = new();

        public JobManager(SettlementState settlement)
        {
            _settlement = settlement;
        }

        public void InitializeDefinitions(Dictionary<string, JobDefinition> jobDefinitions)
        {
            _jobDefinitions = jobDefinitions;

            // Связываем определения с существующими заданиями
            foreach (ActiveJob job in _settlement.ActiveJobs)
                if (_jobDefinitions.TryGetValue(job.DefinitionId, out JobDefinition definition))
                    job.Definition = definition;
        }

        public ActiveJob? CreateJob(string definitionId, string customName = "")
        {
            if (!_jobDefinitions.TryGetValue(definitionId, out JobDefinition definition))
                return null;

            var job = new ActiveJob(Guid.NewGuid(), definitionId)
            {
                Name = string.IsNullOrEmpty(customName) ? definition.Name : customName,
                Definition = definition
            };

            _settlement.ActiveJobs.Add(job);
            return job;
        }

        public void RemoveJob(Guid jobId)
        {
            ActiveJob job = _settlement.ActiveJobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null) return;

            // Освобождаем работников
            foreach (Guid citizenId in job.AssignedCitizenIds.ToList())
            {
                var populationManager = new PopulationManager(_settlement);
                populationManager.UnassignCitizen(citizenId);
            }

            _settlement.ActiveJobs.Remove(job);
        }

        public List<ActiveJob> GetActiveJobs()
        {
            return _settlement.ActiveJobs.ToList();
        }

        public bool AreWorkerRequirementsMet(ActiveJob job)
        {
            if (job.Definition == null) return false;

            List<Citizen> assignedCitizens = job.AssignedCitizenIds
                .Select(id => _settlement.Citizens.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .ToList();

            foreach (WorkerRequirement requirement in job.Definition.WorkerRequirements)
            {
                int matchingWorkers = assignedCitizens.Count(c =>
                    c!.AgeCategory == requirement.AgeCategory &&
                    c.Gender == requirement.Gender &&
                    (requirement.CanBeSlave || !c.IsSlave));

                if (matchingWorkers < requirement.Count)
                    return false;
            }

            return true;
        }
    }
}