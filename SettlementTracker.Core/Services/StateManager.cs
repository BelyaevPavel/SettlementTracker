using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SettlementTracker.Core.Services
{
    /// <summary>
    /// Реализация менеджера состояния, управляющего коллекцией IStatefulEntity.
    /// </summary>
    public class StateManager : IStateManager
    {
        private readonly IEnumerable<IStatefulEntity> _entities;
        private readonly ILogger<StateManager> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly Timer _autoSaveTimer;
        private readonly StateManagerOptions _options;
        private bool _disposed;

        public event EventHandler<StateSavedEventArgs>? StateSaved;

        public StateManager(
            IEnumerable<IStatefulEntity> entities,
            StateManagerOptions options,
            ILogger<StateManager> logger)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new StateManagerOptions();

            // Инициализация таймера, но не запускаем сразу
            _autoSaveTimer = new Timer(
                callback: AutoSaveCallback,
                state: null,
                dueTime: Timeout.Infinite,
                period: Timeout.Infinite);
        }

        /// <inheritdoc />
        public async Task SaveAllAsync(CancellationToken cancellationToken = default)
        {
            await ExecuteWithLockAsync(async () =>
            {
                _logger.LogInformation("Начало сохранения состояния всех сущностей");
                var errors = new List<string>();
                var tasks = _entities.Select(entity => SafeExecuteAsync(
                    () => entity.SaveStateAsync(cancellationToken),
                    entity.GetType().Name,
                    errors)).ToList();

                await Task.WhenAll(tasks);

                var args = new StateSavedEventArgs
                {
                    IsManual = true,
                    Success = errors.Count == 0,
                    Errors = errors.AsReadOnly()
                };

                if (errors.Count > 0)
                {
                    _logger.LogWarning("Сохранение завершено с ошибками. Ошибок: {ErrorCount}", errors.Count);
                }
                else
                {
                    _logger.LogInformation("Сохранение всех сущностей успешно завершено");
                }

                // Вызов события
                OnStateSaved(args);
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task LoadAllAsync(CancellationToken cancellationToken = default)
        {
            await ExecuteWithLockAsync(async () =>
            {
                _logger.LogInformation("Начало загрузки состояния всех сущностей");
                var errors = new List<string>();
                var tasks = _entities.Select(entity => SafeExecuteAsync(
                    () => entity.LoadStateAsync(cancellationToken),
                    entity.GetType().Name,
                    errors)).ToList();

                await Task.WhenAll(tasks);

                if (errors.Count > 0)
                {
                    _logger.LogWarning("Загрузка завершена с ошибками. Ошибок: {ErrorCount}", errors.Count);
                }
                else
                {
                    _logger.LogInformation("Загрузка всех сущностей успешно завершена");
                }
            }, cancellationToken);
        }

        /// <inheritdoc />
        public void StartAutoSave()
        {
            if (_options.AutoSaveIntervalSeconds <= 0)
            {
                _logger.LogWarning("Автосохранение не запущено, так как интервал автосохранения не задан или равен 0.");
                return;
            }

            var dueTime = TimeSpan.FromSeconds(_options.AutoSaveIntervalSeconds);
            _autoSaveTimer.Change(dueTime, dueTime);
            _logger.LogInformation("Автосохранение запущено с интервалом {Interval} секунд",
                _options.AutoSaveIntervalSeconds);

            // Если указано в конфигурации, загружаем состояние при старте
            if (_options.LoadOnStart)
            {
                // Запускаем асинхронную загрузку без ожидания (fire and forget) с обработкой ошибок
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadAllAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при автоматической загрузке состояния при старте");
                    }
                });
            }
        }

        /// <inheritdoc />
        public void StopAutoSave()
        {
            _autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Автосохранение остановлено");
        }

        private async void AutoSaveCallback(object? state)
        {
            // Проверяем, не выполняется ли уже операция сохранения
            if (!await _semaphore.WaitAsync(0))
            {
                _logger.LogDebug("Автосохранение пропущено, так как предыдущая операция ещё выполняется");
                return;
            }

            try
            {
                _logger.LogInformation("Автоматическое сохранение состояния всех сущностей");
                var errors = new List<string>();
                var tasks = _entities.Select(entity => SafeExecuteAsync(
                    () => entity.SaveStateAsync(),
                    entity.GetType().Name,
                    errors)).ToList();

                await Task.WhenAll(tasks);

                var args = new StateSavedEventArgs
                {
                    IsManual = false,
                    Success = errors.Count == 0,
                    Errors = errors.AsReadOnly()
                };

                if (errors.Count > 0)
                {
                    _logger.LogWarning("Автосохранение завершено с ошибками. Ошибок: {ErrorCount}", errors.Count);
                }
                else
                {
                    _logger.LogInformation("Автосохранение всех сущностей успешно завершено");
                }

                OnStateSaved(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при автосохранении");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ExecuteWithLockAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await action();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SafeExecuteAsync(Func<Task> action, string entityName, List<string> errors)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении операции для сущности {EntityName}", entityName);
                errors.Add($"{entityName}: {ex.Message}");
            }
        }

        protected virtual void OnStateSaved(StateSavedEventArgs e)
        {
            StateSaved?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _autoSaveTimer?.Dispose();
            _semaphore?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}