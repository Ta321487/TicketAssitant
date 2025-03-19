using System.Collections.ObjectModel;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows.Input;

namespace TA_WPF.ViewModels
{
    public class YearOption
    {
        public int? Year { get; set; }
        public string DisplayName { get; set; }
        public bool IsCustom { get; set; }

        public YearOption(int? year, string displayName, bool isCustom = false)
        {
            Year = year;
            DisplayName = displayName;
            IsCustom = isCustom;
        }
    }
    
    public class DepartStationItem
    {
        public string DepartStation { get; set; }
        
        public DepartStationItem(string departStation)
        {
            DepartStation = departStation;
        }
    }

    public class QueryAllTicketsViewModel : TicketBaseViewModel
    {
        #region 字段

        private bool _isQueryPanelVisible;
        private string _trainNumberFilter;
        private string _selectedTrainPrefix;
        private DepartStationItem _selectedDepartStation;
        private YearOption _selectedYearOption;
        private List<YearOption> _yearOptions;
        private ObservableCollection<DepartStationItem> _departStations;
        private List<string> _trainPrefixes;
        private bool _isAndCondition = true;
        private bool _isOrCondition;
        private bool _hasActiveFilters;
        private int? _customYear;
        private bool _isCustomYearSelected;
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private bool _isDepartStationDropdownOpen;
        private string _departStationSearchText;
        private bool _isUpdatingDepartStation = false;

        #endregion

        public QueryAllTicketsViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
            : base(databaseService, paginationViewModel, mainViewModel)
        {
            // 初始化命令
            ToggleQueryPanelCommand = new RelayCommand(ToggleQueryPanel);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
            CustomYearCommand = new RelayCommand(SelectCustomYear);
            ClearDepartStationCommand = new RelayCommand(ClearDepartStation);
            ClearTrainNumberCommand = new RelayCommand(ClearTrainNumber);
            ClearYearCommand = new RelayCommand(ClearYear);
            SelectDepartStationCommand = new RelayCommand<StationInfo>(SelectDepartStation);
            
            // 初始化年份选项
            InitializeYearOptions();
            
            // 初始化车次前缀
            InitializeTrainPrefixes();
            
            // 初始化站点建议列表
            DepartStationSuggestions = new ObservableCollection<StationInfo>();
            
            // 异步加载出发站列表
            LoadDepartStationsAsync();
        }

