#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;
using SettlementTracker.Core.Repositories;

namespace SettlementTracker.Core.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IBuildingDefinitionRepository _buildingDefinitionRepository;

        private readonly string _buildingsStatePath = "State/builtBuildings.json";

        private readonly object _lock = new();

        // private readonly string _definitionsPath = "Data/buildingDefinitions.json";
        private readonly ISettlementResourcesService _resourcesService;
        private List<Building> _builtBuildings = new();

        private IReadOnlyDictionary<string, BuildingDefinition> _definitions =
            new Dictionary<string, BuildingDefinition>();

        public BuildingService(IBuildingDefinitionRepository buildingDefinitionRepository,
            ISettlementResourcesService resourcesService, ILogger<BuildingService> logger)
        {
            _buildingDefinitionRepository = buildingDefinitionRepository ??
                                            throw new ArgumentNullException(nameof(buildingDefinitionRepository));
            _resourcesService = resourcesService ?? throw new ArgumentNullException(nameof(resourcesService));
            Logger = logger;
            if (!Directory.Exists("State"))
                Directory.CreateDirectory("State");
            LoadDefinitions();
        }

        public ILogger<BuildingService>? Logger { get; set; }

        public async Task<IEnumerable<BuildingDefinition>> GetAvailableDefinitionsAsync()
        {
            return await Task.FromResult(
                _definitions.Values.Where(d => !_builtBuildings.Select(b => b.DefinitionId).Contains(d.Id)));
        }

        public async Task<BuildingDefinition?> GetDefinitionByIdAsync(string definitionId)
        {
            return await Task.FromResult(_definitions.GetValueOrDefault(definitionId));
        }

        public async Task<IEnumerable<Building>> GetBuiltBuildingsAsync()
        {
            return await Task.FromResult(_builtBuildings);
        }

        public async Task<Building?> GetBuildingAsync(Guid id)
        {
            return await Task.FromResult(_builtBuildings.FirstOrDefault(b => b.Id == id));
        }

        public async Task<bool> IsBuildingBuiltAsync(string definitionId)
        {
            return await Task.FromResult(
                _builtBuildings.Any(b => b.DefinitionId == definitionId)
            );
        }

        public async Task<bool> TryBuildAsync(string definitionId, (int X, int Y) position,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(definitionId) || string.IsNullOrWhiteSpace(definitionId))
            {
                Logger?.LogWarning("definitionId cannot be null or empty or whitespace.");
                return false;
            }

            BuildingDefinition? buildingDefinition = _definitions.GetValueOrDefault(definitionId);

            if (buildingDefinition == null)
            {
                Logger?.LogWarning("Building definition for {definitionId} not found", definitionId);
                return false;
            }

            if (await IsBuildingBuiltAsync(definitionId))
            {
                Logger?.LogWarning("Здание {definitionId} уже построено", definitionId);
                return false;
            }

            if (CanAffordBuild(definitionId))
            {
                lock (_lock)
                {
                    foreach (ResourceEffect resourceEffect in buildingDefinition.BuildCost)
                        _resourcesService.TryApplyResourceEffectAsync(resourceEffect, cancellationToken);
                    // .Wait(cancellationToken); // Wait here is bad but should be solved by replacing state saving on every change with autosave by timer and manual save)
                }

                var building = new Building(Guid.NewGuid(), definitionId, position)
                {
                    Definition = buildingDefinition
                };

                lock (_lock)
                {
                    _builtBuildings.Add(building);
                }

                await SaveChangesAsync();
                OnBuildingsChanged();

                return true;
            }

            Logger?.LogWarning("Недостаточно ресурсов для строительства {definitionId}", definitionId);
            return false;
        }

        public async Task<bool> TryDemolishAsync(Guid buildingId,
            CancellationToken cancellationToken = default)
        {
            Building? building = _builtBuildings.FirstOrDefault(b => b.Id == buildingId);
            if (building == null) return false;

            if (building.Definition == null) return false;

            lock (_lock)
            {
                foreach (ResourceEffect resourceEffect in building.Definition.BuildCost)
                {
                    var effect = new ResourceEffect
                    {
                        ResourceId = resourceEffect.ResourceId,
                        Amount = resourceEffect.Amount,
                        IsProduction = !resourceEffect.IsProduction
                    };
                    _resourcesService.TryApplyResourceEffectAsync(effect, cancellationToken);
                    // .Wait(cancellationToken); // Wait here is bad but should be solved by replacing state saving on every change with autosave by timer and manual save)
                }

                building.UnassignAllCitizens();
                _builtBuildings.Remove(building);
            }

            await SaveChangesAsync();
            OnBuildingsChanged();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(Guid id)
        {
            Building? building =
                await Task.FromResult(
                    _builtBuildings.FirstOrDefault(b => b.Id == id)
                );
            if (building == null)
                return false;

            building.IsActive = !building.IsActive;

            return true;
        }

        public async Task<bool> AssignCitizenAsync(Guid buildingId, Guid citizenId)
        {
            // TODO: Реализовать приписку рабочих
            throw new NotImplementedException();
        }

        public async Task<bool> UnassignCitizenAsync(Guid buildingId, Guid citizenId)
        {
            // TODO: Реализовать отписку рабочих
            throw new NotImplementedException();
        }

        public async Task SaveChangesAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new TupleConverter() } // Для сериализации (int X, int Y)
            };

            string json = JsonSerializer.Serialize(_builtBuildings, options);

            Directory.CreateDirectory(Path.GetDirectoryName(_buildingsStatePath)!);
            await File.WriteAllTextAsync(_buildingsStatePath, json);
        }

        public async Task LoadFromFileAsync()
        {
            if (File.Exists(_buildingsStatePath))
            {
                string json = await File.ReadAllTextAsync(_buildingsStatePath);
                List<Building> buildings = JsonSerializer.Deserialize<List<Building>>(json)
                                           ?? new List<Building>();

                // Восстанавливаем ссылки на определения
                foreach (Building building in buildings)
                    building.Definition = _definitions.GetValueOrDefault(building.DefinitionId);

                _builtBuildings = buildings;
            }
        }

        public bool CanAffordBuild(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId) || string.IsNullOrWhiteSpace(definitionId))
            {
                Logger?.LogWarning("definitionId cannot be null or empty or whitespace.");
                return false;
            }

            BuildingDefinition? buildingDefinition = _definitions.GetValueOrDefault(definitionId);

            if (buildingDefinition == null)
            {
                Logger?.LogWarning("Building definition for {definitionId} not found", definitionId);
                return false;
            }

            var canAfford = true;
            lock (_lock)
            {
                foreach (ResourceEffect resourceEffect in buildingDefinition.BuildCost)
                    if (!_resourcesService.CanSpend(resourceEffect.ResourceId, resourceEffect.Amount))
                    {
                        canAfford = false;
                        break;
                    }
            }

            return canAfford;
        }

        public event EventHandler? BuildingChange;

        private void OnBuildingsChanged()
        {
            BuildingChange?.Invoke(this, EventArgs.Empty);
        }

        private void LoadDefinitions()
        {
            _definitions = _buildingDefinitionRepository.LoadBuildingDefinitions();
            if (_definitions == null) throw new ArgumentNullException(nameof(_definitions));

            if (_definitions.Count == 0)
                throw new ArgumentException("Can't be an empty collection", nameof(_definitions));
        }
    }

    public class TupleConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof((int X, int Y));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new TupleConverterInner();
        }

        private class TupleConverterInner : JsonConverter<(int X, int Y)>
        {
            public override (int X, int Y) Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                // Реализация чтения кортежа
                var obj = JsonSerializer.Deserialize<JsonElement>(ref reader);
                return (obj.GetProperty("X").GetInt32(), obj.GetProperty("Y").GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, (int X, int Y) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("X", value.X);
                writer.WriteNumber("Y", value.Y);
                writer.WriteEndObject();
            }
        }
    }
}