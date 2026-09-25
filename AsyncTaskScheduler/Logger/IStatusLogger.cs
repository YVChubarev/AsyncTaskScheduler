namespace AsyncTaskScheduler.Logger
{
    /// <summary>
    /// Интерфейс для логирования строки сатуса и сообщения.
    /// </summary>
    /// <typeparam name="T">Перечисление статусов.</typeparam>
    public interface IStatusLogger<T> where T : Enum
    {
        /// <summary>
        /// Записывает лог.
        /// </summary>
        /// <param name="status">Статус лога.</param>
        /// <param name="message">Сообщение.</param>
        void Log(T status, string message);
    }
}
