using SettlementTracker.Core;

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