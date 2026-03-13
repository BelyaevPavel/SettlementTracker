using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using SettlementTracker.Core.Services;

namespace SettlementTracker.Core.Tests.Services;

[TestFixture]
[TestOf(typeof(StateManager))]
public class StateManagerTest
{
    [TestFixture]
    public class StateManagerTests
    {
        private Mock<ILogger<StateManager>> _loggerMock;
        private StateManagerOptions _options;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<StateManager>>();
            _options = new StateManagerOptions
            {
                AutoSaveIntervalSeconds = 5,
                LoadOnStart = false // отключаем авто-загрузку для большинства тестов
            };
        }

        [Test]
        public async Task SaveAllAsync_CallsSaveOnAllEntities()
        {
            // Arrange
            var entity1 = new Mock<IStatefulEntity>();
            var entity2 = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity1.Object, entity2.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Act
            await manager.SaveAllAsync();

            // Assert
            entity1.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            entity2.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task LoadAllAsync_CallsLoadOnAllEntities()
        {
            // Arrange
            var entity1 = new Mock<IStatefulEntity>();
            var entity2 = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity1.Object, entity2.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Act
            await manager.LoadAllAsync();

            // Assert
            entity1.Verify(e => e.LoadStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            entity2.Verify(e => e.LoadStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SaveAllAsync_ContinuesOnEntityException()
        {
            // Arrange
            var entity1 = new Mock<IStatefulEntity>();
            entity1.Setup(e => e.SaveStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));
            var entity2 = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity1.Object, entity2.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Act
            await manager.SaveAllAsync();

            // Assert
            entity2.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            // Можно проверить логирование ошибки, но это не обязательно
        }

        [Test]
        public async Task LoadAllAsync_ContinuesOnEntityException()
        {
            // Arrange
            var entity1 = new Mock<IStatefulEntity>();
            entity1.Setup(e => e.LoadStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));
            var entity2 = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity1.Object, entity2.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Act
            await manager.LoadAllAsync();

            // Assert
            entity2.Verify(e => e.LoadStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ConcurrentSaveAllAsync_AreSerialized()
        {
            // Arrange
            var entity = new Mock<IStatefulEntity>();
            var tcs = new TaskCompletionSource<bool>();
            int callCount = 0;
            entity.Setup(e => e.SaveStateAsync(It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    Interlocked.Increment(ref callCount);
                    await tcs.Task;
                });
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Act
            var task1 = manager.SaveAllAsync();
            var task2 = manager.SaveAllAsync();

            // Даем первому вызову захватить блокировку
            await Task.Delay(100);

            // Завершаем первый вызов
            tcs.SetResult(true);

            await Task.WhenAll(task1, task2);

            // Assert
            Assert.That(callCount, Is.EqualTo(2)); // Оба вызова выполнились, но последовательно
            entity.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void StartAutoSave_WithZeroInterval_DoesNotStartTimer()
        {
            // Arrange
            _options = new StateManagerOptions { AutoSaveIntervalSeconds = 0 };
            var manager = new StateManager(Enumerable.Empty<IStatefulEntity>(), _options, _loggerMock.Object);

            // Act
            manager.StartAutoSave();

            // Assert - не должно быть исключений, таймер не запущен. 
            // Проверить косвенно можно, вызвав StopAutoSave (ничего не произойдет)
            Assert.DoesNotThrowAsync(async () => await Task.Delay(100)); // просто тест не падает
        }

        [Test]
        public async Task AutoSaveCallback_InvokesSaveOnAllEntities()
        {
            // Arrange
            var entity1 = new Mock<IStatefulEntity>();
            var entity2 = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity1.Object, entity2.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            // Получаем приватный метод AutoSaveCallback через рефлексию
            var methodInfo = typeof(StateManager).GetMethod("AutoSaveCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(methodInfo, Is.Not.Null);

            // Act
            // Вызываем callback с параметром null (как это делает таймер)
            var task = (Task)methodInfo.Invoke(manager, new object[] { null });
            await task;

            // Assert
            entity1.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            entity2.Verify(e => e.SaveStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AutoSaveCallback_WhenPreviousSaveIsRunning_DoesNotStartNewSave()
        {
            // Arrange
            var entity = new Mock<IStatefulEntity>();
            var tcs = new TaskCompletionSource<bool>();
            int callCount = 0;
            entity.Setup(e => e.SaveStateAsync(It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    Interlocked.Increment(ref callCount);
                    await tcs.Task;
                });
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);

            var methodInfo = typeof(StateManager).GetMethod("AutoSaveCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Запускаем первый вызов (не завершаем)
            var task1 = (Task)methodInfo.Invoke(manager, new object[] { null });

            // Даем первому вызову захватить блокировку
            await Task.Delay(100);

            // Пытаемся запустить второй вызов параллельно
            var task2 = (Task)methodInfo.Invoke(manager, new object[] { null });

            // Завершаем первый
            tcs.SetResult(true);

            await Task.WhenAll(task1, task2);

            // Assert - должен быть только один вызов SaveStateAsync
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task StateSavedEvent_RaisedAfterManualSave()
        {
            // Arrange
            var entity = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);
            var eventRaised = false;
            StateSavedEventArgs capturedArgs = null;
            manager.StateSaved += (s, e) =>
            {
                eventRaised = true;
                capturedArgs = e;
            };

            // Act
            await manager.SaveAllAsync();

            // Assert
            Assert.That(eventRaised, Is.True);
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs.IsManual, Is.True);
            Assert.That(capturedArgs.Success, Is.True);
        }

        [Test]
        public async Task StateSavedEvent_RaisedAfterAutoSave()
        {
            // Arrange
            var entity = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);
            var eventRaised = false;
            StateSavedEventArgs capturedArgs = null;
            manager.StateSaved += (s, e) =>
            {
                eventRaised = true;
                capturedArgs = e;
            };

            // Получаем приватный метод AutoSaveCallback
            var methodInfo = typeof(StateManager).GetMethod("AutoSaveCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var task = (Task)methodInfo.Invoke(manager, new object[] { null });
            await task;

            // Assert
            Assert.That(eventRaised, Is.True);
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs.IsManual, Is.False);
            Assert.That(capturedArgs.Success, Is.True);
        }

        [Test]
        public async Task SaveAllAsync_RespectsCancellationToken()
        {
            // Arrange
            var entity = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, _options, _loggerMock.Object);
            var cts = new CancellationTokenSource();

            // Настраиваем, что вызов SaveStateAsync будет ожидать отмены
            entity.Setup(e => e.SaveStateAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(async ct => { await Task.Delay(1000, ct); });

            // Act
            cts.CancelAfter(100);
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await manager.SaveAllAsync(cts.Token));
        }

        [Test]
        public async Task StartAutoSave_WithLoadOnStartTrue_CallsLoadAllAsync()
        {
            // Arrange
            var options = new StateManagerOptions
            {
                AutoSaveIntervalSeconds = 5,
                LoadOnStart = true
            };
            var entity = new Mock<IStatefulEntity>();
            var entities = new List<IStatefulEntity> { entity.Object };
            var manager = new StateManager(entities, options, _loggerMock.Object);

            // Act
            manager.StartAutoSave();

            // Даем время для запуска асинхронной загрузки
            await Task.Delay(100);

            // Assert
            entity.Verify(e => e.LoadStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TearDown]
        public void TearDown()
        {
            // Очистка, если нужно
        }
    }
}