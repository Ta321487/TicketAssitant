using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Diagnostics;


namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 添加车票到收藏夹的视图模型
    /// </summary>
    public class AddTicketsToCollectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private readonly TicketCollectionInfo _collection;
        private readonly PaginationViewModel _paginationViewModel;

        private ObservableCollection<TrainRideInfo> _tickets;
        private ObservableCollection<TrainRideInfo> _selectedTickets;
        private bool _isLoading;
        private string _departStationFilter = string.Empty;
        private string _trainNoFilter = string.Empty;
        private string _arriveStationFilter = string.Empty;
        private int? _yearFilter;
        private bool _isAndCondition = true;
        private bool _excludeExistingTickets = true;
        private bool _hasDataChanged = false;
        private bool _hasSelectedItems;
        private int _selectedItemsCount;
        private bool _canSelectAll = true;
        private string _selectedTrainPrefix = "G";
        private string _trainNumberFilter = string.Empty;
        private List<string> _trainPrefixes;
        private bool _isDepartStationDropdownOpen;
        private bool _isArriveStationDropdownOpen;
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private ObservableCollection<StationInfo> _arriveStationSuggestions;
        private string _departStationSearchText = string.Empty;
        private string _arriveStationSearchText = string.Empty;
        private bool _isUpdatingDepartStation;
        private bool _isUpdatingArriveStation;
        private StationSearchService _stationSearchService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">目标收藏夹</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public AddTicketsToCollectionViewModel(TicketCollectionInfo collection, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            
            // 初始化分页视图模型
            _paginationViewModel = new PaginationViewModel();
            _paginationViewModel.PageChanged += PageChanged;
            _paginationViewModel.PageSizeChanged += PageSizeChanged;
            
            // 初始化集合
            _tickets = new ObservableCollection<TrainRideInfo>();
            _selectedTickets = new ObservableCollection<TrainRideInfo>();
            _departStationSuggestions = new ObservableCollection<StationInfo>();
            _arriveStationSuggestions = new ObservableCollection<StationInfo>();
            
            // 初始化车站搜索服务
            _stationSearchService = new StationSearchService(_databaseService);
            
            // 初始化车次前缀列表
            InitializeTrainPrefixes();
            
            // 初始化命令
            SearchTicketsCommand = new RelayCommand(SearchTickets);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            AddSelectedTicketsCommand = new RelayCommand(AddSelectedTickets, CanAddSelectedTickets);
            
            // 初始化车站相关命令
            SelectDepartStationCommand = new RelayCommand<StationInfo>(station => SelectStation(station, true));
            SelectArriveStationCommand = new RelayCommand<StationInfo>(station => SelectStation(station, false));
            
            // 初始加载数据
            LoadTickets();
            
            // 异步初始化站点数据
            _ = _stationSearchService.LoadStationsAsync();
        }

        /// <summary>
        /// 初始化车次前缀
        /// </summary>
        private void InitializeTrainPrefixes()
        {
            // 设置车次前缀列表
            _trainPrefixes = new List<string>
            {
                "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字"
            };
        }

        /// <summary>
        /// 当前收藏夹
        /// </summary>
        public TicketCollectionInfo Collection => _collection;

        /// <summary>
        /// 分页视图模型
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        /// <summary>
        /// 车次前缀列表
        /// </summary>
        public List<string> TrainPrefixes => _trainPrefixes;

        /// <summary>
        /// 选中的车次前缀
        /// </summary>
        public string SelectedTrainPrefix
        {
            get => _selectedTrainPrefix;
            set
            {
                if (_selectedTrainPrefix != value)
                {
                    _selectedTrainPrefix = value;
                    OnPropertyChanged(nameof(SelectedTrainPrefix));
                    UpdateFullTrainNo();
                }
            }
        }

        /// <summary>
        /// 车次号数字部分
        /// </summary>
        public string TrainNumberFilter
        {
            get => _trainNumberFilter;
            set
            {
                if (_trainNumberFilter != value)
                {
                    _trainNumberFilter = value;
                    OnPropertyChanged(nameof(TrainNumberFilter));
                    UpdateFullTrainNo();
                }
            }
        }

        /// <summary>
        /// 更新完整的车次号
        /// </summary>
        private void UpdateFullTrainNo()
        {
            _trainNoFilter = FormValidationHelper.FormatTrainNo(_selectedTrainPrefix, _trainNumberFilter);
            OnPropertyChanged(nameof(TrainNoFilter));
        }

        /// <summary>
        /// 出发车站下拉列表是否打开
        /// </summary>
        public bool IsDepartStationDropdownOpen
        {
            get => _isDepartStationDropdownOpen;
            set
            {
                if (_isDepartStationDropdownOpen != value)
                {
                    _isDepartStationDropdownOpen = value;
                    OnPropertyChanged(nameof(IsDepartStationDropdownOpen));
                }
            }
        }

        /// <summary>
        /// 到达车站下拉列表是否打开
        /// </summary>
        public bool IsArriveStationDropdownOpen
        {
            get => _isArriveStationDropdownOpen;
            set
            {
                if (_isArriveStationDropdownOpen != value)
                {
                    _isArriveStationDropdownOpen = value;
                    OnPropertyChanged(nameof(IsArriveStationDropdownOpen));
                }
            }
        }

        /// <summary>
        /// 出发车站搜索文本
        /// </summary>
        public string DepartStationSearchText
        {
            get => _departStationSearchText;
            set
            {
                if (_departStationSearchText != value)
                {
                    _departStationSearchText = value;
                    OnPropertyChanged(nameof(DepartStationSearchText));
                    
                    if (!_isUpdatingDepartStation)
                    {
                        SearchStations(value, true);
                    }
                }
            }
        }

        /// <summary>
        /// 到达车站搜索文本
        /// </summary>
        public string ArriveStationSearchText
        {
            get => _arriveStationSearchText;
            set
            {
                if (_arriveStationSearchText != value)
                {
                    _arriveStationSearchText = value;
                    OnPropertyChanged(nameof(ArriveStationSearchText));
                    
                    if (!_isUpdatingArriveStation)
                    {
                        SearchStations(value, false);
                    }
                }
            }
        }

        /// <summary>
        /// 出发车站推荐列表
        /// </summary>
        public ObservableCollection<StationInfo> DepartStationSuggestions
        {
            get => _departStationSuggestions;
            set
            {
                if (_departStationSuggestions != value)
                {
                    _departStationSuggestions = value;
                    OnPropertyChanged(nameof(DepartStationSuggestions));
                }
            }
        }

        /// <summary>
        /// 到达车站推荐列表
        /// </summary>
        public ObservableCollection<StationInfo> ArriveStationSuggestions
        {
            get => _arriveStationSuggestions;
            set
            {
                if (_arriveStationSuggestions != value)
                {
                    _arriveStationSuggestions = value;
                    OnPropertyChanged(nameof(ArriveStationSuggestions));
                }
            }
        }

        /// <summary>
        /// 选择出发车站命令
        /// </summary>
        public ICommand SelectDepartStationCommand { get; }

        /// <summary>
        /// 选择到达车站命令
        /// </summary>
        public ICommand SelectArriveStationCommand { get; }

        /// <summary>
        /// 搜索车站
        /// </summary>
        private async void SearchStations(string searchText, bool isDepartStation)
        {
            try
            {
                // 如果正在更新或搜索文本为空，不执行搜索
                if ((isDepartStation && _isUpdatingDepartStation) || 
                    (!isDepartStation && _isUpdatingArriveStation) || 
                    string.IsNullOrWhiteSpace(searchText))
                {
                    // 清空搜索结果并关闭下拉框
                    if (isDepartStation)
                    {
                        DepartStationSuggestions.Clear();
                        IsDepartStationDropdownOpen = false;
                    }
                    else
                    {
                        ArriveStationSuggestions.Clear();
                        IsArriveStationDropdownOpen = false;
                    }
                    return;
                }

                // 确保车站数据已加载
                await _stationSearchService.EnsureInitializedAsync();

                // 先尝试加载用户已有的出发/到达站点
                List<string> userStations;
                if (isDepartStation)
                {
                    userStations = await _databaseService.GetDistinctDepartStationsAsync();
                }
                else
                {
                    userStations = await _databaseService.GetDistinctArriveStationsAsync();
                }

                // 从用户历史站点中筛选符合条件的
                var userFilteredStations = userStations
                    .Where(s => s != null && s.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .Select(name => new StationInfo 
                    { 
                        StationName = name, 
                        // 用更高的排序权重让历史站点排在前面
                        StationLevel = 0 
                    })
                    .ToList();

                // 从所有站点中搜索
                var dbStations = await _databaseService.SearchStationsByNameAsync(searchText);

                // 合并结果，去重
                var mergedStations = userFilteredStations
                    .Union(dbStations.Where(s => !userFilteredStations.Any(us => us.StationName == s.StationName)))
                    .ToList();

                // 限制结果数量
                var suggestions = mergedStations.Take(10).ToList();

                if (isDepartStation)
                {
                    // 更新出发车站建议列表
                    DepartStationSuggestions.Clear();
                    foreach (var station in suggestions)
                    {
                        DepartStationSuggestions.Add(station);
                    }
                    IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                }
                else
                {
                    // 更新到达车站建议列表
                    ArriveStationSuggestions.Clear();
                    foreach (var station in suggestions)
                    {
                        ArriveStationSuggestions.Add(station);
                    }
                    IsArriveStationDropdownOpen = ArriveStationSuggestions.Count > 0;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 选择车站
        /// </summary>
        private void SelectStation(StationInfo station, bool isDepartStation)
        {
            if (station == null)
                return;

            // 获取选择的车站名称
            string stationName = station.StationName;

            // 设置相应的车站筛选条件
            if (isDepartStation)
            {
                // 先关闭下拉框，防止触发搜索
                IsDepartStationDropdownOpen = false;

                // 暂时取消DepartStationSearchText的PropertyChanged事件触发
                _isUpdatingDepartStation = true;
                DepartStationSearchText = StationNameHelper.RemoveStationSuffix(stationName);
                DepartStationFilter = stationName;
                _isUpdatingDepartStation = false;
            }
            else
            {
                // 先关闭下拉框，防止触发搜索
                IsArriveStationDropdownOpen = false;

                // 暂时取消ArriveStationSearchText的PropertyChanged事件触发
                _isUpdatingArriveStation = true;
                ArriveStationSearchText = StationNameHelper.RemoveStationSuffix(stationName);
                ArriveStationFilter = stationName;
                _isUpdatingArriveStation = false;
            }
        }

        /// <summary>
        /// 车票列表
        /// </summary>
        public ObservableCollection<TrainRideInfo> Tickets
        {
            get => _tickets;
            set
            {
                if (_tickets != value)
                {
                    _tickets = value;
                    OnPropertyChanged(nameof(Tickets));
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(HasNoData));
                }
            }
        }

        /// <summary>
        /// 选中的车票
        /// </summary>
        public ObservableCollection<TrainRideInfo> SelectedTickets
        {
            get => _selectedTickets;
            set
            {
                if (_selectedTickets != value)
                {
                    _selectedTickets = value;
                    OnPropertyChanged(nameof(SelectedTickets));
                    UpdateSelectedItemsCount();
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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 加载中状态
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
        /// 出发车站筛选
        /// </summary>
        public string DepartStationFilter
        {
            get => _departStationFilter;
            set
            {
                if (_departStationFilter != value)
                {
                    _departStationFilter = value;
                    OnPropertyChanged(nameof(DepartStationFilter));
                }
            }
        }

        /// <summary>
        /// This is a string filter for the train number
        /// </summary>
        public string TrainNoFilter
        {
            get => _trainNoFilter;
            set
            {
                if (_trainNoFilter != value)
                {
                    _trainNoFilter = value;
                    OnPropertyChanged(nameof(TrainNoFilter));
                }
            }
        }

        /// <summary>
        /// 到达车站筛选
        /// </summary>
        public string ArriveStationFilter
        {
            get => _arriveStationFilter;
            set
            {
                if (_arriveStationFilter != value)
                {
                    _arriveStationFilter = value;
                    OnPropertyChanged(nameof(ArriveStationFilter));
                }
            }
        }

        /// <summary>
        /// 年份筛选
        /// </summary>
        public int? YearFilter
        {
            get => _yearFilter;
            set
            {
                if (_yearFilter != value)
                {
                    _yearFilter = value;
                    OnPropertyChanged(nameof(YearFilter));
                }
            }
        }

        /// <summary>
        /// 条件是否用AND连接
        /// </summary>
        public bool IsAndCondition
        {
            get => _isAndCondition;
            set
            {
                if (_isAndCondition != value)
                {
                    _isAndCondition = value;
                    OnPropertyChanged(nameof(IsAndCondition));
                    OnPropertyChanged(nameof(IsOrCondition));
                }
            }
        }

        /// <summary>
        /// 条件是否用OR连接
        /// </summary>
        public bool IsOrCondition
        {
            get => !_isAndCondition;
            set
            {
                if (!_isAndCondition != value)
                {
                    _isAndCondition = !value;
                    OnPropertyChanged(nameof(IsOrCondition));
                    OnPropertyChanged(nameof(IsAndCondition));
                }
            }
        }

        /// <summary>
        /// 是否排除已添加的车票
        /// </summary>
        public bool ExcludeExistingTickets
        {
            get => _excludeExistingTickets;
            set
            {
                if (_excludeExistingTickets != value)
                {
                    _excludeExistingTickets = value;
                    OnPropertyChanged(nameof(ExcludeExistingTickets));
                }
            }
        }

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => Tickets != null && Tickets.Count > 0;

        /// <summary>
        /// 是否没有数据
        /// </summary>
        public bool HasNoData => Tickets == null || Tickets.Count == 0;

        /// <summary>
        /// 是否有数据变更
        /// </summary>
        public bool HasDataChanged
        {
            get => _hasDataChanged;
            set
            {
                if (_hasDataChanged != value)
                {
                    _hasDataChanged = value;
                    OnPropertyChanged(nameof(HasDataChanged));
                }
            }
        }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle => $"添加车票到收藏夹 - {Collection.CollectionName}";

        /// <summary>
        /// 主视图模型，用于绑定字体大小等UI属性
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;

        /// <summary>
        /// 搜索车票命令
        /// </summary>
        public ICommand SearchTicketsCommand { get; }

        /// <summary>
        /// 重置筛选条件命令
        /// </summary>
        public ICommand ResetFiltersCommand { get; }

        /// <summary>
        /// 全选命令
        /// </summary>
        public ICommand SelectAllCommand { get; }

        /// <summary>
        /// 取消全选命令
        /// </summary>
        public ICommand UnselectAllCommand { get; }

        /// <summary>
        /// 添加选中车票命令
        /// </summary>
        public ICommand AddSelectedTicketsCommand { get; }

        /// <summary>
        /// 是否可以全选
        /// </summary>
        public bool CanSelectAllProperty
        {
            get => _canSelectAll;
            private set
            {
                if (_canSelectAll != value)
                {
                    _canSelectAll = value;
                    OnPropertyChanged(nameof(CanSelectAllProperty));
                }
            }
        }

        /// <summary>
        /// 处理页码变更
        /// </summary>
        private void PageChanged(object sender, EventArgs e)
        {
            LoadTickets();
        }

        /// <summary>
        /// 处理页大小变更
        /// </summary>
        private void PageSizeChanged(object sender, EventArgs e)
        {
            LoadTickets();
        }

        /// <summary>
        /// 搜索车票
        /// </summary>
        private void SearchTickets()
        {
            // 重置到第一页
            _paginationViewModel.CurrentPage = 1;
            LoadTickets();
        }

        /// <summary>
        /// 重置筛选条件
        /// </summary>
        private void ResetFilters()
        {
            DepartStationFilter = string.Empty;
            TrainNoFilter = string.Empty;
            ArriveStationFilter = string.Empty;
            YearFilter = null;
            IsAndCondition = true;
            ExcludeExistingTickets = true;
            
            // 重置到第一页并加载数据
            _paginationViewModel.CurrentPage = 1;
            LoadTickets();
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            foreach (var ticket in Tickets)
            {
                ticket.IsSelected = true;
                if (!SelectedTickets.Contains(ticket))
                {
                    SelectedTickets.Add(ticket);
                }
            }
            
            // 确保更新选中项计数
            SelectedItemsCount = Tickets.Count;
            HasSelectedItems = SelectedItemsCount > 0;
            
            // 通知UI
            OnPropertyChanged(nameof(SelectedTickets));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 是否可以全选
        /// </summary>
        private bool CanSelectAll()
        {
            return HasData && SelectedItemsCount < Tickets.Count;
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        private void UnselectAll()
        {
            foreach (var ticket in Tickets)
            {
                ticket.IsSelected = false;
            }
            
            // 清空选中项集合
            SelectedTickets.Clear();
            
            // 确保更新选中项计数
            SelectedItemsCount = 0;
            HasSelectedItems = false;
            
            // 通知UI
            OnPropertyChanged(nameof(SelectedTickets));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 是否可以取消全选
        /// </summary>
        private bool CanUnselectAll()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 添加选中的车票
        /// </summary>
        private async void AddSelectedTickets()
        {
            // 确保有选中的车票
            if (!HasSelectedItems)
            {
                MessageBoxHelper.ShowError("请先选择要添加的车票",  "错误");
                return;
            }
            
            IsLoading = true;
            
            try
            {
                // 获取选中的车票ID
                var selectedTicketIds = SelectedTickets.Select(t => t.Id).ToList();
                
                // 获取收藏夹中已有的车票ID
                var existingTicketIds = await GetExistingTicketIdsAsync();
                
                // 找出需要添加的车票ID（排除已存在的）
                var ticketsToAdd = selectedTicketIds.Except(existingTicketIds).ToList();
                
                if (ticketsToAdd.Count == 0)
                {
                    // 根据选择记录数量显示不同的提示文本
                    string message = SelectedTickets.Count == 1 
                        ? "所选车票已在收藏夹中，无需重复添加" 
                        : "所选车票已全部在收藏夹中，无需重复添加";
                    MessageBoxHelper.ShowInfo(message, "提示");
                    return;
                }
                
                // 构建映射记录
                var mappings = ticketsToAdd.Select(ticketId => new CollectionMappedTicketInfo
                {
                    CollectionId = Collection.Id,
                    TicketId = ticketId,
                    AddTime = DateTime.Now
                }).ToList();
                
                // 批量添加映射记录
                int addedCount = await AddMappingsAsync(mappings);
                
                // 更新收藏夹中的车票数量
                Collection.TicketCount += addedCount;
                
                // 设置数据有变更
                HasDataChanged = true;
                
                // 提示用户
                MessageBoxHelper.ShowInfo($"成功添加 {addedCount} 张车票到收藏夹", "操作成功");
                
                // 记录调试信息
                Debug.WriteLine($"已添加 {addedCount} 张车票到收藏夹 {Collection.CollectionName}，ID: {Collection.Id}");
                
                // 重新加载数据，展示最新状态
                LoadTickets();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"添加车票失败: {ex.Message}", "错误");
                LogHelper.LogError($"添加车票失败: {ex.Message}", ex);
                Debug.WriteLine($"添加车票失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 是否可以添加选中的车票
        /// </summary>
        private bool CanAddSelectedTickets()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 加载车票数据
        /// </summary>
        private async void LoadTickets()
        {
            IsLoading = true;
            
            try
            {
                // 如果排除已有车票，获取已存在的车票ID列表
                List<int> existingTicketIds = ExcludeExistingTickets ? await GetExistingTicketIdsAsync() : null;
                
                // 获取总记录数
                int totalCount = await GetFilteredTicketCountAsync(existingTicketIds);
                _paginationViewModel.TotalItems = totalCount;
                
                // 获取分页数据
                var tickets = await GetFilteredTicketsAsync(existingTicketIds);
                
                // 更新UI显示
                Tickets.Clear();
                foreach (var ticket in tickets)
                {
                    Tickets.Add(ticket);
                }
                
                // 清空选中状态
                SelectedTickets.Clear();
                
                // 通知UI更新
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                
                // 更新选中项计数
                UpdateSelectedItemsCount();
                
                // 确保CanSelectAllProperty也被更新
                CanSelectAllProperty = CanSelectAll();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载车票数据失败: {ex.Message}", "错误");
                LogHelper.LogError($"加载车票数据失败: {ex.Message}", ex);
                }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 获取收藏夹中已有的车票ID
        /// </summary>
        private async Task<List<int>> GetExistingTicketIdsAsync()
        {
            try
            {
                // 使用数据库服务获取收藏夹中已有的车票ID
                return await _databaseService.GetCollectionTicketIdsAsync(_collection.Id);
            }
            catch (Exception ex)
            {
                // 记录错误但继续执行，返回空列表
                Debug.WriteLine($"获取已有车票ID失败: {ex.Message}");
                LogHelper.LogError($"获取已有车票ID失败: {ex.Message}", ex);
                return new List<int>();
            }
        }

        /// <summary>
        /// 获取筛选后的车票总数
        /// </summary>
        private async Task<int> GetFilteredTicketCountAsync(List<int> excludeTicketIds)
        {
            try
            {
                // 根据筛选条件获取车票总数
                // 使用对应的FilteredTrainRideInfoCount方法
                int count = await _databaseService.GetFilteredTrainRideInfoCountAsync(
                    DepartStationFilter, 
                    TrainNoFilter, 
                    YearFilter, 
                    SeatPositionType.None, // 添加座位位置参数，默认为None
                    IsAndCondition);
                
                // 如果需要排除已有车票，并且有需要排除的车票ID
                if (ExcludeExistingTickets && excludeTicketIds != null && excludeTicketIds.Count > 0)
                {
                    // 获取所有符合条件的车票ID，然后排除已有的
                    var allTickets = await _databaseService.GetFilteredTrainRideInfosAsync(
                        1, int.MaxValue, 
                        DepartStationFilter,
                        TrainNoFilter,
                        YearFilter,
                        SeatPositionType.None, // 添加座位位置参数，默认为None
                        IsAndCondition);
                    
                    // 计算排除后的数量
                    count = allTickets.Count(t => !excludeTicketIds.Contains(t.Id));
                }
                
                return count;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"获取车票总数失败: {ex.Message}", "错误");
                LogHelper.LogError($"获取车票总数失败: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取筛选后的车票列表
        /// </summary>
        private async Task<List<TrainRideInfo>> GetFilteredTicketsAsync(List<int> excludeTicketIds)
        {
            try 
            {
                // 获取符合条件的车票
                var tickets = await _databaseService.GetFilteredTrainRideInfosAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize,
                    DepartStationFilter,
                    TrainNoFilter,
                    YearFilter,
                    SeatPositionType.None, // 添加座位位置参数，默认为None
                    IsAndCondition);
                
                // 如果需要排除已有车票
                if (ExcludeExistingTickets && excludeTicketIds != null && excludeTicketIds.Count > 0)
                {
                    // 过滤掉已有的车票
                    tickets = tickets.Where(t => !excludeTicketIds.Contains(t.Id)).ToList();
                }
                
                return tickets;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"获取车票列表失败: {ex.Message}", "错误");
                LogHelper.LogError($"获取车票列表失败: {ex.Message}", ex);
                return new List<TrainRideInfo>();
            }
        }

        /// <summary>
        /// 批量添加映射记录
        /// </summary>
        private async Task<int> AddMappingsAsync(List<CollectionMappedTicketInfo> mappings)
        {
            try
            {
                // 使用数据库服务添加车票到收藏夹
                return await _databaseService.AddTicketsToCollectionAsync(mappings);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"添加车票到收藏夹失败: {ex.Message}", "错误");
                LogHelper.LogError($"添加车票到收藏夹失败: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 更新选中项计数
        /// </summary>
        private void UpdateSelectedItemsCount()
        {
            // 计算当前选中的车票数量
            int count = Tickets?.Count(t => t.IsSelected) ?? 0;
            
            // 更新选中项集合
            SelectedTickets.Clear();
            foreach (var ticket in Tickets?.Where(t => t.IsSelected) ?? Enumerable.Empty<TrainRideInfo>())
            {
                SelectedTickets.Add(ticket);
            }
            
            // 更新选中项数量
            SelectedItemsCount = count;
            
            // 更新是否有选中项
            HasSelectedItems = count > 0;
            
            // 更新是否可以全选的属性
            CanSelectAllProperty = CanSelectAll();
            
            // 更新命令状态
            CommandManager.InvalidateRequerySuggested();
        }
    }
}