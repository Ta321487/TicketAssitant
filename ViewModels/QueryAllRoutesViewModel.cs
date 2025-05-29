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
using System.Windows.Media;

namespace TA_WPF.ViewModels
{
    public class QueryAllRoutesViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<RouteInfo> _routes;
        private int _totalCount;
        private RouteInfo _selectedRoute;
        private ObservableCollection<RouteInfo> _selectedRoutes;
        private bool _isLoading;
        private double _dataGridRowHeight = 45; // 默认行高为45

        public QueryAllRoutesViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

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

        private void AddRoute()
        {
            MessageBoxHelper.ShowInfo("添加路线功能暂未实现");
        }

        private void EditRoute(RouteInfo route)
        {
            MessageBoxHelper.ShowInfo("编辑路线功能暂未实现");
        }

        private void DeleteRoute(RouteInfo route)
        {
            MessageBoxHelper.ShowInfo("删除路线功能暂未实现");
        }

        private void DeleteSelectedRoutes()
        {
            MessageBoxHelper.ShowInfo("批量删除路线功能暂未实现");
        }

        private void OpenAdvancedQuery()
        {
            MessageBoxHelper.ShowInfo("高级查询功能暂未实现");
        }
        
        private void DoubleClickEditRoute(RouteInfo route)
        {
            MessageBoxHelper.ShowInfo("双击编辑路线功能暂未实现");
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
            _paginationViewModel.CurrentPage = 1; // Reset to first page
            await LoadRoutesAsync();
        }

        public async Task LoadRoutesAsync()
        {
            IsLoading = true;
            try
            {
                TotalCount = await _databaseService.GetRouteCountAsync();
                var routesData = await _databaseService.GetRoutesAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize);

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

        // 添加方法用于通知UI更新选择状态
        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(CanEditSelectedRoute));
        }
    }
} 