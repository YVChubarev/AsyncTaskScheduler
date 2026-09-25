using System;
using System.Collections.Generic;
using System.Text;
using AsyncTaskScheduler.Logger;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace AsyncTaskScheduler.Tests
{
    public class StatusLoggerTests
    {
        public enum OneStatus
        {
            Status,
        }
        public enum TestStatus
        {
            VeryLongStatusNameISoLong,
            S,
            EightSym,
        }

        [Fact]
        public void Log_ShouldWriteFormattedMessage()
        {
            // Arrange
            var msg = "Test message";
            var writer = new StringWriter();
            var fakeTime = new FakeTimeProvider();
            fakeTime.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(3), "Test", "Test"));
            fakeTime.SetUtcNow(new DateTimeOffset(new DateTime(2025, 2, 4, 9, 4, 8, 123), TimeSpan.Zero));
            var logger = new StatusLogger<OneStatus>(fakeTime, writer);
            var expectedOutput = $"09:04:08.12 [STATUS] {msg}{Environment.NewLine}";

            // Act
            logger.Log(OneStatus.Status, msg);

            // Assert
            writer.ToString().Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData(TestStatus.S,                         "[S]                        ")]
        [InlineData(TestStatus.EightSym,                  "[EIGHTSYM]                 ")]
        [InlineData(TestStatus.VeryLongStatusNameISoLong, "[VERYLONGSTATUSNAMEISOLONG]")]
        public void Log_PadsStatusToStatusMaxLength(TestStatus status, string expectedStatusWithSpace)
        {
            // Arrange
            var msg = "Test message";
            var writer = new StringWriter();
            var fakeTime = new FakeTimeProvider();
            fakeTime.SetUtcNow(new DateTimeOffset(new DateTime(2025, 2, 4, 9, 4, 8, 123), TimeSpan.Zero));
            var logger = new StatusLogger<TestStatus>(fakeTime, writer);
            var expectedOutput = $"09:04:08.12 {expectedStatusWithSpace} {msg}{Environment.NewLine}";

            // Act
            logger.Log(status, msg);

            // Assert
            writer.ToString().Should().Be(expectedOutput);
        }
    }
}
