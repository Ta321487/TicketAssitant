using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// CopyMoveCollectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CopyMoveCollectionWindow : Window
    {
        private readonly CopyMoveCollectionViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sourceCollection">源收藏夹</param>
        /// <param name="isMove">是否为移动操作</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public CopyMoveCollectionWindow(TicketCollectionInfo sourceCollection, bool isMove, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();
            
            // 获取主题服务
            _themeService = ThemeService.Instance;
            
            // 创建视图模型
            _viewModel = new CopyMoveCollectionViewModel(sourceCollection, isMove, databaseService, mainViewModel);
            
            // 设置DataContext
            DataContext = _viewModel;
            
            // 应用当前主题
            ApplyTheme(_viewModel.MainViewModel.IsDarkMode);
            
            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;
            
            // 窗口加载完成后加载数据
            this.Loaded += CopyMoveCollectionWindow_Loaded;
            
            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
        }
        
        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme(bool isDarkMode)
        {
            // 设置主题助手
            ThemeAssist.SetTheme(this, 
                isDarkMode ? BaseTheme.Dark : 
                            BaseTheme.Light);
            
            // 更新调色板
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            
            // 设置深色/浅色模式
            theme.SetBaseTheme(isDarkMode ? 
                Theme.Dark : 
                Theme.Light);
                
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
            ApplyTheme(isDarkMode);
        }
        
        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void CopyMoveCollectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载目标收藏夹列表
            _viewModel.LoadTargetCollections();
        }
        
        /// <summary>
        /// 获取操作结果
        /// </summary>
        public CopyMoveCollectionResult Result => _viewModel.Result;
        
        /// <summary>
        /// 复制/移动操作结果
        /// </summary>
        public class CopyMoveCollectionResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }
            
            /// <summary>
            /// 目标收藏夹
            /// </summary>
            public TicketCollectionInfo TargetCollection { get; set; }
            
            /// <summary>
            /// 是否创建了新收藏夹
            /// </summary>
            public bool IsNewCollection { get; set; }
            
            /// <summary>
            /// 操作的车票数量
            /// </summary>
            public int AffectedTicketsCount { get; set; }
        }
    }
} 