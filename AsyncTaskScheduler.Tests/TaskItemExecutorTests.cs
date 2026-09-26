using AsyncTaskScheduler.Logger;
using FluentAssertions;

namespace AsyncTaskScheduler.Tests
{
    public class TaskItemExecutorTests
    {
        public class InMemoryStatusLogger<T> : IStatusLogger<T> where T : Enum
        {
            public List<(T Status, string Message)> Logs { get; } = [];
            public void Log(T status, string message) => Logs.Add((status, message));
        }

        [Fact]
        public async Task ExecuteAsync_WhenSuccess_LogsStartAndStop()
        {
            // Arrange
            TaskItem task = new() { Id = 1, Name = "Test", DurationMs = 10 };
            InMemoryStatusLogger<TaskPhase> logger = new ();
            TaskItemExecutor executor = new(task, logger);

            // Act
            await executor.ExecuteAsync(CancellationToken.None);

            // Assert
            logger.Logs.Should().Equal((TaskPhase.Start, task.Name),
                                       (TaskPhase.Stop, task.Name));
        }

        [Fact]
        public async Task ExecuteAsync_WhenCancelled_LogsCancelAndThrows()
        {
            // Arrange
            TaskItem task = new() { Id = 1, Name = "Test", DurationMs = 1000 };
            InMemoryStatusLogger<TaskPhase> logger = new();
            TaskItemExecutor executor = new(task, logger);
            using CancellationTokenSource cts = new();
            cts.CancelAfter(10);

            // Act
            var act = () => executor.ExecuteAsync(cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            logger.Logs.Should().Equal((TaskPhase.Start, task.Name),
                                       (TaskPhase.Cancel, task.Name));
        }
    }
}
