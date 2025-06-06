using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
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
        private readonly ThemeService _themeService;
        private int _currentTabIndex = 0; // 当前选中的标签页索引，默认为车票列表

        public RouteDetailWindow(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();

            // 创建ViewModel并设置为DataContext
            _viewModel = new RouteDetailViewModel(route, databaseService, mainViewModel);
            DataContext = _viewModel;

            // 获取主题服务
            _themeService = ThemeService.Instance;

            // 应用当前主题
            ApplyTheme(_viewModel.MainViewModel.IsDarkMode);

            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;

            // 注册关闭事件
            _viewModel.CloseRequested += ViewModel_CloseRequested;

            // 窗口加载完成后加载数据
            Loaded += RouteDetailWindow_Loaded;

            // 初始化页码提示工具提示
            InitializePageComponents();

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
        }

        private void RouteDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后的初始化操作
            
            // 初始化分页控件数据绑定
            UpdatePaginationBindings();
            
            // 确保ViewModel已完成数据加载
            if (_viewModel != null)
            {
                // 刷新数据（如果需要）
                // _ = _viewModel.RefreshDataAsync();
            }
        }

        private void ViewModel_CloseRequested(object sender, EventArgs e)
        {
            // 关闭窗口
            Close();
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
            ApplyTheme(isDarkMode);
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消注册事件
            _viewModel.CloseRequested -= ViewModel_CloseRequested;
            _themeService.ThemeChanged -= OnThemeChanged;
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
        /// 处理标签页切换事件
        /// </summary>
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl == null || _viewModel == null)
                return;

            // 保存当前选中的标签页索引
            _currentTabIndex = MainTabControl.SelectedIndex;

            // 根据选中的标签页更新分页控件的数据绑定
            UpdatePaginationBindings();
        }

        /// <summary>
        /// 更新分页控件的数据绑定
        /// </summary>
        private void UpdatePaginationBindings()
        {
            if (_viewModel == null)
                return;

            // 清除旧的绑定
            BindingOperations.ClearBinding(PageSizeComboBox, ComboBox.ItemsSourceProperty);
            BindingOperations.ClearBinding(PageSizeComboBox, ComboBox.SelectedItemProperty);
            BindingOperations.ClearBinding(TotalCountTextBlock, TextBlock.TextProperty);
            BindingOperations.ClearBinding(SelectedItemsTextBlock, TextBlock.TextProperty);
            BindingOperations.ClearBinding(SelectedItemsTextBlock, TextBlock.VisibilityProperty);
            BindingOperations.ClearBinding(FirstPageButton, Button.CommandProperty);
            BindingOperations.ClearBinding(FirstPageButton, Button.IsEnabledProperty);
            BindingOperations.ClearBinding(PreviousPageButton, Button.CommandProperty);
            BindingOperations.ClearBinding(PreviousPageButton, Button.IsEnabledProperty);
            BindingOperations.ClearBinding(NextPageButton, Button.CommandProperty);
            BindingOperations.ClearBinding(NextPageButton, Button.IsEnabledProperty);
            BindingOperations.ClearBinding(LastPageButton, Button.CommandProperty);
            BindingOperations.ClearBinding(LastPageButton, Button.IsEnabledProperty);
            BindingOperations.ClearBinding(CurrentPageTextBlock, TextBlock.TextProperty);
            BindingOperations.ClearBinding(TotalPagesTextBlock, TextBlock.TextProperty);

            // 根据当前选中的标签页创建新的绑定
            switch (_currentTabIndex)
            {
                case 0: // 车票列表
                    SetTicketsBindings();
                    break;
                case 1: // 车站列表
                    SetStationsBindings();
                    break;
                case 2: // 统计摘要（无分页功能）
                default:
                    // 隐藏分页控件或使用默认的车票绑定
                    SetTicketsBindings();
                    break;
            }
        }

        /// <summary>
        /// 设置车票数据的绑定
        /// </summary>
        private void SetTicketsBindings()
        {
            // 页大小下拉框
            PageSizeComboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Tickets.PaginationViewModel.PageSizeOptions"));
            PageSizeComboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("Tickets.PaginationViewModel.PageSize") 
            { 
                Mode = BindingMode.TwoWay, 
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged 
            });

            // 总记录数
            TotalCountTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Tickets.TotalCount") 
            { 
                StringFormat = "总记录数: {0}" 
            });

            // 选中项数量
            SelectedItemsTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Tickets.SelectedItemsCount") 
            { 
                StringFormat = "已选择 {0} 项" 
            });
            SelectedItemsTextBlock.SetBinding(TextBlock.VisibilityProperty, new Binding("Tickets.HasSelectedItems") 
            { 
                Converter = (System.Windows.Data.IValueConverter)FindResource("BooleanToVisibilityConverter") 
            });

            // 分页按钮
            FirstPageButton.SetBinding(Button.CommandProperty, new Binding("Tickets.PaginationViewModel.FirstPageCommand"));
            FirstPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Tickets.PaginationViewModel.CanNavigateToFirstPage"));
            PreviousPageButton.SetBinding(Button.CommandProperty, new Binding("Tickets.PaginationViewModel.PreviousPageCommand"));
            PreviousPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Tickets.PaginationViewModel.CanNavigateToPreviousPage"));
            NextPageButton.SetBinding(Button.CommandProperty, new Binding("Tickets.PaginationViewModel.NextPageCommand"));
            NextPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Tickets.PaginationViewModel.CanNavigateToNextPage"));
            LastPageButton.SetBinding(Button.CommandProperty, new Binding("Tickets.PaginationViewModel.LastPageCommand"));
            LastPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Tickets.PaginationViewModel.CanNavigateToLastPage"));

            // 页码显示
            CurrentPageTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Tickets.PaginationViewModel.CurrentPage"));
            TotalPagesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Tickets.PaginationViewModel.TotalPages"));
        }

        /// <summary>
        /// 设置车站数据的绑定
        /// </summary>
        private void SetStationsBindings()
        {
            // 页大小下拉框
            PageSizeComboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Stations.PaginationViewModel.PageSizeOptions"));
            PageSizeComboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("Stations.PaginationViewModel.PageSize") 
            { 
                Mode = BindingMode.TwoWay, 
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged 
            });

            // 总记录数
            TotalCountTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Stations.TotalCount") 
            { 
                StringFormat = "总记录数: {0}" 
            });

            // 选中项数量
            SelectedItemsTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Stations.SelectedItemsCount") 
            { 
                StringFormat = "已选择 {0} 项" 
            });
            SelectedItemsTextBlock.SetBinding(TextBlock.VisibilityProperty, new Binding("Stations.HasSelectedItems") 
            { 
                Converter = (System.Windows.Data.IValueConverter)FindResource("BooleanToVisibilityConverter") 
            });

            // 分页按钮
            FirstPageButton.SetBinding(Button.CommandProperty, new Binding("Stations.PaginationViewModel.FirstPageCommand"));
            FirstPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Stations.PaginationViewModel.CanNavigateToFirstPage"));
            PreviousPageButton.SetBinding(Button.CommandProperty, new Binding("Stations.PaginationViewModel.PreviousPageCommand"));
            PreviousPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Stations.PaginationViewModel.CanNavigateToPreviousPage"));
            NextPageButton.SetBinding(Button.CommandProperty, new Binding("Stations.PaginationViewModel.NextPageCommand"));
            NextPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Stations.PaginationViewModel.CanNavigateToNextPage"));
            LastPageButton.SetBinding(Button.CommandProperty, new Binding("Stations.PaginationViewModel.LastPageCommand"));
            LastPageButton.SetBinding(Button.IsEnabledProperty, new Binding("Stations.PaginationViewModel.CanNavigateToLastPage"));

            // 页码显示
            CurrentPageTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Stations.PaginationViewModel.CurrentPage"));
            TotalPagesTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Stations.PaginationViewModel.TotalPages"));
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
            if (_viewModel != null)
            {
                if (_currentTabIndex == 0 && _viewModel.Tickets != null && _viewModel.Tickets.PaginationViewModel != null)
                {
                    PageNumberInput.Text = _viewModel.Tickets.PaginationViewModel.CurrentPage.ToString();
                }
                else if (_currentTabIndex == 1 && _viewModel.Stations != null && _viewModel.Stations.PaginationViewModel != null)
                {
                    PageNumberInput.Text = _viewModel.Stations.PaginationViewModel.CurrentPage.ToString();
                }
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
            if (PageInfoPanel == null || PageNumberInput == null || _viewModel == null)
                return;

            // 根据当前选中的标签页获取相应的PaginationViewModel
            var paginationViewModel = _currentTabIndex == 0 ? 
                _viewModel.Tickets?.PaginationViewModel : 
                _viewModel.Stations?.PaginationViewModel;

            if (paginationViewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(PageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= paginationViewModel.TotalPages)
                {
                    // 设置新的页码
                    paginationViewModel.CurrentPage = pageNumber;

                    // 确保页码变更后触发数据加载
                    paginationViewModel.IsInitialized = true;

                    // 根据当前标签页刷新数据
                    if (_currentTabIndex == 0)
                    {
                        _ = _viewModel.Tickets.RefreshDataAsync();
                    }
                    else if (_currentTabIndex == 1)
                    {
                        _ = _viewModel.Stations.RefreshDataAsync();
                    }
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {paginationViewModel.TotalPages} 之间";
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
                    PageNumberInput.Text = paginationViewModel.CurrentPage.ToString();
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