using AsyncTaskScheduler.Logger;
using FluentAssertions;
using NSubstitute;
using System.Collections.Concurrent;

namespace AsyncTaskScheduler.Tests
{
    public class TaskItemsOrchestratorTests
    {

        [Theory]
        [InlineData(1, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 5)]
        [InlineData(3, 3)]
        [InlineData(3, 1)]
        [InlineData(3, 0)]
        public async Task RunAsync_ShouldObserveMaxParallelismInFlight(int maxParallelism, int itemCount)
        {
            // Arrange
            // Общий счётчик "сколько fake сейчас внутри ExecuteAsync".
            int inFlight = 0;

            ConcurrentQueue<int> entryQueue = new();

            // Счетчик сигналов "кто-то вошёл".
            SemaphoreSlim entrySignal = new(0);

            // По одной release-TCS на каждую fake.
            var releaseTcs =
                Enumerable.Range(0, itemCount)
                          .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                          .ToArray();

            // fake TaskItemExecutor
            ITaskItemExecutor Factory(TaskItem item)
            {
                var executor = Substitute.For<ITaskItemExecutor>();
                executor.ExecuteAsync(Arg.Any<CancellationToken>())
                        .Returns(async _ =>
                        {
                            try
                            {
                                Interlocked.Increment(ref inFlight);
                                entryQueue.Enqueue(item.Id); // регистрация id для release
                                entrySignal.Release(); // сигнал тесту
                                await releaseTcs[item.Id].Task; // ждем от теста
                            }
                            finally
                            {
                                Interlocked.Decrement(ref inFlight);
                            }
                        });
                return executor;
            }

            // триггер отмены, который не вызовется
            var trigger = Substitute.For<ITrigger>();
            trigger.WaitAsync(Arg.Any<CancellationToken>())
                   .Returns(callinfo => Task.Delay(Timeout.Infinite, callinfo.Arg<CancellationToken>()));

            var logger = Substitute.For<IStatusLogger<SystemStatus>>(); // заглушка
            var sut = new TaskItemsOrchestrator(Factory, logger, trigger);

            // массив задач, id по порядку, нужна корреляция с releaseTcs
            var items = Enumerable.Range(0, itemCount)
                .Select(i => new TaskItem { Id = i, Name = $"task_{i}" })
                .ToArray();

            // Токен на случай зависания задачи, уроним тест с ошибкой
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            // Act
            var runTask =  sut.RunAsync(items, maxParallelism);

            for (int i = 0; i < Math.Min(maxParallelism, itemCount); i++)
                await entrySignal.WaitAsync(cts.Token);

            // Assert
            for (int i = maxParallelism; i < itemCount; i++)
            {
                //  release-return: за каждый релиз должен войти ровно один новый
                Volatile.Read(ref inFlight).Should().Be(maxParallelism);
                entryQueue.TryDequeue(out var id).Should().BeTrue();
                releaseTcs[id].SetResult();
                await entrySignal.WaitAsync(cts.Token);
            }
            // последняя задача запущена
            Volatile.Read(ref inFlight).Should().Be(Math.Min(maxParallelism, itemCount));

            // отпускаем всех для завершения
            foreach (var tcs in releaseTcs)
                tcs.TrySetResult();
            await runTask;

            // все задачи завершились
            Volatile.Read(ref inFlight).Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_WhenTriggerFires_ShouldCancelTasksAndLogShutdown()
        {
            // Arrange
            const int ITEMS_COUNT = 5;
            const int MAX_PARALLELISM = 3;
            var TIMEOUT = TimeSpan.FromSeconds(5);

            // Общий счётчик "сколько fake сейчас внутри ExecuteAsync".
            int inFlight = 0;

            // Счетчик сигналов "кто-то вошёл".
            SemaphoreSlim entrySignal = new(0);

            // fake trigger.
            TaskCompletionSource triggerTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var trigger = Substitute.For<ITrigger>();
            trigger.WaitAsync(Arg.Any<CancellationToken>())
                   .Returns(_ => triggerTcs.Task);

            // fake TaskItemExecutor
            ITaskItemExecutor Factory(TaskItem item)
            {
                var executor = Substitute.For<ITaskItemExecutor>();
                executor.ExecuteAsync(Arg.Any<CancellationToken>())
                        .Returns(async ci =>
                        {
                            try
                            {
                                Interlocked.Increment(ref inFlight);
                                entrySignal.Release(); // сигнал тесту
                                await Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()); // ждём отмену через trigger
                            }
                            finally
                            {
                                Interlocked.Decrement(ref inFlight);
                            }
                        });
                return executor;
            }

            var logger = Substitute.For<IStatusLogger<SystemStatus>>(); // заглушка
            var sut = new TaskItemsOrchestrator(Factory, logger, trigger);

            // массив задач, id по порядку
            var items = Enumerable.Range(0, ITEMS_COUNT)
                .Select(i => new TaskItem { Id = i, Name = $"task_{i}" })
                .ToArray();

            // Act
            var runTask = sut.RunAsync(items, MAX_PARALLELISM);

            //ждем запуск задач
            for (int i = 0; i < Math.Min(MAX_PARALLELISM, ITEMS_COUNT); i++)
                await entrySignal.WaitAsync(TIMEOUT);

            triggerTcs.SetResult(); // сработал триггер

            await runTask.WaitAsync(TIMEOUT); // ждем завершения или роняем по таймауту

            // Assert
            Volatile.Read(ref inFlight).Should().Be(0); // все задачи завершились
            logger.Received(1).Log(SystemStatus.Shutdown, "Stopped by user"); // логирование выполнено
        }

