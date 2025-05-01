using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Services;
using MaterialDesignThemes.Wpf;
using TA_WPF.Utils;

namespace TA_WPF.Views
{
    /// <summary>
    /// SelectDialog.xaml 的交互逻辑，用于多选项选择
    /// </summary>
    public partial class SelectDialog : Window, INotifyPropertyChanged
    {
        private int _selectedIndex = -1;
        private List<string> _items;
        private string _dialogTitle;
        private ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="items">选项列表</param>
        /// <param name="title">对话框标题</param>
        public SelectDialog(List<string> items, string title = "请选择")
        {
            InitializeComponent();
            DataContext = this;
            
            Items = items ?? new List<string>();
            DialogTitle = title;
            
            // 获取主题服务
            _themeService = ThemeService.Instance;

            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            ApplyTheme(isDarkMode);

            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) => {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
            
            // 设置所有者窗口
            if (Application.Current?.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }
            
            // 添加键盘事件处理
            this.KeyDown += SelectDialog_KeyDown;
            
            // 添加窗口加载事件处理
            this.Loaded += SelectDialog_Loaded;
        }

        /// <summary>
        /// 应用主题
        /// </summary>
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

        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            // 更新窗口主题
            ApplyTheme(isDarkMode);
        }

        /// <summary>
        /// 键盘事件处理
        /// </summary>
        private void SelectDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsItemSelected)
            {
                // 回车键确认选择
                DialogResult = true;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Escape键取消
                DialogResult = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 窗口加载事件处理
        /// </summary>
        private void SelectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保窗口处于激活状态并显示在前面
            this.Activate();
            this.Focus();
            
            // 设置焦点到列表
            if (ItemsListView.Items.Count > 0)
            {
                ItemsListView.Focus();
            }
        }

        /// <summary>
        /// 选项列表
        /// </summary>
        public List<string> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                }
            }
        }

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle
        {
            get => _dialogTitle;
            set
            {
                if (_dialogTitle != value)
                {
                    _dialogTitle = value;
                    OnPropertyChanged(nameof(DialogTitle));
                }
            }
        }

        /// <summary>
        /// 选中项索引
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnPropertyChanged(nameof(SelectedIndex));
                    OnPropertyChanged(nameof(IsItemSelected));
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool IsItemSelected => SelectedIndex >= 0;

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedIndex >= 0)
            {
                DialogResult = true;
            }
            else
            {
                MessageBoxHelper.ShowInfo("请选择一个选项");
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 