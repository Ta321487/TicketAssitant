using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Data;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using TA_WPF.Utils;
using TA_WPF.ViewModels;
using System.Windows.Threading;
using TA_WPF.Services;
using System.Diagnostics;

namespace TA_WPF.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private MainWindow? _mainWindow;
        public bool LoginSuccessful { get; private set; } = false;
        public string ConnectionString { get; private set; } = string.Empty;
        private string _lastDatabaseName = ""; // 默认数据库名称
        private string _lastServerAddress = ""; // 默认服务器地址
        private List<string> _requiredTables = new List<string> { "station_info", "train_ride_info" }; // 必要的表
        private readonly LoginInfoService _loginInfoService;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();

                // 设置窗口属性
                this.ResizeMode = ResizeMode.CanMinimize; // 禁用最大化，只允许最小化
                this.SizeToContent = SizeToContent.Height; // 根据内容自动调整高度
                this.MaxHeight = 950; // 设置最大高度

                UsernameTextBox.Focus();

                // 初始化登录信息服务
                _loginInfoService = new LoginInfoService();

                // 确保端口号文本框的清空按钮初始状态是禁用的
                MaterialDesignThemes.Wpf.TextFieldAssist.SetHasClearButton(PortTextBox, false);

                // 初始化Snackbar消息队列
                LoginSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(3000));

                // 从配置文件中加载上次使用的数据库名称和服务器地址
                LoadLastDatabaseName();
                LoadLastServerAddress();

                // 从配置文件中加载字体大小设置
                LoadFontSizeFromConfig();

                // 注意：UpdateThemeIcon已移至Window_Loaded事件中，确保UI元素已完全加载
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginWindow构造函数异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"初始化登录窗口时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 首先初始化主题，确保在加载其他内容前应用正确的主题
                InitializeTheme();

                // 确保窗口加载后应用字体大小设置
                LoadFontSizeFromConfig();

                // 延迟执行UpdateThemeIcon，确保UI元素已完全加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdateThemeIcon();

                        // 智能设置焦点
                        SetInitialFocus();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"延迟执行UpdateThemeIcon异常: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                    }
                }), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Window_Loaded异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 根据服务器地址和数据库名称的内容智能设置焦点
        /// </summary>
        private void SetInitialFocus()
        {
            try
            {
                // 检测服务器地址和数据库名称是否有内容
                bool hasServerAddress = !string.IsNullOrWhiteSpace(ServerAddressTextBox.Text);
                bool hasDatabaseName = !string.IsNullOrWhiteSpace(DatabaseNameComboBox.Text);

                // 如果服务器地址和数据库名称都有内容，将焦点设置在用户名上
                if (hasServerAddress && hasDatabaseName)
                {
                    UsernameTextBox.Focus();
                    System.Diagnostics.Debug.WriteLine("焦点设置在用户名上");
                }
                else
                {
                    // 否则，焦点放在服务器地址上
                    ServerAddressTextBox.Focus();
                    System.Diagnostics.Debug.WriteLine("焦点设置在服务器地址上");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetInitialFocus异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private void InitializeTheme()
        {
            try
            {
                // 创建主题服务
                var themeService = TA_WPF.Services.ThemeService.Instance;

                // 从配置文件加载主题设置
                bool isDarkMode = themeService.LoadThemeFromConfig();

                // 使用集中的方法应用主题到窗口
                themeService.ApplyThemeToWindow(this, isDarkMode, ThemeIcon, MainCard);

                System.Diagnostics.Debug.WriteLine($"已初始化为{(isDarkMode ? "深色" : "浅色")}主题");
                LogHelper.LogSystem("登录", $"窗口已初始化为{(isDarkMode ? "深色" : "浅色")}主题");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeTheme异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private void LoadFontSizeFromConfig()
        {
            try
            {
                // 从配置文件中加载字体大小设置
                double fontSize = ConfigUtils.GetDoubleValue("FontSize", 13);
                
                // 确保字体大小不小于最小可读值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }

                // 更新滑块值
                FontSizeSlider.Value = fontSize;
                FontSizeValueText.Text = $"{fontSize:N0}pt";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载字体大小设置时出错: {ex.Message}");
            }
        }

        private void FontSizeButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示/隐藏字体大小调整弹出框
            FontSizePopup.IsOpen = !FontSizePopup.IsOpen;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;

            double fontSize = Math.Round(e.NewValue);
            FontSizeValueText.Text = $"{fontSize:N0}pt";

            try
            {
                // 应用字体大小设置
                var resources = Application.Current.Resources;
                resources["MaterialDesignFontSize"] = fontSize;
                resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;

                // 更新窗口字体大小
                this.FontSize = fontSize;

                // 保存字体大小设置到配置文件
                SaveFontSizeToConfig(fontSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用字体大小时出错: {ex.Message}");
            }
        }

        private void SaveFontSizeToConfig(double fontSize)
        {
            try
            {
                // 确保字体大小不小于最小可读值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }

                // 保存字体大小到配置文件
                ConfigUtils.SaveDoubleValue("FontSize", fontSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存字体大小设置时出错: {ex.Message}");
            }
        }

        // 设置数据库名称（从MainViewModel调用）
        public void SetDatabaseName(string databaseName)
        {
            if (!string.IsNullOrEmpty(databaseName))
            {
                _lastDatabaseName = databaseName;
                DatabaseNameTextBox.Text = databaseName;
                DatabaseNameComboBox.Text = databaseName;
            }
        }

        private void LoadLastDatabaseName()
        {
            try
            {
                // 从配置文件中加载上次使用的数据库名称
                string lastDbName = ConfigUtils.GetStringValue("LastDatabaseName");
                if (!string.IsNullOrEmpty(lastDbName))
                {
                    _lastDatabaseName = lastDbName;
                    DatabaseNameTextBox.Text = lastDbName;
                    DatabaseNameComboBox.Text = lastDbName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载上次数据库名称时出错: {ex.Message}");
            }
        }

        private void LoadLastServerAddress()
        {
            try
            {
                // 从配置文件中加载上次使用的服务器地址
                string lastAddress = ConfigUtils.GetStringValue("LastServerAddress");
                if (!string.IsNullOrEmpty(lastAddress))
                {
                    _lastServerAddress = lastAddress;
                    ServerAddressTextBox.Text = lastAddress;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载上次服务器地址时出错: {ex.Message}");
            }
        }

        private void SaveLastServerAddress(string serverAddress)
        {
            try
            {
                // 保存服务器地址到配置文件
                ConfigUtils.SaveStringValue("LastServerAddress", serverAddress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存服务器地址时出错: {ex.Message}");
            }
        }

        private void SaveLastDatabaseName(string databaseName)
        {
            try
            {
                // 保存数据库名称到配置文件
                ConfigUtils.SaveStringValue("LastDatabaseName", databaseName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据库名称时出错: {ex.Message}");
            }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                // 检测ThemeIcon是否已初始化
                if (ThemeIcon == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateThemeIcon: ThemeIcon为空");
                    return;
                }

                // 使用主题服务获取当前主题状态
                var themeService = TA_WPF.Services.ThemeService.Instance;
                bool isDarkTheme = themeService.IsDarkThemeActive();

                // 使用集中的方法应用主题到窗口
                themeService.ApplyThemeToWindow(this, isDarkTheme, ThemeIcon, MainCard);

                System.Diagnostics.Debug.WriteLine($"当前主题: {(isDarkTheme ? "深色" : "浅色")}");
            }
            catch (Exception ex)
            {
                // 记录异常但不中断操作
                System.Diagnostics.Debug.WriteLine($"UpdateThemeIcon异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");

                // 尝试设置默认图标
                try
                {
                    if (ThemeIcon != null)
                    {
                        ThemeIcon.Kind = PackIconKind.WeatherNight;
                    }
                }
                catch
                {
                    // 忽略进一步的异常
                }
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检测ThemeIcon是否已初始化
                if (ThemeIcon == null)
                {
                    System.Diagnostics.Debug.WriteLine("ThemeToggleButton_Click: ThemeIcon为空");
                    return;
                }

                // 创建主题服务
                var themeService = TA_WPF.Services.ThemeService.Instance;

                // 获取当前主题状态
                bool isDarkTheme = themeService.IsDarkThemeActive();

                // 切换到相反的主题
                bool newIsDarkTheme = !isDarkTheme;

                // 使用集中的方法应用主题到窗口
                themeService.ApplyThemeToWindow(this, newIsDarkTheme, ThemeIcon, MainCard);

                // 应用全局主题
                themeService.ApplyTheme(newIsDarkTheme);

                System.Diagnostics.Debug.WriteLine($"主题已切换为: {(newIsDarkTheme ? "深色" : "浅色")}");
                LogHelper.LogSystem("登录", $"窗口主题已切换为: {(newIsDarkTheme ? "深色" : "浅色")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeToggleButton_Click异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        // 处理窗口的键盘事件
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 如果按下回车键，触发登录按钮点击事件
                LoginButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }

            // 检测大写锁定状态
            CheckCapsLock();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检测大写锁定状态
            CheckCapsLock();
        }

        private void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            // 检测大写锁定状态
            CheckCapsLock();
        }

        private void CheckCapsLock()
        {
            // 检测大写锁定是否开启
            bool isCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);

            // 只有在密码框获得焦点时才显示大写锁定图标
            if (PasswordBox.IsFocused)
            {
                CapsLockIcon.Visibility = isCapsLockOn ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                CapsLockIcon.Visibility = Visibility.Collapsed;
            }
        }

        private void CustomPortCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PortTextBox.IsEnabled = true;
            // 启用清空按钮
            MaterialDesignThemes.Wpf.TextFieldAssist.SetHasClearButton(PortTextBox, true);
        }

        private void CustomPortCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PortTextBox.IsEnabled = false;
            PortTextBox.Text = "3306";
            MaterialDesignThemes.Wpf.TextFieldAssist.SetHasClearButton(PortTextBox, false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccessful = false;
            Close();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个计时器变量，用于控制加载动画
            System.Threading.Timer loadingTimer = null;

            try
            {
                // 获取输入值
                string serverAddress = ServerAddressTextBox.Text.Trim();
                string databaseName = DatabaseNameComboBox.Text.Trim();
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password;
                string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";

                // 检测MySQL是否已安装或可连接
                var connectionStatus = await CheckMySqlConnectionStatus(serverAddress, port);
                if (connectionStatus != MySqlConnectionStatus.Connected)
                {
                    System.Diagnostics.Debug.WriteLine($"MySQL连接状态: {connectionStatus}");
                    LogHelper.LogWarning($"MySQL连接状态: {connectionStatus}，无法继续登录");
                    await ShowDatabaseConnectionError(serverAddress, port, connectionStatus);
                    return;
                }

                // 验证输入
                if (string.IsNullOrEmpty(serverAddress))
                {
                    ShowError("请输入服务器地址");
                    return;
                }

                if (string.IsNullOrEmpty(databaseName))
                {
                    ShowError("请输入数据库名称");
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    ShowError("请输入用户名");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowError("请输入密码");
                    return;
                }

                // 验证端口号
                if (CustomPortCheckBox.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(port))
                    {
                        ShowError("请输入端口号");
                        return;
                    }

                    if (!Regex.IsMatch(port, @"^\d+$"))
                    {
                        ShowError("端口号必须是数字");
                        return;
                    }

                    int portNumber;
                    if (!int.TryParse(port, out portNumber) || portNumber < 1 || portNumber > 65535)
                    {
                        ShowError("端口号必须在1-65535之间");
                        return;
                    }
                }

                // 处理本地连接地址
                bool isLocalConnection = false;
                if (serverAddress.ToLower() == "localhost" || serverAddress == "127.0.0.1" || serverAddress == "::1")
                {
                    // 统一使用localhost作为本地连接地址
                    serverAddress = "localhost";
                    isLocalConnection = true;
                }

                // 禁用登录按钮，防止重复点击
                LoginButton.IsEnabled = false;

                // 创建加载动画内容
                var loadingContent = new StackPanel { Margin = new Thickness(24) };
                loadingContent.Children.Add(new ProgressBar
                {
                    IsIndeterminate = true,
                    Style = (Style)Application.Current.Resources["MaterialDesignCircularProgressBar"],
                    Width = 60,
                    Height = 60,
                    Margin = new Thickness(0, 0, 0, 16)
                });

                var loadingTextBlock = new TextBlock
                {
                    Text = "正在连接数据库...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                loadingContent.Children.Add(loadingTextBlock);

                // 创建一个计时器，如果连接超过300ms，则显示加载动画
                loadingTimer = new System.Threading.Timer(async (state) =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MaterialDesignThemes.Wpf.DialogHost.Show(loadingContent, "LoginDialogHost");
                    });
                }, null, 300, System.Threading.Timeout.Infinite);

                try
                {
                    // 检测服务器是否可达（仅对非本地连接进行Ping测试）
                    // 注意：许多云服务器会禁用ICMP协议（Ping），但实际上数据库连接（TCP）可能正常
                    bool serverReachable = true;
                    if (!isLocalConnection)
                    {
                        // 更新加载文本
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                            {
                                loadingTextBlock.Text = $"正在检测服务器 {serverAddress} 是否可达...";
                            }
                        });

                        // 尝试Ping服务器，但即使失败也继续尝试连接
                        await Task.Run(() =>
                        {
                            try
                            {
                                using (Ping ping = new Ping())
                                {
                                    PingReply reply = ping.Send(serverAddress, 1000);
                                    serverReachable = reply.Status == IPStatus.Success;
                                }
                            }
                            catch
                            {
                                serverReachable = false;
                                // 记录日志但不终止连接
                                LogHelper.LogSystemWarning("网络", $"无法Ping通服务器 {serverAddress}，但仍将尝试连接数据库");
                            }
                        });

                        // 如果Ping不通，记录警告但不抛出异常，继续尝试连接数据库
                        if (!serverReachable)
                        {
                            LogHelper.LogSystemWarning("网络", $"服务器 {serverAddress} 无法Ping通，但仍将尝试连接数据库");
                        }
                    }
                    else
                    {
                        // 更新加载文本
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                            {
                                loadingTextBlock.Text = "正在检测本地MySQL服务...";
                            }
                        });

                        // 对于本地连接，检测MySQL服务是否在运行
                        bool mysqlServiceRunning = await Task.Run(() =>
                        {
                            try
                            {
                                // 尝试连接到MySQL默认端口
                                using (var client = new System.Net.Sockets.TcpClient())
                                {
                                    // 设置较短的超时时间
                                    var connectTask = client.ConnectAsync("127.0.0.1", int.Parse(port));
                                    var timeoutTask = Task.Delay(500); // 500ms超时

                                    // 等待连接或超时
                                    var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;

                                    // 如果连接任务完成且客户端已连接，则MySQL服务正在运行
                                    return completedTask == connectTask && client.Connected;
                                }
                            }
                            catch
                            {
                                return false;
                            }
                        });

                        if (!mysqlServiceRunning)
                        {
                            throw new Exception($"无法连接到本地MySQL服务，请确认MySQL服务是否已启动，以及端口{port}是否正确。");
                        }
                    }

                    // 更新加载文本
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                        {
                            loadingTextBlock.Text = $"正在连接到数据库 {databaseName}...";
                        }
                    });

                    // 构建连接字符串
                    string connectionString = $"Server={serverAddress};Port={port};Database={databaseName};User ID={username};Password={password};CharSet=utf8;Connect Timeout=15;AllowPublicKeyRetrieval=true;UseCompression=false;Default Command Timeout=30;SslMode=none;Max Pool Size=50;";

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
                                connectionException = new Exception($"连接数据库失败: {ex.Message}", ex);
                            }
                            catch (Exception ex)
                            {
                                connectionException = ex;
                            }
                        }
                    });

                    // 关闭加载动画
                    loadingTimer.Dispose();
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // 只有在DialogHost已打开的情况下才尝试关闭它
                        if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                        {
                            MaterialDesignThemes.Wpf.DialogHost.Close("LoginDialogHost");
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
                        SaveLastDatabaseName(databaseName);
                        SaveLastServerAddress(serverAddress);
                        SaveDatabaseNameToHistory(databaseName);

                        // 如果必要的表不存在，检测SQL文件夹中是否有对应的SQL文件
                        if (!tablesExist)
                        {
                            // 检测SqlData文件夹是否存在
                            string sqlDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlData");

                            // 如果在应用程序目录中找不到SqlData文件夹，尝试在当前目录中查找
                            if (!Directory.Exists(sqlDataFolderPath))
                            {
                                sqlDataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "SqlData");
                            }

                            // 如果仍然找不到，尝试在上级目录中查找
                            if (!Directory.Exists(sqlDataFolderPath))
                            {
                                string? parentDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
                                if (parentDir != null)
                                {
                                    sqlDataFolderPath = Path.Combine(parentDir, "SqlData");
                                }
                            }

                            // 如果仍然找不到，尝试在项目根目录中查找
                            if (!Directory.Exists(sqlDataFolderPath))
                            {
                                // 尝试查找解决方案文件所在的目录
                                string currentDir = Directory.GetCurrentDirectory();
                                string[] solutionFiles = Directory.GetFiles(currentDir, "*.sln", SearchOption.AllDirectories);

                                if (solutionFiles.Length > 0)
                                {
                                    string solutionDir = Path.GetDirectoryName(solutionFiles[0]);
                                    if (solutionDir != null)
                                    {
                                        sqlDataFolderPath = Path.Combine(solutionDir, "SqlData");
                                    }
                                }
                            }

                            if (!Directory.Exists(sqlDataFolderPath))
                            {
                                throw new Exception($"数据库中缺少必要的表结构（{string.Join(", ", _requiredTables)}），且未找到SqlData文件夹。请确保SqlData文件夹存在于应用程序目录中。");
                            }

                            System.Diagnostics.Debug.WriteLine($"找到SqlData文件夹: {sqlDataFolderPath}");

                            // 检测必要的SQL文件是否存在
                            bool allSqlFilesExist = true;
                            List<string> missingSqlFiles = new List<string>();

                            foreach (string requiredTable in _requiredTables)
                            {
                                string sqlFilePath = Path.Combine(sqlDataFolderPath, $"{requiredTable}.sql");
                                if (!File.Exists(sqlFilePath))
                                {
                                    allSqlFilesExist = false;
                                    missingSqlFiles.Add($"{requiredTable}.sql");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"找到SQL文件: {sqlFilePath}");
                                }
                            }

                            if (!allSqlFilesExist)
                            {
                                throw new Exception($"数据库中缺少必要的表结构（{string.Join(", ", _requiredTables)}），且SqlData文件夹中缺少以下SQL文件：{string.Join(", ", missingSqlFiles)}");
                            }

                            // 显示导入确认对话框
                            var dialogContent = new StackPanel { Margin = new Thickness(16) };

                            dialogContent.Children.Add(new TextBlock
                            {
                                Text = "缺少表结构",
                                FontWeight = FontWeights.Bold,
                                FontSize = (double)Application.Current.Resources["MaterialDesignHeadline6FontSize"],
                                Margin = new Thickness(0, 0, 0, 16)
                            });

                            dialogContent.Children.Add(new TextBlock
                            {
                                Text = $"数据库中缺少必要的表结构（{string.Join(", ", _requiredTables)}）。是否从SqlData文件夹导入表结构？",
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 0, 0, 16)
                            });

                            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                            var noButton = new Button
                            {
                                Content = "否",
                                Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                                Margin = new Thickness(0, 0, 8, 0),
                                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand
                            };

                            var yesButton = new Button
                            {
                                Content = "是",
                                Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand
                            };

                            buttonPanel.Children.Add(noButton);
                            buttonPanel.Children.Add(yesButton);
                            dialogContent.Children.Add(buttonPanel);

                            var result = await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "LoginDialogHost");

                            if (result != null && result.Equals(yesButton))
                            {
                                // 显示导入中的加载动画
                                var importingContent = new StackPanel { Margin = new Thickness(24) };
                                importingContent.Children.Add(new ProgressBar
                                {
                                    IsIndeterminate = true,
                                    Style = (Style)Application.Current.Resources["MaterialDesignCircularProgressBar"],
                                    Width = 60,
                                    Height = 60,
                                    Margin = new Thickness(0, 0, 0, 16)
                                });

                                var importingTextBlock = new TextBlock
                                {
                                    Text = "正在准备导入表结构...",
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 8, 0, 0)
                                };
                                importingContent.Children.Add(importingTextBlock);

                                await Dispatcher.InvokeAsync(() =>
                                {
                                    MaterialDesignThemes.Wpf.DialogHost.Show(importingContent, "LoginDialogHost");
                                });

                                // 导入SQL文件
                                Exception importException = null;

                                await Task.Run(async () =>
                                {
                                    try
                                    {
                                        using (var connection = new MySqlConnection(connectionString))
                                        {
                                            connection.Open();

                                            // 导入每个SQL文件
                                            foreach (string requiredTable in _requiredTables)
                                            {
                                                // 更新导入状态
                                                await Dispatcher.InvokeAsync(() =>
                                                {
                                                    if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                                                    {
                                                        importingTextBlock.Text = $"正在导入表 {requiredTable}...";
                                                    }
                                                });

                                                string sqlFilePath = Path.Combine(sqlDataFolderPath, $"{requiredTable}.sql");
                                                string sqlScript = File.ReadAllText(sqlFilePath, Encoding.UTF8);

                                                // 分割SQL脚本为多个语句
                                                string[] sqlStatements = SplitSqlStatements(sqlScript);

                                                foreach (string statement in sqlStatements)
                                                {
                                                    if (!string.IsNullOrWhiteSpace(statement))
                                                    {
                                                        using (var command = new MySqlCommand(statement, connection))
                                                        {
                                                            try
                                                            {
                                                                command.ExecuteNonQuery();
                                                            }
                                                            catch (MySqlException ex)
                                                            {
                                                                // 记录错误但继续执行其他语句
                                                                System.Diagnostics.Debug.WriteLine($"执行SQL语句时出错: {ex.Message}");
                                                                System.Diagnostics.Debug.WriteLine($"有问题的SQL语句: {statement}");
                                                            }
                                                        }
                                                    }
                                                }

                                                // 短暂延迟，确保UI更新
                                                await Task.Delay(100);
                                            }

                                            // 更新导入状态
                                            await Dispatcher.InvokeAsync(() =>
                                            {
                                                if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                                                {
                                                    importingTextBlock.Text = "表结构导入完成，正在验证...";
                                                }
                                            });

                                            // 验证表是否已成功导入
                                            bool tablesImported = CheckRequiredTables(connection);
                                            if (!tablesImported)
                                            {
                                                importException = new Exception("表结构导入后验证失败，请检测SQL文件是否正确。");
                                            }
                                        }
                                    }
                                    catch (MySqlException ex)
                                    {
                                        importException = new Exception($"导入SQL文件时出错: {ex.Message}", ex);
                                    }
                                    catch (Exception ex)
                                    {
                                        importException = new Exception($"导入SQL文件时出错: {ex.Message}", ex);
                                    }
                                });

                                // 关闭导入中的加载动画
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                                    {
                                        MaterialDesignThemes.Wpf.DialogHost.Close("LoginDialogHost");
                                    }
                                });

                                // 如果导入过程中发生异常，抛出异常
                                if (importException != null)
                                {
                                    throw importException;
                                }

                                // 显示导入成功消息
                                LoginSnackbar.MessageQueue?.Enqueue("表结构导入成功", null, null, null, false, true, TimeSpan.FromSeconds(3));
                            }
                            else
                            {
                                // 用户选择不导入，提示无法继续
                                throw new Exception($"数据库中缺少必要的表结构（{string.Join(", ", _requiredTables)}），无法继续。");
                            }
                        }

                        // 登录成功
                        LoginSuccessful = true;

                        // 保存登录时间
                        _loginInfoService.SaveLastLoginTime();

                        // 确保之前的MainViewModel被销毁
                        if (_mainWindow != null)
                        {
                            try
                            {
                                // 关闭之前的主窗口
                                _mainWindow.Close();
                                _mainWindow = null;

                                // 强制垃圾回收
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"关闭之前的主窗口时出错: {ex.Message}");
                                LogHelper.LogError($"关闭之前的主窗口时出错: {ex.Message}");
                            }
                        }

                        // 获取当前主题状态
                        var themeService = TA_WPF.Services.ThemeService.Instance;
                        bool isDarkMode = themeService.IsDarkThemeActive();

                        // 确保主题设置已保存到配置文件
                        themeService.ApplyTheme(isDarkMode);

                        // 显式应用字体大小设置，确保在连接数据库切换后，所有UI元素都能正确应用设置
                        try {
                            // 从配置文件中获取当前字体大小
                            double fontSize = ConfigUtils.GetDoubleValue("FontSize", 13);
                            
                            // 创建UIService实例并应用字体大小
                            var uiService = new UIService();
                            uiService.ApplyFontSize(fontSize);
                            
                            // 在应用级别同步字体大小设置
                            if (Application.Current is App app) {
                                app.SyncFontSizeSettings(fontSize);
                            }
                            
                            LogHelper.LogInfo($"重新登录后应用字体大小设置: {fontSize}pt");
                        } catch (Exception ex) {
                            LogHelper.LogError($"重新登录后应用字体大小设置时出错: {ex.Message}", ex);
                        }

                        // 创建新的主窗口和MainViewModel
                        _mainWindow = new MainWindow(ConnectionString);

                        // 确保主题设置同步到主窗口
                        if (_mainWindow.DataContext is MainViewModel mainViewModel)
                        {
                            // 显式设置主题模式
                            mainViewModel.IsDarkMode = isDarkMode;
                            System.Diagnostics.Debug.WriteLine($"已设置MainViewModel.IsDarkMode = {isDarkMode}");
                            LogHelper.LogInfo($"已设置MainViewModel.IsDarkMode = {isDarkMode}");
                        }

                        // 显式应用主题到主窗口
                        MaterialDesignThemes.Wpf.ThemeAssist.SetTheme(_mainWindow,
                            isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light);

                        // 强制应用主题
                        themeService.ApplyTheme(isDarkMode);

                        // 重置车站检测服务的忽略状态
                        // StationCheckService.Instance.ResetIgnoreStationCheck();
                        LogHelper.LogInfo("已登录成功");

                        // 强制刷新主窗口
                        _mainWindow.UpdateLayout();

                        // 设置主窗口
                        Application.Current.MainWindow = _mainWindow;

                        // 显示主窗口
                        _mainWindow.Show();

                        // 显示登录成功提示
                        _mainWindow.ShowLoginSuccessNotification();

                        // 记录日志
                        LogHelper.LogSystem("登录", $"用户登录成功，已创建新的MainViewModel，主题模式：{(isDarkMode ? "深色" : "浅色")}");

                        // 关闭登录窗口
                        this.Close();
                    }
                }
                catch (Exception innerEx)
                {
                    // 记录内部异常
                    System.Diagnostics.Debug.WriteLine($"登录过程中发生内部异常: {innerEx.Message}");
                    LogHelper.LogError($"登录过程中发生内部异常: {innerEx.Message}");

                    // 关闭加载动画
                    loadingTimer.Dispose();
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                        {
                            MaterialDesignThemes.Wpf.DialogHost.Close("LoginDialogHost");
                        }
                    });

                    // 显示错误消息
                    ShowError(innerEx.Message);
                    LoginButton.IsEnabled = true;
                }
            }
            catch (MySqlException ex)
            {
                // 关闭加载动画
                loadingTimer.Dispose();
                await Dispatcher.InvokeAsync(() =>
                {
                    // 只有在DialogHost已打开的情况下才尝试关闭它
                    if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                    {
                        MaterialDesignThemes.Wpf.DialogHost.Close("LoginDialogHost");
                    }
                });

                // 特殊处理MySQL异常
                ShowError($"连接数据库失败: {ex.Message}");
                LoginButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // 关闭加载动画
                loadingTimer.Dispose();
                await Dispatcher.InvokeAsync(() =>
                {
                    // 只有在DialogHost已打开的情况下才尝试关闭它
                    if (MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen("LoginDialogHost"))
                    {
                        MaterialDesignThemes.Wpf.DialogHost.Close("LoginDialogHost");
                    }
                });

                ShowError(ex.Message);
                LoginButton.IsEnabled = true;
            }
        }

        // 检测必要的表是否存在
        private bool CheckRequiredTables(MySqlConnection connection)
        {
            try
            {
                // 获取数据库中的所有表
                DataTable tables = connection.GetSchema("Tables");
                List<string> existingTables = new List<string>();

                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString().ToLower();
                    existingTables.Add(tableName);
                }

                // 检测必要的表是否都存在
                foreach (string requiredTable in _requiredTables)
                {
                    if (!existingTables.Contains(requiredTable.ToLower()))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            // 解析错误消息，提供更友好的提示
            string userFriendlyMessage = GetUserFriendlyErrorMessage(message);

            // 使用MaterialDesign对话框替代MessageBox
            var dialogContent = new StackPanel { Margin = new Thickness(16) };

            dialogContent.Children.Add(new TextBlock
            {
                Text = "连接错误",
                FontWeight = FontWeights.Bold,
                FontSize = (double)Application.Current.Resources["MaterialDesignHeadline6FontSize"],
                Margin = new Thickness(0, 0, 0, 16)
            });

            // 创建一个支持多行显示的TextBlock
            var messageTextBlock = new TextBlock
            {
                Text = userFriendlyMessage,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };

            // 如果消息包含换行符，设置行高以改善可读性
            if (userFriendlyMessage.Contains("\n"))
            {
                messageTextBlock.LineHeight = 24;
            }

            dialogContent.Children.Add(messageTextBlock);

            // 如果是认证方式错误，添加复制SQL命令的按钮
            if (userFriendlyMessage.Contains("认证方式不兼容"))
            {
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

                var copyLocalButton = new Button
                {
                    Content = "复制localhost命令",
                    Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                    Margin = new Thickness(0, 0, 8, 0)
                };

                copyLocalButton.Click += (s, e) =>
                {
                    try
                    {
                        string username = UsernameTextBox.Text.Trim();
                        string password = PasswordBox.Password;
                        string sqlCommand = $"ALTER USER '{username}'@'localhost' IDENTIFIED WITH mysql_native_password BY '{password}';";
                        Clipboard.SetText(sqlCommand);

                        // 显示复制成功提示
                        LoginSnackbar.MessageQueue?.Enqueue("localhost SQL命令已复制到剪贴板", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"复制SQL命令时出错: {ex.Message}");
                    }
                };

                var copyAnyButton = new Button
                {
                    Content = "复制通配符命令",
                    Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"]
                };

                copyAnyButton.Click += (s, e) =>
                {
                    try
                    {
                        string username = UsernameTextBox.Text.Trim();
                        string password = PasswordBox.Password;
                        string sqlCommand = $"ALTER USER '{username}'@'%' IDENTIFIED WITH mysql_native_password BY '{password}';";
                        Clipboard.SetText(sqlCommand);

                        // 显示复制成功提示
                        LoginSnackbar.MessageQueue?.Enqueue("通配符SQL命令已复制到剪贴板", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"复制SQL命令时出错: {ex.Message}");
                    }
                };

                buttonPanel.Children.Add(copyLocalButton);
                buttonPanel.Children.Add(copyAnyButton);
                dialogContent.Children.Add(buttonPanel);

                // 添加说明文本
                dialogContent.Children.Add(new TextBlock
                {
                    Text = "注意: 如果不确定用户的主机模式，可以尝试两种命令。通配符命令适用于远程连接，localhost命令适用于本地连接。",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 16),
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                });
            }

            var okButton = new Button
            {
                Content = "确定",
                Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand
            };

            dialogContent.Children.Add(okButton);

            // 显示对话框
            MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "LoginDialogHost");
        }

        // 获取用户友好的错误消息
        private string GetUserFriendlyErrorMessage(string originalMessage)
        {
            // 数据库不存在错误
            if (originalMessage.Contains("Unknown database") || originalMessage.Contains("Unknown schema"))
            {
                string dbName = DatabaseNameComboBox.Text.Trim();
                return $"数据库 '{dbName}' 不存在，请检测数据库名称是否正确。\n" +
                       "如果确认数据库名称正确，您可以：\n" +
                       "1. 检测数据库是否已创建\n" +
                       "2. 使用正确的数据库名称\n" +
                       "3. 点击\"创建数据库\"按钮创建新数据库";
            }

            // 认证方法错误 - 增强处理
            if (originalMessage.Contains("Authentication to host") ||
                originalMessage.Contains("using method 'caching_sha2_password'") ||
                originalMessage.Contains("using method 'sha256_password'") ||
                originalMessage.Contains("Authentication plugin") ||
                originalMessage.Contains("is not supported"))
            {
                // 首先检测是否是访问被拒绝的错误
                if (originalMessage.Contains("Access denied for user"))
                {
                    string username = UsernameTextBox.Text.Trim();
                    return $"登录失败：用户'{username}'的用户名或密码错误。\n" +
                           "请检测：\n" +
                           "1. 用户名拼写是否正确\n" +
                           "2. 密码是否正确\n" +
                           "3. 该用户是否有权限访问MySQL服务器\n" +
                           "4. 如果确认用户名密码无误，请联系数据库管理员检测用户权限";
                }

                return "认证方式不兼容，请尝试以下解决方案：\n" +
                       "1. 在MySQL中修改用户认证方式为mysql_native_password\n" +
                       "   执行SQL: ALTER USER '用户名'@'%' IDENTIFIED WITH mysql_native_password BY '密码';\n" +
                       "   或者: ALTER USER '用户名'@'localhost' IDENTIFIED WITH mysql_native_password BY '密码';\n" +
                       "2. 在MySQL配置文件中添加default_authentication_plugin=mysql_native_password\n" +
                       "3. 重启MySQL服务";
            }

            // 用户名或密码错误
            if (originalMessage.Contains("Access denied for user") && originalMessage.Contains("using password"))
            {
                string username = UsernameTextBox.Text.Trim();
                return $"登录失败：用户'{username}'的用户名或密码错误。\n" +
                       "请检测：\n" +
                       "1. 用户名拼写是否正确\n" +
                       "2. 密码是否正确\n" +
                       "3. 该用户是否有权限访问MySQL服务器\n" +
                       "4. 如果确认用户名密码无误，请联系数据库管理员检测用户权限";
            }

            // 连接超时
            if (originalMessage.Contains("Connection timeout") || originalMessage.Contains("Reading from the stream has timed out"))
            {
                return "连接服务器超时，请检测网络连接或服务器地址是否正确。";
            }

            // 无法连接到服务器
            if (originalMessage.Contains("Unable to connect") || originalMessage.Contains("Could not connect to MySQL server"))
            {
                // 检测是否是本地连接
                string serverAddress = ServerAddressTextBox.Text.Trim();
                if (serverAddress == "localhost" || serverAddress == "127.0.0.1")
                {
                    return "无法连接到本地MySQL服务器，请确认MySQL服务是否已启动。";
                }
                else
                {
                    return "无法连接到MySQL服务器，请检测服务器地址是否正确或服务器是否在线。";
                }
            }

            // 端口错误
            if (originalMessage.Contains("No connection could be made because the target machine actively refused it") ||
                originalMessage.Contains("Connection refused"))
            {
                string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";
                return $"连接被拒绝，请检测端口号({port})是否正确或MySQL服务是否已在该端口启动。";
            }

            // 主机名解析错误
            if (originalMessage.Contains("Unknown MySQL server host") || originalMessage.Contains("No such host is known"))
            {
                string serverAddress = ServerAddressTextBox.Text.Trim();
                return $"无法解析服务器地址'{serverAddress}'，请检测拼写是否正确或网络DNS设置。";
            }

            // 认证方式错误
            if (originalMessage.Contains("Authentication method") && originalMessage.Contains("not supported"))
            {
                return "不支持的认证方式，可能是MySQL版本过高或客户端驱动过旧，请尝试使用旧版认证方式。";
            }

            // 字符集错误
            if (originalMessage.Contains("Unknown character set"))
            {
                return "未知的字符集，请检测数据库配置或连接字符串设置。";
            }

            // SSL连接错误
            if (originalMessage.Contains("SSL connection error"))
            {
                return "SSL连接错误，请检测SSL配置或尝试禁用SSL连接。";
            }

            // 服务器已关闭连接
            if (originalMessage.Contains("server has gone away") || originalMessage.Contains("Connection closed"))
            {
                return "服务器已关闭连接，可能是网络不稳定或服务器超时设置过短。";
            }

            // 公钥检索错误
            if (originalMessage.Contains("AllowPublicKeyRetrieval"))
            {
                return "需要启用公钥检索，请在连接字符串中添加AllowPublicKeyRetrieval=true参数。";
            }

            // 输入验证错误
            if (string.IsNullOrEmpty(ServerAddressTextBox.Text.Trim()))
            {
                return "请输入服务器地址。";
            }

            if (string.IsNullOrEmpty(DatabaseNameComboBox.Text.Trim()))
            {
                return "请输入数据库名称。";
            }

            if (string.IsNullOrEmpty(UsernameTextBox.Text.Trim()))
            {
                return "请输入用户名。";
            }

            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                return "请输入密码。";
            }

            if (CustomPortCheckBox.IsChecked == true && string.IsNullOrEmpty(PortTextBox.Text.Trim()))
            {
                return "请输入端口号。";
            }

            if (CustomPortCheckBox.IsChecked == true && !Regex.IsMatch(PortTextBox.Text.Trim(), @"^\d+$"))
            {
                return "端口号必须是数字。";
            }

            // 如果无法识别具体错误，返回原始错误消息
            return originalMessage;
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
                MessageBox.Show($"保存连接字符串时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DatabaseNameComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabaseHistory();
        }

        private void LoadDatabaseHistory()
        {
            try
            {
                // 从配置文件中加载数据库历史记录
                string historyString = ConfigUtils.GetStringValue("DatabaseHistory");
                if (!string.IsNullOrEmpty(historyString))
                {
                    // 解析历史记录
                    List<string> history = historyString.Split(',').ToList();

                    // 清空当前项
                    DatabaseNameComboBox.Items.Clear();

                    // 添加历史记录
                    foreach (string item in history)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            DatabaseNameComboBox.Items.Add(item);
                        }
                    }

                    // 设置当前选中项
                    if (!string.IsNullOrEmpty(_lastDatabaseName) && DatabaseNameComboBox.Items.Contains(_lastDatabaseName))
                    {
                        DatabaseNameComboBox.SelectedItem = _lastDatabaseName;
                    }
                    else if (DatabaseNameComboBox.Items.Count > 0)
                    {
                        DatabaseNameComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载数据库历史记录时出错: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存数据库历史记录时出错: {ex.Message}");
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 密码框获得焦点时检测大写锁定状态
            CheckCapsLock();
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 密码框失去焦点时隐藏大写锁定图标
            CapsLockIcon.Visibility = Visibility.Collapsed;
        }

        // 创建数据库按钮点击事件
        private async void CreateDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取输入值并验证
            string serverAddress = ServerAddressTextBox.Text.Trim();
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";

            // 验证服务器地址
            if (string.IsNullOrEmpty(serverAddress))
            {
                ShowError("请输入服务器地址");
                return;
            }

            // 验证用户名和密码
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("请输入用户名和密码");
                return;
            }

            string newDatabaseName = NewDatabaseNameTextBox.Text.Trim();

            // 如果未输入数据库名称，提示用户输入
            if (string.IsNullOrEmpty(newDatabaseName))
            {
                ShowError("请输入新数据库名称");
                // 聚焦到数据库名称输入框
                NewDatabaseNameTextBox.Focus();
                return;
            }

            // 禁用按钮，防止重复点击
            CreateDatabaseButton.IsEnabled = false;

            try
            {
                // 构建连接字符串（不包含数据库名称）
                string connectionString = $"Server={serverAddress};Port={port};User ID={username};Password={password};CharSet=utf8;Connect Timeout=15;AllowPublicKeyRetrieval=true;UseCompression=false;Default Command Timeout=30;SslMode=none;Max Pool Size=50;";

                // 创建数据库
                Exception createException = null;

                await Task.Run(() =>
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();

                            // 创建数据库
                            using (MySqlCommand cmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{newDatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", connection))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            // 使用新创建的数据库
                            connection.ChangeDatabase(newDatabaseName);

                            // 创建必要的表结构
                            CreateRequiredTables(connection);
                        }
                        catch (MySqlException ex)
                        {
                            createException = new Exception($"创建数据库时出错: {ex.Message}", ex);
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

                // 更新数据库名称下拉框，选择刚刚创建的数据库
                DatabaseNameComboBox.Text = newDatabaseName;
                SaveDatabaseNameToHistory(newDatabaseName);

                // 确保下拉框正确显示新创建的数据库名称
                if (!DatabaseNameComboBox.Items.Contains(newDatabaseName))
                {
                    DatabaseNameComboBox.Items.Add(newDatabaseName);
                }
                DatabaseNameComboBox.SelectedItem = newDatabaseName;

                // 显示成功消息
                LoginSnackbar.MessageQueue.Enqueue($"数据库 '{newDatabaseName}' 创建成功");

                // 清空新数据库名称文本框，以便下次使用
                NewDatabaseNameTextBox.Text = string.Empty;

                // 日志记录
                LogHelper.LogSystem("数据库", $"成功创建数据库: {newDatabaseName}");
            }
            catch (MySqlException ex)
            {
                // 特殊处理MySQL异常
                ShowError($"创建数据库失败: {ex.Message}");
                LogHelper.LogError($"创建数据库失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                LogHelper.LogError($"创建数据库失败: {ex.Message}");
            }
            finally
            {
                // 重新启用按钮
                CreateDatabaseButton.IsEnabled = true;
            }
        }

        // 创建必要的表结构
        private void CreateRequiredTables(MySqlConnection connection)
        {
            try
            {
                // 禁用外键约束检测
                using (MySqlCommand disableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                {
                    disableChecksCmd.ExecuteNonQuery();
                }

                // 创建station_info表
                using (MySqlCommand cmd = new MySqlCommand(@"
                    DROP TABLE IF EXISTS `station_info`;
                    CREATE TABLE `station_info`  (
                    `id` int NOT NULL AUTO_INCREMENT COMMENT 'id',
                    `station_name` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站名称',
                    `province` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在省',
                    `city` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在市',
                    `district` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站所在区',
                    `longitude` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '经度',
                    `latitude` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '纬度',
                    `station_code` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站代码',
                    `station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车站拼音',
                    PRIMARY KEY (`id`) USING BTREE,
                    INDEX `station_name`(`station_name` ASC) USING BTREE,
                    INDEX `fk_arrive_code`(`station_code` ASC) USING BTREE,
                    INDEX `station_pinyin`(`station_pinyin` ASC) USING BTREE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = DYNAMIC;
                    ALTER TABLE `station_info` AUTO_INCREMENT = 1;", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建train_ride_info表
                using (MySqlCommand cmd = new MySqlCommand(@"
                    DROP TABLE IF EXISTS `train_ride_info`;
                    CREATE TABLE `train_ride_info`  (
                    `id` int NOT NULL AUTO_INCREMENT COMMENT 'id',
                    `ticket_number` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '取票号',
                    `check_in_location` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '检票位置',
                    `depart_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站',
                    `train_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车次号',
                    `arrive_station` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站',
                    `depart_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站拼音',
                    `arrive_station_pinyin` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站拼音',
                    `depart_date` date NULL DEFAULT NULL COMMENT '出发日期',
                    `depart_time` time NULL DEFAULT NULL COMMENT '出发时间',
                    `coach_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车厢号',
                    `seat_no` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '座位号',
                    `money` decimal(6, 2) NULL DEFAULT NULL COMMENT '金额',
                    `seat_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '席别',
                    `additional_info` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '附加信息（退票费/限乘当日当次车）',
                    `ticket_purpose` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票用途',
                    `ticket_modification_type` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '车票改签类型',
                    `ticket_type_flags` int NULL DEFAULT 0 COMMENT '票种类型（枚举）',
                    `payment_channel_flags` int NULL DEFAULT 0 COMMENT '支付渠道（枚举）',
                    `hint` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '提示信息',
                    `depart_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '出发车站代码',
                    `arrive_station_code` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '到达车站代码',
                    PRIMARY KEY (`id`) USING BTREE,
                    INDEX `station_code`(`depart_station_code` ASC) USING BTREE,
                    INDEX `arrive_station_code`(`arrive_station_code` ASC) USING BTREE,
                    INDEX `fk_depart_station_pinyin`(`depart_station_pinyin` ASC) USING BTREE,
                    INDEX `fk_arrive_station_pinyin`(`arrive_station_pinyin` ASC) USING BTREE,
                    INDEX `idx_train_no`(`train_no` ASC, `depart_date` ASC) USING BTREE,
                    INDEX `idx_depart_station`(`depart_station` ASC, `depart_date` ASC) USING BTREE,
                    CONSTRAINT `fc_dc_arrive` FOREIGN KEY (`arrive_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE,
                    CONSTRAINT `fc_dp_arrive` FOREIGN KEY (`arrive_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
                    CONSTRAINT `fc_sp_depart` FOREIGN KEY (`depart_station_pinyin`) REFERENCES `station_info` (`station_pinyin`) ON DELETE CASCADE ON UPDATE CASCADE,
                    CONSTRAINT `fk_sc_depart` FOREIGN KEY (`depart_station_code`) REFERENCES `station_info` (`station_code`) ON DELETE CASCADE ON UPDATE CASCADE
                    ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = DYNAMIC;
                    ALTER TABLE `train_ride_info` AUTO_INCREMENT = 1;", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 重新启用外键约束检测
                using (MySqlCommand enableChecksCmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                {
                    enableChecksCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建表结构时出错: {ex.Message}");
                LogHelper.LogError($"创建表结构时出错: {ex.Message}");
                throw new Exception($"创建表结构时出错: {ex.Message}", ex);
            }
        }

        // 分割SQL脚本为多个语句
        private string[] SplitSqlStatements(string sqlScript)
        {
            // 移除注释
            sqlScript = RemoveSqlComments(sqlScript);

            // 按分号分割SQL语句
            List<string> statements = new List<string>();

            // 使用正则表达式分割SQL语句，考虑到分号可能出现在字符串中
            string pattern = @";(?=(?:[^']*'[^']*')*[^']*$)";
            string[] parts = System.Text.RegularExpressions.Regex.Split(sqlScript, pattern);

            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    statements.Add(trimmedPart);
                }
            }

            return statements.ToArray();
        }

        // 移除SQL注释
        private string RemoveSqlComments(string sql)
        {
            // 移除单行注释 (-- 开头的行)
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"--.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            // 移除多行注释 (/* ... */)
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"/\*[\s\S]*?\*/", "");

            return sql;
        }

        /// <summary>
        /// 添加枚举以区分MySQL连接问题的不同情况
        /// </summary>
        private enum MySqlConnectionStatus
        {
            Connected,                // 成功连接
            NotInstalled,             // MySQL未安装
            PortError,                // 端口错误
            ConnectionError           // 其他连接错误
        }

        /// <summary>
        /// 检测MySQL是否已安装或远程服务器是否可连接
        /// </summary>
        /// <param name="serverAddress">服务器地址</param>
        /// <param name="port">端口</param>
        /// <returns>MySQL连接状态</returns>
        private async Task<MySqlConnectionStatus> CheckMySqlConnectionStatus(string serverAddress, string port)
        {
            // 测试模式：模拟MySQL未安装的情况
            bool testMode = false;
            if (testMode)
            {
                System.Diagnostics.Debug.WriteLine("测试模式：模拟MySQL未安装");
                return MySqlConnectionStatus.NotInstalled;
            }

            try
            {
                // 检测是否是远程连接
                bool isLocalConnection = string.IsNullOrEmpty(serverAddress) ||
                                         serverAddress.ToLower() == "localhost" ||
                                         serverAddress == "127.0.0.1" ||
                                         serverAddress == "::1";

                // 如果是远程连接，检测远程服务器是否可达
                if (!isLocalConnection)
                {
                    System.Diagnostics.Debug.WriteLine($"检测远程MySQL连接: {serverAddress}:{port}");

                    bool canConnect = await Task.Run(() =>
                    {
                        try
                        {
                            using (var client = new System.Net.Sockets.TcpClient())
                            {
                                // 尝试连接MySQL端口
                                var connectTask = client.ConnectAsync(serverAddress, int.Parse(port));
                                var timeoutTask = Task.Delay(2000); // 2秒超时

                                var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                                return completedTask == connectTask && client.Connected;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"连接远程MySQL服务器失败: {ex.Message}");
                            return false;
                        }
                    });

                    if (canConnect)
                    {
                        System.Diagnostics.Debug.WriteLine($"成功连接到远程MySQL服务器: {serverAddress}:{port}");
                        return MySqlConnectionStatus.Connected;
                    }
                    else
                    {
                        // 对于远程连接，判断是否为端口问题
                        // 尝试使用默认端口3306连接
                        if (port != "3306" && CustomPortCheckBox.IsChecked == true)
                        {
                            bool defaultPortWorks = await Task.Run(() =>
                            {
                                try
                                {
                                    using (var client = new System.Net.Sockets.TcpClient())
                                    {
                                        var connectTask = client.ConnectAsync(serverAddress, 3306);
                                        var timeoutTask = Task.Delay(2000);
                                        var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                                        return completedTask == connectTask && client.Connected;
                                    }
                                }
                                catch
                                {
                                    return false;
                                }
                            });

                            if (defaultPortWorks)
                            {
                                // 如果默认端口可以连接，说明是端口号问题
                                System.Diagnostics.Debug.WriteLine($"默认端口3306可连接，但自定义端口{port}不可连接");
                                return MySqlConnectionStatus.PortError;
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"无法连接到远程MySQL服务器: {serverAddress}:{port}");
                        return MySqlConnectionStatus.ConnectionError;
                    }
                }

                // 对于本地连接，检测本地MySQL安装
                System.Diagnostics.Debug.WriteLine("检测本地MySQL安装");

                // 检测MySQL服务是否存在
                bool serviceExists = false;
                try
                {
                    // 使用sc命令查询MySQL服务
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "query MySQL",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            serviceExists = !output.Contains("指定的服务未安装") && !output.Contains("The specified service does not exist");
                        }
                    }
                }
                catch
                {
                    // 忽略服务检测错误
                }

                // 检测MySQL注册表项
                bool registryExists = false;
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MySQL"))
                    {
                        registryExists = key != null;
                    }

                    if (!registryExists)
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\MySQL"))
                        {
                            registryExists = key != null;
                        }
                    }
                }
                catch
                {
                    // 忽略注册表检测错误
                }

                // 检测MySQL程序文件夹
                bool folderExists = false;
                string[] possiblePaths = new string[]
                {
                    @"C:\Program Files\MySQL",
                    @"C:\Program Files (x86)\MySQL",
                    @"C:\MySQL"
                };

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        folderExists = true;
                        break;
                    }
                }

                // 如果本地任何一项检测通过，检测MySQL服务端口是否可连接
                if (serviceExists || registryExists || folderExists)
                {
                    bool canConnect = await Task.Run(() =>
                    {
                        try
                        {
                            using (var client = new System.Net.Sockets.TcpClient())
                            {
                                // 尝试连接到MySQL默认端口
                                var connectTask = client.ConnectAsync("127.0.0.1", int.Parse(port));
                                var timeoutTask = Task.Delay(1000); // 1秒超时

                                var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                                return completedTask == connectTask && client.Connected;
                            }
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    if (canConnect)
                    {
                        return MySqlConnectionStatus.Connected;
                    }
                    else if (port != "3306" && CustomPortCheckBox.IsChecked == true)
                    {
                        // 检测默认端口是否可连接
                        bool defaultPortWorks = await Task.Run(() =>
                        {
                            try
                            {
                                using (var client = new System.Net.Sockets.TcpClient())
                                {
                                    var connectTask = client.ConnectAsync("127.0.0.1", 3306);
                                    var timeoutTask = Task.Delay(1000);
                                    var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;
                                    return completedTask == connectTask && client.Connected;
                                }
                            }
                            catch
                            {
                                return false;
                            }
                        });

                        if (defaultPortWorks)
                        {
                            // 如果默认端口可以连接，说明是端口号问题
                            System.Diagnostics.Debug.WriteLine($"默认端口3306可连接，但自定义端口{port}不可连接");
                            return MySqlConnectionStatus.PortError;
                        }
                        else
                        {
                            // MySQL已安装但服务未启动
                            return MySqlConnectionStatus.ConnectionError;
                        }
                    }
                    else
                    {
                        // MySQL已安装但服务未启动
                        return MySqlConnectionStatus.ConnectionError;
                    }
                }

                return MySqlConnectionStatus.NotInstalled;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检测MySQL安装或连接时出错: {ex.Message}");
                return MySqlConnectionStatus.ConnectionError;
            }
        }

        // 保留旧方法，但内部调用新方法，以保持兼容性
        private async Task<bool> IsMySqlInstalledOrConnectable(string serverAddress, string port)
        {
            var status = await CheckMySqlConnectionStatus(serverAddress, port);
            return status == MySqlConnectionStatus.Connected;
        }

        /// <summary>
        /// 显示数据库连接错误提示
        /// </summary>
        private async Task ShowDatabaseConnectionError(string serverAddress, string port, MySqlConnectionStatus status)
        {
            try
            {
                var content = new StackPanel
                {
                    Margin = new Thickness(16)
                };

                // 添加警告图标
                var warningIcon = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = MaterialDesignThemes.Wpf.PackIconKind.DatabaseAlert,
                    Width = 48,
                    Height = 48,
                    Foreground = new SolidColorBrush(Colors.Orange),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                content.Children.Add(warningIcon);

                // 判断是否为远程连接
                bool isLocalConnection = string.IsNullOrEmpty(serverAddress) ||
                                         serverAddress.ToLower() == "localhost" ||
                                         serverAddress == "127.0.0.1" ||
                                         serverAddress == "::1";

                // 添加标题和消息
                string titleText, messageText;

                // 根据连接状态设置不同的错误消息
                switch (status)
                {
                    case MySqlConnectionStatus.PortError:
                        titleText = "MySQL端口号错误";
                        
                        if (isLocalConnection)
                        {
                            messageText = $"无法连接到本地MySQL服务器的端口 {port}。\n\n" +
                                          $"您的MySQL服务器似乎运行在默认端口（3306）上，而不是您指定的端口 {port} 上。\n\n" +
                                          $"请修改端口号为3306或取消勾选\"自定义端口号\"选项。";
                        }
                        else
                        {
                            messageText = $"无法连接到MySQL服务器 {serverAddress} 的端口 {port}。\n\n" +
                                          $"请确认服务器上的MySQL实例是否正在使用端口 {port}。\n" +
                                          $"您可能需要：\n" +
                                          $"1. 使用默认端口3306\n" +
                                          $"2. 确认服务器防火墙是否允许该端口连接\n" +
                                          $"3. 联系服务器管理员确认正确的端口号";
                        }
                        break;

                    case MySqlConnectionStatus.NotInstalled:
                        titleText = "未检测到MySQL数据库";
                        messageText = "系统未检测到MySQL数据库服务器。本系统需要MySQL才能正常运行。\n\n请安装MySQL数据库服务器后再使用本系统。";
                        break;

                    case MySqlConnectionStatus.ConnectionError:
                    default:
                        if (!isLocalConnection)
                        {
                            titleText = $"无法连接到MySQL服务器 {serverAddress}";
                            messageText = $"无法连接到指定的MySQL服务器。请确认以下几点：\n\n" +
                                          $"1. 服务器地址 {serverAddress} 是否正确\n" +
                                          $"2. MySQL服务是否在该服务器上运行\n" +
                                          $"3. 服务器防火墙是否允许MySQL端口连接\n" +
                                          $"4. 网络连接是否正常";
                        }
                        else
                        {
                            titleText = "MySQL服务未运行";
                            messageText = "检测到MySQL已安装，但服务未运行或无法访问。\n\n" +
                                          "请确认以下几点：\n" +
                                          "1. MySQL服务是否已启动（可在服务管理器中检测）\n" +
                                          "2. 防火墙是否允许MySQL连接\n" +
                                          "3. MySQL是否配置正确";
                        }
                        break;
                }

                var title = new TextBlock
                {
                    Text = titleText,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16),
                    TextWrapping = TextWrapping.Wrap
                };
                content.Children.Add(title);

                var message = new TextBlock
                {
                    Text = messageText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 16),
                    LineHeight = 24
                };
                content.Children.Add(message);

                // 对于MySQL未安装的情况，添加安装步骤
                if (status == MySqlConnectionStatus.NotInstalled)
                {
                    var stepsTitle = new TextBlock
                    {
                        Text = "安装步骤：",
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    content.Children.Add(stepsTitle);

                    var steps = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 16),
                        LineHeight = 24,
                        Text = "1. 从MySQL官方网站下载安装程序\n" +
                               "2. 运行安装程序，选择「开发者默认设置」\n" +
                               "3. 设置root用户密码（请记住此密码）\n" +
                               "4. 完成安装并确保MySQL服务已启动\n" +
                               "5. 重新启动本应用程序"
                    };
                    content.Children.Add(steps);

                    // 添加下载链接
                    var downloadLink = new TextBlock
                    {
                        Text = "MySQL官方下载地址：",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    content.Children.Add(downloadLink);

                    var hyperlink = new TextBlock
                    {
                        Text = "https://dev.mysql.com/downloads/installer/",
                        Foreground = new SolidColorBrush(Color.FromRgb(124, 77, 255)),
                        TextDecorations = TextDecorations.Underline,
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 0, 0, 24)
                    };
                    hyperlink.MouseDown += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://dev.mysql.com/downloads/installer/",
                        UseShellExecute = true
                    });
                    content.Children.Add(hyperlink);
                }

                // 添加按钮
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new Button
                {
                    Content = "我知道了",
                    Style = TryFindResource("MaterialDesignFlatButton") as Style,
                    Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand
                };
                buttonPanel.Children.Add(okButton);

                content.Children.Add(buttonPanel);

                // 显示对话框
                await MaterialDesignThemes.Wpf.DialogHost.Show(content, "LoginDialogHost");

                // 记录日志
                LogHelper.LogWarning($"用户尝试登录，但MySQL连接状态为: {status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示数据库连接错误提示时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show("连接数据库时发生错误。请检测您的连接设置。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 保留旧方法，但内部调用新方法，以保持兼容性
        private async Task ShowMySqlNotInstalledWarning(string serverAddress = "")
        {
            await ShowDatabaseConnectionError(serverAddress, "3306", MySqlConnectionStatus.NotInstalled);
        }
    }
}