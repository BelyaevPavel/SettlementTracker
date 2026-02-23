using System.Text.Json;
using SettlementTracker.Core.Managers;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Repositories;

namespace SettlementTracker.WebInterface.Data.Services;

public class SettlementResourcesService : ISettlementResourcesService
{
    private readonly IResourceDefinitionRepository _definitionRepository;
    private Dictionary<string, ResourceDefinition> _definitions;
    private Dictionary<string, float> _resources;
    private string _stateFilePath = "State\\resources.json";

    public SettlementResourcesService(IResourceDefinitionRepository definitionRepository)
    {
        _definitionRepository = definitionRepository;
        _resources = new Dictionary<string, float>();
        _definitions = new Dictionary<string, ResourceDefinition>();
        LoadDefinitionsAsync();
    }

    private async Task OnResourcesChanged()
    {
        await SaveChangesAsync();
        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs(_resources));
    }


    public async Task LoadDefinitionsAsync(string json)
    {
        var definitions = JsonSerializer.Deserialize<List<ResourceDefinition>>(json);

        var _definitions = new Dictionary<string, ResourceDefinition>();
        if (definitions != null)
            foreach (var def in definitions)
                _definitions[def.Id] = def;

        foreach (var resourceDef in _definitions.Values)
            if (!_resources.ContainsKey(resourceDef.Id))
                _resources[resourceDef.Id] = 0;
    }

    public async Task LoadDefinitionsAsync()
    {
        _definitions = await _definitionRepository.LoadResourceDefinitionsAsync();

        foreach (var resourceDef in _definitions.Values)
            if (!_resources.ContainsKey(resourceDef.Id))
                _resources[resourceDef.Id] = 0;
    }

    public async Task SaveChangesAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_resources, options);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_stateFilePath))!);
        await File.WriteAllTextAsync(_stateFilePath, json);
    }

    public async Task LoadFromFileAsync()
    {
        if (File.Exists(_stateFilePath))
        {
            var json = await File.ReadAllTextAsync(_stateFilePath);
            _resources = JsonSerializer.Deserialize<Dictionary<string, float>>(json) ??
                         new Dictionary<string, float>();
        }
    }

    public IReadOnlyList<ResourceDefinition> GetResourceDefinitions()
    {
        return _definitions.Values.ToList();
    }

    public IReadOnlyDictionary<string, float> GetCurrentBalance()
    {
        return _resources;
    }

    public async Task AddResourceAsync(string resourceId, float amount)
    {
        if (_resources.ContainsKey(resourceId))
        {
            _resources[resourceId] += amount;
            await OnResourcesChanged();
        }
        else
        {
            throw new ArgumentException(null, nameof(resourceId));
        }
    }

    public async Task<bool> TrySpendResourceAsync(string resourceId, float amount)
    {
        if (!CanSpend(resourceId, amount))
            return false;

        _resources[resourceId] -= amount;

        await OnResourcesChanged();

        return true;
    }

    public bool CanSpend(string resourceId, float amount)
    {
        if (_resources.TryGetValue(resourceId, out var resource))
        {
            return resource >= amount;
        }
        else
        {
            throw new ArgumentException(null, nameof(resourceId));
        }
    }

    public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

    public Task ApplyDailyEffectsAsync(IEnumerable<ResourceEffect> dailyEffects)
    {
        throw new NotImplementedException();
    }
}