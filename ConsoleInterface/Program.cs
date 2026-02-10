using System;
using SettlementTracker.Core;
using SettlementTracker.Core.Models.Definitions;

namespace ConsoleInterface
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var game = new SettlementGame("data", "save");

            game.LoadGame();

            game.StartNewGame("NewSettlement");

            game.SaveGame();
        }
    }
}