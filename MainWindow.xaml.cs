using MaterialDesignThemes.Wpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 菜单按钮引用
        private ToggleButton _menuToggleButton;
        // 设置按钮引用
        private Button _settingsButton;
        // 防抖计时器
        private DispatcherTimer _resizeTimer;

        // 登录信息服务
        private readonly LoginInfoService _loginInfoService;
        // 连接字符串
        private readonly string _connectionString;

        public MainWindow(string connectionString)
        {
            try
            {
                InitializeComponent();

                // 保存连接字符串
                _connectionString = connectionString;

                // 初始化登录信息服务
                _loginInfoService = new LoginInfoService();

                // 设置DataContext
                DataContext = new MainViewModel(connectionString);

                // 初始化防抖计时器
                _resizeTimer = new DispatcherTimer();
                _resizeTimer.Interval = TimeSpan.FromMilliseconds(300); // 300毫秒的防抖延迟
                _resizeTimer.Tick += (s, e) =>
                {
                    _resizeTimer.Stop();
                };

                // 注册窗口大小变化事件
                this.SizeChanged += Window_SizeChanged;

                // 注册窗口状态变化事件
                this.StateChanged += Window_StateChanged;

                // 注册窗口关闭事件
                this.Closing += MainWindow_Closing;

                // 等待UI完全加载后再获取控件引用
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow构造函数异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"初始化窗口时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置窗口图标
                try
                {
                    Uri iconUri = new Uri("pack://application:,,,/Assets/Icons/app_icon.ico", UriKind.Absolute);
                    this.Icon = new BitmapImage(iconUri);

                    // 确保任务栏图标也被设置
                    WindowInteropHelper helper = new WindowInteropHelper(this);
                    HwndSource source = HwndSource.FromHwnd(helper.Handle);
                    source.AddHook(new HwndSourceHook(WndProc));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"设置窗口图标时出错: {ex.Message}");
                }

                // 获取控件引用
                _menuToggleButton = this.FindName("MenuToggleButton") as ToggleButton;
                _settingsButton = this.FindName("SettingsButton") as Button;

                // 检测控件引用是否有效
                if (_menuToggleButton == null)
                {
                    Debug.WriteLine("警告: MenuToggleButton引用为空");
                }
                else
                {
                    // 注册侧边栏状态变化事件
                    _menuToggleButton.Checked += MenuToggleButton_CheckedChanged;
                    _menuToggleButton.Unchecked += MenuToggleButton_CheckedChanged;
                }

                if (_settingsButton == null)
                {
                    Debug.WriteLine("警告: SettingsButton引用为空");
                }

                // 确保窗口加载时应用正确的主题
                if (DataContext is MainViewModel viewModel)
                {
                    // 获取主题服务实例
                    var themeService = ThemeService.Instance;

                    // 获取当前主题状态
                    bool isDarkMode = themeService.IsDarkThemeActive();

                    Debug.WriteLine($"MainWindow_Loaded: 当前主题状态 isDarkMode = {isDarkMode}");
                    Debug.WriteLine($"MainWindow_Loaded: ViewModel.IsDarkMode = {viewModel.IsDarkMode}");

                    // 确保视图模型的IsDarkMode属性与当前主题同步
                    if (viewModel.IsDarkMode != isDarkMode)
                    {
                        Debug.WriteLine($"MainWindow_Loaded: 主题状态不一致，正在同步...");
                        viewModel.IsDarkMode = isDarkMode;
                    }

                    // 显式设置窗口的ThemeAssist.Theme属性
                    ThemeAssist.SetTheme(this,
                    isDarkMode ? BaseTheme.Dark : BaseTheme.Light);

                    // 强制应用主题
                    themeService.ApplyTheme(isDarkMode);

                    // 强制刷新窗口
                    this.UpdateLayout();

                    Debug.WriteLine($"窗口加载时应用了{(isDarkMode ? "深色" : "浅色")}主题");
                }

                // 添加键盘事件处理
                this.KeyDown += MainWindow_KeyDown;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow_Loaded异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理键盘按键事件
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // 获取视图模型
                if (DataContext is MainViewModel mainViewModel &&
                    mainViewModel.DashboardViewModel != null)
                {
                    // 如果按下ESC键且当前处于全屏模式，则退出全屏模式
                    if (e.Key == Key.Escape && mainViewModel.DashboardViewModel.IsFullScreen)
                    {
                        Debug.WriteLine("检测到ESC键，退出全屏模式");

                        // 使用命令切换全屏模式
                        mainViewModel.DashboardViewModel.ToggleFullScreenCommand.Execute(null);

                        // 标记事件已处理
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow_KeyDown异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理设置按钮点击事件
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowSettings = true;
                    viewModel.ShowWelcome = false;
                    viewModel.ShowQueryAllTickets = false;

                    // 关闭侧边栏
                    if (_menuToggleButton != null)
                    {
                        _menuToggleButton.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsButton_Click异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口大小变化时调整表格列宽
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                // 使用防抖计时器延迟调整列宽
                if (_resizeTimer != null)
                {
                    _resizeTimer.Stop();
                    _resizeTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window_SizeChanged异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口状态变化时调整表格列宽的
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            try
            {
                // 使用防抖计时器延迟调整列宽
                if (_resizeTimer != null)
                {
                    _resizeTimer.Stop();
                    _resizeTimer.Start();
                }

                // 获取视图模型
                if (DataContext is MainViewModel mainViewModel &&
                    mainViewModel.DashboardViewModel != null)
                {
                    // 记录窗口状态变化
                    Debug.WriteLine($"窗口状态变化: {this.WindowState}, 窗口样式: {this.WindowStyle}, 全屏模式: {mainViewModel.DashboardViewModel.IsFullScreen}");

                    // 如果不是全屏模式，保存窗口状态
                    if (!mainViewModel.DashboardViewModel.IsFullScreen)
                    {
                        // 只有在窗口不是最小化的情况下才保存状态
                        if (this.WindowState != WindowState.Minimized)
                        {
                            mainViewModel.DashboardViewModel.PreviousWindowState = this.WindowState;
                            Debug.WriteLine($"保存窗口状态: {this.WindowState}");
                        }
                    }
                    // 不要在这里处理全屏模式的情况，让 ToggleFullScreen 方法完全控制全屏模式
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window_StateChanged异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 侧边栏状态变化事件处理
        /// </summary>
        private void MenuToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                // 侧边栏状态变化时，可以添加一些动画效果或其他处理
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuToggleButton_CheckedChanged异常: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 检测是否是用户手动关闭窗口
                if (Application.Current.MainWindow == this && Owner == null)
                {
                    // 取消当前关闭操作
                    e.Cancel = true;

                    // 创建对话框内容
                    var dialogContent = new StackPanel
                    {
                        Margin = new Thickness(16)
                    };

                    // 添加标题和消息
                    dialogContent.Children.Add(new TextBlock
                    {
                        Text = "确认退出",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 8)
                    });

                    dialogContent.Children.Add(new TextBlock
                    {
                        Text = "确定要退出应用程序吗？",
                        Margin = new Thickness(0, 0, 0, 16)
                    });

                    // 创建按钮面板
                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    // 创建对话框按钮
                    var noButton = new Button
                    {
                        Content = "否",
                        Command = DialogHost.CloseDialogCommand,
                        CommandParameter = false,
                        Style = TryFindResource("MaterialDesignFlatButton") as Style,
                        Margin = new Thickness(8, 0, 0, 0)
                    };

                    var yesButton = new Button
                    {
                        Content = "是",
                        Command = DialogHost.CloseDialogCommand,
                        CommandParameter = true,
                        Style = TryFindResource("MaterialDesignFlatButton") as Style,
                        Margin = new Thickness(8, 0, 0, 0)
                    };

                    // 添加按钮到按钮面板
                    buttonPanel.Children.Add(noButton);
                    buttonPanel.Children.Add(yesButton);

                    // 添加按钮面板到对话框内容
                    dialogContent.Children.Add(buttonPanel);

                    // 显示对话框并等待结果
                    DialogHost.Show(dialogContent, "RootDialog", (sender, args) =>
                    {
                        // 检测用户选择
                        if (args.Parameter is bool result && result)
                        {
                            // 用户确认退出，清理资源
                            if (DataContext is MainViewModel viewModel)
                            {
                                // 记录日志
                                LogHelper.LogInfo("用户手动关闭了主窗口，应用程序将退出");
                            }

                            // 关闭应用程序
                            Application.Current.Shutdown();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理窗口关闭事件时出错: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");

                // 发生异常时，允许应用程序关闭
                e.Cancel = false;
            }
        }

        /// <summary>
        /// 窗口消息处理
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 处理窗口消息，确保任务栏图标正确显示
            return IntPtr.Zero;
        }

        /// <summary>
        /// 显示登录成功提示
        /// </summary>
        public void ShowLoginSuccessNotification()
        {
            try
            {
                Debug.WriteLine("MainWindow.ShowLoginSuccessNotification 被调用");

                // 使用Dispatcher.BeginInvoke确保在主窗口完全加载后显示提示框
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 获取登录信息
                        string loginIP = NetworkHelper.GetLocalIPAddress();
                        string databaseIP = NetworkHelper.GetDatabaseServerIP(_connectionString);
                        string lastLoginTime = _loginInfoService.GetLastLoginTime();
                        string databaseName = _loginInfoService.GetDatabaseName(_connectionString);

                        Debug.WriteLine($"登录信息: IP={loginIP}, 数据库={databaseName}, 上次登录={lastLoginTime}");

                        // 构建提示内容
                        string message = $"登录IP：{loginIP}\n上次登录时间：{lastLoginTime}\n登录数据库：{databaseName}";

                        // 获取当前主题
                        bool isDarkMode = false;
                        if (DataContext is ViewModels.MainViewModel viewModel)
                        {
                            isDarkMode = Application.Current.Resources["Theme.Dark"] as bool? == true;
                        }

                        // 创建通知卡片 - 减少对不支持操作的依赖
                        var card = new Card
                        {
                            Padding = new Thickness(16),
                            Margin = new Thickness(8),
                            UniformCornerRadius = 8,
                            Width = 380, // 增加宽度
                            Background = isDarkMode ?
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D")) :
                                Brushes.White
                        };

                        // 设置阴影深度
                        ShadowAssist.SetShadowDepth(card, ShadowDepth.Depth3);

                        // 创建一个简单的变换而不是使用ScaleTransform
                        card.RenderTransformOrigin = new Point(0.5, 0.5);
                        card.RenderTransform = new ScaleTransform(1, 1);
                        card.Opacity = 0; // 初始透明度为0

                        // 创建内容面板
                        var panel = new StackPanel
                        {
                            Margin = new Thickness(8)
                        };

                        // 添加标题
                        panel.Children.Add(new Grid
                        {
                            Margin = new Thickness(0, 0, 0, 12) // 增加下边距
                        });

                        // 获取标题面板
                        var titleGrid = panel.Children[0] as Grid;

                        // 添加列定义
                        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        // 添加成功图标
                        var successIcon = new PackIcon
                        {
                            Kind = PackIconKind.CheckCircle,
                            Width = 24,
                            Height = 24,
                            Foreground = isDarkMode ?
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) :
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 8, 0)
                        };
                        Grid.SetColumn(successIcon, 0);
                        titleGrid.Children.Add(successIcon);

                        // 添加标题文本
                        var titleText = new TextBlock
                        {
                            Text = "登录成功",
                            FontWeight = FontWeights.Bold,
                            FontSize = 16,
                            Foreground = isDarkMode ? Brushes.White : Brushes.Black,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(titleText, 1);
                        titleGrid.Children.Add(titleText);

                        // 添加关闭按钮
                        var closeButton = new Button
                        {
                            Style = (Style)FindResource("MaterialDesignIconButton"),
                            Content = new PackIcon { Kind = PackIconKind.Close, Width = 16, Height = 16 },
                            Padding = new Thickness(4),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Foreground = isDarkMode ? Brushes.White : Brushes.Black
                        };
                        Grid.SetColumn(closeButton, 2);
                        titleGrid.Children.Add(closeButton);

                        // 添加分隔线
                        panel.Children.Add(new Separator
                        {
                            Margin = new Thickness(0, 0, 0, 12), // 增加下边距
                            Background = isDarkMode ?
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) :
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))
                        });

                        // 添加消息内容
                        var messageLines = message.Split('\n');
                        foreach (var line in messageLines)
                        {
                            var parts = line.Split('：');
                            if (parts.Length == 2)
                            {
                                var contentGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                                // 添加标签
                                var label = new TextBlock
                                {
                                    Text = parts[0] + "：",
                                    FontWeight = FontWeights.SemiBold,
                                    Foreground = isDarkMode ?
                                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB")) :
                                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                                    FontSize = 13,
                                    Margin = new Thickness(0, 0, 8, 0),
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                Grid.SetColumn(label, 0);
                                contentGrid.Children.Add(label);

                                // 添加值
                                var value = new TextBlock
                                {
                                    Text = parts[1],
                                    TextWrapping = TextWrapping.Wrap,
                                    Foreground = isDarkMode ? Brushes.White : Brushes.Black,
                                    FontSize = 13,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                Grid.SetColumn(value, 1);
                                contentGrid.Children.Add(value);

                                panel.Children.Add(contentGrid);
                            }
                            else
                            {
                                panel.Children.Add(new TextBlock
                                {
                                    Text = line,
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(0, 4, 0, 4),
                                    FontSize = 13,
                                    Foreground = isDarkMode ? Brushes.White : Brushes.Black
                                });
                            }
                        }

                        // 将面板添加到卡片
                        card.Content = panel;

                        // 创建弹出窗口前确保它是在UI线程上并且所有对象都已被冻结或安全
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 创建弹出窗口
                            var popup = new Popup
                            {
                                Child = card,
                                Placement = PlacementMode.Center,
                                HorizontalOffset = 0,
                                VerticalOffset = 0,
                                AllowsTransparency = true,
                                PopupAnimation = PopupAnimation.None, // 禁用默认动画，使用自定义动画
                                IsOpen = true,
                                StaysOpen = true,
                                PlacementTarget = this
                            };

                            Debug.WriteLine("Popup已创建并设置为打开状态");

                            // 设置关闭按钮的点击事件
                            closeButton.Click += (s, e) =>
                            {
                                // 创建关闭动画
                                var fadeOutAnimation = new DoubleAnimation
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = TimeSpan.FromMilliseconds(300),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };

                                fadeOutAnimation.Completed += (sender, args) =>
                                {
                                    popup.IsOpen = false;
                                };

                                // 应用动画 - 只使用不会引发异常的动画
                                card.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
                            };

                            // 创建进入动画 - 简化动画以避免异常
                            var fadeInAnimation = new DoubleAnimation
                            {
                                From = 0,
                                To = 1,
                                Duration = TimeSpan.FromMilliseconds(400),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };

                            // 应用进入动画
                            card.BeginAnimation(OpacityProperty, fadeInAnimation);

                            // 2秒后自动关闭
                            var timer = new DispatcherTimer
                            {
                                Interval = TimeSpan.FromSeconds(2)
                            };

                            timer.Tick += (s, e) =>
                            {
                                timer.Stop();

                                // 创建关闭动画
                                var fadeOutAnimation = new DoubleAnimation
                                {
                                    From = 1,
                                    To = 0,
                                    Duration = TimeSpan.FromMilliseconds(300),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };

                                fadeOutAnimation.Completed += (sender, args) =>
                                {
                                    popup.IsOpen = false;
                                };

                                // 应用动画 - 只使用不会引发异常的动画
                                card.BeginAnimation(OpacityProperty, fadeOutAnimation);
                            };

                            timer.Start();
                        });

                        Debug.WriteLine("登录成功提示显示完成");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"显示登录成功提示时出错: {ex.Message}");
                        Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                    }
                }), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"准备显示登录成功提示时出错: {ex.Message}");
                Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
    }
}