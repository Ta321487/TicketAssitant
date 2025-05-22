using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        #region 属性和字段

        private readonly LoginInfoService _loginInfoService;
        private string _serverAddress = string.Empty;
        private string _databaseName = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isCustomPort = false;
        private string _port = "3306";
        private string _newDatabaseName = string.Empty;
        private ObservableCollection<string> _databaseHistory = new ObservableCollection<string>();
        private bool _isLoginButtonEnabled = true;
        private bool _isCapsLockOn = false;
        private double _fontSize = 13;
        private bool _isLoading = false;

        // 表和连接字符串相关
        public List<string> RequiredTables { get; } = new List<string> { "station_info", "train_ride_info", "ticket_collections_info", "collection_mapped_tickets_info" };
        public string ConnectionString { get; private set; } = string.Empty;
        public bool LoginSuccessful { get; private set; } = false;

        #region 事件定义
        // 添加事件定义
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> LoginSuccess;
        public event EventHandler<string> ShowMessage;
        public event EventHandler<string> DatabaseCreated;
        #endregion

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        // 是否正在加载（连接中）
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DatabaseName
        {
            get => _databaseName;
            set
            {
                if (_databaseName != value)
                {
                    _databaseName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCustomPort
        {
            get => _isCustomPort;
            set
            {
                if (_isCustomPort != value)
                {
                    _isCustomPort = value;
                    OnPropertyChanged();
                    if (!value)
                    {
                        Port = "3306";
                    }
                }
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewDatabaseName
        {
            get => _newDatabaseName;
            set
            {
                if (_newDatabaseName != value)
                {
                    _newDatabaseName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> DatabaseHistory
        {
            get => _databaseHistory;
            set
            {
                if (_databaseHistory != value)
                {
                    _databaseHistory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoginButtonEnabled
        {
            get => _isLoginButtonEnabled;
            set
            {
                if (_isLoginButtonEnabled != value)
                {
                    _isLoginButtonEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCapsLockOn
        {
            get => _isCapsLockOn;
            set
            {
                if (_isCapsLockOn != value)
                {
                    _isCapsLockOn = value;
                    OnPropertyChanged();
                }
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                    // 应用字体大小变更
                    ApplyFontSize(value);
                }
            }
        }

        #endregion

        #region 命令

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateDatabaseCommand { get; }
        public ICommand ClearServerAddressCommand { get; }
        public ICommand ClearDatabaseNameCommand { get; }
        public ICommand ClearUsernameCommand { get; }
        public ICommand ClearPasswordCommand { get; }
        public ICommand ClearPortCommand { get; }
        public ICommand CheckCapsLockCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        #endregion

        public LoginViewModel()
        {
            // 初始化服务
            _loginInfoService = new LoginInfoService();

            // 初始化命令
            // 使用来自Utils的RelayCommand
            LoginCommand = new RelayCommand(async () => await LoginAsync());
            CancelCommand = new RelayCommand(Cancel);
            CreateDatabaseCommand = new RelayCommand(async () => await CreateDatabaseAsync());
            ClearServerAddressCommand = new RelayCommand(() => ServerAddress = string.Empty);
            ClearDatabaseNameCommand = new RelayCommand(() => DatabaseName = string.Empty);
            ClearUsernameCommand = new RelayCommand(() => Username = string.Empty);
            ClearPasswordCommand = new RelayCommand(() => Password = string.Empty);
            ClearPortCommand = new RelayCommand(() => Port = string.Empty);
            CheckCapsLockCommand = new RelayCommand(CheckCapsLock);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // 加载配置
            LoadLastDatabaseName();
            LoadLastServerAddress();
            LoadDatabaseHistory();
            LoadFontSizeFromConfig();
        }

        // 这里实现必要的方法，保持原来的业务逻辑
        // 为了保持代码简洁，我只展示主要方法的框架结构

        #region 数据加载方法

        public void LoadLastDatabaseName()
        {
            try
            {
                string lastDbName = ConfigUtils.GetStringValue("LastDatabaseName");
                if (!string.IsNullOrEmpty(lastDbName))
                {
                    DatabaseName = lastDbName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载上次数据库名称时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"加载上次数据库名称时出错", ex);
            }
        }

        public void LoadLastServerAddress()
        {
            try
            {
                string lastAddress = ConfigUtils.GetStringValue("LastServerAddress");
                if (!string.IsNullOrEmpty(lastAddress))
                {
                    ServerAddress = lastAddress;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载上次服务器地址时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"加载上次服务器地址时出错", ex);
            }
        }

        public void LoadDatabaseHistory()
        {
            try
            {
                string historyString = ConfigUtils.GetStringValue("DatabaseHistory");
                if (!string.IsNullOrEmpty(historyString))
                {
                    List<string> history = historyString.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
                    DatabaseHistory.Clear();
                    foreach (string item in history)
                    {
                        DatabaseHistory.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载数据库历史记录时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", $"加载数据库历史记录时出错", ex);
            }
        }

        public void LoadFontSizeFromConfig()
        {
            try
            {
                double fontSize = ConfigUtils.GetDoubleValue("FontSize", 13);
                FontSize = ValidateFontSize(fontSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载字体大小设置时出错: {ex.Message}");
                LogHelper.LogSystemError("登录窗口", $"加载字体大小设置时出错: {ex.Message}", ex);
            }
        }

        #endregion

        #region 业务逻辑方法

        public async Task LoginAsync()
        {
            if (!ValidateLoginInput())
            {
                ErrorOccurred?.Invoke(this, "请检查输入信息是否完整");
                return;
            }

            IsLoginButtonEnabled = false;
            IsLoading = true;

            try
            {
                // 检测MySQL是否已安装或可连接
                var connectionStatus = await CheckMySqlConnectionStatus(ServerAddress, Port);
                if (connectionStatus != MySqlConnectionStatus.Connected)
                {
                    Debug.WriteLine($"MySQL连接状态: {connectionStatus}");
                    LogHelper.LogWarning($"MySQL连接状态: {connectionStatus}，无法继续登录");
                    
                    string errorMessage;
                    switch (connectionStatus)
                    {
                        case MySqlConnectionStatus.NotInstalled:
                            errorMessage = "未检测到MySQL数据库服务器或服务未启动。\n\n请确认以下事项：\n1. MySQL服务器已正确安装\n2. MySQL服务已启动\n3. 您有足够的权限访问MySQL服务";
                            break;
                        case MySqlConnectionStatus.PortError:
                            errorMessage = $"无法连接到MySQL数据库的指定端口({Port})。\n\n请确认以下事项：\n1. 您指定的端口号是否正确\n2. MySQL是否配置为使用此端口\n3. 防火墙是否允许此端口的TCP连接";
                            break;
                        default:
                            errorMessage = $"无法连接到MySQL数据库服务器({ServerAddress})。\n\n请检查：\n1. 服务器地址是否正确\n2. 网络连接是否正常\n3. MySQL服务是否运行";
                            break;
                    }
                    
                    ErrorOccurred?.Invoke(this, errorMessage);
                    IsLoginButtonEnabled = true;
                    IsLoading = false;
                    return;
                }

                // 处理本地连接地址
                bool isLocalConnection = false;
                string serverAddressToUse = ServerAddress;
                if (serverAddressToUse.ToLower() == "localhost" || serverAddressToUse == "127.0.0.1" || serverAddressToUse == "::1")
                {
                    serverAddressToUse = "localhost";
                    isLocalConnection = true;
                }

                // 构建连接字符串
                string connectionString = BuildConnectionString(serverAddressToUse, DatabaseName, Username, Password, Port);

                // 尝试连接数据库
                bool connected = false;
                bool tablesExist = false;
                Exception connectionException = null;

                await Task.Run(() =>
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            connected = true;

                            // 检测必要的表是否存在
                            tablesExist = CheckRequiredTables(connection);
                        }
                        catch (MySqlException ex)
                        {
                            string errorMessage = ex.Message;
                            
                            // 增强错误消息
                            if (ex.Message.Contains("Access denied"))
                            {
                                errorMessage = $"连接数据库失败: {ex.Message}\n\n可能原因：\n1. 用户名或密码错误\n2. 用户没有访问指定数据库的权限\n3. 用户账户被限制";
                            }
                            else if (ex.Message.Contains("Unknown database"))
                            {
                                errorMessage = $"连接数据库失败: 数据库'{DatabaseName}'不存在\n\n请创建数据库或选择一个已有的数据库";
                            }
                            else if (ex.Message.Contains("Authentication method"))
                            {
                                errorMessage = $"认证方式不兼容: {ex.Message}\n\n这通常是因为MySQL 8.0+默认使用了新的认证插件(caching_sha2_password)，而当前连接库需要使用mysql_native_password\n\n您可以使用以下SQL命令修复此问题：";
                            }
                            
                            connectionException = new Exception(errorMessage, ex);
                        }
                        catch (Exception ex)
                        {
                            connectionException = ex;
                        }
                    }
                });

                // 如果连接过程中发生异常，抛出异常
                if (connectionException != null)
                {
                    throw connectionException;
                }

                if (connected)
                {
                    // 保存连接信息
                    ConnectionString = connectionString;
                    SaveConnectionString(connectionString);
                    SaveLastDatabaseName(DatabaseName);
                    SaveLastServerAddress(ServerAddress);
                    SaveDatabaseNameToHistory(DatabaseName);

                    // 处理表不存在的情况，通过View处理详细逻辑
                    if (!tablesExist)
                    {
                        // 设置标志位，让View知道需要创建表
                        ShowMessage?.Invoke(this, "数据库缺少必要表结构，需要导入表结构");
                    }
                    
                    // 登录成功
                    LoginSuccessful = true;
                    _loginInfoService.SaveLastLoginTime();

                    // 触发登录成功事件，让View处理后续操作
                    LoginSuccess?.Invoke(this, ConnectionString);
                }
            }
            catch (Exception ex)
            {
                LoginSuccessful = false;
                Debug.WriteLine($"登录时出错: {ex.Message}");
                LogHelper.LogError($"登录时出错: {ex.Message}", ex);
                
                // 触发错误事件，让View显示错误消息
                ErrorOccurred?.Invoke(this, ex.Message);
            }
            finally
            {
                IsLoginButtonEnabled = true;
                IsLoading = false;
            }
        }

        public void Cancel()
        {
            LoginSuccessful = false;
            // View将关闭窗口
        }

        public async Task CreateDatabaseAsync()
        {
            if (!ValidateCreateDatabaseInput())
            {
                ErrorOccurred?.Invoke(this, "请检查输入信息是否完整");
                return;
            }

            try
            {
                // 构建连接字符串（不包含数据库名称）
                string serverLevelConnectionString = $"Server={ServerAddress};Port={Port};User ID={Username};Password={Password};CharSet=utf8;Connect Timeout=15;AllowPublicKeyRetrieval=true;UseCompression=false;Default Command Timeout=30;SslMode=none;Max Pool Size=50;AllowUserVariables=true;";

                // 创建数据库
                Exception createException = null;

                await Task.Run(async () =>
                {
                    using (MySqlConnection connection = new MySqlConnection(serverLevelConnectionString))
                    {
                        try
                        {
                            connection.Open();

                            // 创建数据库
                            using (MySqlCommand cmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{NewDatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", connection))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            // 使用新创建的数据库
                            connection.ChangeDatabase(NewDatabaseName);

                            // 构建包含新数据库名称的新连接字符串
                            string dbSpecificConnectionString = $"Server={ServerAddress};Port={Port};Database={NewDatabaseName};User ID={Username};Password={Password};CharSet=utf8;Connect Timeout=15;AllowPublicKeyRetrieval=true;UseCompression=false;Default Command Timeout=30;SslMode=none;Max Pool Size=50;AllowUserVariables=true;";

                            // 创建必要的表结构
                            await CreateRequiredTablesUsingService(dbSpecificConnectionString);
                        }
                        catch (MySqlException ex)
                        {
                            createException = new Exception($"创建数据库时出错: {ex.Message}", ex);
                            LogHelper.LogSystemError("登录 - 数据库", $"创建数据库时出错: {ex.Message}", ex);
                        }
                        catch (Exception ex)
                        {
                            createException = new Exception($"创建数据库时出错: {ex.Message}", ex);
                        }
                    }
                });

                // 如果创建过程中发生异常，抛出异常
                if (createException != null)
                {
                    throw createException;
                }

                // 更新数据库名称，选择刚刚创建的数据库
                string createdDbName = NewDatabaseName;
                DatabaseName = createdDbName;
                SaveDatabaseNameToHistory(createdDbName);
                
                // 清空新数据库名称
                NewDatabaseName = string.Empty;

                // 触发数据库创建成功事件
                DatabaseCreated?.Invoke(this, createdDbName);

                // 日志记录
                LogHelper.LogSystem("数据库", $"成功创建数据库: {createdDbName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建数据库失败: {ex.Message}");
                LogHelper.LogError($"创建数据库失败: {ex.Message}", ex);
                
                // 触发错误事件，让View显示错误消息
                ErrorOccurred?.Invoke(this, $"创建数据库失败: {ex.Message}");
            }
        }

        public void CheckCapsLock()
        {
            IsCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);
        }

        public void ToggleTheme()
        {
            var themeService = ThemeService.Instance;
            bool isDarkTheme = themeService.IsDarkThemeActive();
            bool newIsDarkTheme = !isDarkTheme;
            themeService.ApplyTheme(newIsDarkTheme);
        }

        #endregion

        #region 辅助方法

        private bool ValidateLoginInput()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                ErrorOccurred?.Invoke(this, "请输入服务器地址");
                return false;
            }

            if (string.IsNullOrEmpty(DatabaseName))
            {
                ErrorOccurred?.Invoke(this, "请输入数据库名称");
                return false;
            }

            if (string.IsNullOrEmpty(Username))
            {
                ErrorOccurred?.Invoke(this, "请输入用户名");
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                ErrorOccurred?.Invoke(this, "请输入密码");
                return false;
            }

            if (IsCustomPort)
            {
                if (string.IsNullOrEmpty(Port))
                {
                    ErrorOccurred?.Invoke(this, "请输入端口号");
                    return false;
                }

                if (!Regex.IsMatch(Port, @"^\d+$"))
                {
                    ErrorOccurred?.Invoke(this, "端口号必须是数字");
                    return false;
                }

                int portNumber;
                if (!int.TryParse(Port, out portNumber) || portNumber < 1 || portNumber > 65535)
                {
                    ErrorOccurred?.Invoke(this, "端口号必须在1-65535之间");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateCreateDatabaseInput()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                ErrorOccurred?.Invoke(this, "请输入服务器地址");
                return false;
            }

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorOccurred?.Invoke(this, "请输入用户名和密码");
                return false;
            }

            if (string.IsNullOrEmpty(NewDatabaseName))
            {
                ErrorOccurred?.Invoke(this, "请输入新数据库名称");
                return false;
            }

            return true;
        }

        private string BuildConnectionString(string server, string database, string username, string password, string port)
        {
            string connectionString = $"Server={server};Port={port};Database={database};User ID={username};Password={password};CharSet=utf8;Connect Timeout=15;AllowPublicKeyRetrieval=true;UseCompression=false;Default Command Timeout=30;SslMode=none;Max Pool Size=50;";

            // 添加支持不同认证方式的参数
            if (!connectionString.Contains("Auth") && !connectionString.Contains("allow"))
            {
                connectionString += "AllowUserVariables=true;";
            }

            return connectionString;
        }

        private bool CheckRequiredTables(MySqlConnection connection)
        {
            try
            {
                // 获取数据库中的所有表
                DataTable tables = connection.GetSchema("Tables");
                
                // 将表名转为小写存入HashSet以便快速查找
                HashSet<string> existingTables = new HashSet<string>(
                    tables.Rows.Cast<DataRow>()
                          .Select(row => row["TABLE_NAME"].ToString().ToLower())
                );
                
                // 检测所有必要的表是否都存在
                return RequiredTables.All(table => existingTables.Contains(table.ToLower()));
            }
            catch
            {
                return false;
            }
        }

        private async Task CreateRequiredTablesUsingService(string connectionString)
        {
            try
            {
                // 创建DatabaseService实例
                var databaseService = new DatabaseService(connectionString);
                
                // 创建station_info表
                await databaseService.CreateStationInfoTableAsync();
                
                // 创建train_ride_info表
                await databaseService.CreateTrainRideInfoTableAsync();

                // 创建ticket_collections_info表
                await databaseService.CreateTicketCollectionsInfoTableAsync();

                // 创建collection_mapped_tickets_info表
                await databaseService.CreateCollectionMappedTicketsInfoTableAsync();
                
                Debug.WriteLine("使用DatabaseService创建表成功");
                LogHelper.LogSystem("数据库", "使用DatabaseService创建必要的表成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"使用DatabaseService创建表时出错: {ex.Message}");
                LogHelper.LogError($"使用DatabaseService创建表时出错: {ex.Message}");
                throw new Exception($"创建表结构时出错: {ex.Message}", ex);
            }
        }

        private void SaveConnectionString(string connectionString)
        {
            try
            {
                // 获取配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // 检测连接字符串是否已存在
                if (config.ConnectionStrings.ConnectionStrings["DefaultConnection"] != null)
                {
                    // 更新现有连接字符串
                    config.ConnectionStrings.ConnectionStrings["DefaultConnection"].ConnectionString = connectionString;
                }
                else
                {
                    // 添加新的连接字符串
                    config.ConnectionStrings.ConnectionStrings.Add(
                        new ConnectionStringSettings("DefaultConnection", connectionString, "MySql.Data.MySqlClient"));
                }

                // 保存配置
                config.Save(ConfigurationSaveMode.Modified);

                // 刷新配置
                ConfigurationManager.RefreshSection("connectionStrings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存连接字符串时出错: {ex.Message}");
                LogHelper.LogError($"保存连接字符串时出错: {ex.Message}", ex);
                ErrorOccurred?.Invoke(this, $"保存连接字符串时出错: {ex.Message}");
            }
        }

        private void SaveLastDatabaseName(string databaseName)
        {
            try
            {
                ConfigUtils.SaveStringValue("LastDatabaseName", databaseName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存数据库名称时出错: {ex.Message}");
                LogHelper.LogError($"保存数据库名称时出错: {ex.Message}", ex);
            }
        }

        private void SaveLastServerAddress(string serverAddress)
        {
            try
            {
                ConfigUtils.SaveStringValue("LastServerAddress", serverAddress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存服务器地址时出错: {ex.Message}");
                LogHelper.LogError($"保存服务器地址时出错: {ex.Message}", ex);
            }
        }

        private void SaveDatabaseNameToHistory(string databaseName)
        {
            try
            {
                // 从配置文件中读取历史数据库名称
                List<string> historyList = new List<string>();

                // 读取现有历史记录
                string history = ConfigUtils.GetStringValue("DatabaseHistory");
                if (!string.IsNullOrEmpty(history))
                {
                    historyList = history.Split(',').ToList();
                }

                // 如果历史记录中已存在该数据库名称，则移除它
                historyList.Remove(databaseName);

                // 将新的数据库名称添加到列表开头
                historyList.Insert(0, databaseName);

                // 只保留最近的10个记录
                if (historyList.Count > 10)
                {
                    historyList = historyList.Take(10).ToList();
                }

                // 保存回配置文件
                string newHistory = string.Join(",", historyList);
                ConfigUtils.SaveStringValue("DatabaseHistory", newHistory);

                // 更新界面集合
                LoadDatabaseHistory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存数据库历史记录时出错: {ex.Message}");
                LogHelper.LogSystemError("登录", "保存数据库历史记录时出错", ex);
            }
        }

        private void ApplyFontSize(double fontSize)
        {
            try
            {
                // 应用字体大小设置到配置
                SaveFontSizeToConfig(fontSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用字体大小时出错: {ex.Message}");
                LogHelper.LogError($"应用字体大小时出错: {ex.Message}", ex);
            }
        }

        private void SaveFontSizeToConfig(double fontSize)
        {
            try
            {
                // 确保字体大小有效
                fontSize = ValidateFontSize(fontSize);

                // 保存字体大小到配置文件
                ConfigUtils.SaveDoubleValue("FontSize", fontSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存字体大小设置时出错: {ex.Message}");
                LogHelper.LogError($"保存字体大小设置时出错: {ex.Message}", ex);
            }
        }

        private double ValidateFontSize(double fontSize)
        {
            // 确保字体大小不小于最小可读值
            return Math.Max(fontSize, 12);
        }

        private enum MySqlConnectionStatus
        {
            Connected,
            NotInstalled,
            PortError,
            ConnectionError
        }

        private async Task<MySqlConnectionStatus> CheckMySqlConnectionStatus(string serverAddress, string port)
        {
            try
            {
                // 检测是否是本地连接
                bool isLocalConnection = string.IsNullOrEmpty(serverAddress) ||
                                         serverAddress.ToLower() == "localhost" ||
                                         serverAddress == "127.0.0.1" ||
                                         serverAddress == "::1";

                // 尝试直接连接到MySQL端口
                bool canConnect = await Task.Run(() =>
                {
                    try
                    {
                        using (var client = new System.Net.Sockets.TcpClient())
                        {
                            var connectTask = client.ConnectAsync(
                                isLocalConnection ? "127.0.0.1" : serverAddress, 
                                int.Parse(port));
                            var timeoutTask = Task.Delay(isLocalConnection ? 1000 : 2000);
                            var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                            return completedTask == connectTask && client.Connected;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"连接MySQL服务器失败: {ex.Message}");
                        LogHelper.LogSystemError("登录窗口", $"连接MySQL服务器失败: {ex.Message}");
                        return false;
                    }
                });

                if (canConnect)
                {
                    return MySqlConnectionStatus.Connected;
                }

                // 如果无法连接，尝试确定具体原因
                if (isLocalConnection)
                {
                    // 本地连接失败，可能是MySQL没有安装或没有启动
                    bool isInstalled = await CheckMySqlInstalled();
                    if (!isInstalled)
                    {
                        Debug.WriteLine("MySQL数据库未安装或未启动");
                        LogHelper.LogWarning("MySQL数据库未安装或未启动");
                        return MySqlConnectionStatus.NotInstalled;
                    }
                    else
                    {
                        // MySQL已安装但无法连接，可能是端口不正确
                        Debug.WriteLine("MySQL数据库已安装但无法连接，可能是端口不正确");
                        LogHelper.LogWarning("MySQL数据库已安装但无法连接，可能是端口不正确");
                        return MySqlConnectionStatus.PortError;
                    }
                }

                // 远程连接失败，可能是网络问题或服务器未开放端口
                Debug.WriteLine("无法连接到远程MySQL服务器，可能是网络问题或服务器未开放端口");
                LogHelper.LogWarning("无法连接到远程MySQL服务器，可能是网络问题或服务器未开放端口");
                return MySqlConnectionStatus.ConnectionError;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测MySQL安装或连接时出错: {ex.Message}");
                LogHelper.LogSystemError("登录窗口", "检测MySQL安装或连接时出错", ex);
                return MySqlConnectionStatus.ConnectionError;
            }
        }

        private async Task<bool> CheckMySqlInstalled()
        {
            try
            {
                // 方法1：检查是否有MySQL相关进程
                bool processExists = false;
                await Task.Run(() => {
                    try
                    {
                        processExists = Process.GetProcesses().Any(p => 
                            p.ProcessName.ToLower().Contains("mysql") || 
                            p.ProcessName.ToLower().Contains("mysqld"));
                            
                        Debug.WriteLine($"检查MySQL进程: {(processExists ? "找到" : "未找到")}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"检查MySQL进程时出错: {ex.Message}");
                        LogHelper.LogSystemError("登录窗口", $"检查MySQL进程时出错: {ex.Message}", ex);
                    }
                });

                if (processExists)
                {
                    return true;
                }

                // 方法2：检查MySQL常见安装路径
                string[] possiblePaths = {
                    @"C:\Program Files\MySQL",
                    @"C:\Program Files (x86)\MySQL",
                    @"C:\MySQL",
                    @"D:\MySQL",
                    @"E:\MySQL",
                    @"C:\xampp\mysql",
                    @"C:\wamp\bin\mysql"
                };

                bool pathExists = false;
                await Task.Run(() => {
                    pathExists = possiblePaths.Any(path => Directory.Exists(path));
                    Debug.WriteLine($"检查MySQL安装路径: {(pathExists ? "找到" : "未找到")}");
                });
                
                if (pathExists)
                {
                    return true;
                }
                
                // 方法3：检查注册表
                bool registryEntryExists = false;
                await Task.Run(() => {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MySQL"))
                        {
                            registryEntryExists = key != null;
                            Debug.WriteLine($"检查MySQL注册表项: {(registryEntryExists ? "找到" : "未找到")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"检查MySQL注册表项时出错: {ex.Message}");
                    }
                    
                    if (!registryEntryExists)
                    {
                        try
                        {
                            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\MySQL"))
                            {
                                registryEntryExists = key != null;
                                Debug.WriteLine($"检查MySQL 32位注册表项: {(registryEntryExists ? "找到" : "未找到")}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"检查MySQL 32位注册表项时出错: {ex.Message}");
                        }
                    }
                });
                
                if (registryEntryExists)
                {
                    return true;
                }

                // 方法4：尝试在默认端口ping本地MySQL
                bool canPing = false;
                await Task.Run(() => {
                    try
                    {
                        using (var client = new System.Net.Sockets.TcpClient())
                        {
                            // 尝试连接到默认端口3306
                            var connectTask = client.ConnectAsync("127.0.0.1", 3306);
                            var timeoutTask = Task.Delay(500);
                            var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                            canPing = completedTask == connectTask && client.Connected;
                            Debug.WriteLine($"尝试连接本地MySQL默认端口: {(canPing ? "成功" : "失败")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"尝试连接本地MySQL默认端口时出错: {ex.Message}");
                    }
                });
                
                if (canPing)
                {
                    return true;
                }

                // 如果以上方法都没有检测到MySQL，则认为未安装
                Debug.WriteLine("所有检测方法都未发现MySQL，认为未安装");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测MySQL安装时出错: {ex.Message}");
                LogHelper.LogSystemError("登录窗口", $"检测MySQL安装时出错: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 