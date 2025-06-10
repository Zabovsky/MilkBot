using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TelegramBotWindowsService.Service
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly object LockObject = new object();
        private static string CurrentLogFile => Path.Combine(LogDirectory, $"bot_{DateTime.Now:yyyy-MM-dd}.log");
        private static bool _isInitialized = false;

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                _isInitialized = true;
                Log("Logger initialized successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                // Если не удалось создать директорию, будем использовать базовую директорию
                LogDirectory = AppDomain.CurrentDomain.BaseDirectory;
                try
                {
                    File.AppendAllText(
                        Path.Combine(LogDirectory, "logger_init_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to initialize logger: {ex}\n"
                    );
                }
                catch { /* игнорируем ошибку записи */ }
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!_isInitialized)
            {
                try
                {
                    File.AppendAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logger_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Logger not initialized. Message: {message}\n"
                    );
                }
                catch { /* игнорируем ошибку записи */ }
                return;
            }

            try
            {
                var processId = Process.GetCurrentProcess().Id;
                var threadId = Environment.CurrentManagedThreadId;
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [PID:{processId}] [TID:{threadId}] {message}";
                
                lock (LockObject)
                {
                    File.AppendAllText(CurrentLogFile, logMessage + Environment.NewLine);
                }

                // Для ошибок также выводим в консоль
                if (level == LogLevel.Error)
                {
                    Console.Error.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // Пытаемся записать ошибку логирования в отдельный файл
                    File.AppendAllText(
                        Path.Combine(LogDirectory, "logger_error.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to write log: {ex}\nOriginal message: {message}\n"
                    );
                }
                catch { /* игнорируем ошибку записи */ }
            }
        }

        public static void LogError(string message, Exception ex = null)
        {
            string fullMessage = ex != null 
                ? $"{message}\nException Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}"
                : message;
            
            Log(fullMessage, LogLevel.Error);
        }

        public static void LogWarning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        public static void LogInfo(string message)
        {
            Log(message, LogLevel.Info);
        }

        public static void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public static void LogServiceEvent(string eventType, string details = null)
        {
            var message = $"Service Event: {eventType}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $"\nDetails: {details}";
            }
            Log(message, LogLevel.Info);
        }

        public static void LogStartup()
        {
            var process = Process.GetCurrentProcess();
            var message = $"Application starting up\n" +
                         $"Process ID: {process.Id}\n" +
                         $"Working Directory: {AppDomain.CurrentDomain.BaseDirectory}\n" +
                         $"OS Version: {Environment.OSVersion}\n" +
                         $".NET Version: {Environment.Version}\n" +
                         $"Machine Name: {Environment.MachineName}";
            
            Log(message, LogLevel.Info);
        }

        public static void LogShutdown(string reason = null)
        {
            var message = "Application shutting down";
            if (!string.IsNullOrEmpty(reason))
            {
                message += $"\nReason: {reason}";
            }
            Log(message, LogLevel.Info);
        }

        public static async Task CleanOldLogs(int daysToKeep = 30)
        {
            try
            {
                var directory = new DirectoryInfo(LogDirectory);
                var files = directory.GetFiles("bot_*.log");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    if (file.CreationTime < cutoffDate)
                    {
                        try
                        {
                            file.Delete();
                            Log($"Deleted old log file: {file.Name}", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            LogError($"Failed to delete old log file: {file.Name}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to clean old logs", ex);
            }
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
} 