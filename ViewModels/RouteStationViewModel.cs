using System.Collections.ObjectModel;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class RouteStationViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private PaginationViewModel _paginationViewModel;
        private RouteInfo _route;
        private ObservableCollection<RouteStationMapping> _stations;
        private ObservableCollection<RouteStationMapping> _selectedStations;
        private RouteStationMapping _selectedStation;
        private bool _isLoading;
        private int _totalCount;
        private bool _hasSelectedItems;
        private int _selectedItemsCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RouteStationViewModel(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            // 初始化服务和数据
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _route = route ?? throw new ArgumentNullException(nameof(route));

            // 初始化分页控制器
            _paginationViewModel = new PaginationViewModel();
            _paginationViewModel.PageChanged += async (s, e) =>
            {
                // 页面变更时清空选择状态
                _selectedStations.Clear();
                SelectedItemsCount = 0;
                await RefreshDataAsync();
            };

            _paginationViewModel.PageSizeChanged += async (s, e) =>
            {
                // 页面大小变更时清空选择状态
                _selectedStations.Clear();
                SelectedItemsCount = 0;
                await RefreshDataAsync();
            };

            // 初始化集合
            _stations = new ObservableCollection<RouteStationMapping>();
            _selectedStations = new ObservableCollection<RouteStationMapping>();

            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            SelectAllCommand = new RelayCommand(SelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection);
            AddStationCommand = new RelayCommand(ShowAddStation);
            RemoveStationsCommand = new RelayCommand(RemoveSelectedStations, CanRemoveStations);
            MoveUpCommand = new RelayCommand(MoveStationsUp, CanMoveUp);
            MoveDownCommand = new RelayCommand(MoveStationsDown, CanMoveDown);

            // 初始加载数据
            _ = InitializeAsync();
        }

        /// <summary>
        /// 初始化并加载数据
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // 设置分页控制器为已初始化状态
                PaginationViewModel.IsInitialized = true;

                // 加载数据
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"初始化路线车站数据失败: {ex.Message}", ex);
            }
        }

        #region 属性

        /// <summary>
        /// 主ViewModel引用，用于绑定字体大小等全局设置
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 路线信息
        /// </summary>
        public RouteInfo Route
        {
            get => _route;
            set
            {
                if (_route != value)
                {
                    _route = value;
                    OnPropertyChanged(nameof(Route));
                }
            }
        }

        /// <summary>
        /// 车站列表
        /// </summary>
        public ObservableCollection<RouteStationMapping> Stations
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

        /// <summary>
        /// 选中的车站列表
        /// </summary>
        public ObservableCollection<RouteStationMapping> SelectedStations
        {
            get => _selectedStations;
            set
            {
                if (_selectedStations != value)
                {
                    _selectedStations = value;
                    OnPropertyChanged(nameof(SelectedStations));
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        /// <summary>
        /// 选中的车站
        /// </summary>
        public RouteStationMapping SelectedStation
        {
            get => _selectedStation;
            set
            {
                if (_selectedStation != value)
                {
                    _selectedStation = value;
                    OnPropertyChanged(nameof(SelectedStation));
                }
            }
        }

        /// <summary>
        /// 分页控制器
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        /// <summary>
        /// 是否正在加载
        /// </summary>
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

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged(nameof(TotalCount));
                    PaginationViewModel.TotalItems = value;
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
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

        /// <summary>
        /// 选中项数量
        /// </summary>
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

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => Stations != null && Stations.Count > 0;

        /// <summary>
        /// 是否没有数据
        /// </summary>
        public bool HasNoData => Stations == null || Stations.Count == 0;

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 全选命令
        /// </summary>
        public ICommand SelectAllCommand { get; }

        /// <summary>
        /// 取消选择命令
        /// </summary>
        public ICommand UnselectAllCommand { get; }

        /// <summary>
        /// 反选命令
        /// </summary>
        public ICommand InvertSelectionCommand { get; }

        /// <summary>
        /// 添加车站命令
        /// </summary>
        public ICommand AddStationCommand { get; }

        /// <summary>
        /// 移除车站命令
        /// </summary>
        public ICommand RemoveStationsCommand { get; }

        /// <summary>
        /// 上移命令
        /// </summary>
        public ICommand MoveUpCommand { get; }

        /// <summary>
        /// 下移命令
        /// </summary>
        public ICommand MoveDownCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;

                // 获取总数
                TotalCount = await _databaseService.GetRouteStationsCountAsync(_route.Id);

                System.Diagnostics.Debug.WriteLine($"刷新数据 - 总记录数: {TotalCount}，当前页: {PaginationViewModel.CurrentPage}，每页数量: {PaginationViewModel.PageSize}");

                // 设置分页控制器状态
                if (!PaginationViewModel.IsInitialized)
                {
                    PaginationViewModel.IsInitialized = true;
                }

                // 确保总记录数被正确设置
                PaginationViewModel.TotalItems = TotalCount;

                // 获取分页数据
                var stations = await _databaseService.GetRouteStationsAsync(
                    _route.Id,
                    PaginationViewModel.CurrentPage,
                    PaginationViewModel.PageSize);

                System.Diagnostics.Debug.WriteLine($"获取到分页数据 - 数量: {stations.Count}");

                // 如果获取的数据超过页面大小，进行截断
                if (stations.Count > PaginationViewModel.PageSize)
                {
                    System.Diagnostics.Debug.WriteLine($"警告：获取的数据量({stations.Count})超过页面大小({PaginationViewModel.PageSize})，将进行截断");
                    stations = stations.Take(PaginationViewModel.PageSize).ToList();
                }

                // 清空选中状态
                _selectedStations.Clear();
                SelectedItemsCount = 0;
                OnPropertyChanged(nameof(HasSelectedItems));

                // 清空并重新加载数据
                Stations.Clear();

                // 使用HashSet跟踪已添加的项ID，防止重复
                var addedIds = new HashSet<int>();

                foreach (var station in stations)
                {
                    // 确保IsSelected属性初始化为false
                    station.IsSelected = false;

                    // 防止重复添加相同ID的项
                    if (!addedIds.Contains(station.Id))
                    {
                        Stations.Add(station);
                        addedIds.Add(station.Id);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"警告：检测到重复ID的数据项({station.Id})，已跳过");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"数据加载完成 - Stations集合大小: {Stations.Count}");

                // 更新UI状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(SelectedStations));

                // 强制更新分页状态
                PaginationViewModel.NotifyCurrentPageChanged();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新路线车站数据失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"刷新数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            // 获取当前页面的项数
            int expectedCount = Stations.Count;
            System.Diagnostics.Debug.WriteLine($"开始全选操作 - 当前页数据项数量: {expectedCount}");

            try
            {
                // DataGrid的SelectAll方法会触发多次SelectionChanged事件
                // 这里不需要手动添加项，让SelectionChanged事件处理程序来处理选择状态
                var dataGrid = GetDataGrid();
                if (dataGrid != null)
                {
                    dataGrid.SelectAll();
                    return;
                }

                // 如果无法获取DataGrid，则使用备用方法手动设置选择状态
                // 清空选择集合，避免重复项
                _selectedStations.Clear();

                // 防止重复添加的HashSet
                var addedItems = new HashSet<int>();

                // 为当前页所有项设置选中状态
                foreach (var station in Stations)
                {
                    // 确保只将每个项添加一次（防止重复）
                    if (!addedItems.Contains(station.Id))
                    {
                        station.IsSelected = true;
                        _selectedStations.Add(station);
                        addedItems.Add(station.Id);
                    }
                }

                // 更新UI和计数
                SelectedItemsCount = _selectedStations.Count;

                // 验证选择数量是否与当前页项数一致
                if (SelectedItemsCount != expectedCount)
                {
                    System.Diagnostics.Debug.WriteLine($"警告：选择数量与当前页项数不一致！");
                }

                OnPropertyChanged(nameof(SelectedStations));
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(Stations)); // 强制刷新整个列表
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"全选操作异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试获取DataGrid控件
        /// </summary>
        private System.Windows.Controls.DataGrid GetDataGrid()
        {
            try
            {
                // 尝试从可视化树中查找DataGrid
                var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
                if (window != null)
                {
                    return FindDataGrid(window);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取DataGrid异常: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 递归查找DataGrid控件
        /// </summary>
        private System.Windows.Controls.DataGrid FindDataGrid(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.DataGrid dataGrid)
                {
                    // 确认这是我们的DataGrid
                    if (dataGrid.Name == "StationsDataGrid")
                    {
                        return dataGrid;
                    }
                }

                var result = FindDataGrid(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        private void UnselectAll()
        {
            // 为所有项设置未选中状态
            foreach (var station in Stations)
            {
                station.IsSelected = false;
            }

            // 清空选择集合
            _selectedStations.Clear();
            SelectedItemsCount = 0;

            // 更新UI
            OnPropertyChanged(nameof(SelectedStations));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(Stations)); // 强制刷新整个列表
        }

        /// <summary>
        /// 反选
        /// </summary>
        private void InvertSelection()
        {
            System.Diagnostics.Debug.WriteLine($"开始反选操作 - 当前选中项数量: {_selectedStations.Count}, 当前页项数: {Stations.Count}");

            // 新建临时集合存储将要选中的项
            var newSelection = new List<RouteStationMapping>();
            var addedIds = new HashSet<int>();

            // 反转每一项的选中状态（仅限当前页）
            foreach (var station in Stations)
            {
                station.IsSelected = !station.IsSelected;
                if (station.IsSelected && !addedIds.Contains(station.Id))
                {
                    newSelection.Add(station);
                    addedIds.Add(station.Id);
                }
            }

            // 更新选中集合
            _selectedStations.Clear();
            foreach (var station in newSelection)
            {
                _selectedStations.Add(station);
            }

            // 更新UI和计数
            SelectedItemsCount = _selectedStations.Count;
            System.Diagnostics.Debug.WriteLine($"反选完成 - 新选中项数量: {SelectedItemsCount}");

            OnPropertyChanged(nameof(SelectedStations));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(Stations)); // 强制刷新整个列表
        }

        /// <summary>
        /// 显示添加车站对话框
        /// </summary>
        private void ShowAddStation()
        {
            // 添加车站功能暂未实现
            MessageBoxHelper.ShowInfo("添加车站功能尚未实现");
        }

        /// <summary>
        /// 是否可以移除车站
        /// </summary>
        private bool CanRemoveStations()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 移除选中的车站
        /// </summary>
        private void RemoveSelectedStations()
        {
            // 移除车站功能暂未实现
            MessageBoxHelper.ShowInfo("移除车站功能尚未实现");
        }

        /// <summary>
        /// 是否可以上移车站
        /// </summary>
        private bool CanMoveUp()
        {
            // 暂未实现上移功能
            return HasSelectedItems;
        }

        /// <summary>
        /// 上移车站
        /// </summary>
        private void MoveStationsUp()
        {
            // 上移车站功能暂未实现
            MessageBoxHelper.ShowInfo("上移车站功能尚未实现");
        }

        /// <summary>
        /// 是否可以下移车站
        /// </summary>
        private bool CanMoveDown()
        {
            // 暂未实现下移功能
            return HasSelectedItems;
        }

        /// <summary>
        /// 下移车站
        /// </summary>
        private void MoveStationsDown()
        {
            // 下移车站功能暂未实现
            MessageBoxHelper.ShowInfo("下移车站功能尚未实现");
        }

        /// <summary>
        /// 同步选择状态
        /// </summary>
        public void SynchronizeSelectionStates()
        {
            System.Diagnostics.Debug.WriteLine($"开始同步选择状态 - 当前SelectedStations数量: {_selectedStations.Count}");

            // 清空并重建选择集合
            _selectedStations.Clear();

            // 记录当前页选中项
            int currentPageSelectedCount = 0;

            // 用于防止重复添加的HashSet
            var addedIds = new HashSet<int>();

            // 从当前页数据项中收集选中的项
            foreach (var station in Stations)
            {
                if (station.IsSelected && !addedIds.Contains(station.Id))
                {
                    _selectedStations.Add(station);
                    addedIds.Add(station.Id);
                    currentPageSelectedCount++;
                }
            }

            // 确保总数不超过当前页的项目数
            if (_selectedStations.Count > Stations.Count)
            {
                System.Diagnostics.Debug.WriteLine($"警告：选择数量({_selectedStations.Count})超过当前页项目数({Stations.Count})，将进行截断");

                // 清空集合并重新添加
                var tempList = _selectedStations.Take(Stations.Count).ToList();
                _selectedStations.Clear();

                foreach (var station in tempList)
                {
                    _selectedStations.Add(station);
                }
            }

            // 更新计数和UI
            SelectedItemsCount = _selectedStations.Count;

            System.Diagnostics.Debug.WriteLine($"同步选择状态完成 - 当前页选中项: {currentPageSelectedCount}, SelectedStations: {_selectedStations.Count}, SelectedItemsCount: {SelectedItemsCount}");

            // 验证选择数量
            if (currentPageSelectedCount != SelectedItemsCount)
            {
                System.Diagnostics.Debug.WriteLine($"警告：选择数量不一致！当前页选中: {currentPageSelectedCount}, 选择集合: {SelectedItemsCount}");
            }

            OnPropertyChanged(nameof(SelectedStations));
            OnPropertyChanged(nameof(HasSelectedItems));
        }

        /// <summary>
        /// 更新选中项数量
        /// </summary>
        public void UpdateSelectedItemsCount()
        {
            SelectedItemsCount = _selectedStations.Count;
            HasSelectedItems = SelectedItemsCount > 0;
        }

        #endregion
    }
}