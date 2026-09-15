using AsyncTaskScheduler;
using System.Text.Json;

TaskItem[] array;
using (var stream = File.OpenRead("tasks.json"))
{
    array = await JsonSerializer.DeserializeAsync<TaskItem[]>(stream) ?? [];
}
using (var cts = new CancellationTokenSource())
using (var semaphore = new SemaphoreSlim(3))
{
    var taskList = array.Select(async i =>
    {
        await semaphore.WaitAsync(cts.Token);
        try
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} [START]    {i.Name}");
            await Task.Delay(i.DurationMs, cts.Token);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} [STOP]     {i.Name}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} [CANCEL]   {i.Name}");
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    });
    var workTask = Task.WhenAll(taskList);
    var cancelTask = Task.Run(async () =>
    {
        while (!Console.KeyAvailable && !workTask.IsCompleted)
        {
            await Task.Delay(100);
        }
        if (Console.KeyAvailable)
        {
            Console.ReadKey(true);
            cts.Cancel();
        }
    });
    await Task.WhenAny(workTask, cancelTask);
    try
    {
        await workTask;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} [SHUTDOWN] Stopped by user");
    }
}