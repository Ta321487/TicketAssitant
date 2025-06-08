using System.ComponentModel;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// EditStationToRouteWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditStationToRouteWindow : Window
    {
        private readonly EditStationToRouteViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="routeInfo">路线信息</param>
        /// <param name="stationMapping">车站映射信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        /// <param name="refreshCallback">刷新回调</param>
        public EditStationToRouteWindow(
            RouteInfo routeInfo, 
            RouteStationMapping stationMapping, 
            DatabaseService databaseService, 
            StationSearchService stationSearchService, 
            MainViewModel mainViewModel,
            Action refreshCallback)
        {
            InitializeComponent();

            // 获取主题服务实例
            _themeService = ThemeService.Instance;

            // 获取配置服务和距离计算服务
            var configurationService = new ConfigurationService();
            var distanceCalculationService = new DistanceCalculationService(configurationService);

            // 初始化ViewModel
            _viewModel = new EditStationToRouteViewModel(
                routeInfo,
                stationMapping,
                databaseService,
                stationSearchService,
                distanceCalculationService,
                mainViewModel,
                configurationService,
                refreshCallback);

            // 设置DataContext
            DataContext = _viewModel;

            // 订阅关闭窗口事件
            _viewModel.CloseWindow += (s, e) =>
            {
                this.DialogResult = true;
                Close();
            };

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
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            };
        }

        /// <summary>
        /// 窗口关闭前检查是否有未保存的修改
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // 由于目前只实现UI部分，不需要检查未保存的修改
            // 这里保留方法框架，后续可以实现检查逻辑
        }

        /// <summary>
        /// 主题变化事件处理
        /// </summary>
        private void ThemeService_ThemeChanged(object sender, bool isDarkMode)
        {
            // 应用主题到当前窗口
            _themeService.ApplyThemeToWindow(this, isDarkMode);
        }
    }
} 