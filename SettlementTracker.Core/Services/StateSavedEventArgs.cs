using System;
using System.Collections.Generic;

namespace SettlementTracker.Core.Services
{
    /// <summary>
    /// Аргументы события сохранения состояния.
    /// </summary>
    public class StateSavedEventArgs : EventArgs
    {
        public bool IsManual { get; set; }
        public bool Success { get; set; }
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}