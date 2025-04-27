using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows; // Add this for MessageBoxResult and Application
using System.Linq;
using System.Collections.Generic;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class QueryAllStationsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel; // If interaction with MainViewModel is needed

        private ObservableCollection<StationInfo> _stations;
        private int _totalCount;
        private StationInfo _selectedStation;
        private ObservableCollection<StationInfo> _selectedStations;
        private bool _isLoading;
        private double _dataGridRowHeight = 45; // 默认行高为45

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
            AddStationCommand = new RelayCommand(AddStation, CanAddStation); // Implement logic later
            EditStationCommand = new RelayCommand<StationInfo>(EditStation, CanEditStation); // Implement logic later
            DeleteStationCommand = new RelayCommand<StationInfo>(DeleteStation, CanDeleteStation); // Implement logic later
            DeleteStationsCommand = new RelayCommand(DeleteSelectedStations, CanDeleteSelectedStations); // 添加删除多条命令
            AdvancedQueryCommand = new RelayCommand(OpenAdvancedQuery, CanOpenAdvancedQuery); // Implement logic later
            
            // 添加选择相关命令
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection, CanInvertSelection);
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

        // --- Command Methods (Implement logic later or keep as placeholders) ---
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
            if (station == null) return;
            // Logic to open EditStationWindow with selected station
             MessageBoxHelper.ShowInfo($"编辑车站 '{station.StationName}' 功能稍后实现。");
            // Example: _navigationService.OpenEditStationWindow(station, _mainViewModel);
            // await LoadStationsAsync(); // Refresh after edit
        }
        private bool CanEditStation(StationInfo station) => station != null;

        private async void DeleteStation(StationInfo station)
        {
            if (station == null) return;
            // Use ShowConfirmation instead of ShowConfirm
            var confirmResult = MessageBoxHelper.ShowConfirmation($"确定要删除车站 '{station.StationName}' 吗？", "确认删除");
            if (confirmResult == MessageBoxResult.Yes) // Check the result
            {
                 MessageBoxHelper.ShowInfo($"删除车站 '{station.StationName}' 功能稍后实现。");
                // Logic to delete station from database
                // bool deleted = await _databaseService.DeleteStationAsync(station.Id);
                // if(deleted) {
                //    await LoadStationsAsync(); // Refresh after delete
                // } else {
                //    MessageBoxHelper.ShowError("删除车站失败。");
                // }
            }
        }
        private bool CanDeleteStation(StationInfo station) => station != null;

        private void OpenAdvancedQuery()
        {
             MessageBoxHelper.ShowInfo("高级查询功能稍后实现。");
            // Logic to open/show AdvancedQueryStationPanel
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
                TotalCount = await _databaseService.GetStationCountAsync(); // Need to add this method to DatabaseService
                var stationsData = await _databaseService.GetStationsAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize); // Need to add this method to DatabaseService

                Stations = new ObservableCollection<StationInfo>(stationsData);
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

        // --- Helper Methods (if needed) ---

        // 添加方法用于通知UI更新选择状态
        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
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
                MessageBoxHelper.ShowInfo($"删除选中的 {_selectedStations.Count} 个车站功能稍后实现。");
                // 此处为删除多条记录的实现逻辑
                // var stationIds = _selectedStations.Select(s => s.Id).ToList();
                // bool deleted = await _databaseService.DeleteStationsAsync(stationIds);
                // if (deleted)
                // {
                //     await LoadStationsAsync(); // 刷新数据
                // }
                // else
                // {
                //     MessageBoxHelper.ShowError("删除车站失败。");
                // }
            }
        }
        
        private bool CanDeleteSelectedStations() => HasSelection;
    }
}