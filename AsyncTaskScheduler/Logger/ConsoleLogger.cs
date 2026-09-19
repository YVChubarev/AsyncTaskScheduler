namespace AsyncTaskScheduler.Logger
{
    public class ConsoleLogger<T> : IStatusLogger<T> where T : Enum
    {
        private readonly TimeProvider _timeProvider;

        public ConsoleLogger(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public void Log(T status, string message)
        {
            var time = _timeProvider.GetUtcNow();
            var statusString = status.ToString().ToUpperInvariant();
            var text = $"{time:HH:mm:ss.ff} {$"[{statusString}]".PadRight(10)} {message}";
            Console.WriteLine(text);
        }
    }
}
