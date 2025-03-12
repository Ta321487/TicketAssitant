using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using TA_WPF.ViewModels;
using MaterialDesignThemes.Wpf;

namespace TA_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 数据表格引用
        private DataGrid _mainDataGrid;
        // 菜单按钮引用
        private ToggleButton _menuToggleButton;
        // 设置按钮引用
        private Button _settingsButton;
        // 防抖计时器
        private DispatcherTimer _resizeTimer;
        // 是否正在调整列宽
        private bool _isAdjustingColumns = false;
        
        // 页码输入相关
        private StackPanel _pageInfoPanel;
        private TextBox _pageNumberInput;
        
        public MainWindow(string connectionString)
        {
            try
            {
                InitializeComponent();
                
                // 设置DataContext
                DataContext = new MainViewModel(connectionString);
                
                // 初始化防抖计时器
                _resizeTimer = new DispatcherTimer();
                _resizeTimer.Interval = TimeSpan.FromMilliseconds(300); // 300毫秒的防抖延迟
                _resizeTimer.Tick += (s, e) => 
                {
                    _resizeTimer.Stop();
                    try
                    {
                        AdjustDataGridColumns();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"调整列宽时出错: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                    }
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
                Console.WriteLine($"MainWindow构造函数异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
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
                    // 尝试多种方式加载图标
                    try
                    {
                        // 方式1：从资源加载
                        Uri iconUri = new Uri("pack://application:,,,/Assets/Icons/app_icon.ico", UriKind.Absolute);
                        this.Icon = BitmapFrame.Create(iconUri);
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine($"方式1加载图标失败: {ex1.Message}");
                        try
                        {
                            // 方式2：从应用程序目录加载
                            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "app_icon.ico");
                            if (System.IO.File.Exists(iconPath))
                            {
                                this.Icon = BitmapFrame.Create(new Uri(iconPath));
                            }
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"方式2加载图标失败: {ex2.Message}");
                            try
                            {
                                // 方式3：使用相对路径
                                Uri iconUri = new Uri("/Assets/Icons/app_icon.ico", UriKind.Relative);
                                this.Icon = new BitmapImage(iconUri);
                            }
                            catch (Exception ex3)
                            {
                                Console.WriteLine($"方式3加载图标失败: {ex3.Message}");
                            }
                        }
                    }
                    
                    // 确保任务栏图标也被设置
                    System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(this);
                    System.Windows.Interop.HwndSource source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                    source.AddHook(new System.Windows.Interop.HwndSourceHook(WndProc));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"设置窗口图标时出错: {ex.Message}");
                    Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                }
                
                // 获取控件引用
                _mainDataGrid = this.FindName("MainDataGrid") as DataGrid;
                _menuToggleButton = this.FindName("MenuToggleButton") as ToggleButton;
                _settingsButton = this.FindName("SettingsButton") as Button;
                
                // 检查控件引用是否有效
                if (_mainDataGrid == null)
                {
                    Console.WriteLine("警告: MainDataGrid引用为空");
                }
                
                if (_menuToggleButton == null)
                {
                    Console.WriteLine("警告: MenuToggleButton引用为空");
                }
                else
                {
                    // 注册侧边栏状态变化事件
                    _menuToggleButton.Checked += MenuToggleButton_CheckedChanged;
                    _menuToggleButton.Unchecked += MenuToggleButton_CheckedChanged;
                }
                
                if (_settingsButton == null)
                {
                    Console.WriteLine("警告: SettingsButton引用为空");
                }
                
                // 延迟执行，确保UI已完全加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_mainDataGrid != null)
                        {
                            // 移除并重新添加SizeChanged事件处理程序
                            _mainDataGrid.SizeChanged -= DataGrid_SizeChanged;
                            _mainDataGrid.SizeChanged += DataGrid_SizeChanged;
                            
                            // 只有在DataGrid可见且有列时才调整列宽
                            if (_mainDataGrid.Visibility == Visibility.Visible && 
                                _mainDataGrid.Columns != null && 
                                _mainDataGrid.Columns.Count > 0)
                            {
                                AdjustDataGridColumns();
                            }
                            
                            Console.WriteLine("MainWindow加载完成");
                            Console.WriteLine($"MainDataGrid可见性: {_mainDataGrid.Visibility}");
                            Console.WriteLine($"MainDataGrid项目数: {_mainDataGrid.Items.Count}");
                        }
                        else
                        {
                            Console.WriteLine("MainDataGrid引用为空");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"MainWindow.Loaded延迟执行异常: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                    }
                }), DispatcherPriority.Loaded);

                // 确保窗口加载时应用正确的主题
                if (DataContext is ViewModels.MainViewModel viewModel)
                {
                    // 获取当前主题设置并应用
                    bool isDarkMode = Application.Current.Resources["Theme.Dark"] as bool? == true;
                    viewModel.ApplyTheme(isDarkMode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainWindow.Loaded异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
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
                    viewModel.ShowDataGrid = false;
                    
                    // 关闭侧边栏
                    if (_menuToggleButton != null)
                    {
                        _menuToggleButton.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SettingsButton_Click异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
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
                Console.WriteLine($"Window_SizeChanged异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 窗口状态变化时调整表格列宽
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Window_StateChanged异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 侧边栏状态变化时调整表格列宽
        /// </summary>
        private void MenuToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
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
                Console.WriteLine($"MenuToggleButton_CheckedChanged异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 数据表格大小变化时调整列宽
        /// </summary>
        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
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
                Console.WriteLine($"DataGrid_SizeChanged异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 调整数据表格列宽
        /// </summary>
        private void AdjustDataGridColumns()
        {
            // 防止重入
            if (_isAdjustingColumns)
                return;
                
            // 检查DataGrid是否可用
            if (_mainDataGrid == null)
            {
                Console.WriteLine("无法调整列宽：DataGrid引用为空");
                return;
            }
            
            // 检查DataGrid是否已加载
            if (!_mainDataGrid.IsLoaded)
            {
                Console.WriteLine("无法调整列宽：DataGrid尚未加载");
                return;
            }
            
            // 检查DataGrid是否可见
            if (_mainDataGrid.Visibility != Visibility.Visible)
            {
                Console.WriteLine("无法调整列宽：DataGrid不可见");
                return;
            }
            
            // 检查DataGrid是否有列
            if (_mainDataGrid.Columns == null || _mainDataGrid.Columns.Count == 0)
            {
                Console.WriteLine("无法调整列宽：DataGrid没有列");
                return;
            }
                
            try
            {
                _isAdjustingColumns = true;
                
                // 计算可用宽度
                double totalAvailableWidth = _mainDataGrid.ActualWidth - 30; // 减去滚动条和边距
                
                // 如果可用宽度太小，使用自动宽度
                if (totalAvailableWidth < 800)
                {
                    foreach (var column in _mainDataGrid.Columns)
                    {
                        if (column != null && column is System.Windows.Controls.DataGridTextColumn textColumn)
                        {
                            textColumn.Width = DataGridLength.Auto;
                        }
                    }
                    return;
                }
                
                // 列数较多时，使用固定宽度策略
                if (_mainDataGrid.Columns.Count > 10)
                {
                    // 为每列分配固定宽度，但确保最小宽度
                    foreach (var column in _mainDataGrid.Columns)
                    {
                        if (column != null && column is System.Windows.Controls.DataGridTextColumn textColumn)
                        {
                            double minWidth = textColumn.MinWidth;
                            // 使用较小的固定宽度
                            double fixedWidth = Math.Max(minWidth, 100);
                            textColumn.Width = new DataGridLength(fixedWidth);
                        }
                    }
                    return;
                }
                
                // 列数较少时，使用平均宽度策略
                double avgColumnWidth = totalAvailableWidth / _mainDataGrid.Columns.Count;
                
                // 为每列分配平均宽度，但考虑最小宽度
                foreach (var column in _mainDataGrid.Columns)
                {
                    if (column != null && column is System.Windows.Controls.DataGridTextColumn textColumn)
                    {
                        double minWidth = textColumn.MinWidth;
                        double finalWidth = Math.Max(minWidth, avgColumnWidth);
                        textColumn.Width = new DataGridLength(finalWidth);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调整列宽时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
            finally
            {
                _isAdjustingColumns = false;
            }
        }

        /// <summary>
        /// 处理页码信息面板的点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_pageInfoPanel == null || _pageNumberInput == null)
            {
                _pageInfoPanel = FindName("PageInfoPanel") as StackPanel;
                _pageNumberInput = FindName("PageNumberInput") as TextBox;
            }

            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                _pageInfoPanel.Visibility = Visibility.Collapsed;
                _pageNumberInput.Visibility = Visibility.Visible;
                
                // 设置当前页码为默认值
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    _pageNumberInput.Text = viewModel.CurrentPage.ToString();
                }
                
                // 聚焦并全选
                _pageNumberInput.Focus();
                _pageNumberInput.SelectAll();
            }
        }

        /// <summary>
        /// 处理页码输入框的按键事件
        /// </summary>
        private void PageNumberInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                TryNavigateToPage();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                // 取消输入，恢复显示
                RestorePageInfoDisplay();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理页码输入框的文本输入事件，只允许输入数字
        /// </summary>
        private void PageNumberInput_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // 只允许输入数字
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }
            
            // 检查输入后的值是否超过总页数
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 获取输入后的完整文本
                    string newText = textBox.Text.Substring(0, textBox.SelectionStart) + e.Text + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);
                    
                    // 尝试解析为数字
                    if (int.TryParse(newText, out int pageNumber))
                    {
                        // 如果输入的数字大于总页数，则不允许输入
                        if (pageNumber > viewModel.TotalPages)
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 阻止粘贴非数字内容
        /// </summary>
        private void PageNumberInput_PreviewExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Command == System.Windows.Input.ApplicationCommands.Paste)
            {
                // 获取剪贴板内容
                string clipboardText = System.Windows.Clipboard.GetText();
                
                // 检查是否为纯数字
                if (!clipboardText.All(char.IsDigit))
                {
                    e.Handled = true;
                }
                else
                {
                    // 检查粘贴后的值是否超过总页数
                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null)
                    {
                        var textBox = sender as TextBox;
                        if (textBox != null)
                        {
                            // 获取粘贴后的完整文本
                            string newText = textBox.Text.Substring(0, textBox.SelectionStart) + clipboardText + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);
                            
                            // 尝试解析为数字
                            if (int.TryParse(newText, out int pageNumber))
                            {
                                // 如果粘贴后的数字大于总页数，则不允许粘贴
                                if (pageNumber > viewModel.TotalPages)
                                {
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理页码输入框失去焦点事件
        /// </summary>
        private void PageNumberInput_LostFocus(object sender, RoutedEventArgs e)
        {
            RestorePageInfoDisplay();
        }

        /// <summary>
        /// 尝试导航到输入的页码
        /// </summary>
        private void TryNavigateToPage()
        {
            if (_pageNumberInput == null || _pageInfoPanel == null)
                return;

            var viewModel = DataContext as MainViewModel;
            if (viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(_pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber >= 1 && pageNumber <= viewModel.TotalPages)
                {
                    // 导航到指定页码
                    viewModel.CurrentPage = pageNumber;
                }
                else if (pageNumber < 1)
                {
                    // 如果页码小于1，则导航到第一页
                    viewModel.CurrentPage = 1;
                    ShowPageNumberTooltip($"已自动跳转到第1页");
                }
                else if (pageNumber > viewModel.TotalPages)
                {
                    // 如果页码大于总页数，则导航到最后一页
                    viewModel.CurrentPage = viewModel.TotalPages;
                    ShowPageNumberTooltip($"已自动跳转到第{viewModel.TotalPages}页");
                }
            }
            else if (string.IsNullOrWhiteSpace(_pageNumberInput.Text))
            {
                // 如果输入为空，不做任何操作
            }
            else
            {
                // 输入无效，显示提示
                ShowPageNumberTooltip("请输入有效的页码");
            }

            // 恢复显示
            RestorePageInfoDisplay();
        }

        /// <summary>
        /// 显示页码输入提示
        /// </summary>
        private void ShowPageNumberTooltip(string message)
        {
            var tooltip = new ToolTip
            {
                Content = message,
                PlacementTarget = _pageInfoPanel,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                IsOpen = true,
                StaysOpen = false
            };

            // 2秒后自动关闭
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, e) =>
            {
                tooltip.IsOpen = false;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// 恢复页码信息显示
        /// </summary>
        private void RestorePageInfoDisplay()
        {
            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                _pageInfoPanel.Visibility = Visibility.Visible;
                _pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 检查是否是用户手动关闭窗口
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
                        Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
                        CommandParameter = false,
                        Style = TryFindResource("MaterialDesignFlatButton") as Style,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    
                    var yesButton = new Button
                    {
                        Content = "是",
                        Command = MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand,
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
                    MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog", (sender, args) =>
                    {
                        // 检查用户选择
                        if (args.Parameter is bool result && result)
                        {
                            // 用户确认退出，清理资源
                            if (DataContext is MainViewModel viewModel)
                            {
                                // 记录日志
                                TA_WPF.Utils.LogHelper.LogInfo("用户手动关闭了主窗口，应用程序将退出");
                            }
                            
                            // 关闭应用程序
                            Application.Current.Shutdown();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理窗口关闭事件时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                
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
    }
}