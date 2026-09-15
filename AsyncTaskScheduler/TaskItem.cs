using System;
using System.Collections.Generic;
using System.Text;

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
        public int Id { get; set; }
        /// <summary>
        /// Текст.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Время выполнения.
        /// </summary>
        public int DurationMs { get; set; }
        /// <summary>
        /// Для сортировки.
        /// </summary>
        public int Priority { get; set; }
    }
}
