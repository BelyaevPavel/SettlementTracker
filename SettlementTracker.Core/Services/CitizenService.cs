using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Services
{
    public class CitizenService : ICitizenService
    {
        private readonly PopulationDefinition _definition;
        private readonly string _definitionFilePath = "Data/populationDefinition.json";
        private readonly string _stateFilePath = "State/citizens.json";
        private List<Citizen> _citizens = new();

        public CitizenService()
        {
            // Загружаем определение населения
            string definitionJson = File.ReadAllText(_definitionFilePath);
            _definition = JsonSerializer.Deserialize<PopulationDefinition>(definitionJson)
                          ?? new PopulationDefinition();
        }

        public async Task<IEnumerable<Citizen>> GetCitizensAsync()
        {
            return await Task.FromResult(_citizens);
        }

        public async Task<Citizen?> GetCitizenAsync(Guid id)
        {
            return await Task.FromResult(_citizens.FirstOrDefault(c => c.Id == id));
        }

        public async Task AddCitizenAsync(Citizen citizen)
        {
            citizen.Definition = _definition;
            citizen.Id = Guid.NewGuid();
            _citizens.Add(citizen);
            await SaveChangesAsync();
            OnCitizensChanged();
        }

        public async Task UpdateCitizenAsync(Citizen citizen)
        {
            Citizen existing = await GetCitizenAsync(citizen.Id);
            if (existing != null)
            {
                _citizens.Remove(existing);
                citizen.Definition = _definition;
                _citizens.Add(citizen);
                await SaveChangesAsync();
                OnCitizensChanged();
            }
        }

        public async Task DeleteCitizenAsync(Guid id)
        {
            Citizen citizen = await GetCitizenAsync(id);
            if (citizen != null)
            {
                _citizens.Remove(citizen);
                await SaveChangesAsync();
                OnCitizensChanged();
            }
        }

        public async Task SaveChangesAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_citizens, options);

            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
            await File.WriteAllTextAsync(_stateFilePath, json);
        }

        public async Task LoadFromFileAsync()
        {
            if (File.Exists(_stateFilePath))
            {
                string json = await File.ReadAllTextAsync(_stateFilePath);
                List<Citizen> citizens = JsonSerializer.Deserialize<List<Citizen>>(json) ?? new List<Citizen>();

                foreach (Citizen citizen in citizens) citizen.Definition = _definition;

                _citizens = citizens;
            }
        }

        public event EventHandler? CitizensChange;

        private void OnCitizensChanged()
        {
            CitizensChange?.Invoke(this, EventArgs.Empty);
        }
    }
}