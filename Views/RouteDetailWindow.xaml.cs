using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// RouteDetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RouteDetailWindow : Window
    {
        private readonly RouteDetailViewModel _viewModel;
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;

        public RouteDetailWindow(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();

            // 创建ViewModel并设置为DataContext
            _viewModel = new RouteDetailViewModel(route, databaseService, mainViewModel);
            DataContext = _viewModel;

            // 注册关闭事件
            _viewModel.CloseRequested += ViewModel_CloseRequested;

            // 窗口加载完成后加载数据
            Loaded += RouteDetailWindow_Loaded;
            
            // 初始化页码提示工具提示
            InitializePageComponents();
        }

        private void RouteDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后的初始化操作
            // 暂时不执行数据加载，仅搭建UI框架
        }

        private void ViewModel_CloseRequested(object sender, EventArgs e)
        {
            // 关闭窗口
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消注册事件
            _viewModel.CloseRequested -= ViewModel_CloseRequested;
            base.OnClosed(e);
        }
        
        /// <summary>
        /// 初始化页码相关组件
        /// </summary>
        private void InitializePageComponents()
        {
            // 初始化页码提示工具提示
            _tooltipText = new TextBlock
            {
                Padding = new Thickness(8),
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14
            };

            _pageNumberTooltip = new Popup
            {
                Child = _tooltipText,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };
        }
        
        /// <summary>
        /// 处理页码信息面板的点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PageInfoPanel == null || PageNumberInput == null)
                return;

            // 显示输入框，隐藏页码信息
            PageInfoPanel.Visibility = Visibility.Collapsed;
            PageNumberInput.Visibility = Visibility.Visible;

            // 设置当前页码为默认值
            if (_viewModel != null && _viewModel.PaginationViewModel != null)
            {
                PageNumberInput.Text = _viewModel.PaginationViewModel.CurrentPage.ToString();
            }

            // 聚焦并全选
            PageNumberInput.Focus();
            PageNumberInput.SelectAll();
        }
        
        /// <summary>
        /// 处理页码输入框的键盘事件
        /// </summary>
        private void PageNumberInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryNavigateToPage();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // 取消输入，恢复显示页码信息
                if (PageInfoPanel != null && PageNumberInput != null)
                {
                    PageInfoPanel.Visibility = Visibility.Visible;
                    PageNumberInput.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                }
            }
        }
        
        /// <summary>
        /// 处理页码输入框失去焦点事件
        /// </summary>
        private void PageNumberInput_LostFocus(object sender, RoutedEventArgs e)
        {
            // 恢复显示页码信息
            if (PageInfoPanel != null && PageNumberInput != null)
            {
                PageInfoPanel.Visibility = Visibility.Visible;
                PageNumberInput.Visibility = Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// 尝试导航到指定页码
        /// </summary>
        private void TryNavigateToPage()
        {
            if (PageInfoPanel == null || PageNumberInput == null || _viewModel == null || _viewModel.PaginationViewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(PageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= _viewModel.PaginationViewModel.TotalPages)
                {
                    // 设置新的页码
                    _viewModel.PaginationViewModel.CurrentPage = pageNumber;
                    
                    // 确保页码变更后触发数据加载
                    _viewModel.PaginationViewModel.IsInitialized = true;
                    
                    // 直接调用加载方法确保数据刷新
                    _ = _viewModel.RefreshDataAsync();
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {_viewModel.PaginationViewModel.TotalPages} 之间";
                    _pageNumberTooltip.PlacementTarget = PageNumberInput;
                    _pageNumberTooltip.IsOpen = true;

                    // 3秒后自动关闭提示
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, args) =>
                    {
                        _pageNumberTooltip.IsOpen = false;
                        timer.Stop();
                    };
                    timer.Start();

                    // 恢复原始页码
                    PageNumberInput.Text = _viewModel.PaginationViewModel.CurrentPage.ToString();
                    PageNumberInput.SelectAll();
                    return;
                }
            }

            // 恢复显示页码信息
            PageInfoPanel.Visibility = Visibility.Visible;
            PageNumberInput.Visibility = Visibility.Collapsed;
        }
    }
} 