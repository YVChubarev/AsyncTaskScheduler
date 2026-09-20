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
using var cts = new CancellationTokenSource();
using var semaphore = new SemaphoreSlim(3);
var taskList = array.Select(async i =>
{
    await semaphore.WaitAsync(cts.Token);
    try
    {
        await new TaskItemExecutor(i, logger).ExecuteAsync(cts.Token);
    }
    finally
    {
        semaphore.Release();
    }
});
var workTask = Task.WhenAll(taskList);
var triggerTask = new KeyPressTrigger().WaitAsync(cts.Token);
await Task.WhenAny(workTask, triggerTask);
cts.Cancel();
try { await triggerTask; } catch (OperationCanceledException) { }
try
{
    await workTask;
}
catch (OperationCanceledException)
{
    systemLogger.Log(SystemStatus.Shutdown, "Stopped by user");
}
