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
    public class QueryAllRoutesViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;
        private readonly AdvancedQueryRouteViewModel _advancedQueryViewModel;

        private ObservableCollection<RouteInfo> _routes;
        private int _totalCount;
        private RouteInfo _selectedRoute;
        private ObservableCollection<RouteInfo> _selectedRoutes;
        private bool _isLoading;
        private double _dataGridRowHeight = 45; // 默认行高为45
        private string _currentRouteName;
        private DistanceRangeType _currentDistanceRange = DistanceRangeType.None;
        private bool _currentIsFavorite = false;
        private bool _currentIsAndCondition = true;
        // 添加DatabaseService属性，用于在页面中直接创建RouteDetailWindow
        public DatabaseService DatabaseService => _databaseService;

        public QueryAllRoutesViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            _advancedQueryViewModel = new AdvancedQueryRouteViewModel(databaseService);
            _advancedQueryViewModel.FilterApplied += AdvancedQueryViewModel_FilterApplied;

            _routes = new ObservableCollection<RouteInfo>();
            _selectedRoutes = new ObservableCollection<RouteInfo>();

            _paginationViewModel.PageChanged += async (s, e) => await LoadRoutesAsync();
            _paginationViewModel.PageSizeChanged += async (s, e) => await LoadRoutesAsync();

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadRoutesAsync());
            AddRouteCommand = new RelayCommand(AddRoute);
            EditRouteCommand = new RelayCommand<RouteInfo>(EditRoute);
            DeleteRouteCommand = new RelayCommand<RouteInfo>(DeleteRoute);
            DeleteRoutesCommand = new RelayCommand(DeleteSelectedRoutes);
            AdvancedQueryCommand = new RelayCommand(OpenAdvancedQuery);

            // 添加选择相关命令
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection, CanInvertSelection);

            // 添加双击命令
            DoubleClickEditCommand = new RelayCommand<RouteInfo>(DoubleClickEditRoute);

            // 添加路线详情命令
            ShowRouteDetailsCommand = new RelayCommand<RouteInfo>(ShowRouteDetails);
        }

        // 添加处理高级查询事件的方法
        private void AdvancedQueryViewModel_FilterApplied(object sender, RouteQueryFilterEventArgs e)
        {
            // 保存当前筛选条件
            _currentRouteName = e.RouteName;
            _currentDistanceRange = e.DistanceRange;
            _currentIsFavorite = e.IsFavorite;
            _currentIsAndCondition = e.IsAndCondition;

            // 检查是否所有条件都为空，此时应该查询所有数据
            bool allConditionsEmpty = string.IsNullOrWhiteSpace(e.RouteName) &&
                                     e.DistanceRange == DistanceRangeType.None &&
                                     !e.IsFavorite;

            // 重置到第一页
            _paginationViewModel.CurrentPage = 1;

            // 加载符合条件的数据
            _ = LoadRoutesAsync();
        }

        // 添加AdvancedQueryViewModel属性
        public AdvancedQueryRouteViewModel AdvancedQueryViewModel => _advancedQueryViewModel;

        // 添加MainViewModel属性，解决绑定错误
        public MainViewModel MainViewModel => _mainViewModel;

        // 添加DataGridRowHeight属性，解决绑定错误
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

        public ObservableCollection<RouteInfo> Routes
        {
            get => _routes;
            set
            {
                if (_routes != value)
                {
                    _routes = value;
                    OnPropertyChanged(nameof(Routes));
                }
            }
        }

        public RouteInfo SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                if (_selectedRoute != value)
                {
                    _selectedRoute = value;
                    OnPropertyChanged(nameof(SelectedRoute));
                    OnPropertyChanged(nameof(CanShowRouteDetails));
                }
            }
        }

        // 添加多选支持
        public ObservableCollection<RouteInfo> SelectedRoutes
        {
            get => _selectedRoutes;
            set
            {
                if (_selectedRoutes != value)
                {
                    _selectedRoutes = value;
                    OnPropertyChanged(nameof(SelectedRoutes));
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    OnPropertyChanged(nameof(CanEditSelectedRoute));
                    OnPropertyChanged(nameof(CanShowRouteDetails));
                }
            }
        }

        // 是否有选中的项
        public bool HasSelection => _selectedRoutes != null && _selectedRoutes.Count > 0;

        // 是否选中了全部项
        public bool IsAllSelected => _routes != null && _selectedRoutes != null &&
                                    _routes.Count > 0 && _routes.Count == _selectedRoutes.Count;

        // 选中项的数量，用于控制修改按钮的显示与启用状态
        public int SelectedItemsCount => _selectedRoutes?.Count ?? 0;

        // 是否可以编辑选中的路线（仅当选中一个路线时可编辑）
        public bool CanEditSelectedRoute => SelectedItemsCount == 1;

        // 是否可以显示路线详情（仅当选中一个路线时可显示）
        public bool CanShowRouteDetails => SelectedItemsCount == 1;

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged(nameof(TotalCount));
                    _paginationViewModel.TotalItems = value; // Update pagination
                }
            }
        }

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

        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        // 是否有数据（用于控制UI显示）
        public bool HasData => _routes != null && _routes.Count > 0;

        // 是否没有数据（用于控制"暂无数据"提示的显示）
        public bool HasNoData => _routes == null || _routes.Count == 0;

        // --- Commands ---
        public ICommand RefreshCommand { get; }
        public ICommand AddRouteCommand { get; }
        public ICommand EditRouteCommand { get; }
        public ICommand DeleteRouteCommand { get; }
        public ICommand DeleteRoutesCommand { get; }
        public ICommand AdvancedQueryCommand { get; }

        // 选择相关命令
        public ICommand SelectAllCommand { get; }
        public ICommand UnselectAllCommand { get; }
        public ICommand InvertSelectionCommand { get; }

        // 添加双击命令
        public ICommand DoubleClickEditCommand { get; }

        // 添加路线详情命令
        public ICommand ShowRouteDetailsCommand { get; }

        private void AddRoute()
        {
            var addRouteWindow = new AddRouteWindow(_databaseService, _mainViewModel);
            addRouteWindow.Owner = Application.Current.MainWindow;

            // 显示模态窗口
            var result = addRouteWindow.ShowDialog();

            // 如果添加成功，刷新列表
            if (result == true)
            {
                _ = LoadRoutesAsync();
            }
        }

        private void EditRoute(RouteInfo route)
        {
            if (route == null)
            {
                MessageBoxHelper.ShowInfo("请先选择一条路线");
                return;
            }

            var editRouteWindow = new EditRouteWindow(route, _databaseService, _mainViewModel);
            editRouteWindow.Owner = Application.Current.MainWindow;

            // 显示模态窗口
            var result = editRouteWindow.ShowDialog();

            // 如果编辑成功，刷新列表
            if (result == true)
            {
                _ = LoadRoutesAsync();
            }
        }

        private void DeleteRoute(RouteInfo route)
        {
            if (route != null)
            {
                // 将单个路线添加到选中集合并调用删除方法
                if (!SelectedRoutes.Contains(route))
                {
                    SelectedRoutes.Clear();
                    SelectedRoutes.Add(route);
                }
                DeleteSelectedRoutes();
            }
        }

        private async void DeleteSelectedRoutes()
        {
            if (SelectedRoutes == null || SelectedRoutes.Count == 0)
            {
                return;
            }

            string confirmMessage;
            if (SelectedRoutes.Count == 1)
            {
                confirmMessage = $"确定要删除路线\"{SelectedRoutes[0].RouteName}\"吗？此操作不可撤销。";
            }
            else
            {
                confirmMessage = $"确定要删除选中的 {SelectedRoutes.Count} 条路线吗？此操作不可撤销。";
            }

            // 显示确认对话框
            MessageBoxResult result = MessageBoxHelper.ShowConfirmation(confirmMessage);

            // 如果用户确认删除
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;

                    // 收集要删除的路线ID
                    var routeIds = SelectedRoutes.Select(r => r.Id).ToList();

                    // 调用服务执行删除
                    bool success = await _databaseService.DeleteRoutesByIdsAsync(routeIds);

                    if (success)
                    {
                        // 刷新列表
                        await LoadRoutesAsync();
                        MessageBoxHelper.ShowInfo("删除成功");
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("删除失败，请稍后重试");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"删除路线失败: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"删除失败: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void OpenAdvancedQuery()
        {
            // 切换高级查询面板的可见性
            _advancedQueryViewModel.ToggleQueryPanelCommand.Execute(null);
        }

        private void DoubleClickEditRoute(RouteInfo route)
        {
            // 调用编辑方法
            EditRoute(route);
        }

        // 显示路线详情
        private void ShowRouteDetails(RouteInfo route)
        {
            if (route == null)
            {
                route = SelectedRoute;
            }

            if (route != null)
            {
                // 每次都创建新的窗口实例，因为关闭后的窗口不能再次显示
                var routeDetailWindow = new RouteDetailWindow(route, _databaseService, _mainViewModel);
                routeDetailWindow.Owner = Application.Current.MainWindow;
                routeDetailWindow.ShowDialog();
            }
        }

        // --- 选择相关方法 ---
        public void SelectAll()
        {
            if (_routes == null || _routes.Count == 0)
                return;

            SelectedRoutes.Clear();
            foreach (var route in _routes)
            {
                SelectedRoutes.Add(route);
            }

            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));

            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new RouteSelectionChangedEventArgs(new List<RouteInfo>(), _routes.ToList()));
        }

        public bool CanSelectAll() => HasData && !IsAllSelected;

        public void UnselectAll()
        {
            if (_selectedRoutes == null || _selectedRoutes.Count == 0)
                return;

            // 备份当前选中项以便触发事件
            var previousSelected = new List<RouteInfo>(_selectedRoutes);

            SelectedRoutes.Clear();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));

            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new RouteSelectionChangedEventArgs(previousSelected, new List<RouteInfo>()));
        }

        public bool CanUnselectAll() => HasSelection;

        public void InvertSelection()
        {
            if (_routes == null || _routes.Count == 0)
                return;

            var currentSelection = new HashSet<RouteInfo>(_selectedRoutes);
            var toAdd = new List<RouteInfo>();
            var toRemove = new List<RouteInfo>(_selectedRoutes);

            foreach (var route in _routes)
            {
                if (!currentSelection.Contains(route))
                {
                    toAdd.Add(route);
                }
            }

            SelectedRoutes.Clear();

            foreach (var route in toAdd)
            {
                SelectedRoutes.Add(route);
            }

            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));

            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new RouteSelectionChangedEventArgs(toRemove, toAdd));
        }

        public bool CanInvertSelection() => HasData;

        // 事件用于通知View更新DataGrid的选中状态
        public event EventHandler<RouteSelectionChangedEventArgs> SelectionChanged;

        // 事件参数类
        public class RouteSelectionChangedEventArgs : EventArgs
        {
            public List<RouteInfo> RemovedItems { get; }
            public List<RouteInfo> AddedItems { get; }

            public RouteSelectionChangedEventArgs(List<RouteInfo> removedItems, List<RouteInfo> addedItems)
            {
                RemovedItems = removedItems;
                AddedItems = addedItems;
            }
        }

        // --- Data Loading ---
        public async Task QueryAllAsync()
        {
            // 重置筛选条件
            _currentRouteName = null;
            _currentDistanceRange = DistanceRangeType.None;
            _currentIsFavorite = false;
            _currentIsAndCondition = true;

            // 重置高级查询面板
            if (_advancedQueryViewModel != null)
            {
                _advancedQueryViewModel.ResetFilter();
            }

            _paginationViewModel.CurrentPage = 1; // Reset to first page
            await LoadRoutesAsync();
        }

        public async Task LoadRoutesAsync()
        {
            IsLoading = true;
            try
            {
                // 使用高级查询条件获取路线总数
                TotalCount = await GetFilteredRouteCountAsync();

                // 使用高级查询条件加载路线数据
                var routesData = await GetFilteredRoutesAsync();

                Routes = new ObservableCollection<RouteInfo>(routesData);

                // 清除选择
                SelectedRoutes.Clear();

                // 通知UI更新数据状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(IsAllSelected));
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载路线列表失败: {ex.Message}");
                MessageBoxHelper.ShowError($"加载路线列表失败: {ex.Message}");
                Routes.Clear();
                SelectedRoutes.Clear();
                TotalCount = 0;
                // 通知UI更新数据状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(IsAllSelected));
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 获取筛选的路线总数
        private async Task<int> GetFilteredRouteCountAsync()
        {
            try
            {
                // 构建SQL查询
                string query = BuildFilterQuerySQL(true);
                return await _databaseService.GetRouteCountByCustomQueryAsync(query);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取筛选路线总数失败: {ex.Message}", ex);
                return 0;
            }
        }

        // 获取筛选的路线数据
        private async Task<List<RouteInfo>> GetFilteredRoutesAsync()
        {
            try
            {
                // 构建SQL查询
                string query = BuildFilterQuerySQL(false);
                return await _databaseService.GetRoutesByCustomQueryAsync(
                    query,
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取筛选路线数据失败: {ex.Message}", ex);
                return new List<RouteInfo>();
            }
        }

        // 构建筛选SQL查询
        private string BuildFilterQuerySQL(bool isCountQuery)
        {
            // 所有非空条件列表
            var conditions = new List<string>();

            // 检查是否所有查询条件都为空（查询全部数据的情况）
            bool allConditionsEmpty = string.IsNullOrWhiteSpace(_currentRouteName) &&
                                      _currentDistanceRange == DistanceRangeType.None &&
                                      !_currentIsFavorite;

            // 如果要查询所有数据，就不添加任何条件
            if (!allConditionsEmpty)
            {
                if (_currentIsAndCondition)
                {
                    // AND条件模式 - 对未设置的条件使用IS NULL

                    // 处理路线名称条件
                    if (!string.IsNullOrWhiteSpace(_currentRouteName))
                    {
                        conditions.Add($"route_name LIKE '%{_currentRouteName}%'");
                    }
                    else
                    {
                        conditions.Add("route_name IS NULL");
                    }

                    // 处理总里程范围条件
                    if (_currentDistanceRange == DistanceRangeType.None)
                    {
                        conditions.Add("total_distance IS NULL");
                    }
                    else
                    {
                        // 有明确的距离范围选择
                        switch (_currentDistanceRange)
                        {
                            case DistanceRangeType.Range1: // 0-100公里
                                conditions.Add("(total_distance <= 100)");
                                break;
                            case DistanceRangeType.Range2: // 100-500公里
                                conditions.Add("(total_distance > 100 AND total_distance <= 500)");
                                break;
                            case DistanceRangeType.Range3: // 500-1000公里
                                conditions.Add("(total_distance > 500 AND total_distance <= 1000)");
                                break;
                            case DistanceRangeType.Range4: // 1000-2000公里
                                conditions.Add("(total_distance > 1000 AND total_distance <= 2000)");
                                break;
                            case DistanceRangeType.Range5: // 2000公里以上
                                conditions.Add("(total_distance > 2000)");
                                break;
                        }
                    }

                    // 处理收藏状态条件
                    if (_currentIsFavorite)
                    {
                        conditions.Add("is_favorite = 1");
                    }
                    else
                    {
                        conditions.Add("(is_favorite IS NULL OR is_favorite = 0)");
                    }
                }
                else
                {
                    // OR条件模式 - 只有设置了的条件才添加

                    // 处理路线名称条件（模糊匹配）
                    if (!string.IsNullOrWhiteSpace(_currentRouteName))
                    {
                        conditions.Add($"route_name LIKE '%{_currentRouteName}%'");
                    }

                    // 处理总里程范围条件
                    switch (_currentDistanceRange)
                    {
                        case DistanceRangeType.Range1: // 0-100公里
                            conditions.Add("(total_distance <= 100)");
                            break;
                        case DistanceRangeType.Range2: // 100-500公里
                            conditions.Add("(total_distance > 100 AND total_distance <= 500)");
                            break;
                        case DistanceRangeType.Range3: // 500-1000公里
                            conditions.Add("(total_distance > 500 AND total_distance <= 1000)");
                            break;
                        case DistanceRangeType.Range4: // 1000-2000公里
                            conditions.Add("(total_distance > 1000 AND total_distance <= 2000)");
                            break;
                        case DistanceRangeType.Range5: // 2000公里以上
                            conditions.Add("(total_distance > 2000)");
                            break;
                    }

                    // 处理收藏状态条件
                    if (_currentIsFavorite)
                    {
                        conditions.Add("is_favorite = 1");
                    }
                }
            }

            // 构建完整SQL查询
            string sql;
            if (isCountQuery)
            {
                sql = "SELECT COUNT(*) FROM route_info";
            }
            else
            {
                sql = "SELECT * FROM route_info";
            }

            // 添加WHERE子句
            if (conditions.Count > 0)
            {
                sql += " WHERE ";

                // 根据条件组合方式连接条件
                string connector = _currentIsAndCondition ? " AND " : " OR ";
                sql += string.Join(connector, conditions);
            }

            // 添加排序和分页（仅对数据查询）
            if (!isCountQuery)
            {
                sql += " ORDER BY id DESC";
                sql += $" LIMIT {(_paginationViewModel.CurrentPage - 1) * _paginationViewModel.PageSize}, {_paginationViewModel.PageSize}";
            }

            Debug.WriteLine($"生成的SQL查询语句: {sql}");
            return sql;
        }

        // 添加方法用于通知UI更新选择状态
        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(CanEditSelectedRoute));
            OnPropertyChanged(nameof(CanShowRouteDetails));
        }


    }
}