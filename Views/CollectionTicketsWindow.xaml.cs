using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.Windows.Threading;

namespace TA_WPF.Views
{
    /// <summary>
    /// CollectionTicketsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionTicketsWindow : Window
    {
        private readonly CollectionTicketsViewModel _viewModel;
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        private readonly ThemeService _themeService;
        private bool _isInternalSelectionChange = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public CollectionTicketsWindow(TicketCollectionInfo collection, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();
            
            // 获取主题服务
            _themeService = ThemeService.Instance;

            // 创建视图模型
            _viewModel = new CollectionTicketsViewModel(collection, databaseService, mainViewModel);
            
            // 设置DataContext
            DataContext = _viewModel;
            
            // 初始化页码提示框
            InitializePageNumberTooltip();
            
            // 窗口加载完成后加载车票数据
            this.Loaded += CollectionTicketsWindow_Loaded;

            // 应用当前主题
            ApplyTheme(_viewModel.MainViewModel.IsDarkMode);

            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
            
            // 添加DataGrid的键盘事件处理，支持Ctrl+A全选
            TicketsDataGrid.PreviewKeyDown += TicketsDataGrid_PreviewKeyDown;
            
            // 添加订阅ViewModel的选择变更事件
            _viewModel.SelectionChanged += ViewModel_SelectionChanged;
            
            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _viewModel.SelectionChanged -= ViewModel_SelectionChanged;
            };
        }

        /// <summary>
        /// 处理DataGrid的键盘事件，支持Ctrl+A全选和Delete删除
        /// </summary>
        private void TicketsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 处理Ctrl+A全选
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_viewModel != null && _viewModel.SelectAllCommand.CanExecute(null))
                {
                _viewModel.SelectAllCommand.Execute(null);
                e.Handled = true;
                }
            }
        }
        
        /// <summary>
        /// 处理DataGrid的选择变更事件
        /// </summary>
        private void TicketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 避免循环更新
            if (_isInternalSelectionChange)
                return;
            
            try
            {
                _isInternalSelectionChange = true;
                
                // 当DataGrid的选择变更时，同步更新ViewModel中的选择状态
                if (sender is DataGrid dataGrid && _viewModel != null)
                {
                    // 获取当前激活的键盘修饰键状态
                    bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                    bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    // 先处理移除的项
                    foreach (TrainRideInfo item in e.RemovedItems)
                    {
                        item.IsSelected = false;
                        if (_viewModel.SelectedTickets.Contains(item))
                        {
                            _viewModel.SelectedTickets.Remove(item);
                        }
                    }

                    // 再处理添加的项
                    foreach (TrainRideInfo item in e.AddedItems)
                    {
                        item.IsSelected = true;
                        if (!_viewModel.SelectedTickets.Contains(item))
                        {
                            _viewModel.SelectedTickets.Add(item);
                        }
                    }
                    
                    // 手动通知ViewModel选择状态已变更
                    _viewModel.NotifySelectionChanged();
                }
            }
            finally
            {
                _isInternalSelectionChange = false;
            }
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
        /// 窗口加载完成事件处理
        /// </summary>
        private async void CollectionTicketsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载车票数据
            await _viewModel.LoadTicketsAsync();
        }

        /// <summary>
        /// 初始化页码提示框
        /// </summary>
        private void InitializePageNumberTooltip()
        {
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
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;

            if (pageInfoPanel != null && pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                pageInfoPanel.Visibility = Visibility.Collapsed;
                pageNumberInput.Visibility = Visibility.Visible;

                // 设置当前页码为默认值
                if (_viewModel != null)
                {
                    pageNumberInput.Text = _viewModel.PaginationViewModel.CurrentPage.ToString();
                }

                // 聚焦并全选
                pageNumberInput.Focus();
                pageNumberInput.SelectAll();
            }
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
                var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
                
                if (pageInfoPanel != null && pageNumberInput != null)
                {
                    pageInfoPanel.Visibility = Visibility.Visible;
                    pageNumberInput.Visibility = Visibility.Collapsed;
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
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            
            if (pageInfoPanel != null && pageNumberInput != null)
            {
                pageInfoPanel.Visibility = Visibility.Visible;
                pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 尝试导航到指定页码
        /// </summary>
        private void TryNavigateToPage()
        {
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            
            if (pageInfoPanel == null || pageNumberInput == null || _viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= _viewModel.PaginationViewModel.TotalPages)
                {
                    // 设置新的页码
                    _viewModel.PaginationViewModel.CurrentPage = pageNumber;
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {_viewModel.PaginationViewModel.TotalPages} 之间";
                    _pageNumberTooltip.PlacementTarget = pageNumberInput;
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
                    pageNumberInput.Text = _viewModel.PaginationViewModel.CurrentPage.ToString();
                    pageNumberInput.SelectAll();
                    return;
                }
            }

            // 恢复显示页码信息
            pageInfoPanel.Visibility = Visibility.Visible;
            pageNumberInput.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 处理ViewModel选择变更事件，更新DataGrid的选择状态
        /// </summary>
        private void ViewModel_SelectionChanged(object sender, CollectionTicketsViewModel.TicketSelectionChangedEventArgs e)
        {
            try
            {
                _isInternalSelectionChange = true;
                
                // 清除之前对应的选择项
                foreach (var item in e.RemovedItems)
                {
                    if (TicketsDataGrid.SelectedItems.Contains(item))
                    {
                        TicketsDataGrid.SelectedItems.Remove(item);
                    }
                }
                
                // 添加新的选择项
                foreach (var item in e.AddedItems)
                {
                    if (!TicketsDataGrid.SelectedItems.Contains(item))
                    {
                        TicketsDataGrid.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                _isInternalSelectionChange = false;
            }
        }
    }
}