        [Fact]
        public async Task RunAsync_ShouldNotCompleteWhileAnyItemInFlight()
        {
            // Arrange
            const int ITEMS_COUNT = 5;
            const int MAX_PARALLELISM = 3;
            var TIMEOUT = TimeSpan.FromSeconds(5);

            ConcurrentQueue<int> entryQueue = new();

            // Счетчик сигналов "кто-то вошёл".
            SemaphoreSlim entrySignal = new(0);

            // По одной release-TCS на каждую fake.
            var releaseTcs =
                Enumerable.Range(0, ITEMS_COUNT)
                          .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                          .ToArray();

            // fake TaskItemExecutor
            ITaskItemExecutor Factory(TaskItem item)
            {
                var executor = Substitute.For<ITaskItemExecutor>();
                executor.ExecuteAsync(Arg.Any<CancellationToken>())
                        .Returns(async _ =>
                        {
                            entryQueue.Enqueue(item.Id); // регистрация id для release
                            entrySignal.Release(); // сигнал тесту
                            await releaseTcs[item.Id].Task; // ждем от теста
                        });
                return executor;
            }

            // триггер отмены, который не вызовется
            var trigger = Substitute.For<ITrigger>();
            trigger.WaitAsync(Arg.Any<CancellationToken>())
                   .Returns(callinfo => Task.Delay(Timeout.Infinite, callinfo.Arg<CancellationToken>()));

            var logger = Substitute.For<IStatusLogger<SystemStatus>>(); // заглушка
            var sut = new TaskItemsOrchestrator(Factory, logger, trigger);

            // массив задач, id по порядку, нужна корреляция с releaseTcs
            var items = Enumerable.Range(0, ITEMS_COUNT)
                .Select(i => new TaskItem { Id = i, Name = $"task_{i}" })
                .ToArray();

            // Act
            var runTask = sut.RunAsync(items, MAX_PARALLELISM);

            // Assert
            for (int i = 0; i < ITEMS_COUNT; i++)
            {
                await entrySignal.WaitAsync(TIMEOUT);
                entryQueue.TryDequeue(out var id);
                runTask.IsCompleted.Should().BeFalse();
                releaseTcs[id].SetResult();
            }

            // отпускаем всех для завершения
            foreach (var tcs in releaseTcs)
                tcs.TrySetResult();
            await runTask.WaitAsync(TIMEOUT);

            // все задачи завершились
            runTask.IsCompleted.Should().BeTrue();
        }
    }
}
