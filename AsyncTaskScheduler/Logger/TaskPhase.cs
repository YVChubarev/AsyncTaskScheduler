namespace AsyncTaskScheduler.Logger
{
    /// <summary>
    /// Статусы выполнения задачи
    /// </summary>
    public enum TaskPhase
    {
        /// <summary>
        /// Задача запущена
        /// </summary>
        Start,
        /// <summary>
        /// Задача завершена
        /// </summary>
        Stop,
        /// <summary>
        /// Задача прервана
        /// </summary>
        Cancel,
    }
}
