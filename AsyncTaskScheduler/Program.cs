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
