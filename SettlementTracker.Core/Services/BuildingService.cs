using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Definitions;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly string _buildingsStatePath = "State/builtBuildings.json";
        private readonly string _definitionsPath = "Data/buildingDefinitions.json";
        private List<Building> _builtBuildings = new();
        private List<BuildingDefinition> _definitions = new();

        public BuildingService()
        {
            if (!Directory.Exists("State"))
                Directory.CreateDirectory("State");
            LoadDefinitions();
        }

        public async Task<IEnumerable<BuildingDefinition>> GetAvailableDefinitionsAsync()
        {
            return await Task.FromResult(
                _definitions.Where(d => !_builtBuildings.Select(b => b.DefinitionId).Contains(d.Id)));
        }

        public Task<BuildingDefinition?> GetDefinitionByIdAsync(string definitionId)
        {
            return Task.FromResult(_definitions.FirstOrDefault(b => b.Id == definitionId));
        }

        public async Task<IEnumerable<Building>> GetBuiltBuildingsAsync()
        {
            return await Task.FromResult(_builtBuildings);
        }

        public Task<Building?> GetBuildingAsync(Guid id)
        {
            return Task.FromResult(_builtBuildings.FirstOrDefault(b => b.Id == id));
        }

        public async Task<bool> IsBuildingBuiltAsync(string definitionId)
        {
            return await Task.FromResult(
                _builtBuildings.Any(b => b.DefinitionId == definitionId)
            );
        }

        public async Task<Building> BuildAsync(string definitionId, (int X, int Y) position)
        {
            // Проверка, что здание еще не построено
            if (await IsBuildingBuiltAsync(definitionId))
                throw new InvalidOperationException($"Здание {definitionId} уже построено");

            // Заглушка: проверка ресурсов
            if (!await CanAffordBuildAsync(definitionId))
                throw new InvalidOperationException("Недостаточно ресурсов");

            // Заглушка: списание ресурсов
            await TrySpendResourcesForBuildAsync(definitionId);

            var building = new Building(Guid.NewGuid(), definitionId, position)
            {
                Definition = _definitions.FirstOrDefault(d => d.Id == definitionId)
            };

            _builtBuildings.Add(building);
            await SaveChangesAsync();

            return building;
        }

        public async Task<bool> DemolishAsync(Guid buildingId)
        {
            Building building = _builtBuildings.FirstOrDefault(b => b.Id == buildingId);
            if (building == null) return false;

            building.UnassignAllCitizens();
            _builtBuildings.Remove(building);
            await SaveChangesAsync();

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
                    building.Definition = _definitions.FirstOrDefault(d => d.Id == building.DefinitionId);

                _builtBuildings = buildings;
            }
        }

        public async Task<bool> CanAffordBuildAsync(string definitionId)
        {
            // TODO: Реализовать проверку ресурсов
            return true; // Пока всегда true
        }

        public async Task<bool> TrySpendResourcesForBuildAsync(string definitionId)
        {
            // TODO: Реализовать списание ресурсов
            return true; // Пока всегда true
        }

        public event EventHandler? BuildingChange;

        private void LoadDefinitions()
        {
            if (File.Exists(_definitionsPath))
            {
                string json = File.ReadAllText(_definitionsPath);
                _definitions = JsonSerializer.Deserialize<List<BuildingDefinition>>(json)
                               ?? new List<BuildingDefinition>();
            }
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