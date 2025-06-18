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
                    this.KeyDown -= AddTicketWindow_KeyDown;
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

                // 添加键盘事件处理
                this.KeyDown += AddTicketWindow_KeyDown;

                // 自动设置焦点到第一个文本框
                var firstTextBox = FindVisualChildren<TextBox>(this).FirstOrDefault();
                if (firstTextBox != null)
                {
                    firstTextBox.Focus();
                }

                // 确保窗口居中
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // 创建PreviewKeyDown事件处理程序
                KeyEventHandler previewKeyDownHandler = (s, args) =>
                {
                    if (args.Key == Key.Enter && !(Keyboard.FocusedElement is TextBox) && !(Keyboard.FocusedElement is ComboBox && ((ComboBox)Keyboard.FocusedElement).IsDropDownOpen))
                    {
                        // 模拟点击保存按钮
                        if (_viewModel.SaveCommand.CanExecute(null))
                        {
                            _viewModel.SaveCommand.Execute(null);
                            args.Handled = true;
                        }
                    }
                };

                // 注册全局键盘钩子，确保回车键能正确触发保存按钮
                this.PreviewKeyDown += previewKeyDownHandler;

                // 在窗口关闭时取消订阅事件
                this.Closed += (s, args) =>
                {
                    this.PreviewKeyDown -= previewKeyDownHandler;
                };
            }
            catch (Exception ex)
            {
                LogHelper.LogError("加载添加车票窗口时出错", ex);
                MessageBoxHelper.ShowError("加载窗口时出错: " + ex.Message);
            }
        }

        /// <summary>
        /// 处理键盘按键事件
        /// </summary>
        private void AddTicketWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // 如果按下回车键，执行保存操作
                if (e.Key == Key.Enter)
                {
                    // 获取当前焦点元素
                    var focusedElement = Keyboard.FocusedElement;

                    // 如果当前焦点是TextBox且在编辑状态，不触发保存，除非是最后一个文本框
                    if (focusedElement is TextBox textBox && textBox.IsKeyboardFocused)
                    {
                        // 允许在特定情况下触发保存
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                        {
                            // 使用Ctrl+Enter强制触发保存
                            if (_viewModel.SaveCommand.CanExecute(null))
                            {
                                _viewModel.SaveCommand.Execute(null);
                                e.Handled = true;
                                return;
                            }
                        }
                        return; // 普通回车键不触发保存
                    }

                    // 如果当前焦点是ComboBox且下拉列表打开，不触发保存
                    if (focusedElement is ComboBox comboBox && comboBox.IsDropDownOpen)
                    {
                        return;
                    }

                    // 执行保存命令
                    if (_viewModel.SaveCommand.CanExecute(null))
                    {
                        _viewModel.SaveCommand.Execute(null);
                        e.Handled = true;  // 标记事件已处理
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理键盘事件时出错", ex);
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

                // 检查父窗口是否最大化
                var parentWindow = Window.GetWindow(this.Owner);
                bool isParentMaximized = parentWindow != null && parentWindow.WindowState == WindowState.Maximized;

                // 设置窗口最大尺寸为屏幕的90%
                this.MaxHeight = screenHeight * 0.9;
                this.MaxWidth = screenWidth * 0.9;

                // 确保窗口不会太小
                this.MinHeight = 700;
                this.MinWidth = 800;

                // 设置窗口初始大小
                this.Height = Math.Min(850, screenHeight * 0.8);
                this.Width = Math.Min(900, screenWidth * 0.8);

                // 调整窗口位置，确保标题栏可见
                // 如果父窗口最大化，则将窗口位置调整为屏幕中心位置的偏上位置
                if (isParentMaximized)
                {
                    this.Top = Math.Max(20, screenHeight * 0.1); // 确保顶部有足够空间显示标题栏
                    this.Left = (screenWidth - this.Width) / 2;
                }
                else
                {
                    // 常规居中逻辑
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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

        private void MoneyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 判断是否所有文本都被选中
                    bool allTextSelected = textBox.SelectionLength == textBox.Text.Length && textBox.SelectionLength > 0;

                    // 处理全选后按Delete或Backspace的情况
                    if (allTextSelected && (e.Key == Key.Delete || e.Key == Key.Back))
                    {
                        // 替换为"0.00"而不是空字符串
                        textBox.Text = "0.00";
                        textBox.SelectAll();
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine("金额框全选删除: 已替换为0.00");
                        return;
                    }

                    if (e.Key == Key.Back)
                    {
                        int caretIndex = textBox.CaretIndex;
                        string text = textBox.Text;

                        // 光标在小数点后面时
                        if (caretIndex > 0 && caretIndex < text.Length && text[caretIndex - 1] == '.')
                        {
                            // 记录当前光标位置的前后部分
                            string textBeforeCaret = text.Substring(0, caretIndex - 1);
                            string textAfterCaret = text.Substring(caretIndex);

                            // 构建新值，确保小数部分仍然是小数
                            decimal newValue;
                            bool parseSuccess = false;

                            // 尝试解析小数点前的部分
                            if (decimal.TryParse(textBeforeCaret, out decimal beforePart))
                            {
                                // 尝试解析小数点后的部分作为小数
                                if (decimal.TryParse("0." + textAfterCaret, out decimal afterPart))
                                {
                                    // 合并两个部分
                                    newValue = beforePart + afterPart;
                                    parseSuccess = true;

                                    // 转换为字符串，保持格式
                                    string newText = newValue.ToString("F" + textAfterCaret.Length);

                                    // 日志输出
                                    System.Diagnostics.Debug.WriteLine($"金额框移除小数点: 原值={text}, 光标位置={caretIndex}, 修改后={newText}");

                                    // 更新文本内容
                                    textBox.Text = newText;

                                    // 设置光标位置在原来小数点的位置
                                    textBox.CaretIndex = caretIndex - 1;

                                    // 标记事件已处理
                                    e.Handled = true;
                                }
                            }

                            if (!parseSuccess)
                            {
                                // 如果解析失败，使用原始的方式处理
                                System.Diagnostics.Debug.WriteLine($"金额框移除小数点(解析失败): 原值={text}, 光标位置={caretIndex}, 尝试简单拼接");

                                // 更新文本内容，移除小数点
                                textBox.Text = textBeforeCaret + textAfterCaret;

                                // 设置光标位置在原来小数点的位置
                                textBox.CaretIndex = caretIndex - 1;

                                // 标记事件已处理
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额键盘按键事件时出错", ex);
            }
        }

        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车次号输入时出错", ex);
                e.Handled = true;
            }
        }

        private void SeatNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 检查当前ViewModel
                if (_viewModel != null)
                {
                    // 无论选择什么座位类型，都只允许输入数字
                    Regex regex = new Regex("[^0-9]+");
                    e.Handled = regex.IsMatch(e.Text);
                }
                else
                {
                    // 如果ViewModel不可用，默认只允许输入数字
                    Regex regex = new Regex("[^0-9]+");
                    e.Handled = regex.IsMatch(e.Text);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理座位号输入时出错", ex);
                e.Handled = true;
            }
        }

        private void CoachNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车厢号输入时出错", ex);
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
        /// 出发车站输入框失去焦点事件处理方法
        /// </summary>
        private void DepartStation_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果窗口正在关闭，不触发校验
                if (_isClosing) return;

                // 调用ViewModel中的处理方法，传入参数表示这是出发车站
                _viewModel.OnStationLostFocus(true);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理出发车站失去焦点事件时出错", ex);
            }
        }

        /// <summary>
        /// 到达车站输入框失去焦点事件处理方法
        /// </summary>
        private void ArriveStation_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果窗口正在关闭，不触发校验
                if (_isClosing) return;

                // 调用ViewModel中的处理方法，传入参数表示这是到达车站
                _viewModel.OnStationLostFocus(false);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理到达车站失去焦点事件时出错", ex);
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