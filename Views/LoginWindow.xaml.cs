using MaterialDesignThemes.Wpf;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private LoginViewModel ViewModel => (LoginViewModel)DataContext;
        private MainWindow? _mainWindow;
        
        /// <summary>
        /// 登录是否成功
        /// </summary>
        public bool LoginSuccessful => ViewModel?.LoginSuccessful ?? false;
        
        /// <summary>
        /// 设置数据库名称
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        public void SetDatabaseName(string dbName)
        {
            if (ViewModel != null)
            {
                ViewModel.DatabaseName = dbName;
                
                // 更新UI
                if (DatabaseNameComboBox != null)
                {
                    DatabaseNameComboBox.SelectedItem = dbName;
                }
            }
        }

        public LoginWindow()
        {
            try
            {
                InitializeComponent();

                // 设置窗口属性
                this.ResizeMode = ResizeMode.CanMinimize; // 禁用最大化，只允许最小化
                this.SizeToContent = SizeToContent.Height; // 根据内容自动调整高度
                this.MaxHeight = 950; // 设置最大高度

                // 初始化Snackbar消息队列
                LoginSnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(3000));

                // 添加PasswordBox事件处理(因为无法直接绑定Password属性)
                PasswordBox.PasswordChanged += (s, e) => 
                {
                    if (ViewModel != null)
                        ViewModel.Password = PasswordBox.Password;
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginWindow构造函数异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBoxHelper.ShowError($"初始化登录窗口时出错: {ex.Message}","错误");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 首先初始化主题，确保在加载其他内容前应用正确的主题
                InitializeTheme();

                // 设置初始焦点
                UsernameTextBox.Focus();

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
                        Debug.WriteLine($"延迟执行UpdateThemeIcon异常: {ex.Message}");
                        LogHelper.LogSystemError("登录窗口", "UpdateThemeIcon异常", ex);
                        Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                    }
                }), DispatcherPriority.Loaded);

                // 设置ViewModel的相关事件处理
                SetupViewModelEventHandlers();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window_Loaded异常: {ex.Message}");
                LogHelper.LogSystemError("登录窗口", "Window_Loaded异常", ex);
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        private void SetupViewModelEventHandlers()
        {
            // 错误信息处理
            ViewModel.ErrorOccurred += (sender, message) =>
            {
                ShowError(message);
            };
            
            // 登录成功处理
            ViewModel.LoginSuccess += (sender, connString) =>
            {
                HandleLoginSuccess(connString);
            };

            // 消息提示处理 
            ViewModel.ShowMessage += (sender, message) =>
            {
                LoginSnackbar.MessageQueue?.Enqueue(message);
            };
            
            // 数据库创建成功处理
            ViewModel.DatabaseCreated += (sender, dbName) =>
            {
                // 确保下拉框正确显示新创建的数据库名称
                if (!DatabaseNameComboBox.Items.Contains(dbName))
                {
                    DatabaseNameComboBox.Items.Add(dbName);
                }
                DatabaseNameComboBox.SelectedItem = dbName;
                
                // 显示成功消息
                LoginSnackbar.MessageQueue.Enqueue($"数据库 '{dbName}' 创建成功");
            };
        }

        /// <summary>
        /// 根据服务器地址和数据库名称的内容智能设置焦点
        /// </summary>
        private void SetInitialFocus()
        {
            try
            {
                // 检测服务器地址和数据库名称是否有内容
                bool hasServerAddress = !string.IsNullOrWhiteSpace(ViewModel.ServerAddress);
                bool hasDatabaseName = !string.IsNullOrWhiteSpace(ViewModel.DatabaseName);

                // 如果服务器地址和数据库名称都有内容，将焦点设置在用户名上
                if (hasServerAddress && hasDatabaseName)
                {
                    UsernameTextBox.Focus();
                    Debug.WriteLine("焦点设置在用户名上");
                }
                else
                {
                    // 否则，焦点放在服务器地址上
                    ServerAddressTextBox.Focus();
                    Debug.WriteLine("焦点设置在服务器地址上");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetInitialFocus异常: {ex.Message}");
                LogHelper.LogSystemError("登录窗口", $"SetInitialFocus异常: {ex.Message}", ex);
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        private void InitializeTheme()
        {
            try
            {
                // 创建主题服务
                var themeService = ThemeService.Instance;

                // 从配置文件加载主题设置
                bool isDarkMode = themeService.LoadThemeFromConfig();

                // 使用集中的方法应用主题到窗口
                themeService.ApplyThemeToWindow(this, isDarkMode, ThemeIcon, MainCard);

                LogHelper.LogSystem("登录", $"窗口已初始化为{(isDarkMode ? "深色" : "浅色")}主题");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeTheme异常: {ex.Message}");
                LogHelper.LogSystemError("登录", "InitializeTheme异常", ex);
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
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
            // 字体大小更改由ViewModel处理，这里处理UI相关部分
            ApplyFontSizeToWindow(fontSize);
        }
        
        // 处理窗口字体大小应用
        private void ApplyFontSizeToWindow(double fontSize)
        {
            try
            {
                // 更新窗口字体大小
                this.FontSize = fontSize;
                
                // 应用字体大小设置
                var resources = Application.Current.Resources;
                resources["MaterialDesignFontSize"] = fontSize;
                resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"应用字体大小时出错: {ex.Message}");
            }
        }

        private void UpdateThemeIcon()
        {
            try
            {
                // 检测ThemeIcon是否已初始化
                if (ThemeIcon == null)
                {
                    Debug.WriteLine("UpdateThemeIcon: ThemeIcon为空");
                    return;
                }

                // 使用主题服务获取当前主题状态
                var themeService = ThemeService.Instance;
                bool isDarkTheme = themeService.IsDarkThemeActive();

                // 使用集中的方法应用主题到窗口
                themeService.ApplyThemeToWindow(this, isDarkTheme, ThemeIcon, MainCard);

                Debug.WriteLine($"当前主题: {(isDarkTheme ? "深色" : "浅色")}");
            }
            catch (Exception ex)
            {
                // 记录异常但不中断操作
                Debug.WriteLine($"UpdateThemeIcon异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");

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
                    Debug.WriteLine("ThemeToggleButton_Click: ThemeIcon为空");
                    return;
                }

                // 首先调用ViewModel的ToggleTheme方法来切换和保存主题设置
                ViewModel.ToggleTheme();

                // 创建主题服务
                var themeService = ThemeService.Instance;

                // 获取当前主题状态（已经被ViewModel切换过）
                bool isDarkTheme = themeService.IsDarkThemeActive();

                // 使用集中的方法应用主题到窗口UI
                themeService.ApplyThemeToWindow(this, isDarkTheme, ThemeIcon, MainCard);

                Debug.WriteLine($"主题已设置为: {(isDarkTheme ? "深色" : "浅色")}");
                LogHelper.LogSystem("登录", $"窗口主题已设置为: {(isDarkTheme ? "深色" : "浅色")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ThemeToggleButton_Click异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        // 处理窗口的键盘事件
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 如果按下回车键，触发登录命令
                if (ViewModel.LoginCommand.CanExecute(null))
                    ViewModel.LoginCommand.Execute(null);
                e.Handled = true;
            }

            // 检测大写锁定状态
            ViewModel.CheckCapsLockCommand.Execute(null);
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检测大写锁定状态
            ViewModel.CheckCapsLockCommand.Execute(null);
        }

        private void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            // 检测大写锁定状态
            ViewModel.CheckCapsLockCommand.Execute(null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭窗口
            Close();
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 密码框获得焦点时检测大写锁定状态
            ViewModel.CheckCapsLockCommand.Execute(null);
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 密码框失去焦点时隐藏大写锁定图标
            ViewModel.IsCapsLockOn = false;
        }

        private void ShowError(string message)
        {
            // 检查对话框是否已经打开
            bool isDialogOpen = DialogHost.IsDialogOpen("LoginDialogHost");
            if (isDialogOpen)
            {
                Debug.WriteLine("对话框已经打开，无法显示错误对话框");
                // 改用Snackbar显示消息
                LoginSnackbar.MessageQueue?.Enqueue(message);
                return;
            }

            // 使用MaterialDesign对话框替代MessageBox
            var dialogContent = new StackPanel { Margin = new Thickness(16) };

            // 根据错误类型设置标题
            string title = "连接错误";
            if (message.Contains("未检测到MySQL数据库服务器"))
            {
                title = "数据库服务器未安装";
            }
            else if (message.Contains("无法连接到MySQL数据库的指定端口"))
            {
                title = "端口连接错误";
            }
            else if (message.Contains("数据库") && message.Contains("不存在"))
            {
                title = "数据库不存在";
            }

            dialogContent.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = (double)Application.Current.Resources["MaterialDesignHeadline6FontSize"],
                Margin = new Thickness(0, 0, 0, 16)
            });

            // 创建一个支持多行显示的TextBlock
            var messageTextBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };

            // 如果消息包含换行符，设置行高以改善可读性
            if (message.Contains("\n"))
            {
                messageTextBlock.LineHeight = 24;
            }

            dialogContent.Children.Add(messageTextBlock);

            // 如果是认证方式不兼容错误，添加复制SQL命令的按钮
            if (message.Contains("认证方式不兼容"))
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
                        string username = ViewModel.Username;
                        string password = ViewModel.Password;
                        string sqlCommand = $"ALTER USER '{username}'@'localhost' IDENTIFIED WITH mysql_native_password BY '{password}';";
                        Clipboard.SetText(sqlCommand);

                        // 显示复制成功提示
                        LoginSnackbar.MessageQueue?.Enqueue("localhost SQL命令已复制到剪贴板", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"复制SQL命令时出错: {ex.Message}");
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
                        string username = ViewModel.Username;
                        string password = ViewModel.Password;
                        string sqlCommand = $"ALTER USER '{username}'@'%' IDENTIFIED WITH mysql_native_password BY '{password}';";
                        Clipboard.SetText(sqlCommand);

                        // 显示复制成功提示
                        LoginSnackbar.MessageQueue?.Enqueue("通配符SQL命令已复制到剪贴板", null, null, null, false, true, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"复制SQL命令时出错: {ex.Message}");
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
            // 如果是MySQL未安装错误，添加下载链接
            else if (message.Contains("未检测到MySQL数据库服务器"))
            {
                var infoPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
                
                infoPanel.Children.Add(new TextBlock
                {
                    Text = "您可以从以下链接下载并安装MySQL：",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                var hyperlinkButton = new Button
                {
                    Content = "访问MySQL官方下载页面",
                    Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"))
                };

                hyperlinkButton.Click += (s, e) =>
                {
                    try
                    {
                        // 打开MySQL下载页面
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://dev.mysql.com/downloads/mysql/",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"打开MySQL下载页面时出错: {ex.Message}");
                    }
                };

                infoPanel.Children.Add(hyperlinkButton);
                dialogContent.Children.Add(infoPanel);
            }
            // 如果是数据库不存在错误，添加创建数据库的提示
            else if (message.Contains("数据库") && message.Contains("不存在"))
            {
                dialogContent.Children.Add(new TextBlock
                {
                    Text = "您可以在登录界面展开\"创建新数据库\"部分来创建一个新的数据库。",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 16),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"))
                });
            }

            var okButton = new Button
            {
                Content = "确定",
                Style = (Style)Application.Current.Resources["MaterialDesignFlatButton"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0),
                Command = DialogHost.CloseDialogCommand
            };

            dialogContent.Children.Add(okButton);

            // 显示对话框
            DialogHost.Show(dialogContent, "LoginDialogHost");
        }

        // 处理登录成功后的操作
        private void HandleLoginSuccess(string connectionString)
        {
            try
            {
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
                        Debug.WriteLine($"关闭之前的主窗口时出错: {ex.Message}");
                        LogHelper.LogError($"关闭之前的主窗口时出错: {ex.Message}");
                    }
                }

                // 获取当前主题状态
                var themeService = TA_WPF.Services.ThemeService.Instance;
                bool isDarkMode = themeService.IsDarkThemeActive();

                // 确保主题设置已保存到配置文件
                themeService.ApplyTheme(isDarkMode);

                // 显式应用字体大小设置，确保在连接数据库切换后，所有UI元素都能正确应用设置
                try
                {
                    // 从配置文件中获取当前字体大小
                    double fontSize = ConfigUtils.GetDoubleValue("FontSize", 13);

                    // 创建UIService实例并应用字体大小
                    var uiService = new UIService();
                    uiService.ApplyFontSize(fontSize);

                    // 在应用级别同步字体大小设置
                    if (Application.Current is App app)
                    {
                        app.SyncFontSizeSettings(fontSize);
                    }

                    LogHelper.LogInfo($"重新登录后应用字体大小设置: {fontSize}pt");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"重新登录后应用字体大小设置时出错: {ex.Message}", ex);
                }

                // 创建新的主窗口和MainViewModel
                _mainWindow = new MainWindow(connectionString);

                // 确保主题设置同步到主窗口
                if (_mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    // 显式设置主题模式
                    mainViewModel.IsDarkMode = isDarkMode;
                    Debug.WriteLine($"已设置MainViewModel.IsDarkMode = {isDarkMode}");
                    LogHelper.LogInfo($"已设置MainViewModel.IsDarkMode = {isDarkMode}");
                }

                // 显式应用主题到主窗口
                ThemeAssist.SetTheme(_mainWindow,
                    isDarkMode ? BaseTheme.Dark : BaseTheme.Light);

                // 强制应用主题
                themeService.ApplyTheme(isDarkMode);

                LogHelper.LogInfo("已登录成功");

                // 强制刷新主窗口
                _mainWindow.UpdateLayout();

                // 将主窗口设置为应用程序主窗口
                Application.Current.MainWindow = _mainWindow;

                // 显示主窗口
                _mainWindow.Show();

                // 显示登录成功提示
                // 使用Dispatcher.InvokeAsync以便可以await，确保异步执行完成
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        _mainWindow.ShowLoginSuccessNotification();
                    }
                    catch (Exception notifyEx)
                    {
                        Debug.WriteLine($"显示登录成功提示时出错: {notifyEx.Message}");
                        LogHelper.LogError($"显示登录成功提示时出错: {notifyEx.Message}", notifyEx);
                    }
                }, DispatcherPriority.Background);

                // 记录日志
                LogHelper.LogSystem("登录", $"用户登录成功，已创建新的MainViewModel，主题模式：{(isDarkMode ? "深色" : "浅色")}");

                // 关闭登录窗口
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理登录成功后操作时出错: {ex.Message}");
                LogHelper.LogError($"处理登录成功后操作时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"登录后初始化主窗口时出错: {ex.Message}", "错误");
            }
        }
    }
}