namespace AsyncTaskScheduler.Logger
{
    /// <summary>
    /// Запись логов со статусом.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="timeProvider">Провайдер даты и времени.</param>
    /// <param name="writer">Объект для записи лога.</param>
    public class StatusLogger<T>(TimeProvider timeProvider,
                                 TextWriter writer) : IStatusLogger<T> where T : Enum
    {
        private static readonly int s_statusPadWidth = Enum.GetNames(typeof(T)).Max(i => i.Length) + 2;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly TextWriter _writer = writer;

        /// <inheritdoc/>
        public void Log(T status, string message)
        {
            var time = _timeProvider.GetUtcNow();
            var statusString = status.ToString().ToUpperInvariant();
            var text = $"{time:HH:mm:ss.ff} {$"[{statusString}]".PadRight(s_statusPadWidth)} {message}";
            _writer.WriteLine(text);
        }
    }
}
