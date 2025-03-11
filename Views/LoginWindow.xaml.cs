using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Data;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

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
        private string _lastDatabaseName = "otherbackup"; // 默认数据库名称
        private string _lastServerAddress = "localhost"; // 默认服务器地址
        private List<string> _requiredTables = new List<string> { "station_info", "train_ride_info" }; // 必要的表

        public LoginWindow()
        {
            InitializeComponent();
            UsernameTextBox.Focus();
            
            // 确保端口号文本框的清空按钮初始状态是禁用的
            MaterialDesignThemes.Wpf.TextFieldAssist.SetHasClearButton(PortTextBox, false);
            
            // 初始化Snackbar消息队列
            LoginSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(3000));
            
            // 初始化主题图标
            UpdateThemeIcon();
            
            // 从配置文件中加载上次使用的数据库名称和服务器地址
            LoadLastDatabaseName();
            LoadLastServerAddress();
            
            // 从配置文件中加载字体大小设置
            LoadFontSizeFromConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保窗口加载后应用字体大小设置
            LoadFontSizeFromConfig();
        }
        
        private void LoadFontSizeFromConfig()
        {
            try
            {
                // 从配置文件中加载字体大小设置
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["FontSize"] != null)
                {
                    if (double.TryParse(config.AppSettings.Settings["FontSize"].Value, out double fontSize))
                    {
                        // 确保字体大小不小于最小可读值
                        if (fontSize < 12)
                        {
                            fontSize = 12;
                        }
                        
                        // 更新滑块值
                        FontSizeSlider.Value = fontSize;
                        FontSizeValueText.Text = $"{fontSize:N0}pt";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载字体大小设置时出错: {ex.Message}");
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
                Console.WriteLine($"应用字体大小时出错: {ex.Message}");
            }
        }
        
        private void SaveFontSizeToConfig(double fontSize)
        {
            try
            {
                // 保存字体大小到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                // 确保字体大小不小于最小可读值
                if (fontSize < 12)
                {
                    fontSize = 12;
                }
                
                if (config.AppSettings.Settings["FontSize"] == null)
                {
                    config.AppSettings.Settings.Add("FontSize", fontSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存字体大小设置时出错: {ex.Message}");
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
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["LastDatabaseName"] != null)
                {
                    string lastDbName = config.AppSettings.Settings["LastDatabaseName"].Value;
                    if (!string.IsNullOrEmpty(lastDbName))
                    {
                        _lastDatabaseName = lastDbName;
                        DatabaseNameTextBox.Text = lastDbName;
                        DatabaseNameComboBox.Text = lastDbName;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载上次数据库名称时出错: {ex.Message}");
            }
        }
        
        private void LoadLastServerAddress()
        {
            try
            {
                // 从配置文件中加载上次使用的服务器地址
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["LastServerAddress"] != null)
                {
                    string lastAddress = config.AppSettings.Settings["LastServerAddress"].Value;
                    if (!string.IsNullOrEmpty(lastAddress))
                    {
                        _lastServerAddress = lastAddress;
                        ServerAddressTextBox.Text = lastAddress;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载上次服务器地址时出错: {ex.Message}");
            }
        }
        
        private void SaveLastServerAddress(string serverAddress)
        {
            try
            {
                // 保存服务器地址到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings["LastServerAddress"] == null)
                {
                    config.AppSettings.Settings.Add("LastServerAddress", serverAddress);
                }
                else
                {
                    config.AppSettings.Settings["LastServerAddress"].Value = serverAddress;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存服务器地址时出错: {ex.Message}");
            }
        }
        
        private void SaveLastDatabaseName(string databaseName)
        {
            try
            {
                // 保存数据库名称到配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config.AppSettings.Settings["LastDatabaseName"] == null)
                {
                    config.AppSettings.Settings.Add("LastDatabaseName", databaseName);
                }
                else
                {
                    config.AppSettings.Settings["LastDatabaseName"].Value = databaseName;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存数据库名称时出错: {ex.Message}");
            }
        }

        private void UpdateThemeIcon()
        {
            // 获取当前主题
            var baseTheme = (BaseTheme)Application.Current.Resources["MaterialDesignTheme.BaseTheme"];
            bool isDarkMode = (baseTheme == BaseTheme.Dark);
            
            // 更新图标
            ThemeIcon.Kind = isDarkMode ? PackIconKind.WeatherSunny : PackIconKind.WeatherNight;
        }
        
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前主题
            var baseTheme = (BaseTheme)Application.Current.Resources["MaterialDesignTheme.BaseTheme"];
            bool isDarkMode = (baseTheme == BaseTheme.Dark);
            
            // 切换主题
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            
            theme.SetBaseTheme(!isDarkMode ? Theme.Dark : Theme.Light);
            paletteHelper.SetTheme(theme);
            
            // 更新资源字典
            Application.Current.Resources["MaterialDesignTheme.BaseTheme"] = !isDarkMode ? BaseTheme.Dark : BaseTheme.Light;
            
            // 更新图标
            UpdateThemeIcon();
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
            
            // 检查大写锁定状态
            CheckCapsLock();
        }
        
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查大写锁定状态
            CheckCapsLock();
        }
        
        private void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            // 检查大写锁定状态
            CheckCapsLock();
        }
        
        private void CheckCapsLock()
        {
            // 检查大写锁定是否开启
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
            // 禁用清空按钮
            MaterialDesignThemes.Wpf.TextFieldAssist.SetHasClearButton(PortTextBox, false);
        }

        private async void ImportSqlButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已填写必要的连接信息
            if (string.IsNullOrEmpty(DatabaseNameComboBox.Text) || 
                string.IsNullOrEmpty(UsernameTextBox.Text) || 
                string.IsNullOrEmpty(PasswordBox.Password))
            {
                ShowError("请先填写数据库名称、用户名和密码");
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQL文件|*.sql|所有文件|*.*",
                Title = "选择SQL文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    ImportSqlButton.IsEnabled = false;
                    LoginButton.IsEnabled = false;
                    CancelButton.IsEnabled = false;

                    // 构建连接字符串
                    string serverAddress = ServerAddressTextBox.Text.Trim();
                    string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text : "3306";
                    string connectionString = $"Server={serverAddress};Port={port};Database={DatabaseNameComboBox.Text};User ID={UsernameTextBox.Text};Password={PasswordBox.Password};CharSet=utf8;Connect Timeout=10;";

                    // 读取SQL文件
                    string sqlScript = await File.ReadAllTextAsync(openFileDialog.FileName, Encoding.UTF8);
                    
                    Exception importException = null;

                    // 执行SQL脚本
                    await Task.Run(() => {
                        try
                        {
                            using (var connection = new MySqlConnection(connectionString))
                            {
                                connection.Open();
                                using (var command = new MySqlCommand(sqlScript, connection))
                                {
                                    command.ExecuteNonQuery();
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
                    
                    // 如果导入过程中发生异常，抛出异常
                    if (importException != null)
                    {
                        throw importException;
                    }

                    LoginSnackbar.MessageQueue?.Enqueue("SQL文件导入成功", null, null, null, false, true, TimeSpan.FromSeconds(3));
                    
                    // 保存数据库名称到历史记录
                    SaveDatabaseNameToHistory(DatabaseNameComboBox.Text);
                }
                catch (MySqlException ex)
                {
                    // 特殊处理MySQL异常
                    ShowError($"导入SQL文件时出错: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
                finally
                {
                    ImportSqlButton.IsEnabled = true;
                    LoginButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccessful = false;
            Close();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 清除错误消息
            ErrorMessageTextBlock.Text = string.Empty;
            
            // 获取输入值
            string serverAddress = ServerAddressTextBox.Text.Trim();
            string databaseName = DatabaseNameComboBox.Text.Trim();
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";
            
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
            if (serverAddress.ToLower() == "localhost" || serverAddress == "127.0.0.1")
            {
                // 统一使用localhost作为本地连接地址
                serverAddress = "localhost";
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
            loadingContent.Children.Add(new TextBlock 
            { 
                Text = "正在连接数据库...", 
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });
            
            // 创建一个计时器，如果连接超过500ms，则显示加载动画
            var loadingTimer = new System.Threading.Timer(async (state) => {
                await Dispatcher.InvokeAsync(() => {
                    MaterialDesignThemes.Wpf.DialogHost.Show(loadingContent, "LoginDialogHost");
                });
            }, null, 500, System.Threading.Timeout.Infinite);
            
            try
            {
                // 先检查服务器是否可达
                if (serverAddress != "localhost" && serverAddress != "127.0.0.1")
                {
                    bool isReachable = await Task.Run(() => {
                        try
                        {
                            using (Ping ping = new Ping())
                            {
                                PingReply reply = ping.Send(serverAddress, 1000);
                                return reply.Status == IPStatus.Success;
                            }
                        }
                        catch
                        {
                            return false;
                        }
                    });
                    
                    if (!isReachable)
                    {
                        throw new Exception($"无法连接到服务器 {serverAddress}，请检查服务器地址是否正确或网络连接是否正常。");
                    }
                }
                
                // 构建连接字符串
                string connectionString = $"Server={serverAddress};Port={port};Database={databaseName};User ID={username};Password={password};CharSet=utf8;Connect Timeout=10;";
                
                // 尝试连接数据库
                bool connected = false;
                bool tablesExist = false;
                Exception connectionException = null;
                
                await Task.Run(() => {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            connected = true;
                            
                            // 检查必要的表是否存在
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
                await Dispatcher.InvokeAsync(() => {
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
                    
                    // 如果必要的表不存在，提示用户导入SQL文件
                    if (!tablesExist)
                    {
                        // 使用MaterialDesign对话框替代MessageBox
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
                            Text = "数据库中缺少必要的表结构（station_info, train_ride_info）。是否现在导入SQL文件？", 
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
                            // 调用导入SQL文件的方法
                            ImportSqlButton_Click(sender, e);
                            return;
                        }
                    }
                    
                    // 登录成功
                    LoginSuccessful = true;
                    
                    // 打开主窗口
                    _mainWindow = new MainWindow(ConnectionString);
                    _mainWindow.Show();
                    
                    // 关闭登录窗口
                    this.Close();
                }
            }
            catch (MySqlException ex)
            {
                // 关闭加载动画
                loadingTimer.Dispose();
                await Dispatcher.InvokeAsync(() => {
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
                await Dispatcher.InvokeAsync(() => {
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
        
        // 检查必要的表是否存在
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
                
                // 检查必要的表是否都存在
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
            
            ErrorMessageTextBlock.Text = userFriendlyMessage;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;

            // 使用MaterialDesign对话框替代MessageBox
            var dialogContent = new StackPanel { Margin = new Thickness(16) };
            
            dialogContent.Children.Add(new TextBlock 
            { 
                Text = "连接错误", 
                FontWeight = FontWeights.Bold, 
                FontSize = (double)Application.Current.Resources["MaterialDesignHeadline6FontSize"],
                Margin = new Thickness(0, 0, 0, 16)
            });
            
            dialogContent.Children.Add(new TextBlock 
            { 
                Text = userFriendlyMessage, 
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });
            
            var okButton = new Button 
            { 
                Content = "确定", 
                Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0),
                Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand
            };
            
            dialogContent.Children.Add(okButton);
            
            MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "LoginDialogHost");
        }

        // 获取用户友好的错误消息
        private string GetUserFriendlyErrorMessage(string originalMessage)
        {
            // 用户名或密码错误
            if (originalMessage.Contains("Access denied for user") && originalMessage.Contains("using password"))
            {
                return "用户名或密码错误，请检查输入是否正确。";
            }
            
            // 认证方法错误
            if (originalMessage.Contains("Authentication to host") && 
                (originalMessage.Contains("using method 'caching_sha2_password'") || 
                 originalMessage.Contains("using method 'sha256_password'")))
            {
                return "认证方式不兼容，请尝试以下解决方案：\n" +
                       "1. 在MySQL中修改用户认证方式为mysql_native_password\n" +
                       "2. 更新MySQL连接器版本\n" +
                       "3. 在MySQL配置文件中添加default_authentication_plugin=mysql_native_password";
            }
            
            // 连接超时
            if (originalMessage.Contains("Connection timeout") || originalMessage.Contains("Reading from the stream has timed out"))
            {
                return "连接服务器超时，请检查网络连接或服务器地址是否正确。";
            }
            
            // 无法连接到服务器
            if (originalMessage.Contains("Unable to connect") || originalMessage.Contains("Could not connect to MySQL server"))
            {
                // 检查是否是本地连接
                string serverAddress = ServerAddressTextBox.Text.Trim();
                if (serverAddress == "localhost" || serverAddress == "127.0.0.1")
                {
                    return "无法连接到本地MySQL服务器，请确认MySQL服务是否已启动。";
                }
                else
                {
                    return "无法连接到MySQL服务器，请检查服务器地址是否正确或服务器是否在线。";
                }
            }
            
            // 端口错误
            if (originalMessage.Contains("No connection could be made because the target machine actively refused it") ||
                originalMessage.Contains("Connection refused"))
            {
                string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";
                return $"连接被拒绝，请检查端口号({port})是否正确或MySQL服务是否已在该端口启动。";
            }
            
            // 数据库不存在
            if (originalMessage.Contains("Unknown database") || originalMessage.Contains("Database") && originalMessage.Contains("doesn't exist"))
            {
                string dbName = DatabaseNameComboBox.Text.Trim();
                return $"数据库'{dbName}'不存在，请检查数据库名称或创建新数据库。";
            }
            
            // 主机名解析错误
            if (originalMessage.Contains("Unknown MySQL server host") || originalMessage.Contains("No such host is known"))
            {
                string serverAddress = ServerAddressTextBox.Text.Trim();
                return $"无法解析服务器地址'{serverAddress}'，请检查拼写是否正确或网络DNS设置。";
            }
            
            // 认证方式错误
            if (originalMessage.Contains("Authentication method") && originalMessage.Contains("not supported"))
            {
                return "不支持的认证方式，可能是MySQL版本过高或客户端驱动过旧，请尝试使用旧版认证方式。";
            }
            
            // 字符集错误
            if (originalMessage.Contains("Unknown character set"))
            {
                return "未知的字符集，请检查数据库配置或连接字符串设置。";
            }
            
            // SSL连接错误
            if (originalMessage.Contains("SSL connection error"))
            {
                return "SSL连接错误，请检查SSL配置或尝试禁用SSL连接。";
            }
            
            // 服务器已关闭连接
            if (originalMessage.Contains("server has gone away") || originalMessage.Contains("Connection closed"))
            {
                return "服务器已关闭连接，可能是网络不稳定或服务器超时设置过短。";
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

                // 检查连接字符串是否已存在
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
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["DatabaseHistory"] != null)
                {
                    string historyString = config.AppSettings.Settings["DatabaseHistory"].Value;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载数据库历史记录时出错: {ex.Message}");
            }
        }
        
        private void SaveDatabaseNameToHistory(string databaseName)
        {
            try
            {
                // 从配置文件中读取历史数据库名称
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                List<string> historyList = new List<string>();
                
                // 读取现有历史记录
                if (config.AppSettings.Settings["DatabaseHistory"] != null)
                {
                    string history = config.AppSettings.Settings["DatabaseHistory"].Value;
                    if (!string.IsNullOrEmpty(history))
                    {
                        historyList = history.Split(',').ToList();
                    }
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
                
                if (config.AppSettings.Settings["DatabaseHistory"] == null)
                {
                    config.AppSettings.Settings.Add("DatabaseHistory", newHistory);
                }
                else
                {
                    config.AppSettings.Settings["DatabaseHistory"].Value = newHistory;
                }
                
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存数据库历史记录时出错: {ex.Message}");
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 密码框获得焦点时检查大写锁定状态
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
            string newDatabaseName = NewDatabaseNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newDatabaseName))
            {
                ShowError("请输入新数据库名称");
                return;
            }
            
            // 禁用按钮，防止重复点击
            CreateDatabaseButton.IsEnabled = false;
            
            try
            {
                // 构建不包含数据库名称的连接字符串
                string serverAddress = ServerAddressTextBox.Text.Trim();
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password;
                string port = CustomPortCheckBox.IsChecked == true ? PortTextBox.Text.Trim() : "3306";
                
                // 验证服务器地址
                if (string.IsNullOrEmpty(serverAddress))
                {
                    ShowError("请输入服务器地址");
                    CreateDatabaseButton.IsEnabled = true;
                    return;
                }
                
                // 验证用户名和密码
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("请输入用户名和密码");
                    CreateDatabaseButton.IsEnabled = true;
                    return;
                }
                
                // 构建连接字符串（不包含数据库名称）
                string connectionString = $"Server={serverAddress};Port={port};User ID={username};Password={password};CharSet=utf8;Connect Timeout=10;";
                
                // 创建数据库
                Exception createException = null;
                
                await Task.Run(() => {
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
                
                // 更新数据库名称下拉框
                DatabaseNameComboBox.Text = newDatabaseName;
                SaveDatabaseNameToHistory(newDatabaseName);
                
                // 显示成功消息
                LoginSnackbar.MessageQueue.Enqueue($"数据库 '{newDatabaseName}' 创建成功");
                
                // 清空新数据库名称文本框
                NewDatabaseNameTextBox.Text = string.Empty;
            }
            catch (MySqlException ex)
            {
                // 特殊处理MySQL异常
                ShowError($"创建数据库失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
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
            // 创建station_info表
            using (MySqlCommand cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS `station_info` (
                    `station_id` INT NOT NULL AUTO_INCREMENT,
                    `station_name` VARCHAR(50) NOT NULL,
                    `station_code` VARCHAR(10) NOT NULL,
                    `city` VARCHAR(50) NOT NULL,
                    `province` VARCHAR(50) NOT NULL,
                    `address` VARCHAR(255),
                    `telephone` VARCHAR(20),
                    `status` TINYINT NOT NULL DEFAULT 1,
                    `create_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `update_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (`station_id`),
                    UNIQUE INDEX `idx_station_code` (`station_code`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection))
            {
                cmd.ExecuteNonQuery();
            }
            
            // 创建train_ride_info表
            using (MySqlCommand cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS `train_ride_info` (
                    `ride_id` INT NOT NULL AUTO_INCREMENT,
                    `train_number` VARCHAR(20) NOT NULL,
                    `departure_station_id` INT NOT NULL,
                    `arrival_station_id` INT NOT NULL,
                    `departure_time` DATETIME NOT NULL,
                    `arrival_time` DATETIME NOT NULL,
                    `duration` INT NOT NULL COMMENT '行程时间（分钟）',
                    `distance` DECIMAL(10,2) NOT NULL COMMENT '行程距离（公里）',
                    `ticket_price` DECIMAL(10,2) NOT NULL,
                    `available_seats` INT NOT NULL,
                    `status` TINYINT NOT NULL DEFAULT 1 COMMENT '1:正常 0:取消',
                    `create_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `update_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (`ride_id`),
                    INDEX `idx_train_number` (`train_number`),
                    INDEX `idx_departure_station` (`departure_station_id`),
                    INDEX `idx_arrival_station` (`arrival_station_id`),
                    INDEX `idx_departure_time` (`departure_time`),
                    CONSTRAINT `fk_departure_station` FOREIGN KEY (`departure_station_id`) REFERENCES `station_info` (`station_id`) ON DELETE RESTRICT ON UPDATE CASCADE,
                    CONSTRAINT `fk_arrival_station` FOREIGN KEY (`arrival_station_id`) REFERENCES `station_info` (`station_id`) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
} 