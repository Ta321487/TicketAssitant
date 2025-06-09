using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 编辑路线车站视图模型
    /// </summary>
    public class EditStationToRouteViewModel : BaseViewModel
    {
        private readonly RouteInfo _routeInfo;
        private readonly RouteStationMapping _stationMapping;
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

        // 车站相关属性
        private StationInfo _station;
        private string _stationName;

        // 车站映射属性
        private decimal? _distanceFromStart;
        private decimal? _distanceFromPrev;
        private int? _stayTime;
        private string _notes;

        // 角色属性
        private bool _isStartStation;
        private bool _isEndStation;
        private bool _isPassingStation;
        private bool _isTransferStation;

        // 原始状态记录
        private bool _wasStartStation;
        private bool _wasEndStation;

        /// <summary>
        /// 构造函数
        /// </summary>
        public EditStationToRouteViewModel(
            RouteInfo routeInfo,
            RouteStationMapping stationMapping,
            DatabaseService databaseService,
            StationSearchService stationSearchService,
            DistanceCalculationService distanceCalculationService,
            MainViewModel mainViewModel,
            ConfigurationService configurationService,
            Action refreshCallback)
        {
            _routeInfo = routeInfo ?? throw new ArgumentNullException(nameof(routeInfo));
            _stationMapping = stationMapping ?? throw new ArgumentNullException(nameof(stationMapping));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));
            _distanceCalculationService = distanceCalculationService ?? throw new ArgumentNullException(nameof(distanceCalculationService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _refreshCallback = refreshCallback;

            // 初始化命令
            SaveCommand = new RelayCommand(SaveStationMapping, CanExecuteSave);
            CancelCommand = new RelayCommand(Cancel);
            CalculateDistanceCommand = new RelayCommand(CalculateDistance, CanCalculateDistance);

            // 设置窗口标题
            WindowTitle = $"编辑路线车站 - {_routeInfo.RouteName}";

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

                // 初始化车站数据
                _station = _stationMapping.Station;
                _stationName = _station.StationName; // 车站名称设为只读，避免数据一致性问题
                _distanceFromPrev = _stationMapping.DistanceFromPrev;
                _distanceFromStart = _stationMapping.DistanceFromStart;
                _stayTime = _stationMapping.StayTime;
                _notes = _stationMapping.Notes;

                // 设置角色
                _isStartStation = _stationMapping.IsStartStation;
                _isEndStation = _stationMapping.IsEndStation;
                _isPassingStation = _stationMapping.IsPassingStation;
                _isTransferStation = _stationMapping.IsTransferStation;

                // 记录原始状态
                _wasStartStation = _isStartStation;
                _wasEndStation = _isEndStation;

                // 初始化完成后，设置加载状态为false
                IsLoading = false;

                // 通知所有属性已更改
                OnPropertyChanged(string.Empty);
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
        /// 车站名称
        /// </summary>
        public string StationName
        {
            get => _stationName;
            private set
            {
                if (_stationName != value)
                {
                    _stationName = value;
                    OnPropertyChanged(nameof(StationName));
                }
            }
        }

        /// <summary>
        /// 车站名称不可编辑的提示信息
        /// </summary>
        public string StationNameReadOnlyTip => "* 车站名称不可直接编辑，如需更换车站请删除此车站并添加新车站";

        /// <summary>
        /// 起点站距离提示信息
        /// </summary>
        public string StartStationDistanceTip => IsStartStation
            ? "起点站的距离起点必须为0"
            : "更改距离上一站点的公里数会自动计算此值，你也可以手动调整";

        /// <summary>
        /// 距起点累计距离
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
        /// 距离上一站点距离
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

                    // 如果不是起点站，则根据上一站点距离的变更自动更新距离起点的累计距离
                    if (!_isStartStation)
                    {
                        // 异步获取上一站的累计距离，并更新当前站的累计距离
                        UpdateCumulativeDistance();
                    }
                }
            }
        }

        /// <summary>
        /// 根据上一站点距离更新当前站的累计距离
        /// </summary>
        private async void UpdateCumulativeDistance()
        {
            try
            {
                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count == 0)
                    return;

                // 找到当前车站的索引
                int currentIndex = stations.FindIndex(s => s.Id == _stationMapping.Id);
                if (currentIndex < 0)
                    return;

                // 如果是第一个站，累计距离应该为0
                if (currentIndex == 0)
                {
                    DistanceFromStart = 0;
                    return;
                }

                // 获取上一个站的累计距离
                var prevStation = stations[currentIndex - 1];
                decimal prevCumulativeDistance = prevStation.DistanceFromStart;

                // 当前站的累计距离 = 上一站的累计距离 + 当前站到上一站的距离
                decimal newCumulativeDistance = prevCumulativeDistance + (_distanceFromPrev ?? 0);

                // 更新当前站的累计距离
                DistanceFromStart = newCumulativeDistance;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新累计距离失败: {ex.Message}", ex);
                // 静默失败，不显示错误消息，避免打断用户操作
            }
        }

        /// <summary>
        /// 计划停留时间
        /// </summary>
        public int? StayTime
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
        /// 是否为起点站
        /// </summary>
        public bool IsStartStation
        {
            get => _isStartStation;
            set
            {
                if (_isStartStation != value)
                {
                    _isStartStation = value;
                    OnPropertyChanged(nameof(IsStartStation));
                    // 更新距离提示信息
                    OnPropertyChanged(nameof(StartStationDistanceTip));

                    // 如果是起点，不能同时是终点或经停
                    if (value)
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

                        // 移除与换乘角色的互斥关系

                        // 如果是起点站，自动设置距离起点和距离上一站的距离为0
                        DistanceFromStart = 0;
                        DistanceFromPrev = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 是否为终点站
        /// </summary>
        public bool IsEndStation
        {
            get => _isEndStation;
            set
            {
                if (_isEndStation != value)
                {
                    _isEndStation = value;
                    OnPropertyChanged(nameof(IsEndStation));

                    // 如果是终点，不能同时是起点或经停
                    if (value)
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

                        // 移除与换乘角色的互斥关系
                    }
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

                    // 如果是经停站，不能同时是起点或终点
                    if (value)
                    {
                        if (_isStartStation)
                        {
                            _isStartStation = false;
                            OnPropertyChanged(nameof(IsStartStation));
                        }

                        if (_isEndStation)
                        {
                            _isEndStation = false;
                            OnPropertyChanged(nameof(IsEndStation));
                        }

                        // 移除与换乘角色的互斥关系
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

                    // 移除互斥逻辑，换乘角色可以与其他角色共存
                }
            }
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        public bool CanSave => true;

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
        /// 计算距离命令
        /// </summary>
        public ICommand CalculateDistanceCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 检查是否可以执行保存
        /// </summary>
        private bool CanExecuteSave()
        {
            // 由于所有必需字段都有默认值，这里总是返回true
            return true;
        }

        /// <summary>
        /// 保存车站映射
        /// </summary>
        private async void SaveStationMapping()
        {
            try
            {
                IsLoading = true;

                // 验证数据
                if (!await ValidateData())
                {
                    IsLoading = false;
                    return;
                }

                // 更新对象属性
                _stationMapping.Notes = _notes;
                _stationMapping.DistanceFromPrev = _distanceFromPrev ?? 0;
                _stationMapping.DistanceFromStart = _distanceFromStart ?? 0;
                _stationMapping.StayTime = _stayTime ?? 0; // 确保StayTime为非空值
                _stationMapping.StationRole = 0;

                // 设置角色
                if (_isStartStation) _stationMapping.StationRole |= 1;
                if (_isEndStation) _stationMapping.StationRole |= 2;
                if (_isPassingStation) _stationMapping.StationRole |= 4;
                if (_isTransferStation) _stationMapping.StationRole |= 8;

                // 更新角色文本（方便UI显示）
                _stationMapping.IsStartStation = _isStartStation;
                _stationMapping.IsEndStation = _isEndStation;
                _stationMapping.IsPassingStation = _isPassingStation;
                _stationMapping.IsTransferStation = _isTransferStation;

                // 保存到数据库
                bool success = await _databaseService.UpdateRouteStationAsync(_stationMapping);

                if (success)
                {
                    // 如果车站角色发生变更，或者距离值发生变化，可能需要更新后续车站
                    if (_wasStartStation != _isStartStation || _wasEndStation != _isEndStation ||
                        _stationMapping.DistanceFromPrev != _distanceFromPrev ||
                        _stationMapping.DistanceFromStart != _distanceFromStart)
                    {
                        // 更新后续车站的累计距离
                        await UpdateSubsequentStationsDistanceAsync();

                        // 更新路线总距离
                        await UpdateRouteTotalDistanceAsync();

                        // 如果起点或终点状态发生变化，可能需要额外处理
                        if (_wasStartStation && !_isStartStation)
                        {
                            // 检查路线是否还有起点站
                            bool hasStart = await CheckRouteHasStartStationAsync();
                            if (!hasStart)
                            {
                                MessageBoxHelper.ShowInfo("警告：该路线无起点，请考虑添加车站设置为起点或将本线路其他车站设置为起点。");
                            }
                        }

                        if (_wasEndStation && !_isEndStation)
                        {
                            // 检查路线是否还有终点站
                            bool hasEnd = await CheckRouteHasEndStationAsync();
                            if (!hasEnd)
                            {
                                MessageBoxHelper.ShowInfo("警告：该路线无终点，请考虑添加车站设置为终点或将本线路其他车站设置为终点。");
                            }
                        }
                    }

                    // 显示保存成功的消息
                    MessageBoxHelper.ShowInfo($"车站 {_stationName} 的信息已成功保存");

                    // 调用刷新回调
                    _refreshCallback?.Invoke();

                    // 关闭窗口
                    OnCloseWindow();
                }
                else
                {
                    MessageBoxHelper.ShowError("保存车站信息失败，请重试");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"保存车站信息时发生错误: {ex.Message}");
                LogHelper.LogError($"保存车站信息时发生错误: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 验证数据
        /// </summary>
        private async Task<bool> ValidateData()
        {
            // 验证至少选择了一个角色
            if (!_isStartStation && !_isEndStation && !_isPassingStation && !_isTransferStation)
            {
                MessageBoxHelper.ShowError("请选择车站角色");
                return false;
            }

            // 验证距离数据
            if (_distanceFromPrev == null || _distanceFromPrev < 0)
            {
                MessageBoxHelper.ShowError("距离上一站点距离必须是非负数");
                return false;
            }

            if (_distanceFromStart == null || _distanceFromStart < 0)
            {
                MessageBoxHelper.ShowError("距起点累计距离必须是非负数");
                return false;
            }

            // 起点站的距离起点和距离上一站必须为0
            if (_isStartStation && (_distanceFromStart != 0 || _distanceFromPrev != 0))
            {
                // 自动纠正为0
                DistanceFromStart = 0;
                DistanceFromPrev = 0;
                MessageBoxHelper.ShowInfo("起点站的距离起点和距离上一站已自动设置为0");
            }

            // 验证停留时间
            if (_stayTime != null && _stayTime < 0)
            {
                MessageBoxHelper.ShowError("计划停留时间必须是非负数");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 更新后续车站的累计距离
        /// </summary>
        private async Task UpdateSubsequentStationsDistanceAsync()
        {
            try
            {
                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count == 0)
                    return;

                // 找到当前车站的索引
                int currentIndex = stations.FindIndex(s => s.Id == _stationMapping.Id);
                if (currentIndex < 0 || currentIndex == stations.Count - 1)
                    return; // 当前车站不存在或是最后一个车站，无需更新后续车站

                // 从当前车站的下一个车站开始，更新累计距离
                decimal cumulativeDistance = _distanceFromStart ?? 0;
                for (int i = currentIndex + 1; i < stations.Count; i++)
                {
                    var station = stations[i];
                    cumulativeDistance += station.DistanceFromPrev;
                    station.DistanceFromStart = cumulativeDistance;
                    await _databaseService.UpdateRouteStationAsync(station);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新后续车站累计距离失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 更新路线总距离
        /// </summary>
        private async Task UpdateRouteTotalDistanceAsync()
        {
            try
            {
                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count == 0)
                    return;

                // 计算总距离（最后一个车站的累计距离）
                var lastStation = stations.LastOrDefault();
                if (lastStation != null)
                {
                    _routeInfo.TotalDistance = lastStation.DistanceFromStart;
                    await _databaseService.UpdateRouteAsync(_routeInfo);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新路线总距离失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 检查路线是否有起点站
        /// </summary>
        private async Task<bool> CheckRouteHasStartStationAsync()
        {
            try
            {
                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null)
                    return false;

                // 检查是否有起点站
                return stations.Any(s => s.IsStartStation && s.Id != _stationMapping.Id);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"检查路线起点站失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 检查路线是否有终点站
        /// </summary>
        private async Task<bool> CheckRouteHasEndStationAsync()
        {
            try
            {
                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null)
                    return false;

                // 检查是否有终点站
                return stations.Any(s => s.IsEndStation && s.Id != _stationMapping.Id);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"检查路线终点站失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            OnCloseWindow();
        }

        /// <summary>
        /// 是否可以计算距离
        /// </summary>
        private bool CanCalculateDistance()
        {
            // 需要有前一个站点的信息才能计算距离
            return true;
        }

        /// <summary>
        /// 计算两站间距离
        /// </summary>
        private async void CalculateDistance()
        {
            try
            {
                IsLoading = true;

                // 获取所有车站
                var stations = await _databaseService.GetRouteStationsAsync(_routeInfo.Id, 1, 9999);
                if (stations == null || stations.Count == 0)
                {
                    MessageBoxHelper.ShowError("无法获取车站信息");
                    return;
                }

                // 找到当前车站的索引
                int currentIndex = stations.FindIndex(s => s.Id == _stationMapping.Id);
                if (currentIndex < 0)
                {
                    MessageBoxHelper.ShowError("无法找到当前车站");
                    return;
                }

                // 如果不是第一个站，可以计算与前一个站的距离
                if (currentIndex > 0)
                {
                    var prevStation = stations[currentIndex - 1];

                    // 将字符串经纬度转换为double
                    if (double.TryParse(prevStation.Station.Longitude, out double prevLon) &&
                        double.TryParse(prevStation.Station.Latitude, out double prevLat) &&
                        double.TryParse(_station.Longitude, out double curLon) &&
                        double.TryParse(_station.Latitude, out double curLat))
                    {
                        // 调用距离计算服务
                        var distance = await _distanceCalculationService.CalculateDistanceAsync(
                            prevLon, prevLat, curLon, curLat);

                        if (distance.HasValue)
                        {
                            DistanceFromPrev = distance.Value;
                            MessageBoxHelper.ShowInfo($"计算距离成功：{DistanceFromPrev} 公里");
                        }
                        else
                        {
                            MessageBoxHelper.ShowError("无法计算站间距离，请检查车站坐标信息");
                        }
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("车站坐标格式不正确，无法计算距离");
                    }
                }
                else
                {
                    // 第一个站点距离上一站应为0
                    DistanceFromPrev = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"计算距离时发生错误: {ex.Message}");
                LogHelper.LogError($"计算距离时发生错误: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        public event EventHandler CloseWindow;

        /// <summary>
        /// 触发关闭窗口事件
        /// </summary>
        protected virtual void OnCloseWindow()
        {
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}