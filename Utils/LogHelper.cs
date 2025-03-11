using System;
using System.IO;
using System.Text;
using System.Threading;

namespace TA_WPF.Utils
{
    public static class LogHelper
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFileName = "app_log.txt";
        private static readonly object LockObj = new object();
        private static bool _isInitialized = false;

        static LogHelper()
        {
            try
            {
                // 确保日志目录存在
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化日志系统时出错: {ex.Message}");
                _isInitialized = false;
            }
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public static void LogError(string message)
        {
            Log("ERROR", message);
        }

        public static void LogError(string message, Exception exception)
        {
            if (exception == null)
            {
                Log("ERROR", message);
                return;
            }
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine($"异常类型: {exception.GetType().FullName}");
            sb.AppendLine($"异常消息: {exception.Message}");
            sb.AppendLine($"堆栈跟踪: {exception.StackTrace}");
            
            // 记录内部异常
            var innerException = exception.InnerException;
            int depth = 0;
            while (innerException != null && depth < 5)
            {
                sb.AppendLine($"内部异常 {++depth}: {innerException.GetType().FullName}");
                sb.AppendLine($"内部异常消息: {innerException.Message}");
                sb.AppendLine($"内部异常堆栈: {innerException.StackTrace}");
                innerException = innerException.InnerException;
            }
            
            Log("ERROR", sb.ToString());
        }

        private static void Log(string level, string message)
        {
            if (!_isInitialized)
            {
                Console.WriteLine($"[{level}] {message} (日志系统未初始化)");
                return;
            }
            
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                
                // 使用多次重试机制
                int retryCount = 0;
                bool success = false;
                
                while (!success && retryCount < 3)
                {
                    try
                    {
                        lock (LockObj)
                        {
                            string fullPath = Path.Combine(LogFilePath, LogFileName);
                            File.AppendAllText(fullPath, logEntry + Environment.NewLine, Encoding.UTF8);
                        }
                        success = true;
                    }
                    catch (IOException)
                    {
                        // 文件可能被占用，等待一段时间后重试
                        retryCount++;
                        Thread.Sleep(100 * retryCount);
                    }
                }
                
                // 输出到控制台（调试用）
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志时出错: {ex.Message}");
            }
        }

        public static string[] GetAllLogs()
        {
            if (!_isInitialized)
            {
                return new string[] { "日志系统未初始化" };
            }
            
            try
            {
                string fullPath = Path.Combine(LogFilePath, LogFileName);
                if (File.Exists(fullPath))
                {
                    return File.ReadAllLines(fullPath, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取日志时出错: {ex.Message}");
            }
            
            return new string[0];
        }

        public static bool ExportLogs(string targetPath)
        {
            if (!_isInitialized)
            {
                return false;
            }
            
            try
            {
                string fullPath = Path.Combine(LogFilePath, LogFileName);
                string targetFile = Path.Combine(targetPath, $"app_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                if (File.Exists(fullPath))
                {
                    File.Copy(fullPath, targetFile, true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出日志时出错: {ex.Message}");
            }
            
            return false;
        }
    }
} 