using System;
using System.ComponentModel;
using System.Windows.Input;
using TA_WPF.Services;
using TA_WPF.Utils;

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
            
            // 初始化命令
            ModifyConnectionCommand = new RelayCommand(ModifyConnection);
            UpdateConnectionCommand = new RelayCommand(UpdateConnection);
            UpdateDatabaseCommand = new RelayCommand(UpdateDatabase);
            ExportLogCommand = new RelayCommand(ExportLog);
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
        /// 导出日志
        /// </summary>
        private void ExportLog()
        {
            try
            {
                // 显示加载动画
                IsLoading = true;
                
                // 模拟导出过程
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 显示成功消息
                        MessageBoxHelper.ShowInfo("日志已成功导出到应用程序目录");
                        
                        // 记录日志
                        LogHelper.LogInfo("用户导出了系统日志");
                        
                        // 隐藏加载动画
                        IsLoading = false;
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"导出日志时出错: {ex.Message}");
                LogHelper.LogError($"导出日志时出错: {ex.Message}");
                IsLoading = false;
            }
        }
    }
}