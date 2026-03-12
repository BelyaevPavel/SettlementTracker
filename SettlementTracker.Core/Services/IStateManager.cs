using System;
using System.Threading;
using System.Threading.Tasks;

namespace SettlementTracker.Core.Services
{
    /// <summary>
    /// Интерфейс менеджера состояния, управляющего сохранением и загрузкой всех IStatefulEntity.
    /// </summary>
    public interface IStateManager : IDisposable
    {
        /// <summary>
        /// Событие, возникающее после завершения автоматического или ручного сохранения.
        /// </summary>
        event EventHandler<StateSavedEventArgs>? StateSaved;

        /// <summary>
        /// Асинхронно сохраняет состояние всех зарегистрированных сущностей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Асинхронно загружает состояние всех зарегистрированных сущностей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task LoadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Запускает автоматическое периодическое сохранение с заданным интервалом.
        /// </summary>
        void StartAutoSave();

        /// <summary>
        /// Останавливает автоматическое периодическое сохранение.
        /// </summary>
        void StopAutoSave();
    }
}