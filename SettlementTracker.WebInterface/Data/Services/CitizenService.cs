using SettlementTracker.Core;
using System.Text.Json;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.WebInterface.Data.Services;


public class CitizenService : ICitizenService
{
    private List<Citizen> _citizens = new();
    private readonly string _filePath = "Data/citizens.json";
    private readonly PopulationDefinition _definition;

    public CitizenService()
    {
        // Загружаем определение населения
        var definitionJson = File.ReadAllText("Data/populationDefinition.json");
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
        var existing = await GetCitizenAsync(citizen.Id);
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
        var citizen = await GetCitizenAsync(id);
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
        var json = JsonSerializer.Serialize(_citizens, options);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task LoadFromFileAsync()
    {
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var citizens = JsonSerializer.Deserialize<List<Citizen>>(json) ?? new List<Citizen>();

            foreach (var citizen in citizens)
            {
                citizen.Definition = _definition;
            }

            _citizens = citizens;
        }
    }

    public event EventHandler? CitizensChange;

    private void OnCitizensChanged()
    {
        CitizensChange?.Invoke(this, EventArgs.Empty);
    }
}