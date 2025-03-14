using System.ComponentModel;
using System.Windows;
using TA_WPF.Services;
using MaterialDesignThemes.Wpf;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 视图模型基类，提供通用功能和属性
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isDarkMode;
        protected ThemeService ThemeService => ThemeService.Instance;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseViewModel()
        {
            // 从配置文件加载主题设置
            _isDarkMode = ThemeService.LoadThemeFromConfig();
            
            // 订阅主题变更事件
            ThemeService.ThemeChanged += OnThemeChanged;
            
            // 初始化时同步主题状态
            if (Application.Current != null)
            {
                // 使用MaterialDesignThemes的主题状态
                PaletteHelper paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                _isDarkMode = theme.GetBaseTheme() == BaseTheme.Dark;
                
                // 确保资源字典中的主题标志与当前主题同步
                if (Application.Current.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = _isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !_isDarkMode;
                }
                
                // 订阅应用程序退出事件，以便在应用程序退出时取消订阅
                Application.Current.Exit += (s, e) => UnsubscribeFromEvents();
            }
        }

        /// <summary>
        /// 是否为深色模式
        /// </summary>
        public virtual bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    
                    // 应用主题
                    ThemeService.ApplyTheme(value);
                }
            }
        }

        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            if (_isDarkMode != isDarkMode)
            {
                _isDarkMode = isDarkMode;
                OnPropertyChanged(nameof(IsDarkMode));
            }
        }

        /// <summary>
        /// 更新主题状态
        /// </summary>
        public void UpdateThemeState()
        {
            if (Application.Current?.Resources != null && 
                Application.Current.Resources.Contains("Theme.Dark"))
            {
                bool isDarkMode = (bool)Application.Current.Resources["Theme.Dark"];
                if (_isDarkMode != isDarkMode)
                {
                    _isDarkMode = isDarkMode;
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // 取消订阅ThemeChanged事件
            if (ThemeService != null)
            {
                ThemeService.ThemeChanged -= OnThemeChanged;
            }
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~BaseViewModel()
        {
            UnsubscribeFromEvents();
        }
    }
} 