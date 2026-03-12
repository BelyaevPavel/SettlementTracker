namespace SettlementTracker.Core.Services
{
    /// <summary>
    /// Конфигурация менеджера состояния.
    /// </summary>
    public class StateManagerOptions
    {
        public const string SectionName = "StateManager";

        /// <summary>
        /// Интервал автоматического сохранения в секундах. По умолчанию 300 (5 минут).
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// Загружать ли состояние при запуске автоматически (через StartAutoSave). По умолчанию true.
        /// </summary>
        public bool LoadOnStart { get; set; } = true;
    }
}