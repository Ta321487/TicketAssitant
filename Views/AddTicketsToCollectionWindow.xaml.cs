using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace TA_WPF.Views
{
    /// <summary>
    /// AddTicketsToCollectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddTicketsToCollectionWindow : Window
    {
        private readonly AddTicketsToCollectionViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public AddTicketsToCollectionWindow(TicketCollectionInfo collection, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();
            
            // 获取主题服务
            _themeService = ThemeService.Instance;
            
            // 创建视图模型
            _viewModel = new AddTicketsToCollectionViewModel(collection, databaseService, mainViewModel);
            DataContext = _viewModel;
            
            // 应用当前主题
            ApplyTheme(_viewModel.MainViewModel.IsDarkMode);
            
            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;
            
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
        /// 数据表格选择变更事件
        /// </summary>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // 判断是否由SelectAll或UnselectAll触发的事件
                // 如果SelectedTickets数量与滚动条位置无关，则说明是由VM全选/取消选择触发的
                if ((_viewModel.SelectedTickets.Count == _viewModel.Tickets.Count && _viewModel.SelectedTickets.Count > 0 && 
                    dataGrid.SelectedItems.Count == _viewModel.Tickets.Count) ||
                    (_viewModel.SelectedTickets.Count == 0 && dataGrid.SelectedItems.Count == 0))
                {
                    // 由SelectAll或UnselectAll触发的，不需要更新ViewModel
                    return;
                }
                
                // 清空已选中项集合
                _viewModel.SelectedTickets.Clear();
                
                // 添加所有当前选中的项
                foreach (TrainRideInfo item in dataGrid.SelectedItems)
                {
                    item.IsSelected = true;
                    _viewModel.SelectedTickets.Add(item);
                }

                // 更新未选中项状态
                foreach (TrainRideInfo item in _viewModel.Tickets)
                {
                    if (!dataGrid.SelectedItems.Contains(item))
                    {
                        item.IsSelected = false;
                    }
                }
                
                // 直接更新ViewModel属性
                _viewModel.SelectedItemsCount = dataGrid.SelectedItems.Count;
                _viewModel.HasSelectedItems = dataGrid.SelectedItems.Count > 0;
            }
        }

        /// <summary>
        /// 窗口关闭前触发的事件
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果有数据变更，设置DialogResult为true
            if (_viewModel.HasDataChanged)
            {
                DialogResult = true;
            }
        }
    }
} 