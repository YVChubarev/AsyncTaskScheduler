using AsyncTaskScheduler;
using AsyncTaskScheduler.Logger;
using System.Text.Json;

TaskItem[] array;
using (var stream = File.OpenRead("tasks.json"))
{
    array = await JsonSerializer.DeserializeAsync<TaskItem[]>(stream) ?? [];
}
var writer = Console.Out;
var systemLogger = new StatusLogger<SystemStatus>(TimeProvider.System, writer);
var logger = new StatusLogger<TaskPhase>(TimeProvider.System, writer);

var orchestrator = new TaskItemsOrchestrator(
    item => new TaskItemExecutor(item, logger),
    systemLogger,
    new KeyPressTrigger());

await orchestrator.RunAsync(array, 3);
