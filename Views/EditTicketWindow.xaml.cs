using System.Windows;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.Windows.Controls;
using TA_WPF.Utils;
using System.Windows.Interop;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using TA_WPF.Models;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace TA_WPF.Views
{
    public partial class EditTicketWindow : Window
    {
        private readonly EditTicketViewModel _viewModel;
        private ThemeService _themeService;
        
        public EditTicketWindow(DatabaseService databaseService, MainViewModel mainViewModel, TrainRideInfo ticket)
        {
            try
            {
                InitializeComponent();
                
                // 创建ViewModel并设置为DataContext
                _viewModel = new EditTicketViewModel(databaseService, mainViewModel, ticket);
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
                        LogHelper.LogError("关闭修改车票窗口时出错", ex);
                    }
                };
                
                // 订阅窗口加载事件
                this.Loaded += EditTicketWindow_Loaded;
                
                // 订阅字体大小变化事件
                this.SizeChanged += EditTicketWindow_SizeChanged;
                
                // 窗口关闭时取消订阅事件
                this.Closed += (s, e) => {
                    _themeService.ThemeChanged -= OnThemeChanged;
                };
            }
            catch (Exception ex)
            {
                LogHelper.LogError("初始化修改车票窗口时出错", ex);
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
                foreach (var textBox in FindVisualChildren<TextBox>(this))
                {
                    textBox.Foreground = foregroundBrush;
                }
                
                foreach (var comboBox in FindVisualChildren<ComboBox>(this))
                {
                    comboBox.Foreground = foregroundBrush;
                }
            }
        }
        
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
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

        private void EditTicketWindow_Loaded(object sender, RoutedEventArgs e)
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
                LogHelper.LogError("加载修改车票窗口时出错", ex);
                MessageBoxHelper.ShowError("加载窗口时出错: " + ex.Message);
            }
        }
        
        private void EditTicketWindow_SizeChanged(object sender, SizeChangedEventArgs e)
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
                // 根据窗口大小调整内容布局
                // 这里可以添加一些布局调整的逻辑
            }
            catch (Exception ex)
            {
                LogHelper.LogError("调整内容布局时出错", ex);
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

        protected override void OnClosing(CancelEventArgs e)
        {
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
                LogHelper.LogError("关闭修改车票窗口时出错", ex);
                MessageBoxHelper.ShowError("关闭窗口时出错: " + ex.Message);
            }
        }

        /// <summary>
        /// 出发站输入框失去焦点事件处理方法
        /// </summary>
        private void DepartStation_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
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
                // 调用ViewModel中的处理方法，传入参数表示这是到达站
                _viewModel.OnStationLostFocus(false);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理到达站失去焦点事件时出错", ex);
            }
        }
    }
} 