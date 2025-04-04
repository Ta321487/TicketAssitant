using System.Windows.Input;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 设置视图模型，负责管理设置页面的数据
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configurationService;
        private readonly UIService _uiService;
        private readonly NavigationService _navigationService;
        private readonly DatabaseService _databaseService;
        
        private double _fontSize = 13; // 默认字体大小
        private string _serverAddress = "localhost";
        private string _username = "root";
        private string _password = "password";
        private string _newConnectionString = "";
        private string _newDatabaseName = "";
        private bool _showPassword = false;
        private bool _suspendFontSizeUpdate = false; // 是否暂停字体大小更新
        private bool _isLoading;
        private string _systemLogLocation;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        /// <param name="uiService">UI服务</param>
        /// <param name="navigationService">导航服务</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public SettingsViewModel(
            ConfigurationService configurationService,
            UIService uiService,
            NavigationService navigationService,
            DatabaseService databaseService,
            string connectionString) : base()
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // 解析连接字符串，提取服务器地址、用户名和密码
            var connectionInfo = _configurationService.ParseConnectionString(connectionString);
            _serverAddress = connectionInfo.ServerAddress;
            _username = connectionInfo.Username;
            _password = connectionInfo.Password;
            
            // 从配置文件加载字体大小
            _fontSize = _configurationService.LoadFontSizeFromConfig();
            
            // 获取系统日志位置
            _systemLogLocation = LogHelper.GetSystemLogPath();
            
            // 初始化命令
            ModifyConnectionCommand = new RelayCommand(ModifyConnection);
            UpdateConnectionCommand = new RelayCommand(UpdateConnection);
            UpdateDatabaseCommand = new RelayCommand(UpdateDatabase);
            ExportLogCommand = new RelayCommand(ExportLog);
            ExportSystemLogCommand = new RelayCommand(ExportSystemLog);
            OpenSystemLogDirCommand = new RelayCommand(OpenSystemLogDir);
            OpenAppLogDirCommand = new RelayCommand(OpenAppLogDir);
            ExportAllLogsCommand = new RelayCommand(ExportAllLogs);
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    // 四舍五入到整数值
                    double roundedValue = Math.Round(value);
                    
                    // 确保在有效范围内 (12-20)
                    if (roundedValue < 12) roundedValue = 12;
                    if (roundedValue > 20) roundedValue = 20;
                    
                    _fontSize = roundedValue;
                    OnPropertyChanged(nameof(FontSize));
                    
                    // 如果暂停更新，则不执行后续操作
                    if (SuspendFontSizeUpdate)
                        return;
                    
                    // 应用字体大小
                    _uiService.ApplyFontSize(roundedValue);
                    
                    // 保存字体大小到配置文件
                    _configurationService.SaveFontSizeToConfig(roundedValue);
                }
            }
        }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    OnPropertyChanged(nameof(ServerAddress));
                }
            }
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                }
            }
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                    OnPropertyChanged(nameof(DisplayPassword));
                }
            }
        }

        /// <summary>
        /// 显示密码
        /// </summary>
        public string DisplayPassword
        {
            get => _showPassword ? _password : new string('*', _password.Length);
        }

        /// <summary>
        /// 是否显示密码
        /// </summary>
        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                if (_showPassword != value)
                {
                    _showPassword = value;
                    OnPropertyChanged(nameof(ShowPassword));
                    OnPropertyChanged(nameof(DisplayPassword));
                }
            }
        }

        /// <summary>
        /// 新的连接字符串
        /// </summary>
        public string NewConnectionString
        {
            get => _newConnectionString;
            set
            {
                if (_newConnectionString != value)
                {
                    _newConnectionString = value;
                    OnPropertyChanged(nameof(NewConnectionString));
                }
            }
        }

        /// <summary>
        /// 新的数据库名称
        /// </summary>
        public string NewDatabaseName
        {
            get => _newDatabaseName;
            set
            {
                if (_newDatabaseName != value)
                {
                    _newDatabaseName = value;
                    OnPropertyChanged(nameof(NewDatabaseName));
                }
            }
        }

        /// <summary>
        /// 是否暂停字体大小更新
        /// </summary>
        public bool SuspendFontSizeUpdate
        {
            get => _suspendFontSizeUpdate;
            set
            {
                if (_suspendFontSizeUpdate != value)
                {
                    _suspendFontSizeUpdate = value;
                    OnPropertyChanged(nameof(SuspendFontSizeUpdate));
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        /// <summary>
        /// 系统日志保存位置
        /// </summary>
        public string SystemLogLocation
        {
            get => _systemLogLocation;
            private set
            {
                if (_systemLogLocation != value)
                {
                    _systemLogLocation = value;
                    OnPropertyChanged(nameof(SystemLogLocation));
                }
            }
        }

        /// <summary>
        /// 重写IsDarkMode属性，确保与BaseViewModel同步
        /// </summary>
        public override bool IsDarkMode
        {
            get => base.IsDarkMode;
            set => base.IsDarkMode = value;
        }

        /// <summary>
        /// 修改连接命令
        /// </summary>
        public ICommand ModifyConnectionCommand { get; }

        /// <summary>
        /// 更新连接命令
        /// </summary>
        public ICommand UpdateConnectionCommand { get; }

        /// <summary>
        /// 更新数据库命令
        /// </summary>
        public ICommand UpdateDatabaseCommand { get; }

        /// <summary>
        /// 导出日志命令
        /// </summary>
        public ICommand ExportLogCommand { get; }

        /// <summary>
        /// 导出系统日志命令
        /// </summary>
        public ICommand ExportSystemLogCommand { get; }

        /// <summary>
        /// 打开系统日志目录命令
        /// </summary>
        public ICommand OpenSystemLogDirCommand { get; }
        
        /// <summary>
        /// 打开应用程序日志目录命令
        /// </summary>
        public ICommand OpenAppLogDirCommand { get; }

        /// <summary>
        /// 一键导出所有日志命令（用于问题反馈）
        /// </summary>
        public ICommand ExportAllLogsCommand { get; }

        /// <summary>
        /// 修改连接
        /// </summary>
        private void ModifyConnection()
        {
            // 显示修改连接的UI
            NewConnectionString = $"server={ServerAddress};user={Username};password={Password};";
            
            // 记录日志
            LogHelper.LogInfo("用户请求修改数据库连接");
        }

        /// <summary>
        /// 更新连接
        /// </summary>
        private void UpdateConnection()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewConnectionString))
                {
                    MessageBoxHelper.ShowWarning("连接字符串不能为空");
                    return;
                }
                
                // 显示加载动画
                IsLoading = true;
                
                // 模拟连接过程
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 解析新的连接字符串
                        var connectionInfo = _configurationService.ParseConnectionString(NewConnectionString);
                        ServerAddress = connectionInfo.ServerAddress;
                        Username = connectionInfo.Username;
                        Password = connectionInfo.Password;
                        
                        // 显示成功消息
                        MessageBoxHelper.ShowInfo("已注销连接信息，请重新登录");
                        
                        // 记录日志
                        LogHelper.LogInfo("用户更新了数据库连接信息");
                        
                        // 隐藏加载动画
                        IsLoading = false;
                        
                        // 获取数据库名称
                        string databaseName = _configurationService.ExtractDatabaseName(NewConnectionString);
                        
                        // 返回登录窗口
                        _navigationService.NavigateToLogin(databaseName);
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"更新连接时出错: {ex.Message}");
                LogHelper.LogError($"更新连接时出错: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新数据库
        /// </summary>
        private void UpdateDatabase()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewDatabaseName))
                {
                    MessageBoxHelper.ShowWarning("数据库名称不能为空");
                    return;
                }
                
                // 保存数据库名称到历史记录
                _configurationService.SaveDatabaseNameToHistory(NewDatabaseName);
                
                // 保存数据库名称到配置文件，以便重启后登录窗口能够读取
                _configurationService.SaveLastDatabaseName(NewDatabaseName);
                
                // 显示确认对话框
                var result = MessageBoxHelper.ShowQuestion("修改数据库需要重新登录，是否继续？");
                
                if (result == true)
                {
                    // 显示成功消息
                    MessageBoxHelper.ShowInfo("已更新数据库连接信息，应用程序将返回登录界面");
                    
                    // 记录日志
                    LogHelper.LogInfo($"用户更新了数据库名称为: {NewDatabaseName}，应用程序将返回登录界面");
                    
                    // 强制垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    // 返回登录窗口
                    _navigationService.NavigateToLogin(NewDatabaseName);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"更新数据库名称时出错: {ex.Message}");
                LogHelper.LogError($"更新数据库名称时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出应用程序日志
        /// </summary>
        private void ExportLog()
        {
            try
            {
                // 显示加载动画
                IsLoading = true;
                
                // 创建保存文件对话框
                var dialog = new SaveFileDialog
                {
                    Title = "导出应用程序日志",
                    Filter = "文本文件 (*.txt)|*.txt",
                    DefaultExt = ".txt",
                    FileName = $"app_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };
                
                // 显示对话框
                bool? result = dialog.ShowDialog();
                
                if (result == true)
                {
                    string selectedPath = Path.GetDirectoryName(dialog.FileName);
                    string fileName = Path.GetFileName(dialog.FileName);
                    
                    // 导出日志
                    string appLogPath = Path.Combine(LogHelper.GetAppLogPath(), LogHelper.GetAppLogFileName());
                    
                    if (File.Exists(appLogPath))
                    {
                        File.Copy(appLogPath, dialog.FileName, true);
                        
                        // 显示成功消息
                        MessageBoxHelper.ShowInfo($"应用程序日志已成功导出到：\n{dialog.FileName}");
                        
                        // 记录日志
                        LogHelper.LogInfo($"用户导出了应用程序日志到：{dialog.FileName}");
                    }
                    else
                    {
                        // 显示错误消息
                        MessageBoxHelper.ShowError("导出日志失败，日志文件不存在。");
                    }
                }
                
                // 隐藏加载动画
                IsLoading = false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"导出日志时出错: {ex.Message}");
                LogHelper.LogError($"导出日志时出错: {ex.Message}");
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 导出系统日志
        /// </summary>
        private void ExportSystemLog()
        {
            try
            {
                // 显示加载动画
                IsLoading = true;
                
                // 创建保存文件对话框
                var dialog = new SaveFileDialog
                {
                    Title = "导出系统日志",
                    Filter = "文本文件 (*.txt)|*.txt",
                    DefaultExt = ".txt",
                    FileName = $"system_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };
                
                // 显示对话框
                bool? result = dialog.ShowDialog();
                
                if (result == true)
                {
                    string selectedPath = Path.GetDirectoryName(dialog.FileName);
                    string fileName = Path.GetFileName(dialog.FileName);
                    
                    // 导出系统日志
                    string systemLogPath = Path.Combine(LogHelper.GetSystemLogPath(), LogHelper.GetSystemLogFileName());
                    
                    if (File.Exists(systemLogPath))
                    {
                        File.Copy(systemLogPath, dialog.FileName, true);
                        
                        // 显示成功消息，包含系统日志的原始位置和导出位置
                        string message = $"系统日志已成功导出！\n\n" +
                                         $"原始位置：\n{systemLogPath}\n\n" +
                                         $"导出位置：\n{dialog.FileName}";
                        
                        MessageBoxHelper.ShowInfo(message);
                        
                        // 记录日志
                        LogHelper.LogInfo($"用户导出了系统日志到：{dialog.FileName}");
                    }
                    else
                    {
                        // 显示错误消息
                        MessageBoxHelper.ShowError("导出系统日志失败，系统日志文件不存在。");
                    }
                }
                
                // 隐藏加载动画
                IsLoading = false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"导出系统日志时出错: {ex.Message}");
                LogHelper.LogError($"导出系统日志时出错: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 打开系统日志目录
        /// </summary>
        private void OpenSystemLogDir()
        {
            try
            {
                string systemLogPath = LogHelper.GetSystemLogPath();
                
                if (Directory.Exists(systemLogPath))
                {
                    // 使用资源管理器打开目录
                    System.Diagnostics.Process.Start("explorer.exe", systemLogPath);
                    
                    // 记录日志
                    LogHelper.LogInfo($"用户打开了系统日志目录：{systemLogPath}");
                }
                else
                {
                    MessageBoxHelper.ShowWarning("系统日志目录不存在，可能是权限问题或目录已被删除。");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开系统日志目录时出错: {ex.Message}");
                LogHelper.LogError($"打开系统日志目录时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 打开应用程序日志目录
        /// </summary>
        private void OpenAppLogDir()
        {
            try
            {
                string appLogPath = LogHelper.GetAppLogPath();
                
                if (Directory.Exists(appLogPath))
                {
                    // 使用资源管理器打开目录
                    System.Diagnostics.Process.Start("explorer.exe", appLogPath);
                    
                    // 记录日志
                    LogHelper.LogInfo($"用户打开了应用程序日志目录：{appLogPath}");
                }
                else
                {
                    MessageBoxHelper.ShowWarning("应用程序日志目录不存在，可能是权限问题或目录已被删除。");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开应用程序日志目录时出错: {ex.Message}");
                LogHelper.LogError($"打开应用程序日志目录时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 一键导出所有日志（用于问题反馈）
        /// </summary>
        private void ExportAllLogs()
        {
            try
            {
                // 显示加载动画
                IsLoading = true;
                
                // 创建保存文件对话框
                var dialog = new SaveFileDialog
                {
                    Title = "选择日志导出位置",
                    Filter = "ZIP文件 (*.zip)|*.zip",
                    DefaultExt = ".zip",
                    FileName = $"TicketAssist_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
                };
                
                // 显示对话框
                bool? result = dialog.ShowDialog();
                
                if (result == true)
                {
                    string selectedPath = Path.GetDirectoryName(dialog.FileName);
                    string exportFileName = Path.GetFileName(dialog.FileName);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    
                    // 创建临时文件夹用于存放日志文件
                    string tempFolderPath = Path.Combine(Path.GetTempPath(), $"TicketAssist_Logs_{timestamp}");
                    
                    // 确保临时目录存在
                    if (!Directory.Exists(tempFolderPath))
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }
                    
                    // 导出应用程序日志
                    string appLogPath = Path.Combine(LogHelper.GetAppLogPath(), LogHelper.GetAppLogFileName());
                    string appLogExportPath = Path.Combine(tempFolderPath, $"app_log_{timestamp}.txt");
                    
                    if (File.Exists(appLogPath))
                    {
                        File.Copy(appLogPath, appLogExportPath, true);
                    }
                    
                    // 导出系统日志
                    string systemLogPath = Path.Combine(LogHelper.GetSystemLogPath(), LogHelper.GetSystemLogFileName());
                    string systemLogExportPath = Path.Combine(tempFolderPath, $"system_log_{timestamp}.txt");
                    
                    if (File.Exists(systemLogPath))
                    {
                        File.Copy(systemLogPath, systemLogExportPath, true);
                    }
                    
                    // 导出系统信息
                    ExportSystemInfo(tempFolderPath, timestamp);
                    
                    // 创建README文件，说明如何提交日志
                    CreateReadmeFile(tempFolderPath);
                    
                    // 创建ZIP文件
                    try
                    {
                        // 使用System.IO.Compression创建ZIP文件
                        if (File.Exists(dialog.FileName))
                        {
                            File.Delete(dialog.FileName);
                        }
                        
                        System.IO.Compression.ZipFile.CreateFromDirectory(tempFolderPath, dialog.FileName);
                        
                        // 显示成功消息
                        string message = $"所有日志已成功导出！\n\n" +
                                         $"导出位置：\n{dialog.FileName}\n\n" +
                                         $"请将此ZIP文件发送给开发人员，以便更好地分析和解决问题。";
                        
                        MessageBoxHelper.ShowInfo(message);
                        
                        // 记录日志
                        LogHelper.LogInfo($"用户一键导出了所有日志到：{dialog.FileName}");
                        
                        // 打开导出目录
                        Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
                    }
                    catch (Exception ex)
                    {
                        // 如果ZIP创建失败，则直接打开临时文件夹
                        MessageBoxHelper.ShowWarning($"创建ZIP文件失败，将打开临时文件夹。\n错误信息：{ex.Message}");
                        LogHelper.LogError($"创建ZIP文件失败: {ex.Message}", ex);
                        
                        // 显示成功消息
                        string message = $"所有日志已导出到临时文件夹！\n\n" +
                                         $"导出位置：\n{tempFolderPath}\n\n" +
                                         $"请将此文件夹中的所有文件发送给开发人员，以便更好地分析和解决问题。";
                        
                        MessageBoxHelper.ShowInfo(message);
                        
                        // 记录日志
                        LogHelper.LogInfo($"用户一键导出了所有日志到临时文件夹：{tempFolderPath}");
                        
                        // 打开临时文件夹
                        Process.Start("explorer.exe", tempFolderPath);
                    }
                }
                
                // 隐藏加载动画
                IsLoading = false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"导出所有日志时出错: {ex.Message}");
                LogHelper.LogError($"导出所有日志时出错: {ex.Message}", ex);
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 导出系统信息
        /// </summary>
        private void ExportSystemInfo(string exportPath, string timestamp)
        {
            try
            {
                string systemInfoPath = Path.Combine(exportPath, $"system_info_{timestamp}.txt");
                
                using (StreamWriter writer = new StreamWriter(systemInfoPath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("=== TicketAssist 系统信息 ===");
                    writer.WriteLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
                    
                    // 应用程序信息
                    writer.WriteLine("--- 应用程序信息 ---");
                    writer.WriteLine($"应用版本: 1.0.0");
                    writer.WriteLine($"运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
                    writer.WriteLine();
                    
                    // 系统信息
                    writer.WriteLine("--- 系统信息 ---");
                    writer.WriteLine($"操作系统: {Environment.OSVersion}");
                    writer.WriteLine($"系统架构: {(Environment.Is64BitOperatingSystem ? "64位" : "32位")}");
                    writer.WriteLine($"处理器数量: {Environment.ProcessorCount}");
                    writer.WriteLine($"系统内存: {GetTotalPhysicalMemory()} MB");
                    writer.WriteLine();
                    
                    // 数据库信息
                    writer.WriteLine("--- 数据库信息 ---");
                    writer.WriteLine($"服务器地址: {ServerAddress}");
                    writer.WriteLine($"用户名: {Username}");
                    writer.WriteLine($"密码: ********");
                    writer.WriteLine();
                    
                    // 日志路径信息
                    writer.WriteLine("--- 日志路径信息 ---");
                    writer.WriteLine($"应用程序日志路径: {LogHelper.GetAppLogPath()}");
                    writer.WriteLine($"系统日志路径: {LogHelper.GetSystemLogPath()}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"导出系统信息时出错: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取系统总物理内存（MB）
        /// </summary>
        private long GetTotalPhysicalMemory()
        {
            try
            {
                return Environment.WorkingSet / (1024 * 1024);
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// 创建README文件
        /// </summary>
        private void CreateReadmeFile(string exportPath)
        {
            try
            {
                string readmePath = Path.Combine(exportPath, "README.txt");
                
                using (StreamWriter writer = new StreamWriter(readmePath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("=== TicketAssist 日志提交说明 ===");
                    writer.WriteLine();
                    writer.WriteLine("感谢您提交日志，这将帮助我们更好地解决您遇到的问题。");
                    writer.WriteLine();
                    writer.WriteLine("提交步骤：");
                    writer.WriteLine("1. 将整个文件夹压缩为ZIP格式");
                    writer.WriteLine("2. 发送邮件至：tl_5099@163.com");
                    writer.WriteLine("3. 在邮件中详细描述您遇到的问题，包括：");
                    writer.WriteLine("   - 问题发生的时间");
                    writer.WriteLine("   - 问题发生前您正在执行的操作");
                    writer.WriteLine("   - 问题的具体表现（错误信息、异常行为等）");
                    writer.WriteLine("   - 问题是否可以稳定复现，如果可以，请提供复现步骤");
                    writer.WriteLine();
                    writer.WriteLine("文件说明：");
                    writer.WriteLine("- app_log_*.txt：应用程序日志，记录用户操作和应用程序行为");
                    writer.WriteLine("- system_log_*.txt：系统级别日志，记录系统级别的事件和错误");
                    writer.WriteLine("- system_info_*.txt：系统信息，包含应用程序和系统的基本信息");
                    writer.WriteLine();
                    writer.WriteLine("我们将尽快处理您的反馈，并在必要时与您联系获取更多信息。");
                    writer.WriteLine();
                    writer.WriteLine("TicketAssist 开发团队");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"创建README文件时出错: {ex.Message}", ex);
            }
        }
    }
}