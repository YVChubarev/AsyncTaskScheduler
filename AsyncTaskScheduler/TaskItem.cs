using System.Text.Json.Serialization;

namespace AsyncTaskScheduler
{
    /// <summary>
    /// Задача.
    /// </summary>
    public class TaskItem
    {

        /// <summary>
        /// Идентификатор.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Текст.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Время выполнения.
        /// </summary>
        [JsonPropertyName("duration")]
        public int DurationMs { get; set; }

        /// <summary>
        /// Для сортировки.
        /// </summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }
}
