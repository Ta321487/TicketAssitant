using System;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// EditStationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditStationWindow : Window
    {
        private readonly EditStationViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        /// <param name="stationToEdit">要编辑的车站信息</param>
        /// <param name="refreshCallback">刷新回调</param>
        public EditStationWindow(DatabaseService databaseService, StationSearchService stationSearchService, StationInfo stationToEdit, Action refreshCallback)
        {
            InitializeComponent();

            // 获取主题服务实例
            _themeService = ThemeService.Instance;

            // 初始化ViewModel
            _viewModel = new EditStationViewModel(databaseService, stationSearchService, stationToEdit, refreshCallback);
            
            // 设置DataContext
            DataContext = _viewModel;

            // 订阅关闭窗口事件
            _viewModel.CloseWindow += (s, e) => Close();

            // 设置所有者窗口
            if (Application.Current?.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            _themeService.ApplyThemeToWindow(this, isDarkMode);

            // 订阅主题变化事件
            _themeService.ThemeChanged += ThemeService_ThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) => {
                _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            };

            // 更新字体大小
            UpdateFontSize();
        }

        /// <summary>
        /// 主题变化事件处理
        /// </summary>
        private void ThemeService_ThemeChanged(object sender, bool isDarkMode)
        {
            // 应用主题到当前窗口
            _themeService.ApplyThemeToWindow(this, isDarkMode);
        }

        /// <summary>
        /// 更新字体大小
        /// </summary>
        private void UpdateFontSize()
        {
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.Contains("MaterialDesignFontSize"))
            {
                double fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];
                if (_viewModel != null)
                {
                    _viewModel.FontSize = fontSize;
                }
            }
        }
    }
} 