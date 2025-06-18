using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Windows;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// ImportStationFrom12306Window.xaml 的交互逻辑
    /// </summary>
    public partial class ImportStationFrom12306Window : Window
    {
        private bool _forceClose = false;

        public ImportStationFrom12306Window(StationImportService stationImportService, MainViewModel mainViewModel)
        {
            InitializeComponent();

            // 设置ViewModel
            var viewModel = new ImportStationFrom12306ViewModel(stationImportService, mainViewModel);
            DataContext = viewModel;

            // 订阅主题服务变更事件
            var themeService = ThemeService.Instance;
            themeService.ThemeChanged += OnThemeChanged;

            // 应用当前主题
            ApplyTheme(themeService.IsDarkThemeActive());

            // 窗口关闭时取消订阅
            this.Closed += (s, e) =>
            {
                themeService.ThemeChanged -= OnThemeChanged;
            };

            // 窗口关闭前的确认
            this.Closing += Window_Closing;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_forceClose)
            {
                // 如果是强制关闭，直接退出
                return;
            }

            if (DataContext is ImportStationFrom12306ViewModel viewModel)
            {
                // 检查是否正在导入（仅在实际导入过程中才提示，已完成但未重置状态的不提示）
                if (viewModel.IsImporting && !viewModel.HasImportResult)
                {
                    // 弹出确认对话框
                    var result = MessageBoxHelper.ShowConfirmation(
                        "正在导入车站，关闭会导致导入中断，是否确认关闭？",
                        "确认关闭");

                    if (result == MessageBoxResult.Yes)
                    {
                        // 用户确认关闭，撤销已导入的数据
                        _forceClose = true; // 设置强制关闭标志，避免再次触发确认
                        viewModel.CancelAndRollbackImport();

                        // 等待一小段时间确保取消操作被处理
                        System.Threading.Thread.Sleep(100);
                    }
                    else
                    {
                        // 用户取消关闭，继续导入
                        e.Cancel = true;
                    }
                }
            }
        }

        /// <summary>
        /// 应用主题样式
        /// </summary>
        /// <param name="isDarkMode">是否为深色模式</param>
        private void ApplyTheme(bool isDarkMode)
        {
            // 设置窗口主题
            ThemeAssist.SetTheme(this, isDarkMode ?
                BaseTheme.Dark :
                BaseTheme.Light);

            // 更新主题
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDarkMode ?
                Theme.Dark :
                Theme.Light);
            paletteHelper.SetTheme(theme);
        }

        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            ApplyTheme(isDarkMode);
        }
    }
}