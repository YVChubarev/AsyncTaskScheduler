using AsyncTaskScheduler;
using AsyncTaskScheduler.Logger;
using System.Text.Json;

TaskItem[] array;
using (var stream = File.OpenRead("tasks.json"))
{
    array = await JsonSerializer.DeserializeAsync<TaskItem[]>(stream) ?? [];
}
var systemLogger = new ConsoleLogger<SystemStatus>(TimeProvider.System);
var logger = new ConsoleLogger<TaskPhase>(TimeProvider.System);
using (var cts = new CancellationTokenSource())
using (var semaphore = new SemaphoreSlim(3))
{
    var taskList = array.Select(async i =>
    {
        await semaphore.WaitAsync(cts.Token);
        try
        {
            logger.Log(TaskPhase.Start, i.Name);
            await Task.Delay(i.DurationMs, cts.Token);
            logger.Log(TaskPhase.Stop, i.Name);
        }
        catch (OperationCanceledException)
        {
            logger.Log(TaskPhase.Cancel, i.Name);
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
        systemLogger.Log(SystemStatus.Shutdown, "Stopped by user");
    }
}