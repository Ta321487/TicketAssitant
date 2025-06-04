using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows;

namespace TA_WPF.ViewModels
{
    public class RouteDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private PaginationViewModel _paginationViewModel;
        private RouteInfo _route;
        private bool _isLoading;
        private int _totalCount;
        private int _selectedItemsCount;
        private bool _hasSelectedItems;
        private double _dataGridRowHeight = 45; // 默认行高为45

        // 标签页数据集合 - 为后续实现准备
        private RouteTicketViewModel _tickets;
        private ObservableCollection<object> _stations;
        private object _statistics;

        public RouteDetailViewModel(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            // 初始化服务和数据
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _route = route ?? throw new ArgumentNullException(nameof(route));

            // 初始化分页控制器
            _paginationViewModel = new PaginationViewModel();
            _paginationViewModel.PageChanged += async (s, e) => await RefreshDataAsync();
            _paginationViewModel.PageSizeChanged += async (s, e) => await RefreshDataAsync();

            // 初始化数据集合
            _tickets = new RouteTicketViewModel(route, databaseService, mainViewModel);
            _stations = new ObservableCollection<object>();

            // 设置分页控制器为已初始化状态，防止初次加载时不触发事件
            _tickets.PaginationViewModel.IsInitialized = true;

            // 初始化命令
            CloseCommand = new RelayCommand(Close);
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
        }

        #region 属性

        // 窗口标题
        public string WindowTitle => $"路线详情 - {_route?.RouteName ?? "未知路线"}";

        // 主ViewModel引用，用于绑定字体大小等全局设置
        public MainViewModel MainViewModel => _mainViewModel;

        // 当前路线信息
        public RouteInfo Route
        {
            get => _route;
            set
            {
                if (_route != value)
                {
                    _route = value;
                    OnPropertyChanged(nameof(Route));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        // 分页控制器
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        // 数据总数
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged(nameof(TotalCount));
                    _paginationViewModel.TotalItems = value;
                }
            }
        }

        // 选中项数量
        public int SelectedItemsCount
        {
            get => _selectedItemsCount;
            set
            {
                if (_selectedItemsCount != value)
                {
                    _selectedItemsCount = value;
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    HasSelectedItems = value > 0;
                }
            }
        }

        // 是否有选中项
        public bool HasSelectedItems
        {
            get => _hasSelectedItems;
            set
            {
                if (_hasSelectedItems != value)
                {
                    _hasSelectedItems = value;
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        // 数据行高度
        public double DataGridRowHeight
        {
            get => _dataGridRowHeight;
            set
            {
                if (_dataGridRowHeight != value)
                {
                    _dataGridRowHeight = value;
                    OnPropertyChanged(nameof(DataGridRowHeight));
                }
            }
        }

        // 加载状态
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        // 车票列表视图模型
        public RouteTicketViewModel Tickets
        {
            get => _tickets;
            set
            {
                if (_tickets != value)
                {
                    _tickets = value;
                    OnPropertyChanged(nameof(Tickets));
                }
            }
        }

        // 车站列表
        public ObservableCollection<object> Stations
        {
            get => _stations;
            set
            {
                if (_stations != value)
                {
                    _stations = value;
                    OnPropertyChanged(nameof(Stations));
                }
            }
        }

        // 统计摘要
        public object Statistics
        {
            get => _statistics;
            set
            {
                if (_statistics != value)
                {
                    _statistics = value;
                    OnPropertyChanged(nameof(Statistics));
                }
            }
        }

        // 是否有数据
        public bool HasData => _tickets != null;

        // 是否没有数据
        public bool HasNoData => _tickets == null;

        #endregion

        #region 命令

        public ICommand CloseCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region 方法

        // 关闭窗口
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // 刷新数据 - 可以公开调用
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;

                // 设置分页控制器为已初始化状态
                _paginationViewModel.IsInitialized = true;

                // 加载车票数据
                await _tickets.RefreshDataAsync();
                
                // 更新UI状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                
                // 这里应该有更多的逻辑，比如:
                // await LoadStationsAsync();
                // await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新路线详情数据失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"刷新数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        // 关闭窗口事件
        public event EventHandler CloseRequested;
    }
} 