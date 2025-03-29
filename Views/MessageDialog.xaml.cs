using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Services;
using System.Windows.Input;

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
            this.Closed += (s, e) => {
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
    }
} 