namespace SettlementTracker.Core.Models.Enums
{
    public enum WorkStatus
    {
        Idle, // Без работы
        AssignedToBuilding, // Назначен на здание
        AssignedToJob, // Назначен на задание
        Caretaker // Присматривает за детьми/рабами
    }
}