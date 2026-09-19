namespace AsyncTaskScheduler
{
    /// <summary>
    /// Интерфейс ожидания чего-либо.
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// Ожидание события.
        /// </summary>
        /// <param name="token">Токен отмены.</param>
        Task WaitAsync(CancellationToken token = default);
    }
}
