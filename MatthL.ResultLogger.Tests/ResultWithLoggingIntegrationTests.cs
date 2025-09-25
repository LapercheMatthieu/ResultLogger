using FluentAssertions;
using MatthL.ResultLogger.Core;
using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Managers;
using MatthL.ResultLogger.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MatthL.ResultLogger.Tests
{
    public class ResultWithLoggingIntegrationTests : IDisposable
    {
        private readonly string _testLogPath;

        public ResultWithLoggingIntegrationTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.log");
            LogManager.Clear();
            LogManager.Configure(LogDestination.Memory);
        }

        public void Dispose()
        {
            LogManager.Clear();
            if (File.Exists(_testLogPath))
            {
                File.Delete(_testLogPath);
            }
        }

        [Fact]
        public void Result_Success_Should_Log_Automatically()
        {
            // Act
            var result = Result.Success();

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(1);
            logs[0].Level.Should().Be(LogLevel.Debug);
            logs[0].Message.Should().Be("Operation succeeded");
            logs[0].CallerMethod.Should().Be(nameof(Result_Success_Should_Log_Automatically));
        }

        [Fact]
        public void Result_Failure_Should_Log_Error_Automatically()
        {
            // Arrange
            const string errorMessage = "Database connection failed";

            // Act
            var result = Result.Failure(errorMessage);

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(1);
            logs[0].Level.Should().Be(LogLevel.Error);
            logs[0].Message.Should().Be("Operation failed");
            logs[0].Error.Should().Be(errorMessage);
            logs[0].CallerMethod.Should().Be(nameof(Result_Failure_Should_Log_Error_Automatically));
        }

        [Fact]
        public void ResultT_Success_Should_Log_With_Caller_Info()
        {
            // Act
            var result = Result<int>.Success(42);

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(1);
            logs[0].CallerMethod.Should().Be(nameof(ResultT_Success_Should_Log_With_Caller_Info));
            logs[0].CallerFile.Should().EndWith("ResultWithLoggingIntegrationTests.cs");
            logs[0].CallerLine.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Async_Operations_Should_Log_Correctly()
        {
            // Act
            var result = await SimulateAsyncOperation(true);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(1);
            logs[0].CallerMethod.Should().Be(nameof(SimulateAsyncOperation));
        }

        [Fact]
        public void Multiple_Results_Should_Create_Multiple_Logs()
        {
            // Act
            var result1 = Result.Success();
            var result2 = Result.Failure("Error 1");
            var result3 = Result<string>.Success("Test");
            var result4 = Result<int>.Failure("Error 2");

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(4);
            logs.Count(l => l.Level == LogLevel.Debug).Should().Be(2);
            logs.Count(l => l.Level == LogLevel.Error).Should().Be(2);
        }

        [Fact]
        public void Result_Chain_With_Map_Should_Log_Each_Step()
        {
            // Act
            var initial = Result<int>.Success(10);
            var mapped1 = initial.Map(x => x * 2);
            var mapped2 = mapped1.Map(x => x.ToString());

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(3); // Initial + 2 maps
            logs.All(l => l.Level == LogLevel.Debug).Should().BeTrue();
        }

        [Fact]
        public void Complex_Business_Logic_Should_Generate_Traceable_Logs()
        {
            // Arrange & Act
            var result = SimulateComplexBusinessLogic();

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCountGreaterThan(0);

            // Vérifier qu'on peut tracer le flow
            var errorLogs = logs.Where(l => l.Level == LogLevel.Error).ToList();
            if (result.IsFailure)
            {
                errorLogs.Should().NotBeEmpty();
                errorLogs[0].Error.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public void Log_To_File_Integration_Should_Work()
        {
            // Arrange
            LogManager.Configure(LogDestination.CustomFile, _testLogPath);

            // Act
            var result1 = Result.Success();
            var result2 = Result.Failure("Test error");
            var result3 = Result<string>.Success("Data");

            // Assert
            File.Exists(_testLogPath).Should().BeTrue();
            var content = File.ReadAllText(_testLogPath);
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(3);
            lines[0].Should().Contain("Operation succeeded");
            lines[1].Should().Contain("Test error");
            lines[2].Should().Contain("Operation succeeded");
        }

        // Helper methods
        private async Task<Result> SimulateAsyncOperation(bool shouldSucceed)
        {
            await Task.Delay(10);
            return shouldSucceed
                ? Result.Success()
                : Result.Failure("Async operation failed");
        }

        private Result SimulateComplexBusinessLogic()
        {
            var validation = ValidateInput("test");
            if (validation.IsFailure)
                return validation;

            var processing = ProcessData();
            if (processing.IsFailure)
                return processing;

            return SaveToDatabase();
        }

        private Result ValidateInput(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? Result.Failure("Input validation failed")
                : Result.Success();
        }

        private Result ProcessData()
        {
            return Result.Success();
        }

        private Result SaveToDatabase()
        {
            // Simuler un échec aléatoire
            return DateTime.Now.Millisecond % 2 == 0
                ? Result.Success()
                : Result.Failure("Database save failed");
        }
    }
}