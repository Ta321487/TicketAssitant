using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.Views
{
    public partial class InputDialog : Window, INotifyPropertyChanged
    {
        private string _promptText;
        private string _responseText;
        private ThemeService _themeService;

        public string PromptText
        {
            get => _promptText;
            set
            {
                if (_promptText != value)
                {
                    _promptText = value;
                    OnPropertyChanged(nameof(PromptText));
                }
            }
        }

        public string ResponseText
        {
            get => _responseText;
            set
            {
                if (_responseText != value)
                {
                    _responseText = value;
                    OnPropertyChanged(nameof(ResponseText));
                }
            }
        }

        public InputDialog(string promptText)
        {
            InitializeComponent();
            DataContext = this;
            PromptText = promptText;

            // 获取主题服务
            _themeService = ThemeService.Instance;

            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            ApplyTheme(isDarkMode);

            // 设置最小高度
            AdjustDialogSize();

            // 窗口加载完成后，自动将焦点设置到文本框
            Loaded += (s, e) =>
            {
                ResponseTextBox.Focus();
            };

            // 禁止最大化
            this.SourceInitialized += InputDialog_SourceInitialized;

            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
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

            // 强制刷新窗口
            this.UpdateLayout();
        }

        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            // 更新窗口主题
            ApplyTheme(isDarkMode);
        }

        private void InputDialog_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                // 禁用最大化按钮
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
                Debug.WriteLine($"禁用最大化按钮时出错: {ex.Message}");
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SubmitDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ResponseTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果按下Ctrl+Enter，则确认
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                DialogResult = true;
                Close();
            }
            // 如果按下Esc，则取消
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void SubmitDialog()
        {
            // 验证输入内容不为空
            if (string.IsNullOrWhiteSpace(ResponseText))
            {
                // 使用MessageBoxHelper显示警告
                MessageBoxHelper.ShowWarning("请输入内容，不能为空！", "提示", this);
                ResponseTextBox.Focus();
                return;
            }

            DialogResult = true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 当窗口大小变化时调整内部布局
            AdjustDialogSize();

            // 如果窗口尝试最大化，则恢复正常大小
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void AdjustDialogSize()
        {
            try
            {
                // 获取当前字体大小
                double fontSize = 13; // 默认字体大小

                if (Application.Current != null &&
                    Application.Current.Resources.Contains("MaterialDesignFontSize"))
                {
                    fontSize = Convert.ToDouble(Application.Current.Resources["MaterialDesignFontSize"]);
                }

                // 根据字体大小调整窗口最小高度
                double minHeight = 300 + Math.Max(0, (fontSize - 13) * 10);
                double minWidth = 450 + Math.Max(0, (fontSize - 13) * 15);

                // 设置窗口最小尺寸
                this.MinHeight = minHeight;
                this.MinWidth = minWidth;

                // 如果当前窗口小于最小尺寸，则调整
                if (this.Height < minHeight)
                {
                    this.Height = minHeight;
                }

                if (this.Width < minWidth)
                {
                    this.Width = minWidth;
                }

                // 调整内部控件的边距
                double margin = Math.Max(20, fontSize * 1.2);

                // 获取主Grid
                if (this.Content is Grid mainGrid)
                {
                    mainGrid.Margin = new Thickness(margin);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不显示给用户，避免干扰用户体验
                Debug.WriteLine($"调整对话框大小时出错: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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