using System.Collections.ObjectModel;
using TA_WPF.Models;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 车站搜索服务，提供统一的车站搜索和选择功能
    /// </summary>
    public class StationSearchService
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<StationInfo> _stations;
        private bool _isInitialized = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public StationSearchService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _stations = new ObservableCollection<StationInfo>();
        }

        /// <summary>
        /// 获取所有车站信息列表
        /// </summary>
        public ObservableCollection<StationInfo> Stations => _stations;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 加载所有车站信息
        /// </summary>
        /// <returns>异步任务</returns>
        public async Task LoadStationsAsync()
        {
            if (_isInitialized) return;

            try
            {
                // 从数据库获取站点数据
                var stations = await _databaseService.GetStationsAsync();

                // 清空当前集合并添加新的站点
                _stations.Clear();
                foreach (var station in stations)
                {
                    _stations.Add(station);
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // 使用日志服务记录错误
                LogHelper.LogError("加载车站数据时出错", ex);
                throw;
            }
        }

        /// <summary>
        /// 根据部分名称搜索车站
        /// </summary>
        /// <param name="searchText">搜索关键词</param>
        /// <returns>匹配的车站列表</returns>
        public async Task<ObservableCollection<StationInfo>> SearchStationsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new ObservableCollection<StationInfo>();
            }

            try
            {
                // 确保站点数据已加载
                if (!_isInitialized)
                {
                    await LoadStationsAsync();
                }

                // 使用关键词搜索站点
                var results = await _databaseService.SearchStationsByNameAsync(searchText);
                return new ObservableCollection<StationInfo>(results);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站时出错: {searchText}", ex);
                return new ObservableCollection<StationInfo>();
            }
        }

        /// <summary>
        /// 根据站点名称验证站点信息完整性
        /// </summary>
        /// <param name="stationName">站点名称</param>
        /// <returns>车站信息，如果不存在返回null</returns>
        public StationInfo ValidateStationName(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            // 使用工具类移除"站"后缀进行比较
            string cleanName = StationNameHelper.RemoveStationSuffix(stationName);

            // 在已加载的站点中查找匹配
            return _stations.FirstOrDefault(s =>
                StationNameHelper.RemoveStationSuffix(s.StationName) == cleanName ||
                s.StationName == cleanName);
        }

        /// <summary>
        /// 获取最接近的站点匹配
        /// </summary>
        /// <param name="stationName">站点名称</param>
        /// <returns>最接近的站点匹配</returns>
        public async Task<StationInfo> GetClosestStationMatchAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            try
            {
                // 确保站点数据已加载
                await EnsureInitializedAsync();

                // 获取可能的匹配结果
                var searchResults = await _databaseService.SearchStationsByNameAsync(stationName);

                // 返回第一个最接近的匹配
                return searchResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取车站匹配时出错: {stationName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 处理站点输入框失去焦点事件
        /// </summary>
        /// <param name="stationName">站点名称</param>
        /// <param name="isDepartStation">是否为出发车站</param>
        /// <returns>处理后的站点信息，如果找不到匹配站点则返回null</returns>
        public async Task<StationInfo> HandleStationLostFocusAsync(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            try
            {
                // 确保站点数据已加载
                await EnsureInitializedAsync();

                // 在站点列表中查找匹配的站点，不进行校验
                return ValidateStationName(stationName);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理站点输入框失去焦点事件时出错: {stationName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 校验车站信息完整性（包含station_code和station_pinyin）
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>校验结果：0-校验通过，1-车站不存在，2-车站信息不完整</returns>
        public async Task<(int status, StationInfo station)> ValidateStationCompleteAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return (1, null); // 车站不存在
            }

            try
            {
                // 确保站点数据已加载
                await EnsureInitializedAsync();

                // 检测车站表是否为空
                if (_stations.Count == 0)
                {
                    return (1, null); // 车站表为空
                }

                // 查找匹配的车站
                var station = ValidateStationName(stationName);
                if (station == null)
                {
                    return (1, null); // 车站不存在
                }

                // 检测车站信息是否完整
                bool hasStationCode = !string.IsNullOrWhiteSpace(station.StationCode);
                bool hasStationPinyin = !string.IsNullOrWhiteSpace(station.StationPinyin);

                if (!hasStationCode || !hasStationPinyin)
                {
                    return (2, station); // 车站信息不完整
                }

                return (0, station); // 校验通过
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"校验车站信息完整性时出错: {stationName}", ex);
                return (1, null); // 出错时默认为车站不存在
            }
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        public async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                await LoadStationsAsync();
            }
        }

        /// <summary>
        /// 确保服务已初始化
        /// </summary>
        public async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await LoadStationsAsync();
            }
        }

        /// <summary>
        /// 检测是否为有效的车站名
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>是否有效</returns>
        public async Task<bool> IsValidStationAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return false;
            }

            // 确保站点数据已加载
            await EnsureInitializedAsync();

            // 检测车站表是否为空
            if (_stations.Count == 0)
            {
                return false;
            }

            // 在站点列表中查找匹配的站点
            return ValidateStationName(stationName) != null;
        }

        /// <summary>
        /// 获取车站信息
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>车站信息，如果不存在返回null</returns>
        public async Task<StationInfo> GetStationInfoAsync(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            // 确保站点数据已加载
            await EnsureInitializedAsync();

            // 在站点列表中查找匹配的站点
            return ValidateStationName(stationName);
        }
        
        #region 兼容旧版接口的同步方法（不推荐使用）
        
        /// <summary>
        /// 处理站点输入框失去焦点事件 (同步版本，不推荐使用)
        /// </summary>
        /// <param name="stationName">站点名称</param>
        /// <param name="isDepartStation">是否为出发车站</param>
        /// <returns>处理后的站点信息，如果找不到匹配站点则返回null</returns>
        [Obsolete("请使用异步版本 HandleStationLostFocusAsync，避免阻塞UI线程")]
        public StationInfo HandleStationLostFocus(string stationName, bool isDepartStation)
        {
            var task = HandleStationLostFocusAsync(stationName, isDepartStation);
            task.Wait();
            return task.Result;
        }
        
        /// <summary>
        /// 检测是否为有效的车站名 (同步版本，不推荐使用)
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>是否有效</returns>
        [Obsolete("请使用异步版本 IsValidStationAsync，避免阻塞UI线程")]
        public bool IsValidStation(string stationName)
        {
            var task = IsValidStationAsync(stationName);
            task.Wait();
            return task.Result;
        }
        
        /// <summary>
        /// 获取车站信息 (同步版本，不推荐使用)
        /// </summary>
        /// <param name="stationName">车站名称</param>
        /// <returns>车站信息，如果不存在返回null</returns>
        [Obsolete("请使用异步版本 GetStationInfoAsync，避免阻塞UI线程")]
        public StationInfo GetStationInfo(string stationName)
        {
            var task = GetStationInfoAsync(stationName);
            task.Wait();
            return task.Result;
        }
        
        #endregion
    }
}