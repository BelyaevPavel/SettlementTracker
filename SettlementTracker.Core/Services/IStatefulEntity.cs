using System.Threading;
using System.Threading.Tasks;

namespace SettlementTracker.Core.Services
{
    /// <summary>
    /// Определяет контракт для сущностей, состояние которых необходимо сохранять и загружать.
    /// </summary>
    public interface IStatefulEntity
    {
        /// <summary>
        /// Асинхронно сохраняет состояние сущности.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию сохранения.</returns>
        Task SaveStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Асинхронно загружает состояние сущности.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию загрузки.</returns>
        Task LoadStateAsync(CancellationToken cancellationToken = default);
    }
}