        /// <summary>
        /// 查询所有车票
        /// </summary>
        public async Task QueryAllAsync()
        {
            try
            {
                _paginationViewModel.IsLoading = true;
                
                // 获取总记录数
                int totalCount = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 设置总记录数，这会触发TotalPages的重新计算
                _paginationViewModel.TotalItems = totalCount;
                
                // 重置到第一页
                _paginationViewModel.CurrentPage = 1;
                
                // 清除筛选条件
                ResetFilter();
                
                // 清除缓存
                _paginationViewModel.ClearCache();
                
                // 加载第一页数据
                await LoadPageDataAsync();
                
                // 标记为已初始化
                _paginationViewModel.IsInitialized = true;
                
                // 显示数据表格
                _mainViewModel.ShowQueryAllTickets = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(TrainRideInfos));
                
                // 手动触发导航按钮状态更新
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 手动刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"查询数据时出错: {ex.Message}");
            }
            finally
            {
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 加载页面数据
        /// </summary>
        protected override async Task LoadPageDataAsync()
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 检查缓存中是否已有当前页数据，且页大小未变
                if (_paginationViewModel.PageCache.ContainsKey(_paginationViewModel.CurrentPage) && 
                    _paginationViewModel.CachePageSize == _paginationViewModel.PageSize)
                {
                    // 从缓存加载数据
                    var cachedItems = _paginationViewModel.PageCache[_paginationViewModel.CurrentPage];
                    
                    // 使用批量更新方式更新UI，减少闪烁
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        // 如果数量相同，尝试更新现有项而不是清空重建
                        if (_paginationViewModel.Items.Count == cachedItems.Count)
                        {
                            for (int i = 0; i < cachedItems.Count; i++)
                            {
                                _paginationViewModel.Items[i] = cachedItems[i];
                            }
                        }
                        else
                        {
                            _paginationViewModel.Items.Clear();
                            foreach (var item in cachedItems)
                            {
                                _paginationViewModel.Items.Add(item);
                            }
                        }
                    });
                }
                else
                {
                    // 需要从数据库加载新数据
                    List<TrainRideInfo> items;
                    
                    if (HasActiveFilters)
                    {
                        // 构建完整的车次号
                        string fullTrainNo = null;
                        if (!string.IsNullOrWhiteSpace(TrainNumberFilter))
                        {
                            fullTrainNo = GetFullTrainNo();
                        }
                        
                        // 获取年份值
                        int? yearValue = null;
                        if (SelectedYearOption?.Year.HasValue == true)
                        {
                            yearValue = SelectedYearOption.Year.Value;
                        }
                        
                        // 使用筛选条件加载数据
                        items = await _databaseService.GetFilteredTrainRideInfosAsync(
                            _paginationViewModel.CurrentPage,
                            _paginationViewModel.PageSize,
                            _selectedDepartStation?.DepartStation,
                            fullTrainNo,
                            yearValue,
                            _isAndCondition);
                    }
                    else
                    {
                        // 加载所有数据
                        items = await _databaseService.GetPagedTrainRideInfosAsync(
                            _paginationViewModel.CurrentPage,
                            _paginationViewModel.PageSize);
                    }

                    // 更新缓存
                    _paginationViewModel.PageCache[_paginationViewModel.CurrentPage] = items;
                    _paginationViewModel.CachePageSize = _paginationViewModel.PageSize;
                    
                    // 更新UI
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        _paginationViewModel.Items.Clear();
                        foreach (var item in items)
                        {
                            _paginationViewModel.Items.Add(item);
                        }
                    });
                }

                // 通知数据状态变更
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载页面数据时出错: {ex.Message}");
            }
            finally
            {
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 页码变更事件处理
        /// </summary>
        private void OnPageChanged(object sender, EventArgs e)
        {
            // 确保加载状态已设置，这里不需要条件判断，因为我们希望每次页面变更都显示加载动画
            _paginationViewModel.IsLoading = true;
            
            // 直接加载页面数据，不需要额外的Dispatcher调用
            _ = LoadPageDataAsync();
        }

        /// <summary>
        /// 页大小变更事件处理
        /// </summary>
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageSize));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // 加载新的页面数据
                _ = LoadPageDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"页大小变更处理出错: {ex.Message}");
                // 确保加载状态被重置
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新总记录数
        /// </summary>
        /// <returns>异步任务</returns>
        protected override async Task RefreshTotalItemsAsync()
        {
            try
            {
                // 获取总记录数
                int totalCount = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 设置总记录数，这会触发TotalPages的重新计算
                _paginationViewModel.TotalItems = totalCount;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"刷新总记录数时出错: {ex.Message}");
                LogHelper.LogError($"刷新总记录数时出错", ex);
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            await base.RefreshDataAsync();
        }

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _paginationViewModel.IsLoading;
            set => _paginationViewModel.IsLoading = value;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _paginationViewModel.CurrentPage;
            set => _paginationViewModel.CurrentPage = value;
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _paginationViewModel.TotalPages;

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalItems => _paginationViewModel.TotalItems;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize
        {
            get => _paginationViewModel.PageSize;
            set => _paginationViewModel.PageSize = value;
        }

        /// <summary>
        /// 页大小选项
        /// </summary>
        public int[] PageSizeOptions => _paginationViewModel.PageSizeOptions;

        /// <summary>
        /// 是否可以导航到首页
        /// </summary>
        public bool CanNavigateToFirstPage => _paginationViewModel.CanNavigateToFirstPage;

        /// <summary>
        /// 是否可以导航到上一页
        /// </summary>
        public bool CanNavigateToPreviousPage => _paginationViewModel.CanNavigateToPreviousPage;

        /// <summary>
        /// 是否可以导航到下一页
        /// </summary>
        public bool CanNavigateToNextPage => _paginationViewModel.CanNavigateToNextPage;

        /// <summary>
        /// 是否可以导航到末页
        /// </summary>
        public bool CanNavigateToLastPage => _paginationViewModel.CanNavigateToLastPage;

        /// <summary>
        /// 车票数据
        /// </summary>
        public ObservableCollection<TrainRideInfo> TrainRideInfos => _paginationViewModel.Items;

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;

        /// <summary>
        /// 查询面板是否可见
        /// </summary>
        public bool IsQueryPanelVisible
        {
            get => _isQueryPanelVisible;
            set
            {
                if (_isQueryPanelVisible != value)
                {
                    _isQueryPanelVisible = value;
                    OnPropertyChanged(nameof(IsQueryPanelVisible));
                }
            }
        }

        /// <summary>
        /// 车次号筛选条件
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
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

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
                }
            }
        }

        /// <summary>
        /// 车次前缀列表
        /// </summary>
        public List<string> TrainPrefixes
        {
            get => _trainPrefixes;
            set
            {
                if (_trainPrefixes != value)
                {
                    _trainPrefixes = value;
                    OnPropertyChanged(nameof(TrainPrefixes));
                }
            }
        }

        /// <summary>
        /// 选中的出发站
        /// </summary>
        public DepartStationItem SelectedDepartStation
        {
            get => _selectedDepartStation;
            set
            {
                if (_selectedDepartStation != value)
                {
                    _selectedDepartStation = value;
                    OnPropertyChanged(nameof(SelectedDepartStation));
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

        /// <summary>
        /// 选中的年份选项
        /// </summary>
        public YearOption SelectedYearOption
        {
            get => _selectedYearOption;
            set
            {
                if (_selectedYearOption != value)
                {
                    _selectedYearOption = value;
                    IsCustomYearSelected = value?.IsCustom ?? false;
                    
                    // 如果选择了自定义年份选项，直接弹出对话框
                    if (value?.IsCustom == true)
                    {
                        SelectCustomYear();
                    }
                    
                    OnPropertyChanged(nameof(SelectedYearOption));
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

        /// <summary>
        /// 自定义年份是否被选中
        /// </summary>
        public bool IsCustomYearSelected
        {
            get => _isCustomYearSelected;
            set
            {
                if (_isCustomYearSelected != value)
                {
                    _isCustomYearSelected = value;
                    OnPropertyChanged(nameof(IsCustomYearSelected));
                }
            }
        }

        /// <summary>
        /// 自定义年份
        /// </summary>
        public int? CustomYear
        {
            get => _customYear;
            set
            {
                if (_customYear != value)
                {
                    _customYear = value;
                    OnPropertyChanged(nameof(CustomYear));

                    // 如果已经选择了自定义年份选项，更新它的值
                    if (SelectedYearOption?.IsCustom == true && _yearOptions != null)
                    {
                        var customOption = _yearOptions.FirstOrDefault(y => y.IsCustom);
                        if (customOption != null)
                        {
                            customOption.Year = value;
                            customOption.DisplayName = value.HasValue ? $"自定义: {value}" : "自定义年份";
                            OnPropertyChanged(nameof(YearOptions));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 年份选项列表
        /// </summary>
        public List<YearOption> YearOptions
        {
            get => _yearOptions;
            set
            {
                if (_yearOptions != value)
                {
                    _yearOptions = value;
                    OnPropertyChanged(nameof(YearOptions));
                }
            }
        }

        /// <summary>
        /// 出发站列表
        /// </summary>
        public ObservableCollection<DepartStationItem> DepartStations
        {
            get => _departStations;
            set
            {
                if (_departStations != value)
                {
                    _departStations = value;
                    OnPropertyChanged(nameof(DepartStations));
                }
            }
        }

        /// <summary>
        /// 出发站搜索建议
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
        /// 出发站下拉框是否打开
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
        /// 出发站搜索文本
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
                    
                    // 如果是通过选择项更新的，不触发搜索
                    if (!_isUpdatingDepartStation)
                    {
                        // 移除"站"字后搜索
                        string searchText = value?.Replace("站", "").Trim() ?? string.Empty;
                        SearchStations(searchText);
                    }
                }
            }
        }

        /// <summary>
        /// 是否使用AND条件
        /// </summary>
        public bool IsAndCondition
        {
            get => _isAndCondition;
            set
            {
                if (_isAndCondition != value)
                {
                    _isAndCondition = value;
                    _isOrCondition = !value;
                    OnPropertyChanged(nameof(IsAndCondition));
                    OnPropertyChanged(nameof(IsOrCondition));
                }
            }
        }

        /// <summary>
        /// 是否使用OR条件
        /// </summary>
        public bool IsOrCondition
        {
            get => _isOrCondition;
            set
            {
                if (_isOrCondition != value)
                {
                    _isOrCondition = value;
                    _isAndCondition = !value;
                    OnPropertyChanged(nameof(IsOrCondition));
                    OnPropertyChanged(nameof(IsAndCondition));
                }
            }
        }

        /// <summary>
        /// 是否有激活的筛选条件
        /// </summary>
        public bool HasActiveFilters
        {
            get => _hasActiveFilters;
            set
            {
                if (_hasActiveFilters != value)
                {
                    _hasActiveFilters = value;
                    OnPropertyChanged(nameof(HasActiveFilters));
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

        /// <summary>
        /// 查询按钮文本
        /// </summary>
        public string QueryButtonText
        {
            get => HasAnyActiveFilter() ? "查询" : "查询全部";
        }

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => TrainRideInfos != null && TrainRideInfos.Count > 0;

        /// <summary>
        /// 是否无数据
        /// </summary>
        public bool HasNoData => !HasData;

        #endregion

        #region 命令

        /// <summary>
        /// 首页命令
        /// </summary>
        public System.Windows.Input.ICommand FirstPageCommand => _paginationViewModel.FirstPageCommand;

        /// <summary>
        /// 上一页命令
        /// </summary>
        public System.Windows.Input.ICommand PreviousPageCommand => _paginationViewModel.PreviousPageCommand;

        /// <summary>
        /// 下一页命令
        /// </summary>
        public System.Windows.Input.ICommand NextPageCommand => _paginationViewModel.NextPageCommand;

        /// <summary>
        /// 末页命令
        /// </summary>
        public System.Windows.Input.ICommand LastPageCommand => _paginationViewModel.LastPageCommand;

        /// <summary>
        /// 切换查询面板命令
        /// </summary>
        public ICommand ToggleQueryPanelCommand { get; }

        /// <summary>
        /// 应用筛选条件命令
        /// </summary>
        public ICommand ApplyFilterCommand { get; }

        /// <summary>
        /// 重置筛选条件命令
        /// </summary>
        public ICommand ResetFilterCommand { get; }

        /// <summary>
        /// 选择自定义年份命令
        /// </summary>
        public ICommand CustomYearCommand { get; }

        /// <summary>
        /// 清空出发站条件命令
        /// </summary>
        public ICommand ClearDepartStationCommand { get; }

        /// <summary>
        /// 清空车次号条件命令
        /// </summary>
        public ICommand ClearTrainNumberCommand { get; }

        /// <summary>
        /// 清空年份条件命令
        /// </summary>
        public ICommand ClearYearCommand { get; }

        /// <summary>
        /// 选择出发站命令
        /// </summary>
        public ICommand SelectDepartStationCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 初始化年份选项列表
        /// </summary>
        private void InitializeYearOptions()
        {
            int currentYear = DateTime.Now.Year;
            
            YearOptions = new List<YearOption>
            {
                new YearOption(currentYear, "今年"),
                new YearOption(currentYear - 1, "去年"),
                new YearOption(currentYear - 2, "前年"),
                new YearOption(null, "自定义年份", true)
            };
            
            // 默认选择今年
            SelectedYearOption = YearOptions.FirstOrDefault();
        }

        /// <summary>
        /// 初始化车次前缀
        /// </summary>
        private void InitializeTrainPrefixes()
        {
            TrainPrefixes = new List<string> 
            {
                "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字"
            };
            
            // 默认选择G
            SelectedTrainPrefix = TrainPrefixes.FirstOrDefault();
        }

        /// <summary>
        /// 异步加载出发站列表
        /// </summary>
        private async void LoadDepartStationsAsync()
        {
            try
            {
                // 获取已有的出发站点
                var departStations = await _databaseService.GetDistinctDepartStationsAsync();
                
                // 转换为DepartStationItem列表
                var departStationItems = departStations
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => new DepartStationItem(s))
                    .ToList();
                
                // 添加一个空选项
                departStationItems.Insert(0, null);
                
                DepartStations = new ObservableCollection<DepartStationItem>(departStationItems);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载出发站列表时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索站点
        /// </summary>
        private async void SearchStations(string searchText)
        {
            try
            {
                // 如果正在更新，不执行搜索
                if (_isUpdatingDepartStation)
                    return;

                // 清空搜索结果
                DepartStationSuggestions.Clear();
                IsDepartStationDropdownOpen = false;

                // 如果搜索文本为空，不执行搜索
                if (string.IsNullOrWhiteSpace(searchText))
                    return;

                // 搜索出发站
                var stations = await _databaseService.SearchStationsByNameAsync(searchText);

                // 添加到建议列表
                foreach (var station in stations)
                {
                    DepartStationSuggestions.Add(station);
                }
                
                // 如果有结果，显示下拉框
                IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索出发站时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 选择出发站
        /// </summary>
        private void SelectDepartStation(StationInfo station)
        {
            if (station == null)
                return;
                
            // 确保车站名称不包含"站"字
            string stationName = station.StationName?.Replace("站", "") ?? string.Empty;
            
            // 先关闭下拉框，防止触发搜索
            IsDepartStationDropdownOpen = false;
            
            // 暂时取消DepartStationSearchText的PropertyChanged事件触发
            _isUpdatingDepartStation = true;
            DepartStationSearchText = stationName;
            _isUpdatingDepartStation = false;
            
            // 创建并设置选中的出发站
            SelectedDepartStation = new DepartStationItem(stationName);
            
            // 不要自动应用筛选，等待用户点击查询按钮
            // ApplyFilter();
        }

        /// <summary>
        /// 选择自定义年份
        /// </summary>
        private void SelectCustomYear()
        {
            // 创建一个对话框获取用户输入的年份
            string title = "输入自定义年份";
            string prompt = "请输入年份 (1900-2099):";
            string initialValue = CustomYear?.ToString() ?? DateTime.Now.Year.ToString();
            
            var result = MessageBoxHelper.ShowInputDialog(title, prompt, initialValue);
            
            if (result.IsConfirmed)
            {
                // 验证年份输入
                if (int.TryParse(result.InputText, out int year) && year >= 1900 && year <= 2099)
                {
                    CustomYear = year;
                    
                    // 更新自定义年份选项
                    var customOption = YearOptions.FirstOrDefault(y => y.IsCustom);
                    if (customOption != null)
                    {
                        customOption.Year = year;
                        customOption.DisplayName = $"自定义: {year}";
                        OnPropertyChanged(nameof(YearOptions));
                    }
                    
                    // 不要自动应用筛选条件
                    // ApplyFilter();
                }
                else
                {
                    MessageBoxHelper.ShowError("年份必须是1900-2099之间的整数。");
                    // 恢复选择非自定义年份
                    SelectedYearOption = YearOptions.FirstOrDefault(y => !y.IsCustom);
                }
            }
            else
            {
                // 用户取消，恢复选择非自定义年份
                SelectedYearOption = YearOptions.FirstOrDefault(y => !y.IsCustom);
            }
        }

        /// <summary>
        /// 切换查询面板可见性
        /// </summary>
        private void ToggleQueryPanel()
        {
            IsQueryPanelVisible = !IsQueryPanelVisible;
        }

        /// <summary>
        /// 获取完整的车次号
        /// </summary>
        private string GetFullTrainNo()
        {
            if (string.IsNullOrEmpty(SelectedTrainPrefix) || SelectedTrainPrefix == "纯数字")
            {
                return TrainNumberFilter;
            }
            else
            {
                return $"{SelectedTrainPrefix}{TrainNumberFilter}";
            }
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private async void ApplyFilter()
        {
            try
            {
                _paginationViewModel.IsLoading = true;
                
                // 检查是否有筛选条件
                HasActiveFilters = HasAnyActiveFilter();
                
                // 构建完整的车次号
                string fullTrainNo = null;
                if (!string.IsNullOrWhiteSpace(TrainNumberFilter))
                {
                    fullTrainNo = GetFullTrainNo();
                }
                
                // 获取年份值
                int? yearValue = null;
                if (SelectedYearOption?.Year.HasValue == true)
                {
                    yearValue = SelectedYearOption.Year.Value;
                }
                
                // 记录查询条件
                Console.WriteLine("应用查询条件:");
                Console.WriteLine($"  出发站: {_selectedDepartStation?.DepartStation}");
                Console.WriteLine($"  车次号: {fullTrainNo}");
                Console.WriteLine($"  出发年份: {yearValue}");
                Console.WriteLine($"  查询条件组合方式: {(_isAndCondition ? "AND" : "OR")}");
                
                // 获取带筛选条件的总记录数
                int totalCount = await _databaseService.GetFilteredTrainRideInfoCountAsync(
                    _selectedDepartStation?.DepartStation,
                    fullTrainNo,
                    yearValue,
                    _isAndCondition);
                
                // 设置总记录数，这会触发TotalPages的重新计算
                _paginationViewModel.TotalItems = totalCount;
                
                // 重置到第一页
                _paginationViewModel.CurrentPage = 1;
                
                // 清除缓存
                _paginationViewModel.ClearCache();
                
                // 加载第一页数据
                await LoadPageDataAsync();
                
                // 标记为已初始化
                _paginationViewModel.IsInitialized = true;
                
                // 显示数据表格
                _mainViewModel.ShowQueryAllTickets = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(TrainRideInfos));
                
                // 手动触发导航按钮状态更新
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 手动刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"应用筛选条件时出错: {ex.Message}");
            }
            finally
            {
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 重置筛选条件
        /// </summary>
        private void ResetFilter()
        {
            TrainNumberFilter = null;
            SelectedTrainPrefix = TrainPrefixes.FirstOrDefault();
            SelectedDepartStation = null;
            SelectedYearOption = null;
            CustomYear = null;
            IsAndCondition = true;
            HasActiveFilters = false;
            DepartStationSearchText = null;
        }

        /// <summary>
        /// 检查是否有任何激活的筛选条件
        /// </summary>
        private bool HasAnyActiveFilter()
        {
            return SelectedDepartStation != null || 
                   !string.IsNullOrWhiteSpace(TrainNumberFilter) || 
                   SelectedYearOption?.Year.HasValue == true;
        }

        /// <summary>
        /// 清空出发站条件
        /// </summary>
        private void ClearDepartStation()
        {
            SelectedDepartStation = null;
            DepartStationSearchText = null;
            // 不要自动应用筛选
            // ApplyFilter();
        }

        /// <summary>
        /// 清空车次号条件
        /// </summary>
        private void ClearTrainNumber()
        {
            TrainNumberFilter = null;
            SelectedTrainPrefix = TrainPrefixes.FirstOrDefault();
            // 不要自动应用筛选
            // ApplyFilter();
        }

        /// <summary>
        /// 清空年份条件
        /// </summary>
        private void ClearYear()
        {
            SelectedYearOption = null;
            CustomYear = null;
            // 不要自动应用筛选
            // ApplyFilter();
        }

        #endregion
    }
} 