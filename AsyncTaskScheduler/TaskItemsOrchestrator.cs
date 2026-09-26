using AsyncTaskScheduler.Logger;

namespace AsyncTaskScheduler
{
    /// <summary>
    /// Оркестратор параллельного выполнения <see cref="TaskItem"/> с триггером отмены.
    /// </summary>
    /// <param name="executorFactory">Фабрика преобразования <see cref="TaskItem"/> в <see cref="ITaskItemExecutor"/>.</param>
    /// <param name="systemLogger">Логирование на уровне оркестранта.</param>
    /// <param name="trigger">Триггер отмены.</param>
    public class TaskItemsOrchestrator(Func<TaskItem, ITaskItemExecutor> executorFactory,
                                       IStatusLogger<SystemStatus> systemLogger,
                                       ITrigger trigger)
    {
        /// <summary>
        /// Асинхронное выполнение всех <paramref name="items"/>
        /// с ограничением уровня параллелизма
        /// и с отменой выполнения по триггеру.
        /// </summary>
        /// <param name="items">Список задач.</param>
        /// <param name="maxParallelism">Уровень параллелизма.</param>
        public async Task RunAsync(TaskItem[] items, int maxParallelism)
        {
            using CancellationTokenSource cts = new();
            using SemaphoreSlim semaphore = new(maxParallelism);
            var workTask = ExecuteAllAsync(items, semaphore, cts.Token);
            var triggerTask = trigger.WaitAsync(cts.Token);
            await Task.WhenAny(workTask, triggerTask);
            cts.Cancel();
            try { await triggerTask; } catch (OperationCanceledException) { }
            try
            {
                await workTask;
            }
            catch (OperationCanceledException)
            {
                systemLogger.Log(SystemStatus.Shutdown, "Stopped by user");
            }
        }

        /// <summary>
        /// Собирает <paramref name="items"/> в задачу параллельного выполнения.
        /// </summary>
        /// <param name="items">Список задач.</param>
        /// <param name="semaphore">Ограничение количества одновременно выполняемых задач.</param>
        /// <param name="token">Токен отмены.</param>
        /// <returns>Task.WhenAll().</returns>
        private Task ExecuteAllAsync(TaskItem[] items, SemaphoreSlim semaphore, CancellationToken token)
        {
            var taskList = items.Select(i => ExecuteOneAsync(i, semaphore, token));
            return Task.WhenAll(taskList);
        }

        /// <summary>
        /// Ожидает <paramref name="semaphore"/> и выполняет задачу.
        /// </summary>
        /// <param name="item">Задача.</param>
        /// <param name="semaphore">Ограничение количества одновременно выполняемых задач.</param>
        /// <param name="token">Токен отмены.</param>
        private async Task ExecuteOneAsync(TaskItem item, SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            try
            {
                await executorFactory(item).ExecuteAsync(token);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
