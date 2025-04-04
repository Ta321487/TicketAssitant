using System.Collections.ObjectModel;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows.Input;

namespace TA_WPF.ViewModels
{
    public class AdvancedQueryTicketViewModel : BaseViewModel
    {
        #region 字段

        private bool _isQueryPanelVisible;
        private string _trainNumberFilter = string.Empty;
        private string _selectedTrainPrefix = string.Empty;
        private DepartStationItem? _selectedDepartStation;
        private YearOption? _selectedYearOption;
        private List<YearOption> _yearOptions = new();
        private ObservableCollection<DepartStationItem> _departStations = new();
        private List<string> _trainPrefixes = new();
        private bool _isAndCondition = true;
        private bool _isOrCondition;
        private bool _hasActiveFilters;
        private int? _customYear;
        private bool _isCustomYearSelected;
        private ObservableCollection<StationInfo> _departStationSuggestions = new();
        private bool _isDepartStationDropdownOpen;
        private string _departStationSearchText = string.Empty;
        private bool _isUpdatingDepartStation = false;
        private readonly DatabaseService _databaseService;

        #endregion

        /// <summary>
        /// 已应用筛选事件
        /// </summary>
        public event EventHandler<QueryFilterEventArgs> FilterApplied;

        /// <summary>
        /// 默认构造函数，用于设计时
        /// </summary>
        public AdvancedQueryTicketViewModel()
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
            
            // 设置设计时数据
            _isQueryPanelVisible = true;
            
            // 初始化车次前缀
            InitializeTrainPrefixes();
            
            // 初始化年份选项
            InitializeYearOptions();
            
            // 初始化站点建议列表
            DepartStationSuggestions = new ObservableCollection<StationInfo>();
            
            // 设计时不加载数据，但创建空的出发站列表
            DepartStations = new ObservableCollection<DepartStationItem>();
            
            // 设计时的数据库服务为空
            _databaseService = null;
        }

        public AdvancedQueryTicketViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
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

        #region 初始化方法

        /// <summary>
        /// 初始化年份选项
        /// </summary>
        private void InitializeYearOptions()
        {
            // 获取当前年份
            int currentYear = DateTime.Now.Year;
            
            // 创建年份选项列表
            YearOptions = new List<YearOption>
            {
                null, // 不筛选年份
                new YearOption(currentYear, $"{currentYear}年"),
                new YearOption(currentYear - 1, $"{currentYear - 1}年"),
                new YearOption(currentYear - 2, $"{currentYear - 2}年"),
                new YearOption(currentYear - 3, $"{currentYear - 3}年"),
                new YearOption(null, "自定义年份", true) // 自定义年份选项
            };
        }

