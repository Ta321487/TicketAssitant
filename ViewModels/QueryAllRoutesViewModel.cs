using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;
using System.Text;
using System.Linq;
using System.Collections.Generic;

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

        // 添加排序相关字段
        private string _currentSortField = "sort_order"; // 默认排序字段
        private bool _currentSortAscending = true; // 默认升序

        // 添加静态字段保存排序状态
        private static string _savedSortField = "sort_order";
        private static bool _savedSortAscending = true;
        private static bool _hasCustomSorting = false;

        // 添加DatabaseService属性，用于在页面中直接创建RouteDetailWindow
        public DatabaseService DatabaseService => _databaseService;

        public QueryAllRoutesViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            _routes = new ObservableCollection<RouteInfo>();
            _selectedRoutes = new ObservableCollection<RouteInfo>();

            // 从静态变量中恢复排序状态
            if (_hasCustomSorting)
            {
                _currentSortField = _savedSortField;
                _currentSortAscending = _savedSortAscending;
            }

            _advancedQueryViewModel = new AdvancedQueryRouteViewModel();
            _advancedQueryViewModel.FilterApplied += AdvancedQueryViewModel_FilterApplied;

            _paginationViewModel.PageChanged += (s, e) => LoadRoutesAsync().ConfigureAwait(false);
            _paginationViewModel.PageSizeChanged += (s, e) => LoadRoutesAsync().ConfigureAwait(false);

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadRoutesAsync());
            AddRouteCommand = new RelayCommand(AddRoute);
            EditRouteCommand = new RelayCommand<RouteInfo>(EditRoute);
            DeleteRouteCommand = new RelayCommand<RouteInfo>(DeleteRoute);
            DeleteRoutesCommand = new RelayCommand(DeleteSelectedRoutes);
            AdvancedQueryCommand = new RelayCommand(OpenAdvancedQuery);

            // 添加排序命令
            SortRoutesCommand = new RelayCommand<string>(SortRoutes);

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

        // 添加排序命令
        public ICommand SortRoutesCommand { get; }

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

            try
            {
                Debug.WriteLine($"EditRoute - 创建编辑窗口，路线ID: {route.Id}");

                // 创建编辑窗口实例
                var editRouteWindow = new EditRouteWindow(route, _databaseService, _mainViewModel);
                editRouteWindow.Owner = Application.Current.MainWindow;

                Debug.WriteLine("EditRoute - 显示编辑窗口");

                // 显示模态窗口
                var result = editRouteWindow.ShowDialog();

                Debug.WriteLine($"EditRoute - 编辑窗口已关闭，结果: {result}");

                // 如果编辑成功，刷新列表
                if (result == true)
                {
                    _ = LoadRoutesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EditRoute - 打开编辑窗口发生异常: {ex.Message}");
                MessageBoxHelper.ShowError($"打开编辑窗口失败: {ex.Message}");
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
            try
            {
                // 空检查
                if (route == null)
                {
                    Debug.WriteLine("DoubleClickEditRoute: 路线对象为null，取消操作");
                    return;
                }

                Debug.WriteLine($"DoubleClickEditRoute: 准备编辑路线 - ID={route.Id}, 名称={route.RouteName}");

                // 直接创建一个新的编辑窗口实例 - 使用更明确的方式避免重复打开
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 确保使用新的路线对象副本
                        var routeCopy = new RouteInfo
                        {
                            Id = route.Id,
                            RouteName = route.RouteName,
                            Description = route.Description,
                            TotalDistance = route.TotalDistance,
                            IsFavorite = route.IsFavorite,
                            CoverImage = route.CoverImage?.Clone() as byte[],
                            CreateTime = route.CreateTime,
                            UpdateTime = route.UpdateTime
                        };

                        Debug.WriteLine($"DoubleClickEditRoute: 创建编辑窗口，路线ID: {routeCopy.Id}");
                        var window = new EditRouteWindow(routeCopy, _databaseService, _mainViewModel);
                        window.Owner = Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        // 设置结果处理
                        window.Closed += (sender, args) =>
                        {
                            Debug.WriteLine("DoubleClickEditRoute: 编辑窗口已关闭");
                            if (window.DialogResult == true)
                            {
                                Debug.WriteLine("DoubleClickEditRoute: 编辑成功，刷新列表");
                                _ = LoadRoutesAsync();
                            }
                        };

                        // 显示为对话框
                        Debug.WriteLine("DoubleClickEditRoute: 显示编辑窗口");
                        window.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DoubleClickEditRoute Dispatcher异常: {ex.Message}");
                        LogHelper.LogError("双击编辑窗口打开失败", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DoubleClickEditRoute异常: {ex.Message}");
                LogHelper.LogError("双击编辑处理失败", ex);
            }
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
                // 获取过滤条件下的总记录数
                TotalCount = await GetFilteredRouteCountAsync();

                // 获取当前页的数据
                var routesData = await GetFilteredRoutesAsync();

                // 将列表转换为ObservableCollection并更新UI
                Routes = new ObservableCollection<RouteInfo>(routesData);

                // 如果没有数据，且TotalCount显示应该有数据，则可能是最后一页没有数据了
                // 返回到第一页重新加载
                if (Routes.Count == 0 && TotalCount > 0 && _paginationViewModel.CurrentPage > 1)
                {
                    _paginationViewModel.CurrentPage = 1;
                    await LoadRoutesAsync();
                    return;
                }

                // 更新选中项状态
                UpdateSelectionStates();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载路线列表失败: {ex.Message}");
                Routes.Clear();
                SelectedRoutes.Clear();
                TotalCount = 0;
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
            StringBuilder sb = new StringBuilder();

            if (isCountQuery)
            {
                sb.Append("SELECT COUNT(*) FROM route_info WHERE 1=1");
            }
            else
            {
                // 基本查询
                sb.Append("SELECT * FROM route_info WHERE 1=1");
            }

            // 以下为筛选条件
            List<string> conditions = new List<string>();

            // 路线名称筛选
            if (!string.IsNullOrWhiteSpace(_currentRouteName))
            {
                conditions.Add($"route_name LIKE '%{_currentRouteName}%'");
            }

            // 总里程筛选
            if (_currentDistanceRange != DistanceRangeType.None)
            {
                switch (_currentDistanceRange)
                {
                    case DistanceRangeType.Range1: // 0-100公里
                        conditions.Add("total_distance < 100");
                        break;
                    case DistanceRangeType.Range2: // 100-500公里
                        conditions.Add("total_distance >= 100 AND total_distance <= 500");
                        break;
                    case DistanceRangeType.Range3: // 500-1000公里
                        conditions.Add("total_distance > 500 AND total_distance <= 1000");
                        break;
                    case DistanceRangeType.Range4: // 1000-2000公里
                        conditions.Add("total_distance > 1000 AND total_distance <= 2000");
                        break;
                    case DistanceRangeType.Range5: // 2000公里以上
                        conditions.Add("total_distance > 2000");
                        break;
                }
            }

            // 收藏状态筛选
            if (_currentIsFavorite)
            {
                conditions.Add("is_favorite = 1");
            }

            // 将所有条件用AND或OR连接
            if (conditions.Count > 0)
            {
                string connector = _currentIsAndCondition ? " AND " : " OR ";
                sb.Append(" AND (");
                sb.Append(string.Join(connector, conditions));
                sb.Append(")");
            }

            // 如果不是计数查询，添加排序和分页
            if (!isCountQuery)
            {
                // 添加排序
                sb.Append($" ORDER BY {_currentSortField} {(_currentSortAscending ? "ASC" : "DESC")}");

                // 添加分页
                int offset = (_paginationViewModel.CurrentPage - 1) * _paginationViewModel.PageSize;
                sb.Append($" LIMIT {_paginationViewModel.PageSize} OFFSET {offset}");
            }

            Debug.WriteLine($"生成的SQL查询语句: {sb.ToString()}");
            return sb.ToString();
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

        /// <summary>
        /// 排序路线
        /// </summary>
        /// <param name="sortBy">排序字段：TotalDistance_Asc/Desc, CreateTime_Asc/Desc, UpdateTime_Asc/Desc</param>
        private async void SortRoutes(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy) || Routes == null || Routes.Count == 0)
            {
                return;
            }

            IsLoading = true;
            try
            {
                // 解析排序方式
                string sortField = sortBy.Split('_')[0];
                bool isAscending = sortBy.EndsWith("_Asc");

                // 记住当前页中选中的项
                Dictionary<int, bool> selectedStates = new Dictionary<int, bool>();
                foreach (var route in Routes)
                {
                    selectedStates[route.Id] = route.IsSelected;
                }

                // 保存当前排序状态，以便分页时使用
                switch (sortField)
                {
                    case "TotalDistance":
                        _currentSortField = "total_distance";
                        break;
                    case "CreateTime":
                        _currentSortField = "create_time";
                        break;
                    case "UpdateTime":
                        _currentSortField = "update_time";
                        break;
                    default:
                        _currentSortField = "sort_order";
                        break;
                }
                _currentSortAscending = isAscending;

                // 保存排序状态到静态变量
                _savedSortField = _currentSortField;
                _savedSortAscending = _currentSortAscending;
                _hasCustomSorting = true;

                // 获取所有路线
                var allRoutes = await _databaseService.GetRoutesAsync();

                // 根据当前排序字段排序
                var sortedRoutes = isAscending
                    ? allRoutes.OrderBy(r => GetSortValue(r, sortField))
                    : allRoutes.OrderByDescending(r => GetSortValue(r, sortField));

                // 重新分配排序值，以10为步长
                Dictionary<int, int> newSortOrders = new Dictionary<int, int>();
                int sortOrder = 10;
                foreach (var route in sortedRoutes)
                {
                    newSortOrders[route.Id] = sortOrder;
                    sortOrder += 10;
                }

                // 更新数据库中的排序顺序
                bool success = await _databaseService.UpdateRouteSortOrdersAsync(newSortOrders);
                if (!success)
                {
                    LogHelper.LogError("更新路线排序顺序失败");
                }
                else
                {
                    LogHelper.LogInfo($"成功更新{newSortOrders.Count}条路线的排序顺序");
                }

                // 直接使用DatabaseService按指定字段获取已排序的数据
                TotalCount = await _databaseService.GetRouteCountAsync();
                var routesData = await _databaseService.GetRoutesAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize,
                    _currentSortField,
                    isAscending);

                // 将列表转换为ObservableCollection并更新UI
                var newRoutes = new ObservableCollection<RouteInfo>(routesData);

                // 恢复选中状态
                foreach (var route in newRoutes)
                {
                    if (selectedStates.TryGetValue(route.Id, out bool isSelected))
                    {
                        route.IsSelected = isSelected;
                    }
                }

                // 更新UI显示的集合
                Routes = newRoutes;

                // 提示排序完成
                string sortName = "";
                string sortDirection = isAscending ? "升序" : "降序";

                switch (sortField)
                {
                    case "TotalDistance":
                        sortName = "总里程";
                        break;
                    case "CreateTime":
                        sortName = "创建日期";
                        break;
                    case "UpdateTime":
                        sortName = "修改日期";
                        break;
                }

                LogHelper.LogInfo($"路线已按{sortName}({sortDirection})排序完成");

                // 通知UI更新
                NotifySelectionChanged();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"排序路线时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"排序路线时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 根据排序字段获取对应的值
        /// </summary>
        private object GetSortValue(RouteInfo route, string sortField)
        {
            switch (sortField)
            {
                case "TotalDistance":
                    return route.TotalDistance;
                case "CreateTime":
                    return route.CreateTime;
                case "UpdateTime":
                    return route.UpdateTime;
                default:
                    return route.SortOrder;
            }
        }

        /// <summary>
        /// 更新选中状态
        /// </summary>
        private void UpdateSelectionStates()
        {
            // 清除当前选中项
            SelectedRoutes.Clear();

            // 对于新加载的数据，重新设置选中状态
            foreach (var route in Routes)
            {
                if (route.IsSelected)
                {
                    SelectedRoutes.Add(route);
                }
            }

            // 通知UI更新相关状态
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasNoData));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(CanEditSelectedRoute));
            OnPropertyChanged(nameof(CanShowRouteDetails));
        }
    }
}