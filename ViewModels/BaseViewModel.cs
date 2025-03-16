using System.ComponentModel;
using System.Windows;
using TA_WPF.Services;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using TA_WPF.Utils;
using TA_WPF.Models;
using System.IO;
using System.Text;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 视图模型基类，提供通用功能和属性
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isDarkMode;
        private bool _isAllSelected;
        private bool _isTicketQuery; // 标记是否为车票查询视图
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
                bool currentIsDarkMode = theme.GetBaseTheme() == BaseTheme.Dark;
                
                // 如果当前主题状态与配置文件中的不一致，以当前主题状态为准
                if (_isDarkMode != currentIsDarkMode)
                {
                    System.Console.WriteLine($"BaseViewModel: 主题状态不一致，配置文件中为{(_isDarkMode ? "深色" : "浅色")}，当前主题为{(currentIsDarkMode ? "深色" : "浅色")}");
                    _isDarkMode = currentIsDarkMode;
                    
                    // 保存当前主题状态到配置文件
                    ThemeService.ApplyTheme(_isDarkMode);
                }
                
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
        /// 是否全选
        /// </summary>
        public virtual bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged(nameof(IsAllSelected));
                    
                    // 应用全选状态到所有项
                    ApplySelectionToAll(value);
                }
            }
        }

        /// <summary>
        /// 是否为车票查询视图
        /// </summary>
        public virtual bool IsTicketQuery
        {
            get => _isTicketQuery;
            protected set
            {
                if (_isTicketQuery != value)
                {
                    _isTicketQuery = value;
                    OnPropertyChanged(nameof(IsTicketQuery));
                }
            }
        }

        /// <summary>
        /// 获取选中的项
        /// </summary>
        /// <typeparam name="T">项的类型</typeparam>
        /// <param name="items">项集合</param>
        /// <returns>选中的项集合</returns>
        protected virtual IEnumerable<T> GetSelectedItems<T>(IEnumerable<T> items) where T : TrainRideInfo
        {
            return items?.Where(item => item.IsSelected) ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// 应用选择状态到所有项
        /// </summary>
        /// <param name="isSelected">是否选中</param>
        protected virtual void ApplySelectionToAll(bool isSelected)
        {
            // 此方法需要在子类中重写，以便应用选择状态到具体的项集合
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