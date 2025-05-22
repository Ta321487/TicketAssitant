using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using TA_WPF.Views;
using System.Windows.Media;

namespace TA_WPF.ViewModels
{
    public class QueryAllStationsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<StationInfo> _stations;
        private int _totalCount;
        private StationInfo _selectedStation;
        private ObservableCollection<StationInfo> _selectedStations;
        private bool _isLoading;
        private double _dataGridRowHeight = 45; // 默认行高为45

        private AdvancedQueryStationViewModel _advancedQueryViewModel;
        private AdvancedQueryStationPanel _advancedQueryPanel;

        private StationQueryFilterEventArgs _lastQueryFilter;

        public QueryAllStationsViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel)); // Keep if needed, otherwise remove

            _stations = new ObservableCollection<StationInfo>();
            _selectedStations = new ObservableCollection<StationInfo>();
            _paginationViewModel.PageChanged += async (s, e) => await LoadStationsAsync();
            // 添加PageSizeChanged事件处理
            _paginationViewModel.PageSizeChanged += async (s, e) => await LoadStationsAsync();

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadStationsAsync());
            AddStationCommand = new RelayCommand(AddStation, CanAddStation); 
            EditStationCommand = new RelayCommand<StationInfo>(EditStation, CanEditStation); 
            DeleteStationCommand = new RelayCommand<StationInfo>(DeleteStation, CanDeleteStation); 
            DeleteStationsCommand = new RelayCommand(DeleteSelectedStations, CanDeleteSelectedStations); 
            AdvancedQueryCommand = new RelayCommand(OpenAdvancedQuery, CanOpenAdvancedQuery); 
            
            // 添加选择相关命令
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection, CanInvertSelection);
            
            // 添加双击命令
            DoubleClickEditCommand = new RelayCommand<StationInfo>(DoubleClickEditStation);
        }

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

        public ObservableCollection<StationInfo> Stations
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

        public StationInfo SelectedStation
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
        
        // 添加多选支持
        public ObservableCollection<StationInfo> SelectedStations
        {
            get => _selectedStations;
            set
            {
                if (_selectedStations != value)
                {
                    _selectedStations = value;
                    OnPropertyChanged(nameof(SelectedStations));
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    OnPropertyChanged(nameof(CanEditSelectedStation));
                }
            }
        }
        
        // 是否有选中的项
        public bool HasSelection => _selectedStations != null && _selectedStations.Count > 0;
        
        // 是否选中了全部项
        public bool IsAllSelected => _stations != null && _selectedStations != null && 
                                    _stations.Count > 0 && _stations.Count == _selectedStations.Count;
                                    
        // 选中项的数量，用于控制修改按钮的显示与启用状态
        public int SelectedItemsCount => _selectedStations?.Count ?? 0;

        // 是否可以编辑选中的车站（仅当选中一个车站时可编辑）
        public bool CanEditSelectedStation => SelectedItemsCount == 1;

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
        public bool HasData => _stations != null && _stations.Count > 0;
        
        // 是否没有数据（用于控制"暂无数据"提示的显示）
        public bool HasNoData => _stations == null || _stations.Count == 0;

        // --- Commands ---
        public ICommand RefreshCommand { get; }
        public ICommand AddStationCommand { get; }
        public ICommand EditStationCommand { get; }
        public ICommand DeleteStationCommand { get; }
        public ICommand DeleteStationsCommand { get; }
        public ICommand AdvancedQueryCommand { get; }
        
        // 选择相关命令
        public ICommand SelectAllCommand { get; }
        public ICommand UnselectAllCommand { get; }
        public ICommand InvertSelectionCommand { get; }
        
        // 添加双击命令
        public ICommand DoubleClickEditCommand { get; }

        private async void AddStation()
        {
            // 创建StationImportService
            var stationImportService = new StationImportService(_databaseService);
            
            // 创建并显示ImportStationFrom12306Window
            var importWindow = new ImportStationFrom12306Window(stationImportService, _mainViewModel);
            
            // 获取导入ViewModel并设置刷新回调
            if (importWindow.DataContext is ImportStationFrom12306ViewModel viewModel)
            {
                // 设置回调以在导入完成后刷新数据
                viewModel.DataRefreshCallback = async () => {
                    await LoadStationsAsync(); 
                };
            }
            
            importWindow.Owner = Application.Current.MainWindow;
            importWindow.ShowDialog();
        }
        private bool CanAddStation() => true; // Or based on permissions/state

        private void EditStation(StationInfo station)
        {
            // 确保只有选中一个车站时才能编辑
            if (station == null || _selectedStations.Count != 1) return;
            
            // 创建StationSearchService
            var stationSearchService = new StationSearchService(_databaseService);
            
            // 创建并显示EditStationWindow
            var editStationWindow = new EditStationWindow(
                _databaseService, 
                stationSearchService, 
                station, 
                async () => await LoadStationsAsync());
            
            editStationWindow.Owner = Application.Current.MainWindow;
            editStationWindow.ShowDialog();
        }
        private bool CanEditStation(StationInfo station) => station != null && _selectedStations.Count == 1;

        private async void DeleteStation(StationInfo station)
        {
            if (station == null) return;
            // Use ShowConfirmation instead of ShowConfirm
            var confirmResult = MessageBoxHelper.ShowConfirmation($"确定要删除车站 '{station.StationName}' 吗？", "确认删除");
            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    // 执行删除操作
                    bool deleted = await _databaseService.DeleteStationsByIdsAsync(new List<int> { station.Id });
                    if (deleted) 
                    {
                        await LoadStationsAsync(); // 刷新数据
                        MessageBoxHelper.ShowInfo($"车站 '{station.StationName}' 已成功删除。");
                    } 
                    else 
                    {
                        MessageBoxHelper.ShowError("删除车站失败。");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"删除车站失败: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"删除车站失败: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        private bool CanDeleteStation(StationInfo station) => station != null;

        private void OpenAdvancedQuery()
        {
            try
            {
                // 查找QueryAllStationsPage用户控件
                var queryPage = FindQueryAllStationsPage();
                if (queryPage != null)
                {
                    // 查找QueryPanelContainer
                    var container = queryPage.FindName("QueryPanelContainer") as Grid;
                    if (container != null)
                    {
                        // 如果已经创建了高级查询面板，则切换其可见性
                        if (_advancedQueryViewModel != null && _advancedQueryPanel != null)
                        {
                            _advancedQueryViewModel.IsQueryPanelVisible = !_advancedQueryViewModel.IsQueryPanelVisible;
                            return;
                        }
                        
                        // 创建StationSearchService
                        var stationSearchService = new StationSearchService(_databaseService);
                        
                        // 创建高级查询ViewModel
                        _advancedQueryViewModel = new AdvancedQueryStationViewModel(_databaseService, stationSearchService);
                        
                        // 设置查询面板可见
                        _advancedQueryViewModel.IsQueryPanelVisible = true;
                        
                        // 订阅筛选条件应用事件
                        _advancedQueryViewModel.FilterApplied += AdvancedQueryViewModel_FilterApplied;
                        
                        // 清空容器
                        container.Children.Clear();
                        
                        // 创建高级查询面板
                        _advancedQueryPanel = new AdvancedQueryStationPanel
                        {
                            DataContext = _advancedQueryViewModel
                        };
                        
                        // 添加到容器
                        container.Children.Add(_advancedQueryPanel);
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("无法找到查询面板容器(QueryPanelContainer)。");
                        LogHelper.LogError("无法找到查询面板容器(QueryPanelContainer)。");
                    }
                }
                else
                {
                    MessageBoxHelper.ShowError("无法找到查询页面(QueryAllStationsPage)。");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"打开高级查询面板时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"打开高级查询面板时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 查找QueryAllStationsPage用户控件
        /// </summary>
        private FrameworkElement FindQueryAllStationsPage()
        {
            // 遍历应用程序中的所有窗口
            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsActive)
                {
                    // 查找主窗口中的Frame或ContentPresenter
                    var mainContent = window.Content as DependencyObject;
                    if (mainContent != null)
                    {
                        // 尝试查找QueryAllStationsPage
                        var queryPage = FindChild<Views.QueryAllStationsPage>(mainContent);
                        if (queryPage != null)
                        {
                            return queryPage;
                        }
                        
                        // 如果没有直接找到，可能是嵌套在其他控件中，尝试查找名为QueryAllStationsPageControl的控件
                        var namedQueryPage = FindChildByName(mainContent, "QueryAllStationsPageControl");
                        if (namedQueryPage != null)
                        {
                            return namedQueryPage;
                        }
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// 递归查找指定类型的子控件
        /// </summary>
        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            // 检查当前对象是否为所需类型
            if (parent is T found)
            {
                return found;
            }

            // 获取子元素数量
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            
            // 递归查找每个子元素
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // 递归查找
                var result = FindChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 递归查找指定名称的子控件
        /// </summary>
        private static FrameworkElement FindChildByName(DependencyObject parent, string name)
        {
            // 检查当前对象是否为所需名称
            if (parent is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            // 获取子元素数量
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            
            // 递归查找每个子元素
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // 递归查找
                var result = FindChildByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 处理高级查询筛选条件应用事件
        /// </summary>
        private async void AdvancedQueryViewModel_FilterApplied(object sender, StationQueryFilterEventArgs e)
        {
            try
            {
                IsLoading = true;
                
                // 保存最后一次的查询条件，用于分页时重新应用
                _lastQueryFilter = e;
                
                // 使用新的数据库方法，直接在数据库层面应用查询条件
                // 获取符合条件的车站总数
                TotalCount = await _databaseService.GetStationCountAdvancedAsync(
                    e.StationName,
                    e.Province,
                    e.City,
                    e.District,
                    e.UseMyDepartStations ? e.MyDepartStations : null
                );
                
                // 更新分页信息
                _paginationViewModel.TotalItems = TotalCount;
                _paginationViewModel.CurrentPage = 1; // 重置为第一页
                
                // 获取当前页的数据
                var stations = await _databaseService.QueryStationsAdvancedAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize,
                    e.StationName,
                    e.Province,
                    e.City,
                    e.District,
                    e.UseMyDepartStations ? e.MyDepartStations : null
                );
                
                // 更新UI数据
                Stations = new ObservableCollection<StationInfo>(stations);
                
                // 清空选择
                SelectedStations.Clear();
                
                // 更新UI状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(IsAllSelected));
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"应用高级查询筛选条件时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"应用高级查询筛选条件时出错: {ex.Message}");
                
                // 出错时恢复到初始状态
                await LoadStationsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanOpenAdvancedQuery() => true;
        
        // --- 选择相关方法 ---
        public void SelectAll()
        {
            if (_stations == null || _stations.Count == 0)
                return;
                
            SelectedStations.Clear();
            foreach (var station in _stations)
            {
                SelectedStations.Add(station);
            }
            
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            
            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new StationSelectionChangedEventArgs(new List<StationInfo>(), _stations.ToList()));
        }
        
        public bool CanSelectAll() => HasData && !IsAllSelected;
        
        public void UnselectAll()
        {
            if (_selectedStations == null || _selectedStations.Count == 0)
                return;
                
            // 备份当前选中项以便触发事件
            var previousSelected = new List<StationInfo>(_selectedStations);
            
            SelectedStations.Clear();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            
            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new StationSelectionChangedEventArgs(previousSelected, new List<StationInfo>()));
        }
        
        public bool CanUnselectAll() => HasSelection;
        
        public void InvertSelection()
        {
            if (_stations == null || _stations.Count == 0)
                return;
                
            var currentSelection = new HashSet<StationInfo>(_selectedStations);
            var toAdd = new List<StationInfo>();
            var toRemove = new List<StationInfo>(_selectedStations);
            
            foreach (var station in _stations)
            {
                if (!currentSelection.Contains(station))
                {
                    toAdd.Add(station);
                }
            }
            
            SelectedStations.Clear();
            
            foreach (var station in toAdd)
            {
                SelectedStations.Add(station);
            }
            
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            
            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new StationSelectionChangedEventArgs(toRemove, toAdd));
        }
        
        public bool CanInvertSelection() => HasData;

        // 事件用于通知View更新DataGrid的选中状态
        public event EventHandler<StationSelectionChangedEventArgs> SelectionChanged;

        // 事件参数类
        public class StationSelectionChangedEventArgs : EventArgs
        {
            public List<StationInfo> RemovedItems { get; }
            public List<StationInfo> AddedItems { get; }
            
            public StationSelectionChangedEventArgs(List<StationInfo> removedItems, List<StationInfo> addedItems)
            {
                RemovedItems = removedItems;
                AddedItems = addedItems;
            }
        }

        // --- Data Loading ---
        public async Task QueryAllAsync() // Renamed from LoadStations for consistency with Ticket Center
        {
            _paginationViewModel.CurrentPage = 1; // Reset to first page
            await LoadStationsAsync();
        }

        public async Task LoadStationsAsync()
        {
            IsLoading = true;
            try
            {
                // 检查是否有高级查询条件
                if (_lastQueryFilter != null)
                {
                    // 有高级查询条件，使用高级查询方法
                    var stations = await _databaseService.QueryStationsAdvancedAsync(
                        _paginationViewModel.CurrentPage,
                        _paginationViewModel.PageSize,
                        _lastQueryFilter.StationName,
                        _lastQueryFilter.Province,
                        _lastQueryFilter.City,
                        _lastQueryFilter.District,
                        _lastQueryFilter.UseMyDepartStations ? _lastQueryFilter.MyDepartStations : null
                    );
                    
                    Stations = new ObservableCollection<StationInfo>(stations);
                }
                else
                {
                    // 没有高级查询条件，使用普通查询方法
                    TotalCount = await _databaseService.GetStationCountAsync();
                    var stationsData = await _databaseService.GetStationsAsync(
                        _paginationViewModel.CurrentPage,
                        _paginationViewModel.PageSize);

                    Stations = new ObservableCollection<StationInfo>(stationsData);
                }
                
                // 清除选择
                SelectedStations.Clear();
                
                // 通知UI更新数据状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(IsAllSelected));
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载车站列表失败: {ex.Message}");
                MessageBoxHelper.ShowError($"加载车站列表失败: {ex.Message}");
                Stations.Clear();
                SelectedStations.Clear();
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


        // 添加方法用于通知UI更新选择状态
        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(CanEditSelectedStation));
        }

        private async void DeleteSelectedStations()
        {
            if (_selectedStations == null || _selectedStations.Count == 0) return;
            
            string message;
            if (_selectedStations.Count == 1)
            {
                message = $"确定要删除车站 '{_selectedStations[0].StationName}' 吗？";
            }
            else
            {
                message = $"确定要删除选中的 {_selectedStations.Count} 个车站吗？";
            }
            
            var confirmResult = MessageBoxHelper.ShowConfirmation(message, "确认删除");
            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    // 获取所有选中车站的ID
                    var stationIds = _selectedStations.Select(s => s.Id).ToList();
                    // 执行删除操作
                    bool deleted = await _databaseService.DeleteStationsByIdsAsync(stationIds);
                    
                    if (deleted)
                    {
                        await LoadStationsAsync(); // 刷新数据
                        
                        if (_selectedStations.Count == 1)
                        {
                            MessageBoxHelper.ShowInfo($"车站 '{_selectedStations[0].StationName}' 已成功删除。");
                        }
                        else
                        {
                            MessageBoxHelper.ShowInfo($"已成功删除 {stationIds.Count} 个车站。");
                        }
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("删除车站失败。");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"批量删除车站失败: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"批量删除车站失败: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        
        private bool CanDeleteSelectedStations() => HasSelection;

        // 处理双击车站记录的方法
        private void DoubleClickEditStation(StationInfo station)
        {
            if (station != null)
            {
                EditStation(station);
            }
        }
    }
}