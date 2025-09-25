# ResultLogger

[![NuGet](https://img.shields.io/nuget/v/MatthL.ResultLogger.svg)](https://www.nuget.org/packages/MatthL.ResultLogger/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Simple result pattern with integrated logging for .NET. Handle success/failure cases with automatic logging and debugging information.


## Installation
```bash
dotnet add package MatthL.ResultFlow
```
## 📖 Overview

The Result class is the class that allows logging while a good way to transmit objects in failable methods.
When a new Result class is created it is automatically logged with the optional parameters and informations from the line and function that called it, allowing a better debugging. 
## ⚙️ Configuration
Configure the logging destination at application startup:
```C#
LogManager.Configure(
    destination: LogDestination.Memory,     // Memory, TempFiles, or CustomPath
    customPath: @"C:\Logs\MyApp",          // Required if CustomPath selected
    maxMemoryLogs: 1000                    // Limit memory usage (default: unlimited)
);
```

## 💡 Usage
### Basic Success/Failure
```C#
// Success case
public Result<User> GetUser(int id)
{
    var user = database.Find(id);
    if (user != null)
    {
        return Result<User>.Success(user, "User found successfully");
    }
    
    return Result<User>.Failure($"User {id} not found", "NotFound", LogLevel.Warning);
}
```
### Chaining Operations
```C#
public Result ProcessOrder(Order order)
{
    var validation = ValidateOrder(order);
    if (!validation.IsSuccess)
        return validation; // Propagate failure
        
    var payment = ProcessPayment(order);
    if (!payment.IsSuccess)
        return payment; // Propagate failure
        
    return Result.Success("Order processed successfully");
}
```
### Retrieving Logs
```C#
// Get last 100 logs
var recentLogs = LogManager.GetLogs(100);

// Get all logs
var allLogs = LogManager.GetLogs(-1);

// Display logs
foreach (var log in recentLogs)
{
    Console.WriteLine($"[{log.Level}] {log.TimeStamp}: {log.Message}");
}
```
### 🎯 Features
✅ Automatic Logging - Every Result is logged with context
✅ Failure Tracking - Line numbers and method names included
✅ Flexible Storage - Memory, temp files, or custom path
✅ Type-Safe - Generic Result<T> for any return type
✅ Memory Efficient - Configurable log retention

### 📝 Example: Complete Flow
```C#
public class UserService
{
    public Result<User> CreateUser(string email, string password)
    {
        // Validate input
        if (string.IsNullOrEmpty(email))
            return Result<User>.Failure("Email is required", "ValidationError");
            
        if (!IsValidEmail(email))
            return Result<User>.Failure("Invalid email format", "ValidationError");
            
        // Check if exists
        if (UserExists(email))
            return Result<User>.Failure("User already exists", "Conflict");
            
        // Create user
        try
        {
            var user = new User { Email = email };
            database.Save(user);
            return Result<User>.Success(user, $"User {email} created");
        }
        catch (Exception ex)
        {
            return Result<User>.Failure($"Database error: {ex.Message}", "DatabaseError", LogLevel.Error);
        }
    }
}
```
### 🔍 Log Output Example
```C#
[INFO]  2024-01-24 10:15:23 | CreateUser:45 | User test@example.com created
[WARN]  2024-01-24 10:15:45 | CreateUser:35 | User already exists
[ERROR] 2024-01-24 10:16:02 | CreateUser:52 | Database error: Connection timeout
```

### 📄 License
MIT License - see LICENSE file for details.

<div align="center">
Made with ❤️ by <a href="https://github.com/matthieu-x">Matthieu L</a>
</div>