using FluentAssertions;
using MatthL.ResultLogger.Core;
using MatthL.ResultLogger.Core.Enums;
using MatthL.ResultLogger.Core.Managers;
using MatthL.ResultLogger.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MatthL.ResultLogger.Tests
{
    public class LogManagerTests : IDisposable
    {
        private readonly string _testLogPath;

        public LogManagerTests()
        {
            _testLogPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
            LogManager.Clear(); // Nettoyer avant chaque test
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
        public void Configure_Memory_Should_Set_Destination_To_Memory()
        {
            // Act
            LogManager.Configure(LogDestination.Memory);

            // Assert
            LogManager.GetLogFilePath().Should().BeNull();
        }

        [Fact]
        public void Configure_TempFile_Should_Create_Log_File_In_Temp()
        {
            // Act
            LogManager.Configure(LogDestination.TempFile);

            // Assert
            var logPath = LogManager.GetLogFilePath();
            logPath.Should().NotBeNullOrWhiteSpace();
            Path.GetDirectoryName(logPath).Should().Be(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
        }

        [Fact]
        public void Configure_CustomFile_Should_Use_Provided_Path()
        {
            // Act
            LogManager.Configure(LogDestination.CustomFile, _testLogPath);

            // Assert
            LogManager.GetLogFilePath().Should().Be(_testLogPath);
        }

        [Fact]
        public void Configure_CustomFile_Without_Path_Should_Throw()
        {
            // Act & Assert
            var act = () => LogManager.Configure(LogDestination.CustomFile, null);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Log_Should_Add_Entry_To_Memory()
        {
            // Arrange
            LogManager.Configure(LogDestination.Memory);
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Info,
                Message = "Test message",
                CallerMethod = "TestMethod",
                CallerFile = "TestFile.cs",
                CallerLine = 42
            };

            // Act
            LogManager.Log(entry);

            // Assert
            var logs = LogManager.GetLogs();
            logs.Should().ContainSingle();
            logs[0].Message.Should().Be("Test message");
            logs[0].CallerMethod.Should().Be("TestMethod");
        }

        [Fact]
        public void Log_To_File_Should_Write_To_Disk()
        {
            // Arrange
            LogManager.Configure(LogDestination.CustomFile, _testLogPath);
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Error,
                Message = "Error message",
                Error = "Something went wrong",
                CallerMethod = "TestMethod",
                CallerFile = "TestFile.cs",
                CallerLine = 100
            };

            // Act
            LogManager.Log(entry);
            Thread.Sleep(100); // Attendre que l'écriture soit faite

            // Assert
            File.Exists(_testLogPath).Should().BeTrue();
            var content = File.ReadAllText(_testLogPath);
            content.Should().Contain("Error message");
            content.Should().Contain("Something went wrong");
        }

        [Fact]
        public void GetLogs_With_Count_Should_Return_Last_N_Logs()
        {
            // Arrange
            LogManager.Configure(LogDestination.Memory);
            for (int i = 0; i < 10; i++)
            {
                LogManager.Log(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = $"Message {i}"
                });
            }

            // Act
            var logs = LogManager.GetLogs(3);

            // Assert
            logs.Count.Should().Be(3);
            logs[0].Message.Should().Be("Message 7");
            logs[1].Message.Should().Be("Message 8");
            logs[2].Message.Should().Be("Message 9");
        }

        [Fact]
        public void Clear_Should_Remove_All_Logs_From_Memory()
        {
            // Arrange
            LogManager.Configure(LogDestination.Memory);
            LogManager.Log(new LogEntry { Message = "Test" });
            LogManager.Log(new LogEntry { Message = "Test2" });

            // Act
            LogManager.Clear();

            // Assert
            LogManager.GetLogs().Should().BeEmpty();
        }

        [Fact]
        public void Max_Memory_Logs_Should_Limit_Queue_Size()
        {
            // Arrange
            LogManager.Configure(LogDestination.Memory, maxMemoryLogs: 5);

            // Act
            for (int i = 0; i < 10; i++)
            {
                LogManager.Log(new LogEntry
                {
                    Message = $"Message {i}"
                });
            }

            // Assert
            var logs = LogManager.GetLogs();
            logs.Count.Should().Be(5);
            logs.First().Message.Should().Be("Message 5");
            logs.Last().Message.Should().Be("Message 9");
        }

        [Fact]
        public void LogEntry_ToString_Should_Format_Correctly()
        {
            // Arrange
            var entry = new LogEntry
            {
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 45, 123),
                Level = LogLevel.Error,
                Message = "Test message",
                Error = "Error details",
                CallerMethod = "TestMethod",
                CallerFile = @"C:\Projects\Test\File.cs",
                CallerLine = 42
            };

            // Act
            var result = entry.ToString();

            // Assert
            result.Should().Contain("[2024-01-15 10:30:45.123]");
            result.Should().Contain("[Error]");
            result.Should().Contain("TestMethod");
            result.Should().Contain("Test message");
            result.Should().Contain("Error: Error details");
            result.Should().Contain("File.cs:42");
        }
    }
}