using System.Net.Http;
using System.Text.RegularExpressions;
using TA_WPF.Models;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 车站导入服务，提供从12306导入车站信息的功能
    /// </summary>
    public class StationImportService
    {
        private readonly DatabaseService _databaseService;
        private readonly ConfigurationService _configurationService;
        private readonly HttpClient _httpClient;

        // 基础URL，版本号将动态替换
        private const string Station12306BaseUrl = "https://www.12306.cn/index/script/core/common/station_name_new_v{0}.js";

        // 配置键名
        private const string Station12306VersionKey = "Station12306Version";

        // 默认版本号和最大尝试版本数
        private const int DefaultVersion = 10080;
        private const int MaxVersionAttempts = 5;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="configurationService">配置服务</param>
        public StationImportService(DatabaseService databaseService, ConfigurationService configurationService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间为30秒
        }

        /// <summary>
        /// 获取12306车站数据URL
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>完整URL</returns>
        private string GetStation12306Url(int version)
        {
            return string.Format(Station12306BaseUrl, version);
        }

        /// <summary>
        /// 从配置获取当前保存的版本号
        /// </summary>
        /// <returns>当前版本号</returns>
        private int GetCurrentVersion()
        {
            string versionStr = _configurationService.GetSettingValue(Station12306VersionKey);
            if (int.TryParse(versionStr, out int version))
            {
                return version;
            }
            return DefaultVersion; // 默认版本
        }

        /// <summary>
        /// 保存版本号到配置
        /// </summary>
        /// <param name="version">版本号</param>
        private void SaveCurrentVersion(int version)
        {
            _configurationService.SaveSettingValue(Station12306VersionKey, version.ToString());
            LogHelper.LogInfo($"已更新12306车站数据版本号: v{version}");
        }

        /// <summary>
        /// 尝试获取指定版本的车站数据
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>车站数据内容，获取失败则返回null</returns>
        private async Task<string> TryFetchStationDataAsync(int version)
        {
            try
            {
                string url = GetStation12306Url(version);
                LogHelper.LogInfo($"尝试获取12306车站数据: {url}");
                var response = await _httpClient.GetStringAsync(url);

                // 验证返回的数据是否有效
                if (!string.IsNullOrEmpty(response) && response.Contains("var station_names"))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogWarning($"获取12306 v{version}车站数据失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从12306获取车站数据，自动检测并使用最新版本
        /// </summary>
        /// <returns>车站数据内容</returns>
        public async Task<string> FetchStationDataAsync()
        {
            try
            {
                // 获取当前保存的版本号
                int currentVersion = GetCurrentVersion();
                string stationData = null;

                // 首先尝试使用当前版本
                stationData = await TryFetchStationDataAsync(currentVersion);
                if (!string.IsNullOrEmpty(stationData))
                {
                    return stationData;
                }

                // 当前版本失败，尝试更高版本
                for (int i = 1; i <= MaxVersionAttempts; i++)
                {
                    int newVersion = currentVersion + i;
                    stationData = await TryFetchStationDataAsync(newVersion);

                    if (!string.IsNullOrEmpty(stationData))
                    {
                        // 找到更高版本，保存并返回
                        SaveCurrentVersion(newVersion);
                        return stationData;
                    }
                }

                // 更高版本都失败，尝试更低版本
                for (int i = 1; i <= MaxVersionAttempts; i++)
                {
                    int oldVersion = currentVersion - i;
                    if (oldVersion <= 0) break; // 避免尝试负数或零版本

                    stationData = await TryFetchStationDataAsync(oldVersion);

                    if (!string.IsNullOrEmpty(stationData))
                    {
                        // 找到更低版本，保存并返回
                        SaveCurrentVersion(oldVersion);
                        return stationData;
                    }
                }

                // 所有尝试都失败，抛出异常
                throw new Exception($"无法获取12306车站数据，尝试了v{currentVersion - MaxVersionAttempts}至v{currentVersion + MaxVersionAttempts}的版本");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"从12306获取车站数据失败: {ex.Message}", ex);
                throw new Exception($"获取车站数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析12306车站数据
        /// </summary>
        /// <param name="stationData">12306车站数据</param>
        /// <returns>车站信息列表</returns>
        public List<StationInfo> ParseStationData(string stationData)
        {
            try
            {
                var stations = new List<StationInfo>();

                // 提取var station_names ='...'之间的内容
                var match = Regex.Match(stationData, @"var station_names ='(.*?)';", RegexOptions.Singleline);
                if (!match.Success)
                {
                    LogHelper.LogError("未能从12306数据中提取车站信息");
                    return stations;
                }

                string stationNamesStr = match.Groups[1].Value;

                // 按@符号分割每个车站信息
                var stationEntries = stationNamesStr.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

                int count = 0;
                foreach (var entry in stationEntries)
                {
                    // 按|符号分割车站属性
                    var parts = entry.Split('|');
                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    // 从12306数据中提取关键信息
                    // 格式: @bjb|北京北|VAP|beijingbei|bjb|0|0357|北京|||
                    // 索引: 0:拼音缩写, 1:站名, 2:车站代码, 3:拼音, 4:拼音缩写, 5:索引, 6:ID, 7:所在省...
                    string stationName = parts[1];
                    string stationCode = parts.Length > 2 ? parts[2] : "";  // 确保使用车站代码作为唯一标识
                    string stationPinyin = parts.Length > 3 ? parts[3] : "";

                    // 确保站名以"站"结尾
                    string formattedStationName = StationNameHelper.EnsureStationSuffix(stationName);

                    // 确保拼音字段长度不超过数据库字段最大长度（50个字符）
                    if (stationPinyin.Length > 50)
                    {
                        LogHelper.LogWarning($"车站 '{formattedStationName}' 的拼音字段长度 ({stationPinyin.Length}) 超过数据库限制 (50)，已自动截断");
                        stationPinyin = stationPinyin.Substring(0, 50);
                    }

                    // 创建车站信息对象
                    var station = new StationInfo
                    {
                        StationName = formattedStationName,
                        StationCode = stationCode,
                        StationPinyin = stationPinyin
                    };

                    stations.Add(station);
                    count++;
                }

                LogHelper.LogInfo($"从12306数据中解析了{count}个车站信息");
                return stations;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"解析12306车站数据失败: {ex.Message}", ex);
                throw new Exception($"解析车站数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将车站信息存入数据库
        /// </summary>
        /// <param name="stations">车站信息列表</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>导入统计信息</returns>
        public async Task<(int total, int imported, int skipped, List<string> newStations, List<int> importedIds)> ImportStationsAsync(
            List<StationInfo> stations,
            Action<int, int> progressCallback,
            CancellationToken cancellationToken = default)
        {
            int total = stations.Count;
            int imported = 0;
            int skipped = 0;
            var newStations = new List<string>();
            var importedIds = new List<int>();

            try
            {
                // 获取数据库中现有的车站
                var existingStations = await _databaseService.GetStationsAsync();

                // 使用station_code作为主键，存储已有车站的station_code
                var existingStationCodes = new HashSet<string>(
                    existingStations.Where(s => !string.IsNullOrEmpty(s.StationCode))
                                   .Select(s => s.StationCode));

                // 逐个导入车站
                for (int i = 0; i < stations.Count; i++)
                {
                    // 检查是否请求取消
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogHelper.LogInfo($"导入中断：已导入{imported}个车站，剩余{stations.Count - i}个车站未导入");
                        // 如果已经导入了一些车站，需要回滚
                        if (importedIds.Count > 0)
                        {
                            await RollbackImportedStationsAsync(importedIds);
                            importedIds.Clear();
                            newStations.Clear();
                            imported = 0;
                        }
                        return (total, imported, skipped, newStations, importedIds);
                    }

                    var station = stations[i];

                    // 如果车站代码为空，跳过该车站
                    if (string.IsNullOrEmpty(station.StationCode))
                    {
                        LogHelper.LogWarning($"车站 '{station.StationName}' 的代码为空，已跳过");
                        skipped++;
                        continue;
                    }

                    // 检查车站代码是否已存在
                    if (existingStationCodes.Contains(station.StationCode))
                    {
                        skipped++;
                    }
                    else
                    {
                        // 添加新车站
                        bool success = await _databaseService.AddStationAsync(station);
                        if (success)
                        {
                            // 获取插入的车站ID
                            var insertedStation = await _databaseService.GetStationByCodeAsync(station.StationCode);
                            if (insertedStation != null)
                            {
                                importedIds.Add(insertedStation.Id);
                                // 将新的车站代码添加到已存在列表中，防止重复导入
                                existingStationCodes.Add(station.StationCode);
                            }

                            imported++;
                            newStations.Add(station.StationName);
                        }
                        else
                        {
                            // 记录导入失败
                            LogHelper.LogError($"导入车站 '{station.StationName}' 失败");
                            skipped++;
                        }
                    }

                    // 报告进度
                    progressCallback?.Invoke(i + 1, total);
                }

                LogHelper.LogInfo($"导入完成：总共{total}个车站，新增{imported}个，跳过{skipped}个");
                return (total, imported, skipped, newStations, importedIds);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"导入车站信息失败: {ex.Message}", ex);
                // 如果导入过程中出错，尝试回滚已导入的数据
                if (importedIds.Count > 0)
                {
                    await RollbackImportedStationsAsync(importedIds);
                }
                throw new Exception($"导入车站信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 回滚已导入的车站数据
        /// </summary>
        /// <param name="stationIds">已导入的车站ID列表</param>
        /// <returns>异步任务</returns>
        public async Task RollbackImportedStationsAsync(List<int> stationIds)
        {
            try
            {
                if (stationIds == null || stationIds.Count == 0)
                {
                    return;
                }

                // 调用数据库服务删除指定ID的车站
                await _databaseService.DeleteStationsByIdsAsync(stationIds);

                // 重置自增ID，避免空洞
                await _databaseService.ResetStationsAutoIncrementAsync();

                LogHelper.LogInfo($"已回滚{stationIds.Count}个导入的车站");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"回滚导入的车站数据时出错: {ex.Message}", ex);
                throw;
            }
        }
    }
}