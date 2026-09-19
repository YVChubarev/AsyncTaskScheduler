namespace AsyncTaskScheduler
{
    /// <summary>
    /// Ожидает нажатие на любую клавишу в консоли
    /// </summary>
    public class KeyPressTrigger : ITrigger
    {
        ///<inheritdoc/>
        public async Task WaitAsync(CancellationToken token = default)
        {
            while (!Console.KeyAvailable)
                await Task.Delay(100, token);
            Console.ReadKey(true);
        }
    }
}