        /// <summary>
        /// 初始化车次前缀
        /// </summary>
        private void InitializeTrainPrefixes()
        {
            // 设置车次前缀列表
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
                
                // 添加一个表示"不筛选"的选项，使用空字符串
                departStationItems.Insert(0, new DepartStationItem(string.Empty));
                
                DepartStations = new ObservableCollection<DepartStationItem>(departStationItems);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载出发站列表时出错: {ex.Message}");
            }
        }

        #endregion

        #region 属性

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
        public DepartStationItem? SelectedDepartStation
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
        public YearOption? SelectedYearOption
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
                    if (SelectedYearOption?.IsCustom == true && _yearOptions != null && _yearOptions.Count > 0)
                    {
                        var customOption = _yearOptions.FirstOrDefault(y => y != null && y.IsCustom);
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

        #endregion

        #region 命令

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
        /// 清空出发站命令
        /// </summary>
        public ICommand ClearDepartStationCommand { get; }

        /// <summary>
        /// 清空车次号命令
        /// </summary>
        public ICommand ClearTrainNumberCommand { get; }

        /// <summary>
        /// 清空年份命令
        /// </summary>
        public ICommand ClearYearCommand { get; }

        /// <summary>
        /// 选择出发站命令
        /// </summary>
        public ICommand SelectDepartStationCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 搜索站点
        /// </summary>
        private async void SearchStations(string searchText)
        {
            try
            {
                // 如果正在更新或数据库服务为null，不执行搜索
                if (_isUpdatingDepartStation || _databaseService == null)
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
                    if (YearOptions != null && YearOptions.Count > 0)
                    {
                        var customOption = YearOptions.FirstOrDefault(y => y != null && y.IsCustom);
                        if (customOption != null)
                        {
                            customOption.Year = year;
                            customOption.DisplayName = $"自定义: {year}";
                            OnPropertyChanged(nameof(YearOptions));
                        }
                        else
                        {
                            // 如果没有找到自定义选项，创建一个新的
                            var newCustomOption = new YearOption(year, $"自定义: {year}", true);
                            YearOptions.Add(newCustomOption);
                            SelectedYearOption = newCustomOption;
                            OnPropertyChanged(nameof(YearOptions));
                        }
                    }
                    else
                    {
                        // 如果YearOptions为空，初始化它
                        InitializeYearOptions();
                        // 重新尝试设置自定义年份
                        var customOption = YearOptions.FirstOrDefault(y => y != null && y.IsCustom);
                        if (customOption != null)
                        {
                            customOption.Year = year;
                            customOption.DisplayName = $"自定义: {year}";
                            SelectedYearOption = customOption;
                            OnPropertyChanged(nameof(YearOptions));
                        }
                    }
                }
                else
                {
                    MessageBoxHelper.ShowError("年份必须是1900-2099之间的整数。");
                    // 恢复选择非自定义年份，确保YearOptions不为空
                    if (YearOptions != null && YearOptions.Count > 0)
                    {
                        SelectedYearOption = YearOptions.FirstOrDefault(y => y != null && !y.IsCustom);
                    }
                }
            }
            else
            {
                // 用户取消，恢复选择非自定义年份，确保YearOptions不为空
                if (YearOptions != null && YearOptions.Count > 0)
                {
                    SelectedYearOption = YearOptions.FirstOrDefault(y => y != null && !y.IsCustom);
                }
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
        public string GetFullTrainNo()
        {
            if (string.IsNullOrEmpty(_selectedTrainPrefix) || _selectedTrainPrefix == "纯数字")
            {
                return _trainNumberFilter;
            }
            else
            {
                return $"{_selectedTrainPrefix}{_trainNumberFilter}";
            }
        }

        /// <summary>
        /// 检测是否有任何激活的筛选条件
        /// </summary>
        private bool HasAnyActiveFilter()
        {
            bool hasDepartStation = _selectedDepartStation != null && !string.IsNullOrWhiteSpace(_selectedDepartStation.DepartStation);
            bool hasTrainNumber = !string.IsNullOrWhiteSpace(_trainNumberFilter);
            bool hasYear = _selectedYearOption != null && _selectedYearOption.Year.HasValue;
            
            return hasDepartStation || hasTrainNumber || hasYear;
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                // 检测是否有筛选条件
                HasActiveFilters = HasAnyActiveFilter();
                
                // 构建完整的车次号
                string fullTrainNo = null;
                if (!string.IsNullOrWhiteSpace(_trainNumberFilter))
                {
                    fullTrainNo = GetFullTrainNo();
                }
                
                // 获取年份值
                int? yearValue = null;
                if (_selectedYearOption != null && _selectedYearOption.Year.HasValue)
                {
                    yearValue = _selectedYearOption.Year.Value;
                }
                
                // 获取出发站
                string departStation = null;
                if (_selectedDepartStation != null)
                {
                    departStation = _selectedDepartStation.DepartStation;
                }
                
                // 记录查询条件
                System.Diagnostics.Debug.WriteLine("应用查询条件:");
                System.Diagnostics.Debug.WriteLine($"  出发站: {departStation}");
                System.Diagnostics.Debug.WriteLine($"  车次号: {fullTrainNo}");
                System.Diagnostics.Debug.WriteLine($"  出发年份: {yearValue}");
                System.Diagnostics.Debug.WriteLine($"  查询条件组合方式: {(_isAndCondition ? "AND" : "OR")}");

                // 触发筛选条件应用事件
                FilterApplied?.Invoke(this, new QueryFilterEventArgs
                {
                    DepartStation = departStation,
                    FullTrainNo = fullTrainNo,
                    Year = yearValue,
                    IsAndCondition = _isAndCondition
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"应用筛选条件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置所有筛选条件
        /// </summary>
        public void ResetFilter()
        {
            TrainNumberFilter = string.Empty;
            SelectedDepartStation = null;
            SelectedYearOption = null;
            CustomYear = null;
            SelectedTrainPrefix = TrainPrefixes.FirstOrDefault() ?? "G";
            IsAndCondition = true;
            DepartStationSearchText = string.Empty;
            
            HasActiveFilters = false;
            
            // 触发事件
            FilterApplied?.Invoke(this, new QueryFilterEventArgs
            {
                DepartStation = null,
                FullTrainNo = null,
                Year = null,
                IsAndCondition = true
            });
        }

        /// <summary>
        /// 清空出发站条件
        /// </summary>
        private void ClearDepartStation()
        {
            SelectedDepartStation = null;
            DepartStationSearchText = string.Empty;
            // 不要自动应用筛选，等待用户点击查询按钮
            // ApplyFilter();
        }
        
        /// <summary>
        /// 清空车次号条件
        /// </summary>
        private void ClearTrainNumber()
        {
            TrainNumberFilter = string.Empty;
            SelectedTrainPrefix = TrainPrefixes.FirstOrDefault() ?? "G";
            // 不要自动应用筛选，等待用户点击查询按钮
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

    /// <summary>
    /// 查询筛选事件参数
    /// </summary>
    public class QueryFilterEventArgs : EventArgs
    {
        public string DepartStation { get; set; }
        public string FullTrainNo { get; set; }
        public int? Year { get; set; }
        public bool IsAndCondition { get; set; }
    }
} 