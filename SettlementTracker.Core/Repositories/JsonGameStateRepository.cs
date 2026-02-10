using System;
using System.IO;
using System.Text.Json;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Repositories
{
    public class JsonGameStateRepository
    {
        private readonly string _savePath;

        public JsonGameStateRepository(string savePath)
        {
            _savePath = savePath;
            if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);
        }

        public void SaveGameState(SettlementState settlement, string fileName = "save.json")
        {
            var filePath = Path.Combine(_savePath, fileName);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(settlement, options);
            File.WriteAllText(filePath, json);
        }

        public SettlementState? LoadGameState(string fileName = "save.json")
        {
            var filePath = Path.Combine(_savePath, fileName);
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<SettlementState>(json, options);
        }

        public SettlementState CreateNewGame(string settlementName)
        {
            var settlement = new SettlementState(Guid.NewGuid(), settlementName);

            // Начальные ресурсы
            settlement.Resources["food"] = 50;
            settlement.Resources["wood"] = 100;
            settlement.Resources["stone"] = 50;
            settlement.Resources["water"] = 100;

            // Начальное население
            var populationManager = new Managers.PopulationManager(settlement);
            populationManager.AddFamily(2, 1); // 2 взрослых, 1 ребенок
            populationManager.AddRandomCitizen(); // Еще один взрослый

            return settlement;
        }

        public bool SaveExists(string fileName = "save.json")
        {
            var filePath = Path.Combine(_savePath, fileName);
            return File.Exists(filePath);
        }

        public void DeleteSave(string fileName = "save.json")
        {
            var filePath = Path.Combine(_savePath, fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}