using AsyncTaskScheduler.Logger;

namespace AsyncTaskScheduler
{
    /// <summary>
    /// Класс выполнения <see cref="TaskItem"/>.
    /// </summary>
    /// <param name="taskItem">Задача, которую надо выполнить.</param>
    /// <param name="logger">Логгер.</param>
    public class TaskItemExecutor (TaskItem taskItem,
                                   IStatusLogger < TaskPhase > logger) : ITaskItemExecutor
    {
        private readonly TaskItem _taskItem = taskItem;
        private readonly IStatusLogger<TaskPhase> _logger = logger;

        /// <inheritdoc/>
        public async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                _logger.Log(TaskPhase.Start, _taskItem.Name);
                await Task.Delay(_taskItem.DurationMs, token);
                _logger.Log(TaskPhase.Stop, _taskItem.Name);
            }
            catch (OperationCanceledException)
            {
                _logger.Log(TaskPhase.Cancel, _taskItem.Name);
                throw;
            }
        }
    }
}
