using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

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
            EditStationCommand = new RelayCommand(EditSelectedStation, () => CanEditStation);

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

        /// <summary>
        /// 是否可以编辑车站
        /// </summary>
        public bool CanEditStation => _selectedStations.Count == 1;

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
        /// 编辑车站命令
        /// </summary>
        public ICommand EditStationCommand { get; }

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

                Debug.WriteLine($"刷新数据 - 总记录数: {TotalCount}，当前页: {PaginationViewModel.CurrentPage}，每页数量: {PaginationViewModel.PageSize}");

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

                Debug.WriteLine($"获取到分页数据 - 数量: {stations.Count}");

                // 如果获取的数据超过页面大小，进行截断
                if (stations.Count > PaginationViewModel.PageSize)
                {
                    Debug.WriteLine($"警告：获取的数据量({stations.Count})超过页面大小({PaginationViewModel.PageSize})，将进行截断");
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
                        Debug.WriteLine($"警告：检测到重复ID的数据项({station.Id})，已跳过");
                    }
                }

                Debug.WriteLine($"数据加载完成 - Stations集合大小: {Stations.Count}");

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
            Debug.WriteLine($"开始全选操作 - 当前页数据项数量: {expectedCount}");

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
                    Debug.WriteLine($"警告：选择数量与当前页项数不一致！");
                }

                OnPropertyChanged(nameof(SelectedStations));
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(Stations)); // 强制刷新整个列表
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"全选操作异常: {ex.Message}");
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
                Debug.WriteLine($"获取DataGrid异常: {ex.Message}");
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
            Debug.WriteLine($"开始反选操作 - 当前选中项数量: {_selectedStations.Count}, 当前页项数: {Stations.Count}");

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
            Debug.WriteLine($"反选完成 - 新选中项数量: {SelectedItemsCount}");

            OnPropertyChanged(nameof(SelectedStations));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(Stations)); // 强制刷新整个列表
        }

        /// <summary>
        /// 显示添加车站对话框
        /// </summary>
        private void ShowAddStation()
        {
            try
            {
                // 创建StationSearchService
                var stationSearchService = new StationSearchService(_databaseService);

                // 创建并显示AddStationsToRouteWindow
                var addStationWindow = new AddStationsToRouteWindow(
                    _route,
                    _databaseService,
                    stationSearchService,
                    _mainViewModel,
                    async () => await RefreshDataAsync()
                );

                if (Application.Current.MainWindow != null)
                {
                    addStationWindow.Owner = Application.Current.MainWindow;
                }

                addStationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"打开添加车站窗口失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"打开添加车站窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以移除车站
        /// </summary>
        private bool CanRemoveStations()
        {
            // 没有选中项时不可删除
            if (!HasSelectedItems)
                return false;

            // 选中的项中包含起点站时不可删除
            if (_selectedStations.Any(s => (s.StationRole & 1) == 1)) // 1代表起点站
                return false;

            return true;
        }

        /// <summary>
        /// 移除选中的车站
        /// </summary>
        private async void RemoveSelectedStations()
        {
            if (_selectedStations == null || _selectedStations.Count == 0)
            {
                return;
            }

            // 检查是否选中了起点站
            if (_selectedStations.Any(s => (s.StationRole & 1) == 1)) // 1代表起点站
            {
                MessageBoxHelper.ShowWarning("起点站不能被删除，请取消选择起点站后再试");
                return;
            }

            string confirmMessage;

            if (_selectedStations.Count == 1)
            {
                // 获取车站信息
                var station = await _databaseService.GetStationByIdAsync(_selectedStations[0].StationId);
                string stationName = station?.StationName ?? "未知车站";

                confirmMessage = $"确定要删除 {stationName} 吗？此操作不可撤销。";
            }
            else
            {
                confirmMessage = $"确定要删除选中的 {_selectedStations.Count} 个车站吗？此操作不可撤销。";
            }

            // 显示确认对话框
            MessageBoxResult result = MessageBoxHelper.ShowConfirmation(confirmMessage);

            // 如果用户确认删除
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    // 收集要删除的车站映射ID
                    var stationMappingIds = _selectedStations.Select(s => s.Id).ToList();

                    // 调用服务执行删除
                    bool success = await _databaseService.DeleteRouteStationsByIdsAsync(stationMappingIds);

                    if (success)
                    {
                        // 刷新列表
                        await RefreshDataAsync();
                        MessageBoxHelper.ShowInfo("删除成功");

                        // 检查删除后是否还有终点站
                        bool hasEndStation = _stations.Any(s => (s.StationRole & 2) == 2); // 2代表终点站

                        // 如果没有终点站但有车站，提示用户将最后一个车站设为终点
                        if (!hasEndStation && _stations.Count > 0)
                        {
                            var lastStation = _stations.Last();
                            string stationName = lastStation.Station?.StationName ?? "最后一个车站";

                            MessageBoxResult endStationResult = MessageBoxHelper.ShowConfirmation(
                                $"路线必须有一个终点站，是否将 {stationName} 设为终点站？");

                            if (endStationResult == MessageBoxResult.Yes)
                            {
                                // 更新最后一个车站为终点站
                                lastStation.StationRole |= 2; // 添加终点站角色

                                // 确保IsEndStation属性也被正确设置
                                lastStation.IsEndStation = true;

                                // 更新StationRoleText属性
                                lastStation.UpdateStationRoleText();

                                // 保存变更
                                bool updateSuccess = await _databaseService.UpdateRouteStationAsync(lastStation);

                                if (updateSuccess)
                                {
                                    await RefreshDataAsync();
                                    MessageBoxHelper.ShowInfo($"已将 {stationName} 设为终点站");
                                }
                                else
                                {
                                    MessageBoxHelper.ShowError("设置终点站失败，请手动编辑车站角色");
                                }
                            }
                            else
                            {
                                MessageBoxHelper.ShowWarning("注意：路线必须有一个终点站，请手动设置一个终点站");
                            }
                        }
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("删除失败，请稍后重试");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"删除车站失败: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"删除失败: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 同步选择状态
        /// </summary>
        public void SynchronizeSelectionStates()
        {
            Debug.WriteLine($"开始同步选择状态 - 当前SelectedStations数量: {_selectedStations.Count}");

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
                Debug.WriteLine($"警告：选择数量({_selectedStations.Count})超过当前页项目数({Stations.Count})，将进行截断");

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

            Debug.WriteLine($"同步选择状态完成 - 当前页选中项: {currentPageSelectedCount}, SelectedStations: {_selectedStations.Count}, SelectedItemsCount: {SelectedItemsCount}");

            // 验证选择数量
            if (currentPageSelectedCount != SelectedItemsCount)
            {
                Debug.WriteLine($"警告：选择数量不一致！当前页选中: {currentPageSelectedCount}, 选择集合: {SelectedItemsCount}");
            }

            OnPropertyChanged(nameof(SelectedStations));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(CanEditStation));

            // 刷新RemoveStationsCommand的CanExecute状态
            (RemoveStationsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 更新选中项数量
        /// </summary>
        public void UpdateSelectedItemsCount()
        {
            SelectedItemsCount = _selectedStations.Count;
            HasSelectedItems = SelectedItemsCount > 0;
            OnPropertyChanged(nameof(CanEditStation));

            // 刷新RemoveStationsCommand的CanExecute状态
            (RemoveStationsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 编辑选中的车站
        /// </summary>
        private void EditSelectedStation()
        {
            try
            {
                // 确保只选择了一个车站
                if (_selectedStations.Count != 1)
                {
                    MessageBoxHelper.ShowInfo("请选择一个车站进行编辑");
                    return;
                }

                // 获取选中的车站
                var selectedStation = _selectedStations.First();

                // 创建StationSearchService
                var stationSearchService = new StationSearchService(_databaseService);

                // 创建并显示EditStationToRouteWindow
                var editStationWindow = new EditStationToRouteWindow(
                    _route,
                    selectedStation,
                    _databaseService,
                    stationSearchService,
                    _mainViewModel,
                    async () => await RefreshDataAsync()
                );

                if (Application.Current.MainWindow != null)
                {
                    editStationWindow.Owner = Application.Current.MainWindow;
                }

                editStationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"打开编辑车站窗口失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"打开编辑车站窗口失败: {ex.Message}");
            }
        }

        #endregion
    }
}