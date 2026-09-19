namespace AsyncTaskScheduler.Logger
{
    public interface IStatusLogger<T> where T : Enum
    {
        void Log(T status, string message);
    }
}
