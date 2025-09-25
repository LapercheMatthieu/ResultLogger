using FluentAssertions;
using MatthL.ResultLogger.Core;
using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Managers;
using MatthL.ResultLogger.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MatthL.ResultLogger.Tests
{
    public class PerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            LogManager.Clear();
            LogManager.Configure(LogDestination.Memory);
        }

        public void Dispose()
        {
            LogManager.Clear();
        }

        [Fact]
        public void LogManager_Should_Handle_High_Volume_Efficiently()
        {
            // Arrange
            const int logCount = 10000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < logCount; i++)
            {
                LogManager.Log(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = $"Message {i}",
                    CallerMethod = "TestMethod",
                    CallerFile = "TestFile.cs",
                    CallerLine = i
                });
            }
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Time to log {logCount} entries: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Moins d'une seconde pour 10k logs

            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(logCount);
        }

        [Fact]
        public async Task Concurrent_Logging_Should_Be_Thread_Safe()
        {
            // Arrange
            const int tasksCount = 100;
            const int logsPerTask = 100;
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < tasksCount; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < logsPerTask; j++)
                    {
                        var result = Result.Success();
                        // Simuler un peu de travail
                        Task.Delay(1).Wait();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(tasksCount * logsPerTask);

            // Vérifier qu'il n'y a pas de corruption des données
            logs.All(l => l.Message != null).Should().BeTrue();
            logs.All(l => l.Timestamp != default).Should().BeTrue();
        }

        [Fact]
        public void Result_Creation_Should_Be_Fast()
        {
            // Arrange
            const int iterations = 100000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = i % 2 == 0
                    ? Result.Success()
                    : Result.Failure($"Error {i}");
            }
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Time to create {iterations} Results: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Moins de 5 secondes pour 100k

            var averageTimePerResult = stopwatch.ElapsedMilliseconds / (double)iterations;
            _output.WriteLine($"Average time per Result: {averageTimePerResult:F4}ms");
            averageTimePerResult.Should().BeLessThan(0.05); // Moins de 0.05ms par Result
        }

        [Fact]
        public void Memory_Log_Limit_Should_Prevent_Memory_Leak()
        {
            // Arrange
            LogManager.Configure(LogDestination.Memory, maxMemoryLogs: 1000);
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Logger beaucoup plus que la limite
            for (int i = 0; i < 10000; i++)
            {
                LogManager.Log(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = $"This is a relatively long message to test memory consumption: {i}",
                    Error = "Some error details that take up memory space",
                    CallerMethod = $"VeryLongMethodNameToTestMemoryConsumption{i}",
                    CallerFile = $@"C:\Very\Long\Path\To\Test\Memory\Consumption\File{i}.cs",
                    CallerLine = i
                });
            }

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().HaveCount(1000); // Seulement les 1000 derniers

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            _output.WriteLine($"Memory increase: {memoryIncrease / 1024}KB");

            // La mémoire ne devrait pas augmenter de manière excessive
            memoryIncrease.Should().BeLessThan(10 * 1024 * 1024); // Moins de 10MB
        }

        [Fact]
        public void GetLogs_Should_Be_Performant_With_Large_Dataset()
        {
            // Arrange
            const int logCount = 10000;
            for (int i = 0; i < logCount; i++)
            {
                LogManager.Log(new LogEntry { Message = $"Message {i}" });
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var logs = LogManager.GetLogs(100); // Récupérer les 100 derniers
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Time to get 100 logs from {logCount}: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Moins de 100ms
            logs.Should().HaveCount(100);
        }

        [Fact]
        public void Result_Map_Chain_Performance()
        {
            // Arrange
            const int iterations = 10000;
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = Result<int>.Success(i)
                    .Map(x => x * 2)
                    .Map(x => x + 10)
                    .Map(x => x.ToString())
                    .Map(x => x.Length);
            }
            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Time for {iterations} Map chains: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);

            // Vérifier que tous les logs sont créés
            var logs = LogManager.GetLogs();
            logs.Should().HaveCountGreaterThan(iterations * 4); // Au moins 4 logs par chaîne
        }
    }
}