namespace AsyncTaskScheduler
{
    /// <summary>
    /// Интерфейс выполнения какой-то задачи.
    /// </summary>
    public interface ITaskItemExecutor
    {
        /// <summary>
        /// Асинхронно выполняет задачу.
        /// </summary>
        /// <param name="token">Токен отмены.</param>
        Task ExecuteAsync(CancellationToken token);
    }
}
