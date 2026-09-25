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

var orchestrator = new TaskItemsOrchestrator(
    item => new TaskItemExecutor(item, logger),
    systemLogger,
    new KeyPressTrigger());

await orchestrator.RunAsync(array, 3);
