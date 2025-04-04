using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    public partial class AddTicketWindow : Window
    {
        private readonly AddTicketViewModel _viewModel;
        private ThemeService _themeService;
        private bool _isClosing = false; // 添加窗口关闭标志

        public AddTicketWindow(DatabaseService databaseService, MainViewModel mainViewModel)
        {
            try
            {
                InitializeComponent();

                // 创建ViewModel并设置为DataContext
                _viewModel = new AddTicketViewModel(databaseService, mainViewModel);
                DataContext = _viewModel;

                // 获取主题服务
                _themeService = ThemeService.Instance;

                // 应用当前主题
                bool isDarkMode = _themeService.IsDarkThemeActive();
                ApplyTheme(isDarkMode);

                // 订阅主题变更事件
                _themeService.ThemeChanged += OnThemeChanged;

                // 订阅窗口关闭事件
                _viewModel.CloseWindow += (s, e) =>
                {
                    try
                    {
                        this.DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("关闭添加车票窗口时出错", ex);
                    }
                };

                // 订阅文本框聚焦事件
                _viewModel.FocusTextBox += ViewModel_FocusTextBox;

                // 订阅窗口加载事件
                this.Loaded += AddTicketWindow_Loaded;

                // 订阅字体大小变化事件
                this.SizeChanged += AddTicketWindow_SizeChanged;

                // 窗口关闭时取消订阅事件
                this.Closed += (s, e) =>
                {
                    _themeService.ThemeChanged -= OnThemeChanged;
                };
            }
            catch (Exception ex)
            {
                LogHelper.LogError("初始化添加车票窗口时出错", ex);
                MessageBoxHelper.ShowError("初始化窗口时出错: " + ex.Message);
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            // 设置窗口主题
            ThemeAssist.SetTheme(this, isDarkMode ? BaseTheme.Dark : BaseTheme.Light);

            // 获取当前资源字典
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            // 设置深色/浅色模式
            theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);

            // 应用主题到窗口
            paletteHelper.SetTheme(theme);

            // 获取主题前景色
            var foregroundBrush = Application.Current.Resources["MaterialDesignBody"] as Brush;

            // 更新所有文本框的前景色
            if (foregroundBrush != null)
            {
                // 查找所有TextBox并更新前景色
                var textBoxes = FindVisualChildren<TextBox>(this);
                foreach (var textBox in textBoxes)
                {
                    textBox.Foreground = foregroundBrush;
                }

                // 查找所有ComboBox并更新前景色
                var comboBoxes = FindVisualChildren<ComboBox>(this);
                foreach (var comboBox in comboBoxes)
                {
                    comboBox.Foreground = foregroundBrush;
                }

                // 查找所有TextBlock并更新前景色
                var textBlocks = FindVisualChildren<TextBlock>(this);
                foreach (var textBlock in textBlocks)
                {
                    // 只更新那些没有显式设置样式的TextBlock
                    if (textBlock.Style == null || textBlock.Style.Equals(FindResource("MaterialDesignBody1TextBlock")))
                    {
                        textBlock.Foreground = foregroundBrush;
                    }
                }
            }

            // 强制刷新窗口
            this.UpdateLayout();
        }

        /// <summary>
        /// 查找指定类型的所有可视子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            // 更新窗口主题
            ApplyTheme(isDarkMode);
        }

        private void AddTicketWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置窗口初始大小
                AdjustWindowSize();

                // 自动设置焦点到第一个文本框
                var firstTextBox = FindVisualChildren<TextBox>(this).FirstOrDefault();
                if (firstTextBox != null)
                {
                    firstTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("加载添加车票窗口时出错", ex);
                MessageBoxHelper.ShowError("加载窗口时出错: " + ex.Message);
            }
        }

        private void AddTicketWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                // 当窗口大小变化时，调整内容布局
                AdjustContentLayout();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("调整窗口大小时出错", ex);
            }
        }

        private void AdjustWindowSize()
        {
            try
            {
                // 获取当前屏幕尺寸
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                var screenWidth = SystemParameters.PrimaryScreenWidth;

                // 设置窗口最大尺寸为屏幕的90%
                this.MaxHeight = screenHeight * 0.9;
                this.MaxWidth = screenWidth * 0.9;

                // 确保窗口不会太小
                this.MinHeight = 700;
                this.MinWidth = 800;

                // 设置窗口初始大小
                this.Height = Math.Min(850, screenHeight * 0.8);
                this.Width = Math.Min(900, screenWidth * 0.8);

                // 确保窗口在屏幕内
                if (this.Top + this.Height > screenHeight)
                {
                    this.Top = Math.Max(0, screenHeight - this.Height);
                }

                if (this.Left + this.Width > screenWidth)
                {
                    this.Left = Math.Max(0, screenWidth - this.Width);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("调整窗口大小时出错", ex);
            }
        }

        private void AdjustContentLayout()
        {
            try
            {
                // 根据当前字体大小调整控件间距和大小
                var fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];

                // 调整边距
                double margin = Math.Max(16, fontSize * 0.8);

                // 如果窗口处于最大化状态，增加边距以提高可读性
                if (this.WindowState == WindowState.Maximized)
                {
                    margin = Math.Max(24, fontSize * 1.2);

                    // 为最大化状态设置内容边距
                    var mainGrid = this.Content as Grid;
                    if (mainGrid != null)
                    {
                        mainGrid.Margin = new Thickness(margin);
                    }
                }
                else
                {
                    // 恢复正常边距
                    var mainGrid = this.Content as Grid;
                    if (mainGrid != null)
                    {
                        mainGrid.Margin = new Thickness(16);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("调整内容布局时出错", ex);
            }
        }

        private void MoneyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字和小数点
                Regex regex = new Regex("[^0-9.]+");
                e.Handled = regex.IsMatch(e.Text);

                // 如果输入的是小数点，检测是否已经有小数点
                if (e.Text == ".")
                {
                    TextBox textBox = sender as TextBox;
                    if (textBox != null && textBox.Text.Contains("."))
                    {
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额输入时出错", ex);
                e.Handled = true;
            }
        }

        private void SeatNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理座位号输入时出错", ex);
                e.Handled = true;
            }
        }

        private void MoneyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 保存当前前景色
                    var foreground = textBox.Foreground;

                    // 尝试解析金额
                    if (double.TryParse(textBox.Text, out double amount))
                    {
                        // 格式化为两位小数
                        textBox.Text = amount.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    else if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        // 如果为空，设置为0.00
                        textBox.Text = "0.00";
                    }
                    else
                    {
                        // 如果无法解析，恢复为0.00
                        MessageBoxHelper.ShowWarning("请输入有效的金额数值");
                        textBox.Text = "0.00";
                    }

                    // 确保前景色不变
                    textBox.Foreground = foreground;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额失去焦点事件时出错", ex);
                if (sender is TextBox tb)
                {
                    tb.Text = "0.00";
                    // 确保前景色与主题一致
                    tb.Foreground = Application.Current.Resources["MaterialDesignBody"] as Brush;
                }
            }
        }

        /// <summary>
        /// 处理ViewModel的文本框聚焦事件
        /// </summary>
        private void ViewModel_FocusTextBox(object sender, TextBoxFocusEventArgs e)
        {
            try
            {
                // 在UI线程上执行聚焦操作
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 根据tag查找对应的TextBox
                    var textBoxes = FindVisualChildren<TextBox>(this);
                    var targetTextBox = textBoxes.FirstOrDefault(tb => tb.Tag?.ToString() == e.TextBoxTag);

                    // 如果找到了目标TextBox，将焦点设置到该TextBox
                    if (targetTextBox != null)
                    {
                        targetTextBox.Focus();
                        // 将光标移到末尾
                        targetTextBox.CaretIndex = targetTextBox.Text?.Length ?? 0;
                        // 选中全部文本，方便用户重新输入
                        targetTextBox.SelectAll();
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"设置文本框焦点时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 出发站输入框失去焦点事件处理方法
        /// </summary>
        private void DepartStation_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果窗口正在关闭，不触发校验
                if (_isClosing) return;

                // 调用ViewModel中的处理方法，传入参数表示这是出发站
                _viewModel.OnStationLostFocus(true);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理出发站失去焦点事件时出错", ex);
            }
        }

        /// <summary>
        /// 到达站输入框失去焦点事件处理方法
        /// </summary>
        private void ArriveStation_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果窗口正在关闭，不触发校验
                if (_isClosing) return;

                // 调用ViewModel中的处理方法，传入参数表示这是到达站
                _viewModel.OnStationLostFocus(false);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理到达站失去焦点事件时出错", ex);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                // 设置窗口样式
                IntPtr handle = new WindowInteropHelper(this).Handle;
                var hwndSource = HwndSource.FromHwnd(handle);
                if (hwndSource != null)
                {
                    hwndSource.AddHook(new HwndSourceHook(WindowProc));

                    // 禁用最大化按钮
                    int style = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_STYLE);
                    style &= ~NativeMethods.WS_MAXIMIZEBOX;
                    NativeMethods.SetWindowLong(handle, NativeMethods.GWL_STYLE, style);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("初始化窗口源时出错", ex);
            }
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 处理窗口消息
            switch (msg)
            {
                case NativeMethods.WM_GETMINMAXINFO:
                    // 防止窗口最大化
                    handled = true;
                    return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        // 添加窗口状态变化事件处理
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            try
            {
                // 如果窗口尝试最大化，则恢复正常大小
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }

                // 当窗口状态变化时调整内容布局
                AdjustContentLayout();
            }
            catch (Exception ex)
            {
                LogHelper.LogError("窗口状态变化处理出错", ex);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 标记窗口正在关闭
            _isClosing = true;

            base.OnClosing(e);

            try
            {
                // 如果DialogResult已设置，说明是通过保存按钮关闭的，不需要提示
                if (this.DialogResult.HasValue)
                    return;

                // 只在用户实际修改过表单内容后才提示是否保存
                if (_viewModel.HasUnsavedChanges())
                {
                    // 显示确认对话框
                    bool? result = MessageDialog.Show(
                        "您有未保存的修改，是否保存？",
                        "未保存的修改",
                        MessageType.Question,
                        MessageButtons.YesNoCancel,
                        this);

                    if (result == true) // 是
                    {
                        // 执行保存前先验证表单
                        if (!_viewModel.ValidateForm())
                        {
                            // 显示验证错误信息
                            string errorMessage = _viewModel.GetValidationErrors();
                            MessageBoxHelper.ShowWarning(errorMessage, "表单验证失败");
                            e.Cancel = true;
                            return;
                        }

                        // 执行保存命令
                        if (_viewModel.SaveCommand.CanExecute(null))
                        {
                            _viewModel.SaveCommand.Execute(null);

                            // 如果保存命令执行后窗口仍然打开，说明保存失败或表单验证未通过，取消关闭
                            if (this.IsVisible)
                            {
                                e.Cancel = true;
                            }
                        }
                        else
                        {
                            // 如果保存命令无法执行，取消关闭
                            e.Cancel = true;
                        }
                    }
                    else if (result == null) // 取消
                    {
                        // 取消关闭
                        e.Cancel = true;
                    }
                    // 否则 (result == false) 不保存，直接关闭
                }
                // 如果没有修改，直接关闭窗口，不提示
            }
            catch (Exception ex)
            {
                LogHelper.LogError("关闭添加车票窗口时出错", ex);
                MessageBoxHelper.ShowError("关闭窗口时出错: " + ex.Message);
            }
        }

        // 添加NativeMethods类用于调用Win32 API
        internal static class NativeMethods
        {
            public const int GWL_STYLE = -16;
            public const int WS_MAXIMIZEBOX = 0x10000;
            public const int WM_GETMINMAXINFO = 0x0024;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hwnd, int index);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        }
    }
}