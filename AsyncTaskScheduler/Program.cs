using AsyncTaskScheduler;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

var text = await File.ReadAllTextAsync("tasks.json");
TaskItem[] array;
using (var stream = File.OpenRead("tasks.json"))
{
    array = await JsonSerializer.DeserializeAsync<TaskItem[]>(stream) ?? [];
}
using (var cts = new CancellationTokenSource())
using (var semaphore = new SemaphoreSlim(3))
{
    var tasks = array.Select(async i =>
    {
        await semaphore.WaitAsync(cts.Token);
        if (cts.Token.IsCancellationRequested) return;
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
    var workTask = Task.WhenAll(tasks);
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
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} [SHOTDOWN] Stopped by user");
    }
}