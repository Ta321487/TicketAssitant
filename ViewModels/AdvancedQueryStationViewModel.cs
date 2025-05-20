using System; 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class AdvancedQueryStationViewModel : BaseViewModel
    {
        #region 字段

        private bool _isQueryPanelVisible;
        private string _stationSearchText = string.Empty;
        private string _selectedProvince;
        private string _selectedCity;
        private string _selectedDistrict;
        private bool _useMyDepartStations;
        private bool _hasActiveFilters;
        private ObservableCollection<StationInfo> _stationSuggestions = new();
        private bool _isStationDropdownOpen;
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;
        private List<string> _provinces = new();
        private List<string> _cities = new();
        private List<string> _districts = new();
        private List<string> _myDepartStations = new();

        // 标记是否来自手动选择
        private bool _isFromSelection = false;

        #endregion

        /// <summary>
        /// 已应用筛选事件
        /// </summary>
        public event EventHandler<StationQueryFilterEventArgs> FilterApplied;

        #region 构造函数

        /// <summary>
        /// 默认构造函数，用于设计时
        /// </summary>
        public AdvancedQueryStationViewModel()
        {
            // 初始化命令
            ToggleQueryPanelCommand = new RelayCommand(ToggleQueryPanel);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
            ClearStationNameCommand = new RelayCommand(ClearStationName);
            ClearProvinceCommand = new RelayCommand(ClearProvince);
            SelectStationCommand = new RelayCommand<StationInfo>(SelectStation);

            // 设置设计时数据
            _isQueryPanelVisible = true;

            // 初始化站点建议列表
            StationSuggestions = new ObservableCollection<StationInfo>();

            // 初始化地区数据
            InitializeRegionData();
        }

        public AdvancedQueryStationViewModel(DatabaseService databaseService, StationSearchService stationSearchService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));

            // 初始化命令
            ToggleQueryPanelCommand = new RelayCommand(ToggleQueryPanel);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
            ClearStationNameCommand = new RelayCommand(ClearStationName);
            ClearProvinceCommand = new RelayCommand(ClearProvince);
            SelectStationCommand = new RelayCommand<StationInfo>(SelectStation);

            // 初始化站点建议列表
            StationSuggestions = new ObservableCollection<StationInfo>();

            // 初始化地区数据
            LoadRegionDataAsync();
            
            // 加载我的出发车站
            LoadMyDepartStationsAsync();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化地区数据（设计时）
        /// </summary>
        private void InitializeRegionData()
        {
            // 设计时模拟数据
            Provinces = new List<string> { "北京市", "天津市", "上海市", "重庆市", "河北省" };
            Cities = new List<string> { "北京市", "天津市", "上海市", "重庆市", "石家庄市" };
            Districts = new List<string> { "东城区", "西城区", "朝阳区", "海淀区", "丰台区" };
        }

        /// <summary>
        /// 加载地区数据（运行时）
        /// </summary>
        private async void LoadRegionDataAsync()
        {
            try
            {
                // 确保车站服务已初始化
                await _stationSearchService.InitializeAsync();

                // 获取所有省份列表
                var stations = _stationSearchService.Stations;
                
                // 提取所有不同的省份
                var provinces = stations
                    .Where(s => !string.IsNullOrEmpty(s.Province))
                    .Select(s => s.Province)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
                
                Provinces = provinces;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载省份列表时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据省份加载城市列表
        /// </summary>
        private async void LoadCitiesAsync(string province)
        {
            if (string.IsNullOrEmpty(province))
            {
                Cities = new List<string>();
                return;
            }

            try
            {
                // 确保车站服务已初始化
                await _stationSearchService.InitializeAsync();

                // 获取指定省份的所有城市
                var cities = _stationSearchService.Stations
                    .Where(s => s.Province == province && !string.IsNullOrEmpty(s.City))
                    .Select(s => s.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                // 在列表开头添加一个空字符串，对应ComboBox中的空选项
                cities.Insert(0, "");
                
                Cities = cities;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载城市列表时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据省份和城市加载区县列表
        /// </summary>
        private async void LoadDistrictsAsync(string province, string city)
        {
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(city))
            {
                Districts = new List<string>();
                return;
            }

            try
            {
                // 确保车站服务已初始化
                await _stationSearchService.InitializeAsync();

                // 获取指定省份和城市的所有区县
                var districts = _stationSearchService.Stations
                    .Where(s => s.Province == province && s.City == city && !string.IsNullOrEmpty(s.District))
                    .Select(s => s.District)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
                
                // 在列表开头添加一个空字符串，对应ComboBox中的空选项
                districts.Insert(0, "");

                Districts = districts;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载区县列表时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载我的出发车站
        /// </summary>
        private async void LoadMyDepartStationsAsync()
        {
            try
            {
                var departStations = await _databaseService.GetDistinctDepartStationsAsync();
                MyDepartStations = departStations
                    .Where(s => !string.IsNullOrEmpty(s))
                    .OrderBy(s => s)
                    .ToList();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载我的出发车站列表时出错: {ex.Message}", ex);
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
        /// 车站搜索文本（名称或拼音）
        /// </summary>
        public string StationSearchText
        {
            get => _stationSearchText;
            set
            {
                if (_stationSearchText != value)
                {
                    _stationSearchText = value;
                    OnPropertyChanged(nameof(StationSearchText));
                    OnPropertyChanged(nameof(QueryButtonText));
                    
                    // 如果是从选择触发的文本变更，不要再搜索
                    if (_isFromSelection)
                    {
                        _isFromSelection = false;
                        return;
                    }
                    
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        StationSuggestions.Clear();
                        IsStationDropdownOpen = false;
                        return;
                    }
                    
                    // 判断输入是中文名称还是拼音
                    if (IsPinyin(value))
                    {
                        // 至少2个字母开始搜索拼音
                        if (value.Length >= 2)
                        {
                            SearchStationsByPinyin(value);
                        }
                    }
                    else
                    {
                        SearchStationsByName(value);
                    }
                }
            }
        }

        /// <summary>
        /// 选中的省份
        /// </summary>
        public string SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                if (_selectedProvince != value)
                {
                    _selectedProvince = value;
                    OnPropertyChanged(nameof(SelectedProvince));
                    OnPropertyChanged(nameof(QueryButtonText));
                    
                    // 当省份变化时，重新加载城市列表
                    LoadCitiesAsync(value);
                    
                    // 清空城市和区县选择
                    SelectedCity = null;
                    SelectedDistrict = null;
                }
            }
        }

        /// <summary>
        /// 选中的城市
        /// </summary>
        public string SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (_selectedCity != value)
                {
                    _selectedCity = value;
                    OnPropertyChanged(nameof(SelectedCity));
                    OnPropertyChanged(nameof(QueryButtonText));
                    
                    // 当城市变化时，重新加载区县列表
                    LoadDistrictsAsync(_selectedProvince, value);
                    
                    // 清空区县选择
                    SelectedDistrict = null;
                }
            }
        }

        /// <summary>
        /// 选中的区县
        /// </summary>
        public string SelectedDistrict
        {
            get => _selectedDistrict;
            set
            {
                if (_selectedDistrict != value)
                {
                    _selectedDistrict = value;
                    OnPropertyChanged(nameof(SelectedDistrict));
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

        /// <summary>
        /// 是否使用我的出发车站
        /// </summary>
        public bool UseMyDepartStations
        {
            get => _useMyDepartStations;
            set
            {
                if (_useMyDepartStations != value)
                {
                    _useMyDepartStations = value;
                    OnPropertyChanged(nameof(UseMyDepartStations));
                    OnPropertyChanged(nameof(QueryButtonText));
                }
            }
        }

        /// <summary>
        /// 我的出发车站列表
        /// </summary>
        public List<string> MyDepartStations
        {
            get => _myDepartStations;
            set
            {
                if (_myDepartStations != value)
                {
                    _myDepartStations = value;
                    OnPropertyChanged(nameof(MyDepartStations));
                }
            }
        }

        /// <summary>
        /// 省份列表
        /// </summary>
        public List<string> Provinces
        {
            get => _provinces;
            set
            {
                if (_provinces != value)
                {
                    _provinces = value;
                    OnPropertyChanged(nameof(Provinces));
                }
            }
        }

        /// <summary>
        /// 城市列表
        /// </summary>
        public List<string> Cities
        {
            get => _cities;
            set
            {
                if (_cities != value)
                {
                    _cities = value;
                    OnPropertyChanged(nameof(Cities));
                }
            }
        }

        /// <summary>
        /// 区县列表
        /// </summary>
        public List<string> Districts
        {
            get => _districts;
            set
            {
                if (_districts != value)
                {
                    _districts = value;
                    OnPropertyChanged(nameof(Districts));
                }
            }
        }

        /// <summary>
        /// 车站建议下拉列表
        /// </summary>
        public ObservableCollection<StationInfo> StationSuggestions
        {
            get => _stationSuggestions;
            set
            {
                if (_stationSuggestions != value)
                {
                    _stationSuggestions = value;
                    OnPropertyChanged(nameof(StationSuggestions));
                }
            }
        }

        /// <summary>
        /// 车站下拉列表是否打开
        /// </summary>
        public bool IsStationDropdownOpen
        {
            get => _isStationDropdownOpen;
            set
            {
                if (_isStationDropdownOpen != value)
                {
                    _isStationDropdownOpen = value;
                    OnPropertyChanged(nameof(IsStationDropdownOpen));
                }
            }
        }

        /// <summary>
        /// 是否有活动筛选条件
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
            get
            {
                return HasAnyActiveFilter() ? "查询" : "查询全部";
            }
        }

        #endregion

        #region 命令

        public ICommand ToggleQueryPanelCommand { get; }

        public ICommand ApplyFilterCommand { get; }

        public ICommand ResetFilterCommand { get; }

        public ICommand ClearStationNameCommand { get; }

        public ICommand ClearProvinceCommand { get; }

        public ICommand SelectStationCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 判断文本是否为拼音（仅包含字母）
        /// </summary>
        private bool IsPinyin(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            // 判断是否只包含字母
            return text.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        }

        /// <summary>
        /// 根据站名搜索车站
        /// </summary>
        private async void SearchStationsByName(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                StationSuggestions.Clear();
                IsStationDropdownOpen = false;
                return;
            }

            try
            {
                var stations = await _stationSearchService.SearchStationsAsync(searchText);
                StationSuggestions.Clear();
                foreach (var station in stations)
                {
                    StationSuggestions.Add(station);
                }
                IsStationDropdownOpen = StationSuggestions.Count > 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据拼音搜索车站
        /// </summary>
        private async void SearchStationsByPinyin(string pinyin)
        {
            if (string.IsNullOrWhiteSpace(pinyin) || pinyin.Length < 2)
            {
                StationSuggestions.Clear();
                IsStationDropdownOpen = false;
                return;
            }

            try
            {
                // 获取所有车站
                await _stationSearchService.InitializeAsync();
                var allStations = _stationSearchService.Stations;
                
                // 筛选拼音匹配的车站
                var matchedStations = allStations
                    .Where(s => !string.IsNullOrEmpty(s.StationPinyin) && 
                               s.StationPinyin.StartsWith(pinyin, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.StationPinyin.Length)
                    .ThenBy(s => s.StationPinyin)
                    .Take(10)
                    .ToList();
                
                StationSuggestions.Clear();
                foreach (var station in matchedStations)
                {
                    StationSuggestions.Add(station);
                }
                IsStationDropdownOpen = StationSuggestions.Count > 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"根据拼音搜索车站时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 选择车站
        /// </summary>
        private void SelectStation(StationInfo station)
        {
            if (station != null)
            {
                // 设置标记，防止触发搜索
                _isFromSelection = true;
                
                // 设置车站名称
                StationSearchText = station.StationName;
                
                // 关闭下拉框并清空建议列表
                IsStationDropdownOpen = false;
                StationSuggestions.Clear();
            }
        }

        /// <summary>
        /// 切换查询面板显示状态
        /// </summary>
        private void ToggleQueryPanel()
        {
            IsQueryPanelVisible = !IsQueryPanelVisible;
        }

        /// <summary>
        /// 检查是否有任何活动筛选条件
        /// </summary>
        private bool HasAnyActiveFilter()
        {
            return !string.IsNullOrEmpty(StationSearchText) ||
                   !string.IsNullOrEmpty(SelectedProvince) ||
                   !string.IsNullOrEmpty(SelectedCity) ||
                   !string.IsNullOrEmpty(SelectedDistrict) ||
                   UseMyDepartStations;
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilter()
        {
            // 更新是否有活动筛选条件
            HasActiveFilters = HasAnyActiveFilter();
            
            // 通知查询条件变更
            FilterApplied?.Invoke(this, new StationQueryFilterEventArgs
            {
                StationName = StationSearchText,
                Province = SelectedProvince,
                City = SelectedCity,
                District = SelectedDistrict,
                UseMyDepartStations = UseMyDepartStations,
                MyDepartStations = UseMyDepartStations ? MyDepartStations : null
            });
            
            // 不再关闭查询面板，让用户自己控制关闭
            // IsQueryPanelVisible = false;
        }

        /// <summary>
        /// 重置筛选条件
        /// </summary>
        public void ResetFilter()
        {
            StationSearchText = string.Empty;
            SelectedProvince = null;
            SelectedCity = null;
            SelectedDistrict = null;
            UseMyDepartStations = false;
            
            // 更新活动筛选状态
            HasActiveFilters = false;
            
            // 清空下拉列表
            StationSuggestions.Clear();
            IsStationDropdownOpen = false;
        }

        /// <summary>
        /// 清空车站搜索文本
        /// </summary>
        private void ClearStationName()
        {
            StationSearchText = string.Empty;
        }

        /// <summary>
        /// 清空省份选择
        /// </summary>
        private void ClearProvince()
        {
            SelectedProvince = null;
            SelectedCity = null;
            SelectedDistrict = null;
        }

        #endregion
    }

    /// <summary>
    /// 车站查询筛选条件事件参数
    /// </summary>
    public class StationQueryFilterEventArgs : EventArgs
    {
        public string StationName { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public bool UseMyDepartStations { get; set; }
        public List<string> MyDepartStations { get; set; }
    }
} 