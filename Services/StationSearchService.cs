using System.Collections.ObjectModel;
using TA_WPF.Models;

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
                Utils.LogHelper.LogError("加载车站数据时出错", ex);
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
                Utils.LogHelper.LogError($"搜索车站时出错: {searchText}", ex);
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

            // 移除"站"后缀进行比较
            string cleanName = stationName.Replace("站", "");

            // 在已加载的站点中查找匹配
            return _stations.FirstOrDefault(s => 
                s.StationName?.Replace("站", "") == cleanName ||
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
                if (!_isInitialized)
                {
                    await LoadStationsAsync();
                }

                // 获取可能的匹配结果
                var searchResults = await _databaseService.SearchStationsByNameAsync(stationName);
                
                // 返回第一个最接近的匹配
                return searchResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Utils.LogHelper.LogError($"获取车站匹配时出错: {stationName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 处理站点输入框失去焦点事件
        /// </summary>
        /// <param name="stationName">站点名称</param>
        /// <param name="isDepartStation">是否为出发站</param>
        /// <returns>处理后的站点信息，如果找不到匹配站点则返回null</returns>
        public StationInfo HandleStationLostFocus(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return null;
            }

            try
            {
                // 确保站点数据已加载
                if (!_isInitialized)
                {
                    // 同步加载站点数据
                    var loadTask = LoadStationsAsync();
                    loadTask.Wait();
                }
                
                // 在站点列表中查找匹配的站点
                var station = ValidateStationName(stationName);
                
                return station;
            }
            catch (Exception ex)
            {
                Utils.LogHelper.LogError($"处理站点输入框失去焦点事件时出错: {stationName}", ex);
                return null;
            }
        }
    }
} 