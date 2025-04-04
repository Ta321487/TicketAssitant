using System.IO;
using System.Text;

namespace TA_WPF.Utils
{
    public static class LogHelper
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFileName = "app_log.txt";
        private static readonly string SystemLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TicketAssist", "SystemLogs");
        private static readonly string SystemLogFileName = "system_log.txt";
        private static readonly object LockObj = new object();
        private static bool _isInitialized = false;
        private static readonly int MaxLogSizeBytes = 10 * 1024 * 1024; // 10MB
        private static readonly int MaxSystemLogSizeBytes = 50 * 1024 * 1024; // 50MB
        private static Timer _autoExportTimer;

        static LogHelper()
        {
            try
            {
                // 确保日志目录存在
                if (!Directory.Exists(LogFilePath))
                {
                    Directory.CreateDirectory(LogFilePath);
                }

                // 确保系统日志目录存在
                if (!Directory.Exists(SystemLogPath))
                {
                    Directory.CreateDirectory(SystemLogPath);
                }

                _isInitialized = true;

                // 启动自动导出系统日志的定时器（每小时检测一次）
                _autoExportTimer = new Timer(CheckAndRotateSystemLog, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化日志系统时出错: {ex.Message}");
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

        /// <summary>
        /// 记录车票相关信息日志
        /// </summary>
        public static void LogTicket(string action, string ticketInfo)
        {
            string message = $"车票{action}: {ticketInfo}";
            Log("INFO", message);
        }

        /// <summary>
        /// 记录车票相关警告日志
        /// </summary>
        public static void LogTicketWarning(string action, string ticketInfo)
        {
            string message = $"车票{action}: {ticketInfo}";
            Log("WARNING", message);
        }

        /// <summary>
        /// 记录车票相关错误日志
        /// </summary>
        public static void LogTicketError(string action, string ticketInfo, Exception exception = null)
        {
            string message = $"车票{action}失败: {ticketInfo}";
            if (exception == null)
            {
                Log("ERROR", message);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(message);
                sb.AppendLine($"异常类型: {exception.GetType().FullName}");
                sb.AppendLine($"异常消息: {exception.Message}");
                sb.AppendLine($"堆栈跟踪: {exception.StackTrace}");

                Log("ERROR", sb.ToString());
            }
        }

        /// <summary>
        /// 记录系统相关信息日志
        /// </summary>
        public static void LogSystem(string component, string message)
        {
            string logMessage = $"系统组件[{component}]: {message}";
            Log("INFO", logMessage);
        }

        /// <summary>
        /// 记录系统相关警告日志
        /// </summary>
        public static void LogSystemWarning(string component, string message)
        {
            string logMessage = $"系统组件[{component}]警告: {message}";
            Log("WARNING", logMessage);
        }

        /// <summary>
        /// 记录系统相关错误日志
        /// </summary>
        public static void LogSystemError(string component, string message, Exception exception = null)
        {
            if (exception == null)
            {
                string logMessage = $"系统组件[{component}]错误: {message}";
                Log("ERROR", logMessage);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"系统组件[{component}]错误: {message}");
                sb.AppendLine($"异常类型: {exception.GetType().FullName}");
                sb.AppendLine($"异常消息: {exception.Message}");
                sb.AppendLine($"堆栈跟踪: {exception.StackTrace}");

                Log("ERROR", sb.ToString());
            }
        }

        private static void Log(string level, string message)
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine($"[{level}] {message} (日志系统未初始化)");
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
                            // 判断是否为系统相关日志或车票相关日志
                            bool isSystemLog = IsSystemLog(message);
                            bool isTicketLog = IsTicketLog(message);

                            // 系统级别日志仅写入系统日志文件
                            if (isSystemLog)
                            {
                                string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                                File.AppendAllText(systemLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                            }
                            // 车票相关日志仅写入应用程序日志
                            else if (isTicketLog)
                            {
                                string appLogPath = Path.Combine(LogFilePath, LogFileName);
                                File.AppendAllText(appLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                            }
                            // 其他日志写入两个文件
                            else
                            {
                                // 写入应用程序日志
                                string appLogPath = Path.Combine(LogFilePath, LogFileName);
                                File.AppendAllText(appLogPath, logEntry + Environment.NewLine, Encoding.UTF8);

                                // 写入系统日志
                                string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                                File.AppendAllText(systemLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                            }
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
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入日志时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否为系统级别日志
        /// </summary>
        private static bool IsSystemLog(string message)
        {
            // 系统级别：数据库、创建窗口等相关日志
            string[] systemKeywords =
            {
                "数据库", "连接", "MySQL", "初始化", "配置", "窗口",
                "服务器", "IP", "目录", "文件", "配置文件", "设置",
                "COM初始化", "异常堆栈", "System", "Database", "Window"
            };

            foreach (var keyword in systemKeywords)
            {
                if (message.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断是否为车票相关日志
        /// </summary>
        private static bool IsTicketLog(string message)
        {
            // 车票相关：车票操作、查询等相关日志
            string[] ticketKeywords =
            {
                "车票", "添加车票", "修改车票", "删除车票", "查询车票",
                "保存车票", "加载车票", "刷新车票", "导出车票",
                "Ticket", "车站", "座位", "金额", "出发", "到达"
            };

            foreach (var keyword in ticketKeywords)
            {
                if (message.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有应用程序日志
        /// </summary>
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
                System.Diagnostics.Debug.WriteLine($"读取日志时出错: {ex.Message}");
            }

            return new string[0];
        }

        /// <summary>
        /// 导出应用程序日志到指定路径
        /// </summary>
        public static bool ExportLogs(string targetPath)
        {
            if (!_isInitialized)
            {
                return false;
            }

            try
            {
                // 确保目标目录存在
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                string appLogPath = Path.Combine(LogFilePath, LogFileName);
                string targetFile = Path.Combine(targetPath, $"app_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                if (File.Exists(appLogPath))
                {
                    File.Copy(appLogPath, targetFile, true);
                    // 记录到系统日志中，不要记录到应用日志中
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] 应用程序日志已导出到: {targetFile}";
                    string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                    lock (LockObj)
                    {
                        File.AppendAllText(systemLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出日志时出错: {ex.Message}");
                // 记录错误到系统日志
                string errorEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 导出日志时出错: {ex.Message}";
                try
                {
                    string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                    lock (LockObj)
                    {
                        File.AppendAllText(systemLogPath, errorEntry + Environment.NewLine, Encoding.UTF8);
                    }
                }
                catch
                {
                    // 忽略写入错误的异常
                }
            }

            return false;
        }

        /// <summary>
        /// 导出系统日志到指定路径
        /// </summary>
        public static bool ExportSystemLogs(string targetPath)
        {
            if (!_isInitialized)
            {
                return false;
            }

            try
            {
                // 确保目标目录存在
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                string targetFile = Path.Combine(targetPath, $"system_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                if (File.Exists(systemLogPath))
                {
                    File.Copy(systemLogPath, targetFile, true);
                    // 记录只到系统日志中
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] 系统日志已导出到: {targetFile}";
                    lock (LockObj)
                    {
                        File.AppendAllText(systemLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出系统日志时出错: {ex.Message}");
                // 记录错误到系统日志，尝试写入
                string errorEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 导出系统日志时出错: {ex.Message}";
                try
                {
                    string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                    lock (LockObj)
                    {
                        File.AppendAllText(systemLogPath, errorEntry + Environment.NewLine, Encoding.UTF8);
                    }
                }
                catch
                {
                    // 忽略写入错误的异常  
                }
            }

            return false;
        }

        /// <summary>
        /// 检测并轮换系统日志
        /// </summary>
        private static void CheckAndRotateSystemLog(object state)
        {
            try
            {
                string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);

                if (File.Exists(systemLogPath))
                {
                    FileInfo fileInfo = new FileInfo(systemLogPath);

                    // 如果日志文件超过最大大小，进行轮换
                    if (fileInfo.Length > MaxSystemLogSizeBytes)
                    {
                        // 创建归档目录
                        string archivePath = Path.Combine(SystemLogPath, "Archive");
                        if (!Directory.Exists(archivePath))
                        {
                            Directory.CreateDirectory(archivePath);
                        }

                        // 移动当前日志文件到归档目录
                        string archiveFile = Path.Combine(archivePath, $"system_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        File.Move(systemLogPath, archiveFile);

                        // 创建新的日志文件
                        using (File.Create(systemLogPath)) { }

                        // 记录日志轮换信息
                        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] 系统日志已轮换，旧日志已归档到: {archiveFile}";
                        File.AppendAllText(systemLogPath, logEntry + Environment.NewLine, Encoding.UTF8);

                        // 清理过期的归档日志（保留最近30天的）
                        CleanupOldArchives(archivePath, 30);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"轮换系统日志时出错: {ex.Message}");
                // 不要调用LogError，避免循环依赖
                try
                {
                    // 尝试直接写入系统日志
                    string systemLogPath = Path.Combine(SystemLogPath, SystemLogFileName);
                    if (File.Exists(systemLogPath))
                    {
                        string errorEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] 轮换系统日志时出错: {ex.Message}";
                        lock (LockObj)
                        {
                            File.AppendAllText(systemLogPath, errorEntry + Environment.NewLine, Encoding.UTF8);
                        }
                    }
                }
                catch
                {
                    // 如果写入失败，忽略此异常
                }
            }
        }

        /// <summary>
        /// 清理过期的归档日志
        /// </summary>
        private static void CleanupOldArchives(string archivePath, int daysToKeep)
        {
            try
            {
                if (!Directory.Exists(archivePath))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (string file in Directory.GetFiles(archivePath, "system_log_*.txt"))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理过期归档日志时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取系统日志路径
        /// </summary>
        public static string GetSystemLogPath()
        {
            return SystemLogPath;
        }

        /// <summary>
        /// 获取应用程序日志路径
        /// </summary>
        public static string GetAppLogPath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// 获取应用程序日志文件名
        /// </summary>
        public static string GetAppLogFileName()
        {
            return LogFileName;
        }

        /// <summary>
        /// 获取系统日志文件名
        /// </summary>
        public static string GetSystemLogFileName()
        {
            return SystemLogFileName;
        }
    }
}