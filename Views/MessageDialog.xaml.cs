using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Services;
using System;
using System.Windows.Controls;

namespace TA_WPF.Views
{
    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Question
    }

    public enum MessageButtons
    {
        Ok,
        YesNo,
        YesNoCancel
    }

    public partial class MessageDialog : Window, INotifyPropertyChanged
    {
        private string _message;
        private PackIconKind _iconKind;
        private Brush _iconBrush;
        private MessageButtons _buttons;
        private ThemeService _themeService;

        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        public PackIconKind IconKind
        {
            get => _iconKind;
            set
            {
                if (_iconKind != value)
                {
                    _iconKind = value;
                    OnPropertyChanged(nameof(IconKind));
                }
            }
        }

        public Brush IconBrush
        {
            get => _iconBrush;
            set
            {
                if (_iconBrush != value)
                {
                    _iconBrush = value;
                    OnPropertyChanged(nameof(IconBrush));
                }
            }
        }

        public MessageButtons Buttons
        {
            get => _buttons;
            set
            {
                if (_buttons != value)
                {
                    _buttons = value;
                    OnPropertyChanged(nameof(Buttons));
                    OnPropertyChanged(nameof(IsOkButtonVisible));
                    OnPropertyChanged(nameof(IsYesNoButtonsVisible));
                    OnPropertyChanged(nameof(IsCancelButtonVisible));
                }
            }
        }

        public bool IsOkButtonVisible => Buttons == MessageButtons.Ok;
        public bool IsYesNoButtonsVisible => Buttons == MessageButtons.YesNo || Buttons == MessageButtons.YesNoCancel;
        public bool IsCancelButtonVisible => Buttons == MessageButtons.YesNoCancel;

        public MessageDialog(string message, string title, MessageType type, MessageButtons buttons)
        {
            InitializeComponent();
            DataContext = this;

            Message = message;
            Title = title;
            Buttons = buttons;

            // 获取主题服务
            _themeService = ThemeService.Instance;

            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            ApplyTheme(isDarkMode);

            // 设置图标和颜色
            switch (type)
            {
                case MessageType.Information:
                    IconKind = PackIconKind.Information;
                    IconBrush = new SolidColorBrush(Colors.DodgerBlue);
                    break;
                case MessageType.Warning:
                    IconKind = PackIconKind.Alert;
                    IconBrush = new SolidColorBrush(Colors.Orange);
                    break;
                case MessageType.Error:
                    IconKind = PackIconKind.Error;
                    IconBrush = new SolidColorBrush(Colors.Red);
                    break;
                case MessageType.Question:
                    IconKind = PackIconKind.QuestionMark;
                    IconBrush = new SolidColorBrush(Colors.DodgerBlue);
                    break;
            }

            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };

            // 添加键盘事件处理
            this.KeyDown += MessageDialog_KeyDown;

            // 添加窗口加载事件处理
            this.Loaded += MessageDialog_Loaded;
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

        private void MessageDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 根据按钮类型执行相应操作
                switch (Buttons)
                {
                    case MessageButtons.Ok:
                        // 确定按钮
                        DialogResult = true;
                        break;
                    case MessageButtons.YesNo:
                    case MessageButtons.YesNoCancel:
                        // 是按钮
                        DialogResult = true;
                        break;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Escape键处理
                switch (Buttons)
                {
                    case MessageButtons.Ok:
                    case MessageButtons.YesNo:
                        // 取消或否
                        DialogResult = false;
                        break;
                    case MessageButtons.YesNoCancel:
                        // 取消
                        DialogResult = null;
                        break;
                }
                e.Handled = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
        }

        private void MessageDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保窗口处于激活状态并显示在前面
            this.Activate();
            this.Focus();

            // 根据按钮类型设置焦点
            switch (Buttons)
            {
                case MessageButtons.Ok:
                    OkButton.Focus();
                    break;
                case MessageButtons.YesNo:
                case MessageButtons.YesNoCancel:
                    YesButton.Focus();
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 静态方法，用于显示消息对话框
        public static bool? Show(string message, string title = "提示", MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.Ok, Window owner = null)
        {
            var dialog = new MessageDialog(message, title, type, buttons);

            // 设置为始终在最前
            dialog.Topmost = true;

            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dialog.Owner = Application.Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // 显示对话框，并在显示后强制激活窗口
            dialog.ShowInTaskbar = false;
            var result = dialog.ShowDialog();

            return result;
        }
        
        // 显示带有进度信息的对话框（用于模型下载等操作）
        private static Window _progressWindow = null;
        private static TextBlock _progressMessageBlock = null;
        private static ProgressBar _progressBar = null;
        private static TextBlock _progressTextBlock = null;
        
        public static void ShowProgressInfo(string message, string title = "进度", int progress = 0)
        {
            Application.Current.Dispatcher.Invoke(() => {
                try
                {
                    // 如果进度为100%或消息为空，关闭窗口
                    if (progress >= 100 || string.IsNullOrEmpty(message))
                    {
                        if (_progressWindow != null && _progressWindow.IsVisible)
                        {
                            _progressWindow.Close();
                            _progressWindow = null;
                            _progressMessageBlock = null;
                            _progressBar = null;
                            _progressTextBlock = null;
                        }
                        return;
                    }
                    
                    // 如果窗口不存在或已关闭，创建新窗口
                    if (_progressWindow == null || !_progressWindow.IsVisible)
                    {
                        // 创建一个进度条和文本显示的Grid
                        var grid = new Grid();
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.Margin = new Thickness(24);
                        
                        // 消息文本
                        _progressMessageBlock = new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 16),
                            FontSize = 15,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        Grid.SetRow(_progressMessageBlock, 0);
                        grid.Children.Add(_progressMessageBlock);
                        
                        // 进度条
                        _progressBar = new ProgressBar
                        {
                            Value = progress,
                            Minimum = 0,
                            Maximum = 100,
                            Height = 10,
                            Width = 300,
                            Margin = new Thickness(0, 0, 0, 8)
                        };
                        Grid.SetRow(_progressBar, 1);
                        grid.Children.Add(_progressBar);
                        
                        // 进度文本
                        _progressTextBlock = new TextBlock
                        {
                            Text = $"{progress}%",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 0)
                        };
                        Grid.SetRow(_progressTextBlock, 2);
                        grid.Children.Add(_progressTextBlock);
                        
                        // 创建自定义窗口
                        _progressWindow = new Window
                        {
                            Title = title,
                            Content = grid,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize,
                            ShowInTaskbar = false,
                            Topmost = true,
                            MinWidth = 350,
                            MinHeight = 150,
                            Background = (Brush)Application.Current.Resources["MaterialDesignPaper"]
                        };
                        
                        // 应用MaterialDesign主题
                        var isDarkMode = ThemeService.Instance.IsDarkThemeActive();
                        ThemeAssist.SetTheme(_progressWindow, isDarkMode ? BaseTheme.Dark : BaseTheme.Light);
                        
                        _progressWindow.Show();
                    }
                    else
                    {
                        // 更新现有窗口内容
                        if (_progressMessageBlock != null)
                        {
                            _progressMessageBlock.Text = message;
                        }
                        
                        if (_progressBar != null)
                        {
                            _progressBar.Value = progress;
                        }
                        
                        if (_progressTextBlock != null)
                        {
                            _progressTextBlock.Text = $"{progress}%";
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常但不显示给用户，避免进度对话框导致的其他问题
                    System.Diagnostics.Debug.WriteLine($"进度对话框出错: {ex.Message}");
                }
            });
        }
    }
}