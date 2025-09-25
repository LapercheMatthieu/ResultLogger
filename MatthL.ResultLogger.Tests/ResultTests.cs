using FluentAssertions;
using MatthL.ResultLogger.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MatthL.ResultLogger.Tests
{
    public class ResultTests
    {
        [Fact]
        public void Success_Should_Create_Successful_Result()
        {
            // Act
            var result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Failure_Should_Create_Failed_Result()
        {
            // Arrange
            const string errorMessage = "Operation failed";

            // Act
            var result = Result.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(errorMessage);
        }

        [Fact]
        public void Combine_Should_Return_Success_When_All_Results_Are_Successful()
        {
            // Arrange
            var result1 = Result.Success();
            var result2 = Result.Success();
            var result3 = Result.Success();

            // Act
            var combined = Result.Combine(result1, result2, result3);

            // Assert
            combined.IsSuccess.Should().BeTrue();
            combined.Error.Should().BeNull();
        }

        [Fact]
        public void Combine_Should_Return_First_Failure()
        {
            // Arrange
            var result1 = Result.Success();
            var result2 = Result.Failure("Error 2");
            var result3 = Result.Failure("Error 3");

            // Act
            var combined = Result.Combine(result1, result2, result3);

            // Assert
            combined.IsFailure.Should().BeTrue();
            combined.Error.Should().Be("Error 2");
        }

        [Fact]
        public void Combine_With_Empty_Array_Should_Return_Success()
        {
            // Act
            var combined = Result.Combine();

            // Assert
            combined.IsSuccess.Should().BeTrue();
        }
    }

    public class ResultOfTTests
    {
        [Fact]
        public void Success_With_Value_Should_Create_Successful_Result()
        {
            // Arrange
            const int value = 42;

            // Act
            var result = Result<int>.Success(value);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(value);
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Failure_Should_Create_Failed_Result_With_Default_Value()
        {
            // Arrange
            const string errorMessage = "Failed to get value";

            // Act
            var result = Result<int>.Failure(errorMessage);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Value.Should().Be(default(int));
            result.Error.Should().Be(errorMessage);
        }

        [Fact]
        public void Map_Should_Transform_Value_When_Success()
        {
            // Arrange
            var result = Result<int>.Success(10);

            // Act
            var mapped = result.Map(x => x * 2);

            // Assert
            mapped.IsSuccess.Should().BeTrue();
            mapped.Value.Should().Be(20);
        }

        [Fact]
        public void Map_Should_Preserve_Error_When_Failure()
        {
            // Arrange
            const string error = "Original error";
            var result = Result<int>.Failure(error);

            // Act
            var mapped = result.Map(x => x * 2);

            // Assert
            mapped.IsFailure.Should().BeTrue();
            mapped.Error.Should().Be(error);
            mapped.Value.Should().Be(0);
        }

        [Fact]
        public void Map_Should_Work_With_Different_Types()
        {
            // Arrange
            var result = Result<int>.Success(42);

            // Act
            var mapped = result.Map(x => x.ToString());

            // Assert
            mapped.Should().BeOfType<Result<string>>();
            mapped.IsSuccess.Should().BeTrue();
            mapped.Value.Should().Be("42");
        }

        [Fact]
        public void Map_Chain_Should_Work()
        {
            // Arrange
            var result = Result<int>.Success(10);

            // Act
            var final = result
                .Map(x => x * 2)
                .Map(x => x + 5)
                .Map(x => x.ToString());

            // Assert
            final.Value.Should().Be("25");
        }
    }
}
