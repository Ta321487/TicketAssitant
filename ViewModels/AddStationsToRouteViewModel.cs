using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 添加车站到路线视图模型
    /// </summary>
    public class AddStationsToRouteViewModel : BaseViewModel
    {
        private readonly RouteInfo _routeInfo;
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;
        private readonly DistanceCalculationService _distanceCalculationService;
        private readonly MainViewModel _mainViewModel;
        private readonly ConfigurationService _configurationService;
        private readonly Action _refreshCallback;
        private bool _isLoading;
        private double _fontSize;
        private string _windowTitle;
        private bool _isDarkMode;

        // 防抖定时器
        private Timer _searchDebounceTimer;
        private const int DebounceDelay = 300; // 防抖延迟毫秒数

        // 车站相关属性
        private StationInfo _selectedStation;
        private string _stationSearchText;
        private bool _isStationDropdownOpen;
        private ObservableCollection<StationInfo> _stationSuggestions;

        // 车站映射属性
        private decimal? _distanceFromStart;
        private decimal? _distanceFromPrev;
        private int _stayTime;
        private string _notes;

        // 角色属性
        private bool _isStartStation;
        private bool _isEndStation;
        private bool _isPassingStation;
        private bool _isTransferStation;

        // 添加位置
        private bool _isFirstStation;
        private bool _showAddPositionOptions;
        private bool _addToStart;
        private bool _addToEnd;
        private RouteStationMapping _previousStation; // 前一个站点
        private RouteStationMapping _nextStation; // 后一个站点（添加到开头时使用）

        /// <summary>
        /// 构造函数
        /// </summary>
        public AddStationsToRouteViewModel(
            RouteInfo routeInfo,
            DatabaseService databaseService,
            StationSearchService stationSearchService,
            DistanceCalculationService distanceCalculationService,
            MainViewModel mainViewModel,
            ConfigurationService configurationService,
            Action refreshCallback)
        {
            _routeInfo = routeInfo ?? throw new ArgumentNullException(nameof(routeInfo));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
            _distanceCalculationService = distanceCalculationService ?? throw new ArgumentNullException(nameof(distanceCalculationService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _refreshCallback = refreshCallback;

            // 初始化集合
            _stationSuggestions = new ObservableCollection<StationInfo>();

            // 初始化命令
            SaveCommand = new RelayCommand(SaveStationMapping, CanExecuteSave);
            CancelCommand = new RelayCommand(Cancel);
            StationSearchCommand = new RelayCommand<string>(text => DebounceSearch(text));
            CalculateDistanceCommand = new RelayCommand(CalculateDistance, CanCalculateDistance);

            // 设置窗口标题
            WindowTitle = $"添加车站到路线 - {_routeInfo.RouteName}";

            // 设置主题是否为深色模式
            _isDarkMode = _mainViewModel.IsDarkMode;

            // 设置字体大小
            _fontSize = _mainViewModel.FontSize;

            // 初始化数据
            InitializeAsync();
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // 确保车站搜索服务已初始化
                await _stationSearchService.EnsureInitializedAsync();

                // 检查是否是第一个站点
                int stationCount = await _databaseService.GetRouteStationsCountAsync(_routeInfo.Id);
                _isFirstStation = stationCount == 0;

                // 设置默认值
                if (_isFirstStation)
                {
                    // 首个站点默认为起点
                    IsStartStation = true;
                    _showAddPositionOptions = false;
                    _addToEnd = true; // 默认添加到结尾（此时是唯一选项）
                }
                else
                {
                    // 非首个站点默认为经停站
                    IsPassingStation = true;
                    _showAddPositionOptions = false; // 不显示添加位置选项
                    _addToEnd = true; // 默认添加到结尾
                }

                // 获取前一个站点（用于距离计算）
                if (!_isFirstStation)
                {
                    var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999); // 获取所有站点
                    if (stations != null && stations.Count > 0)
                    {
                        _previousStation = stations.LastOrDefault();
                    }
                }

                // 初始化完成后，设置加载状态为false
                IsLoading = false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                MessageBoxHelper.ShowError($"初始化数据时发生错误: {ex.Message}");
                LogHelper.LogError($"初始化数据时发生错误: {ex.Message}", ex);
            }
        }

        #region 属性

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        /// <summary>
        /// 是否为深色模式
        /// </summary>
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                }
            }
        }

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
        /// 选中的车站
        /// </summary>
        public StationInfo SelectedStation
        {
            get => _selectedStation;
            private set
            {
                if (_selectedStation != value)
                {
                    _selectedStation = value;
                    OnPropertyChanged(nameof(SelectedStation));
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// 车站搜索文本
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

                    // 检查是否与当前选中车站完全匹配，如果匹配则不触发搜索
                    if (_selectedStation != null && _selectedStation.StationName == value)
                    {
                        // 强制关闭下拉框
                        IsStationDropdownOpen = false;
                        return;
                    }

                    // 使用防抖延迟搜索，避免输入时UI卡顿
                    DebounceSearch(value);
                }
            }
        }

        /// <summary>
        /// 是否显示车站下拉列表
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
        /// 车站建议列表
        /// </summary>
        public ObservableCollection<StationInfo> StationSuggestions => _stationSuggestions;

        /// <summary>
        /// 距起点累计距离(公里)
        /// </summary>
        public decimal? DistanceFromStart
        {
            get => _distanceFromStart;
            set
            {
                if (_distanceFromStart != value)
                {
                    _distanceFromStart = value;
                    OnPropertyChanged(nameof(DistanceFromStart));
                }
            }
        }

        /// <summary>
        /// 距离上一站点距离(公里)
        /// </summary>
        public decimal? DistanceFromPrev
        {
            get => _distanceFromPrev;
            set
            {
                if (_distanceFromPrev != value)
                {
                    _distanceFromPrev = value;
                    OnPropertyChanged(nameof(DistanceFromPrev));

                    // 更新累计距离
                    if (_previousStation != null && value.HasValue)
                    {
                        DistanceFromStart = _previousStation.DistanceFromStart + value.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 计划停留时间(分钟)
        /// </summary>
        public int StayTime
        {
            get => _stayTime;
            set
            {
                if (_stayTime != value)
                {
                    _stayTime = value;
                    OnPropertyChanged(nameof(StayTime));
                }
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged(nameof(Notes));
                }
            }
        }

        /// <summary>
        /// 是否为起点
        /// </summary>
        public bool IsStartStation
        {
            get => _isStartStation;
            set
            {
                if (_isStartStation != value)
                {
                    // 如果要设置为起点站，先检查是否已存在起点站
                    if (value)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                // 获取当前路线的所有站点
                                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                                var existingStartStation = stations?.FirstOrDefault(s => (s.StationRole & 1) != 0);
                                
                                // 如果已存在起点站，且不是添加到起点的情况
                                if (existingStartStation != null && !_addToStart)
                                {
                                    // 在UI线程上执行
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MessageBoxHelper.ShowWarning("当前路线已存在起点站，一个路线只能有一个起点站");
                                        // 不改变状态
                                        OnPropertyChanged(nameof(IsStartStation));
                                    });
                                    return;
                                }
                                
                                // 如果没有起点站或是添加到起点的情况，允许设置
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SetStartStationState(true);
                                });
                            }
                            catch (Exception ex)
                            {
                                LogHelper.LogError($"检查起点站时出错: {ex.Message}", ex);
                                // 出错时允许设置，避免阻塞用户操作
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SetStartStationState(true);
                                });
                            }
                        });
                    }
                    else
                    {
                        // 如果是取消起点站状态，直接设置
                        SetStartStationState(false);
                    }
                }
            }
        }

        /// <summary>
        /// 设置起点站状态
        /// </summary>
        private void SetStartStationState(bool isStart)
        {
            _isStartStation = isStart;
            OnPropertyChanged(nameof(IsStartStation));

            // 如果是起点站，不能同时是终点站或经停站
            if (isStart)
            {
                if (_isEndStation)
                {
                    _isEndStation = false;
                    OnPropertyChanged(nameof(IsEndStation));
                }

                if (_isPassingStation)
                {
                    _isPassingStation = false;
                    OnPropertyChanged(nameof(IsPassingStation));
                }

                // 起点站的距离起点和距离上一站必须为0
                DistanceFromStart = 0;
                DistanceFromPrev = 0;
            }
        }

        /// <summary>
        /// 是否为终点
        /// </summary>
        public bool IsEndStation
        {
            get => _isEndStation;
            set
            {
                if (_isEndStation != value)
                {
                    // 如果要设置为终点站，先检查是否已存在终点站
                    if (value)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                // 获取当前路线的所有站点
                                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                                var existingEndStation = stations?.FirstOrDefault(s => (s.StationRole & 2) != 0);
                                
                                // 如果已存在终点站，且不是添加到终点的情况
                                if (existingEndStation != null && !_addToEnd)
                                {
                                    // 在UI线程上执行
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MessageBoxHelper.ShowWarning("当前路线已存在终点站，一个路线只能有一个终点站");
                                        // 不改变状态
                                        OnPropertyChanged(nameof(IsEndStation));
                                    });
                                    return;
                                }
                                
                                // 如果没有终点站或是添加到终点的情况，允许设置
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SetEndStationState(true);
                                });
                            }
                            catch (Exception ex)
                            {
                                LogHelper.LogError($"检查终点站时出错: {ex.Message}", ex);
                                // 出错时允许设置，避免阻塞用户操作
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SetEndStationState(true);
                                });
                            }
                        });
                    }
                    else
                    {
                        // 如果是取消终点站状态，直接设置
                        SetEndStationState(false);
                    }
                }
            }
        }

        /// <summary>
        /// 设置终点站状态
        /// </summary>
        private void SetEndStationState(bool isEnd)
        {
            _isEndStation = isEnd;
            OnPropertyChanged(nameof(IsEndStation));

            // 如果是终点站，不能同时是起点站或经停站
            if (isEnd)
            {
                if (_isStartStation)
                {
                    _isStartStation = false;
                    OnPropertyChanged(nameof(IsStartStation));
                }

                if (_isPassingStation)
                {
                    _isPassingStation = false;
                    OnPropertyChanged(nameof(IsPassingStation));
                }
            }
        }

        /// <summary>
        /// 是否为经停站
        /// </summary>
        public bool IsPassingStation
        {
            get => _isPassingStation;
            set
            {
                if (_isPassingStation != value)
                {
                    _isPassingStation = value;
                    OnPropertyChanged(nameof(IsPassingStation));
                    Debug.WriteLine($"经停站属性设置为: {value}，终点站属性当前值: {_isEndStation}");

                    // 当选择为经停站时，自动取消起点和终点角色
                    if (value)
                    {
                        _isStartStation = false;
                        _isEndStation = false;
                        OnPropertyChanged(nameof(IsStartStation));
                        OnPropertyChanged(nameof(IsEndStation));
                        Debug.WriteLine($"设置为经停站: 终点站属性现在为 {_isEndStation}");
                    }
                }
            }
        }

        /// <summary>
        /// 是否为换乘站
        /// </summary>
        public bool IsTransferStation
        {
            get => _isTransferStation;
            set
            {
                if (_isTransferStation != value)
                {
                    _isTransferStation = value;
                    OnPropertyChanged(nameof(IsTransferStation));

                    // 如果设置为换乘站，不需要再询问是否重新计算路径
                    // 直接勾选换乘角色
                }
            }
        }

        /// <summary>
        /// 是否为第一个站点
        /// </summary>
        public bool IsFirstStation => _isFirstStation;

        /// <summary>
        /// 是否显示添加位置选项
        /// </summary>
        public bool ShowAddPositionOptions
        {
            get => _showAddPositionOptions;
            set
            {
                if (_showAddPositionOptions != value)
                {
                    _showAddPositionOptions = value;
                    OnPropertyChanged(nameof(ShowAddPositionOptions));
                }
            }
        }

        /// <summary>
        /// 添加到开头（保留字段，但不在UI中使用）
        /// </summary>
        public bool AddToStart
        {
            get => _addToStart;
            set
            {
                if (_addToStart != value)
                {
                    _addToStart = value;
                    OnPropertyChanged(nameof(AddToStart));
                }
            }
        }

        /// <summary>
        /// 添加到结尾（保留字段，但不在UI中使用）
        /// </summary>
        public bool AddToEnd
        {
            get => _addToEnd;
            set
            {
                if (_addToEnd != value)
                {
                    _addToEnd = value;
                    OnPropertyChanged(nameof(AddToEnd));
                }
            }
        }

        /// <summary>
        /// 是否能够保存
        /// </summary>
        public bool CanSave => SelectedStation != null &&
                             (IsStartStation || IsEndStation || IsPassingStation || IsTransferStation) &&
                             (!DistanceFromPrev.HasValue || DistanceFromPrev.Value >= 0) &&
                             (StayTime >= 0);

        #endregion

        #region 命令

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 车站搜索命令
        /// </summary>
        public ICommand StationSearchCommand { get; }

        /// <summary>
        /// 计算距离命令
        /// </summary>
        public ICommand CalculateDistanceCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 异步加载最后一个站点作为前一个站点
        /// </summary>
        private async void LoadLastStationAsync()
        {
            try
            {
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations != null && stations.Count > 0)
                {
                    _previousStation = stations.LastOrDefault();
                    // 如果有前一个站点，设置累计距离基于前一个站点
                    if (_previousStation != null && DistanceFromPrev.HasValue)
                    {
                        DistanceFromStart = _previousStation.DistanceFromStart + DistanceFromPrev.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载最后一个站点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步加载第一个站点作为下一个站点
        /// </summary>
        private async void LoadFirstStationAsync()
        {
            try
            {
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 1);
                if (stations != null && stations.Count > 0)
                {
                    _nextStation = stations.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载第一个站点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 是否能够执行保存
        /// </summary>
        private bool CanExecuteSave()
        {
            return CanSave;
        }

        /// <summary>
        /// 保存车站映射
        /// </summary>
        private async void SaveStationMapping()
        {
            try
            {
                // 执行数据验证
                if (!await ValidateData())
                {
                    return;
                }

                IsLoading = true;

                // 创建RouteStationMapping对象
                byte stationRole = 0;
                if (IsStartStation) stationRole |= 1; // 起点 = 1
                if (IsEndStation) stationRole |= 2;   // 终点 = 2
                if (IsPassingStation) stationRole |= 4; // 经停 = 4
                if (IsTransferStation) stationRole |= 8; // 换乘 = 8

                Debug.WriteLine($"保存前状态: 起点={IsStartStation}, 终点={IsEndStation}, 经停={IsPassingStation}, 换乘={IsTransferStation}");
                Debug.WriteLine($"保存车站映射时的角色值: {stationRole}，解析: 起点={IsStartStation}, 终点={IsEndStation}, 经停={IsPassingStation}, 换乘={IsTransferStation}");

                // 构建RouteStationMapping对象
                var mapping = new RouteStationMapping
                {
                    RouteId = _routeInfo.Id,
                    StationId = SelectedStation.Id,
                    StationRole = stationRole,
                    StayTime = StayTime,
                    Notes = Notes,
                    AddTime = DateTime.Now,
                    DistanceFromPrev = DistanceFromPrev ?? 0,
                    DistanceFromStart = DistanceFromStart ?? 0,
                    Station = SelectedStation
                };

                Debug.WriteLine($"正在保存车站映射: 车站名={SelectedStation.StationName}, 角色值={stationRole}");

                // 保存到数据库
                bool success = await _databaseService.AddStationToRouteAsync(mapping);
                Debug.WriteLine($"保存到数据库结果: {success}");

                // 验证保存后的车站角色
                if (success)
                {
                    var savedStation = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                    var newStation = savedStation?.FirstOrDefault(s => s.StationId == SelectedStation.Id);
                    if (newStation != null)
                    {
                        Debug.WriteLine($"保存后验证: 车站={newStation.Station?.StationName}, 角色值={newStation.StationRole}, " +
                                        $"解析: 起点={newStation.IsStartStation}, 终点={newStation.IsEndStation}, " +
                                        $"经停={newStation.IsPassingStation}, 换乘={newStation.IsTransferStation}");
                    }
                }

                // 如果设置为起点，需要将原起点更改为经停站
                if (success && IsStartStation)
                {
                    Debug.WriteLine("执行更新原起点为经停站");
                    await UpdateOriginalStartStationAsync();
                }

                // 如果设置为终点，需要将原终点更改为经停站
                if (success && IsEndStation)
                {
                    Debug.WriteLine("执行更新原终点为经停站");
                    await UpdateOriginalEndStationAsync();
                }

                // 更新路线总里程
                await UpdateRouteTotalDistanceAsync();

                // 刷新回调
                _refreshCallback?.Invoke();

                IsLoading = false;

                // 保存成功
                OnPropertyChanged(nameof(IsLoading));
                // 提示保存成功
                MessageBoxHelper.ShowInfo($"已成功将 {SelectedStation.StationName} 添加到路线");

                // 关闭窗口
                CloseWindow?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                IsLoading = false;
                MessageBoxHelper.ShowError($"保存车站映射失败: {ex.Message}");
                LogHelper.LogError($"保存车站映射失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 数据验证
        /// </summary>
        private async Task<bool> ValidateData()
        {
            // 验证必填字段
            if (SelectedStation == null)
            {
                MessageBoxHelper.ShowWarning("请选择车站");
                return false;
            }

            // 验证角色
            if (!IsStartStation && !IsEndStation && !IsPassingStation && !IsTransferStation)
            {
                MessageBoxHelper.ShowWarning("请选择车站角色");
                return false;
            }

            // 验证距离不能为负数
            if (DistanceFromPrev.HasValue && DistanceFromPrev.Value < 0)
            {
                MessageBoxHelper.ShowWarning("站间距离不能为负数");
                return false;
            }

            // 验证非起点站的距离不能为空或0
            if (!IsStartStation && (!DistanceFromPrev.HasValue || !DistanceFromStart.HasValue || DistanceFromPrev.Value == 0 || DistanceFromStart.Value == 0))
            {
                MessageBoxHelper.ShowError("非起点站的站间距离和累计距离不能为空或0，请填写或使用计算功能");
                return false;
            }

            // 验证停留时间必须为非负整数
            if (StayTime < 0)
            {
                MessageBoxHelper.ShowWarning("停留时间必须为非负整数");
                return false;
            }

            // 验证站点名称是否重复
            try
            {
                // 获取当前路线的所有站点
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);

                // 查找与当前站点同名的站点
                var sameNameStations = stations?.Where(s => s.Station?.StationName == SelectedStation.StationName).ToList();

                if (sameNameStations != null && sameNameStations.Count > 0)
                {
                    // 如果是经停站点，则不允许与现有站点重名
                    if (IsPassingStation)
                    {
                        MessageBoxHelper.ShowError($"站点名称 '{SelectedStation.StationName}' 已存在于当前路线中，经停站不允许重名");
                        return false;
                    }

                    // 如果是起点或终点，则只允许与终点或起点重名（环线情况）
                    if (IsStartStation)
                    {
                        // 检查同名站点是否都是终点站
                        bool isValidCircle = sameNameStations.All(s => (s.StationRole & 2) != 0); // 检查是否都是终点站

                        if (!isValidCircle)
                        {
                            MessageBoxHelper.ShowError($"站点名称 '{SelectedStation.StationName}' 已存在于当前路线中。起点站只允许与终点站同名（环线）");
                            return false;
                        }
                    }
                    else if (IsEndStation)
                    {
                        // 检查同名站点是否都是起点站
                        bool isValidCircle = sameNameStations.All(s => (s.StationRole & 1) != 0); // 检查是否都是起点站

                        if (!isValidCircle)
                        {
                            MessageBoxHelper.ShowError($"站点名称 '{SelectedStation.StationName}' 已存在于当前路线中。终点站只允许与起点站同名（环线）");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"验证站点名称重复性时出错: {ex.Message}", ex);
                // 验证出错仍然允许继续，避免阻塞用户操作
            }

            // 验证起点和终点唯一性
            try
            {
                // 获取当前路线的所有站点
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);

                // 检查是否已存在起点
                if (IsStartStation)
                {
                    var existingStartStation = stations?.FirstOrDefault(s => (s.StationRole & 1) != 0); // 检查StationRole的第一位是否为1
                    if (existingStartStation != null && !_addToStart)
                    {
                        MessageBoxHelper.ShowError("当前路线已存在起点站，一个路线只能有一个起点站");
                        return false;
                    }
                }

                // 检查是否已存在终点
                if (IsEndStation)
                {
                    var existingEndStation = stations?.FirstOrDefault(s => (s.StationRole & 2) != 0); // 检查StationRole的第二位是否为1
                    if (existingEndStation != null && !_addToEnd)
                    {
                        MessageBoxHelper.ShowError("当前路线已存在终点站，一个路线只能有一个终点站");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"验证起点终点唯一性时出错: {ex.Message}", ex);
                // 验证出错仍然允许继续，避免阻塞用户操作
            }

            return true;
        }

        /// <summary>
        /// 更新后续站点的累计距离
        /// </summary>
        private async Task UpdateSubsequentStationsDistanceAsync()
        {
            try
            {
                // 获取所有站点
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count <= 1)
                    return;

                // 按ID排序
                stations = stations.OrderBy(s => s.Id).ToList();

                decimal cumulativeDistance = 0;

                // 遍历所有站点，更新累计距离
                for (int i = 0; i < stations.Count; i++)
                {
                    var currentStation = stations[i];

                    if (i == 0)
                    {
                        // 第一个站点的累计距离为0
                        currentStation.DistanceFromStart = 0;
                    }
                    else
                    {
                        // 其他站点的累计距离为前一站累计距离加上站间距离
                        cumulativeDistance += currentStation.DistanceFromPrev;
                        currentStation.DistanceFromStart = cumulativeDistance;
                    }

                    // 更新数据库
                    await _databaseService.UpdateRouteStationAsync(currentStation);
                }

                // 更新路线总里程
                await UpdateRouteTotalDistanceAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新后续站点距离失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"更新后续站点距离失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新路线总里程
        /// </summary>
        private async Task UpdateRouteTotalDistanceAsync()
        {
            try
            {
                // 获取所有站点
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count == 0)
                    return;

                // 计算总里程（最后一个站点的累计距离）
                decimal totalDistance = stations.OrderByDescending(s => s.DistanceFromStart).First().DistanceFromStart;

                // 更新路线信息
                _routeInfo.TotalDistance = totalDistance;
                await _databaseService.UpdateRouteAsync(_routeInfo);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新路线总里程失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"更新路线总里程失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新原起点站为经停站
        /// </summary>
        private async Task UpdateOriginalStartStationAsync()
        {
            try
            {
                // 获取原起点站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                var originalStartStation = stations?.FirstOrDefault(s => (s.StationRole & 1) != 0); // 检查StationRole的第一位是否为1

                if (originalStartStation != null)
                {
                    // 确保不是正在添加的同一个站点
                    if (originalStartStation.StationId == SelectedStation.Id)
                    {
                        Debug.WriteLine("原起点站与当前添加的站点相同，不进行修改");
                        return;
                    }
                    
                    // 修改角色为经停站
                    originalStartStation.StationRole = (byte)(originalStartStation.StationRole & ~1); // 清除起点标志
                    originalStartStation.StationRole |= 4; // 设置经停站标志

                    // 保存更新
                    await _databaseService.UpdateRouteStationAsync(originalStartStation);
                    Debug.WriteLine($"已将原起点站 {originalStartStation.Station?.StationName} 更改为经停站");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新原起点站失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"更新原起点站失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新原终点站为经停站
        /// </summary>
        private async Task UpdateOriginalEndStationAsync()
        {
            try
            {
                // 获取原终点站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                var originalEndStation = stations?.FirstOrDefault(s => (s.StationRole & 2) != 0); // 检查StationRole的第二位是否为1

                Debug.WriteLine($"查找到的原终点站: {(originalEndStation != null ? originalEndStation.Station?.StationName : "无")}");
                Debug.WriteLine($"当前添加的站点: {SelectedStation.StationName}");

                if (originalEndStation != null)
                {
                    // 确保不是正在添加的同一个站点
                    if (originalEndStation.StationId == SelectedStation.Id)
                    {
                        Debug.WriteLine("原终点站与当前添加的站点相同，不进行修改");
                        return;
                    }

                    // 修改角色为经停站
                    byte originalRole = originalEndStation.StationRole;
                    originalEndStation.StationRole = (byte)(originalEndStation.StationRole & ~2); // 清除终点标志
                    originalEndStation.StationRole |= 4; // 设置经停站标志

                    Debug.WriteLine($"更新原终点站角色: 从 {originalRole} 到 {originalEndStation.StationRole}");

                    // 保存更新
                    bool updateResult = await _databaseService.UpdateRouteStationAsync(originalEndStation);
                    Debug.WriteLine($"更新原终点站结果: {updateResult}");
                }
                else
                {
                    Debug.WriteLine("未找到原终点站，无需更新");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新原终点站失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"更新原终点站失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            // 触发关闭窗口事件
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 使用防抖机制搜索车站
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private void DebounceSearch(string searchText)
        {
            // 停止之前的定时器（如果存在）
            _searchDebounceTimer?.Dispose();

            // 如果搜索文本为空，则清空结果并隐藏下拉列表
            if (string.IsNullOrWhiteSpace(searchText))
            {
                IsStationDropdownOpen = false;
                _stationSuggestions.Clear();
                return;
            }

            // 如果少于2个字符，不执行搜索
            if (searchText.Length < 2)
                return;

            // 创建新的定时器，延迟执行搜索
            _searchDebounceTimer = new Timer(async state =>
            {
                await SearchStationsAsync(searchText);
            }, null, DebounceDelay, Timeout.Infinite); // 延迟DebounceDelay毫秒后执行一次
        }

        /// <summary>
        /// 异步搜索车站
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private async Task SearchStationsAsync(string searchText)
        {
            try
            {
                // 在UI线程上清空列表
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _stationSuggestions.Clear();
                });

                // 执行搜索
                var stations = await _stationSearchService.SearchStationsAsync(searchText);

                // 在UI线程上更新结果
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var station in stations.Take(10)) // 限制显示10个结果
                    {
                        _stationSuggestions.Add(station);
                    }

                    IsStationDropdownOpen = _stationSuggestions.Count > 0;
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置选中的车站
        /// </summary>
        /// <param name="station">车站信息</param>
        public void SetSelectedStation(StationInfo station)
        {
            if (station != null)
            {
                SelectedStation = station;
                StationSearchText = station.StationName;

                // 强制关闭下拉框
                IsStationDropdownOpen = false;

                // 使用延迟定时器再次确保下拉框关闭（解决焦点问题）
                var forceCloseTimer = new Timer(state =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsStationDropdownOpen = false;
                    });
                }, null, 100, Timeout.Infinite);

            }
        }

        /// <summary>
        /// 是否能够计算距离
        /// </summary>
        private bool CanCalculateDistance()
        {
            // 默认返回true，让计算按钮始终可用
            return true;
        }

        /// <summary>
        /// 计算距离
        /// </summary>
        private async void CalculateDistance()
        {
            try
            {
                IsLoading = true;

                // 检查是否选择了车站
                if (SelectedStation == null)
                {
                    MessageBoxHelper.ShowWarning("请先选择车站");
                    IsLoading = false;
                    return;
                }

                // 检查选择的车站是否有经纬度信息
                if (string.IsNullOrWhiteSpace(SelectedStation.Longitude) || string.IsNullOrWhiteSpace(SelectedStation.Latitude))
                {
                    MessageBoxHelper.ShowWarning("所选车站缺少经纬度信息，无法计算距离，请在车站中心添加对应的经纬度信息");
                    IsLoading = false;
                    return;
                }

                // 检查API密钥
                string apiKey = _configurationService?.GetSettingValue("AmapWebServiceKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBoxHelper.ShowWarning("未配置高德地图API密钥，请在系统设置中添加相关信息");
                    IsLoading = false;
                    return;
                }

                // 如果是首站（第一个站点），直接设置距离为0，不需要计算
                if (_isFirstStation)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DistanceFromPrev = 0;
                        DistanceFromStart = 0;
                        IsLoading = false;
                    });
                    MessageBoxHelper.ShowInfo("首站不需要计算距离，已自动设置为0");
                    return;
                }

                // 检查是否有前一个站点信息
                if (_previousStation == null ||
                    string.IsNullOrWhiteSpace(_previousStation.Station?.Longitude) ||
                    string.IsNullOrWhiteSpace(_previousStation.Station?.Latitude))
                {
                    MessageBoxHelper.ShowWarning("前序站点缺少经纬度信息，无法计算距离");
                    IsLoading = false;
                    return;
                }

                await Task.Run(async () =>
                {
                    try
                    {
                        // 计算与前一站的距离
                        decimal distance = await _distanceCalculationService.CalculateDistanceAsync(
                            _previousStation.Station.Longitude,
                            _previousStation.Station.Latitude,
                            SelectedStation.Longitude,
                            SelectedStation.Latitude);

                        // 在UI线程上更新距离
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DistanceFromPrev = distance;
                            // 累计距离 = 前一站累计距离 + 站间距离
                            DistanceFromStart = _previousStation.DistanceFromStart + distance;
                        });
                    }
                    catch (Exception ex)
                    {
                        // 在UI线程上显示错误
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxHelper.ShowError($"计算距离失败: {ex.Message}");
                        });
                        LogHelper.LogError($"计算距离失败: {ex.Message}", ex);
                    }
                    finally
                    {
                        // 在UI线程上更新加载状态
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsLoading = false;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                IsLoading = false;
                MessageBoxHelper.ShowError($"计算距离时发生错误: {ex.Message}");
                LogHelper.LogError($"计算距离时发生错误: {ex.Message}", ex);
            }
        }

        #endregion

        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        public event EventHandler CloseWindow;
    }
}