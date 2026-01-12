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

        // 基础URL，版本号将作为查询参数
        private const string Station12306BaseUrl = "https://kyfw.12306.cn/otn/resources/js/framework/station_name.js";

        // 配置键名
        private const string Station12306VersionKey = "Station12306Version";

        // 默认版本号
        private const string DefaultVersion = "1.9365";

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
        private string GetStation12306Url(string version)
        {
            return $"{Station12306BaseUrl}?station_version={version}";
        }

        /// <summary>
        /// 从配置获取当前保存的版本号
        /// </summary>
        /// <returns>当前版本号</returns>
        private string GetCurrentVersion()
        {
            string version = _configurationService.GetSettingValue(Station12306VersionKey);
            if (string.IsNullOrEmpty(version))
            {
                return DefaultVersion; // 默认版本
            }
            return version;
        }

        /// <summary>
        /// 保存版本号到配置
        /// </summary>
        /// <param name="version">版本号</param>
        private void SaveCurrentVersion(string version)
        {
            _configurationService.SaveSettingValue(Station12306VersionKey, version);
            LogHelper.LogInfo($"已更新12306车站数据版本号: {version}");
        }

        /// <summary>
        /// 尝试获取指定版本的车站数据
        /// </summary>
        /// <param name="version">版本号，如果为null或空则不带版本号参数</param>
        /// <returns>车站数据内容，获取失败则返回null</returns>
        private async Task<string> TryFetchStationDataAsync(string version = null)
        {
            try
            {
                string url = string.IsNullOrEmpty(version) 
                    ? Station12306BaseUrl 
                    : GetStation12306Url(version);
                LogHelper.LogInfo($"尝试获取12306车站数据: {url}");
                
                // 使用HttpResponseMessage以便获取响应头信息
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                string content = await response.Content.ReadAsStringAsync();

                // 验证返回的数据是否有效
                if (!string.IsNullOrEmpty(content) && content.Contains("var station_names"))
                {
                    // 尝试从响应头或URL中提取版本号
                    string detectedVersion = ExtractVersionFromResponse(response, url);
                    if (!string.IsNullOrEmpty(detectedVersion) && detectedVersion != version)
                    {
                        LogHelper.LogInfo($"检测到新版本号: {detectedVersion}");
                        SaveCurrentVersion(detectedVersion);
                    }
                    else if (string.IsNullOrEmpty(version))
                    {
                        // 如果不带版本号参数也能成功，保存当前使用的版本号（如果有的话）
                        // 从URL的Location头或Content-Location头中提取
                        var location = response.Headers.Location?.ToString() ?? 
                                      response.Content.Headers.ContentLocation?.ToString();
                        if (!string.IsNullOrEmpty(location))
                        {
                            var versionMatch = Regex.Match(
                                location, @"station_version=([\d.]+)");
                            if (versionMatch.Success)
                            {
                                SaveCurrentVersion(versionMatch.Groups[1].Value);
                            }
                        }
                    }
                    
                    return content;
                }
            }
            catch (Exception ex)
            {
                string versionInfo = string.IsNullOrEmpty(version) ? "无版本号" : $"station_version={version}";
                LogHelper.LogWarning($"获取12306 {versionInfo} 车站数据失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从HTTP响应中提取版本号
        /// </summary>
        /// <param name="response">HTTP响应</param>
        /// <param name="requestUrl">请求URL</param>
        /// <returns>提取的版本号，如果未找到则返回null</returns>
        private string ExtractVersionFromResponse(HttpResponseMessage response, string requestUrl)
        {
            try
            {
                // 从Location头中提取版本号
                if (response.Headers.Location != null)
                {
                    var location = response.Headers.Location.ToString();
                    var match = Regex.Match(
                        location, @"station_version=([\d.]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                // 从Content-Location头中提取版本号
                if (response.Content.Headers.ContentLocation != null)
                {
                    var contentLocation = response.Content.Headers.ContentLocation.ToString();
                    var match = Regex.Match(
                        contentLocation, @"station_version=([\d.]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                // 从请求URL中提取版本号（如果重定向了）
                var urlMatch = Regex.Match(
                    requestUrl, @"station_version=([\d.]+)");
                if (urlMatch.Success)
                {
                    return urlMatch.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogWarning($"提取版本号时出错: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从12306获取车站数据
        /// </summary>
        /// <returns>车站数据内容</returns>
        public async Task<string> FetchStationDataAsync()
        {
            try
            {
                // 首先尝试不带版本号参数（12306可能返回最新版本）
                LogHelper.LogInfo("尝试不带版本号参数获取12306车站数据...");
                string stationData = await TryFetchStationDataAsync(null);
                if (!string.IsNullOrEmpty(stationData))
                {
                    return stationData;
                }

                // 如果失败，尝试使用配置的版本号
                string currentVersion = GetCurrentVersion();
                LogHelper.LogInfo($"尝试使用配置的版本号 station_version={currentVersion}...");
                stationData = await TryFetchStationDataAsync(currentVersion);
                if (!string.IsNullOrEmpty(stationData))
                {
                    return stationData;
                }

                // 如果都失败，抛出异常
                throw new Exception($"无法获取12306车站数据。已尝试：不带版本号参数和 station_version={currentVersion}");
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

        /// <summary>
        /// 检查数据库中的车站是否与12306官方数据一致
        /// </summary>
        /// <returns>同步统计信息</returns>
        public async Task<(int officialCount, int databaseCount, int missingInDatabase, int extraInDatabase, List<string> extraStationCodes, List<string> missingStationCodes)> CheckDataConsistencyAsync()
        {
            try
            {
                // 获取12306官方数据
                string stationData = await FetchStationDataAsync();
                List<StationInfo> officialStations = ParseStationData(stationData);
                
                // 获取官方车站代码集合
                var officialStationCodes = new HashSet<string>(
                    officialStations.Where(s => !string.IsNullOrEmpty(s.StationCode))
                                   .Select(s => s.StationCode),
                    StringComparer.OrdinalIgnoreCase);

                // 获取数据库中的所有车站
                var databaseStations = await _databaseService.GetStationsAsync();
                
                // 获取数据库中的车站代码集合（包括有代码和没代码的）
                var databaseStationCodes = new HashSet<string>(
                    databaseStations.Where(s => !string.IsNullOrEmpty(s.StationCode))
                                   .Select(s => s.StationCode),
                    StringComparer.OrdinalIgnoreCase);

                // 找出不在官方数据中的车站（数据库中有但官方没有）
                var extraInDatabase = databaseStationCodes.Except(officialStationCodes, StringComparer.OrdinalIgnoreCase).ToList();
                
                // 找出不在数据库中的官方车站（官方有但数据库没有）
                var missingInDatabase = officialStationCodes.Except(databaseStationCodes, StringComparer.OrdinalIgnoreCase).ToList();

                // 统计没有车站代码的记录
                int stationsWithoutCode = databaseStations.Count(s => string.IsNullOrEmpty(s.StationCode));

                LogHelper.LogInfo($"数据一致性检查完成：" +
                    $"官方车站数: {officialStationCodes.Count}, " +
                    $"数据库车站数: {databaseStations.Count}, " +
                    $"数据库中有但官方没有: {extraInDatabase.Count + stationsWithoutCode}, " +
                    $"官方有但数据库没有: {missingInDatabase.Count}");

                return (
                    officialCount: officialStationCodes.Count,
                    databaseCount: databaseStations.Count,
                    missingInDatabase: missingInDatabase.Count,
                    extraInDatabase: extraInDatabase.Count + stationsWithoutCode,
                    extraStationCodes: extraInDatabase,
                    missingStationCodes: missingInDatabase
                );
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"检查数据一致性失败: {ex.Message}", ex);
                throw new Exception($"检查数据一致性失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 同步数据库，删除不在12306官方数据中的车站（仅删除有车站代码但不在官方数据中的）
        /// </summary>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除统计信息</returns>
        public async Task<(int deletedCount, List<string> deletedStationNames)> SyncWithOfficialDataAsync(
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 获取一致性检查结果
                var (officialCount, databaseCount, missingInDatabase, extraInDatabase, extraStationCodes, missingStationCodes) = 
                    await CheckDataConsistencyAsync();

                if (extraInDatabase == 0)
                {
                    LogHelper.LogInfo("数据库已与12306官方数据一致，无需同步");
                    return (0, new List<string>());
                }

                // 获取需要删除的车站信息
                var stationsToDelete = new List<StationInfo>();
                var deletedStationNames = new List<string>();

                // 删除有车站代码但不在官方数据中的车站
                foreach (var stationCode in extraStationCodes)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var station = await _databaseService.GetStationByCodeAsync(stationCode);
                    if (station != null)
                    {
                        stationsToDelete.Add(station);
                        deletedStationNames.Add(station.StationName);
                    }
                }

                // 获取所有数据库中的车站，找出没有车站代码的记录
                var allDatabaseStations = await _databaseService.GetStationsAsync();
                var stationsWithoutCode = allDatabaseStations.Where(s => string.IsNullOrEmpty(s.StationCode)).ToList();
                
                // 将没有车站代码的记录也加入删除列表
                foreach (var station in stationsWithoutCode)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    stationsToDelete.Add(station);
                    deletedStationNames.Add(station.StationName);
                }

                // 删除不在官方数据中的车站
                int deletedCount = 0;
                if (stationsToDelete.Count > 0)
                {
                    var stationIdsToDelete = stationsToDelete.Select(s => s.Id).ToList();
                    await _databaseService.DeleteStationsByIdsAsync(stationIdsToDelete);
                    deletedCount = stationIdsToDelete.Count;
                    
                    LogHelper.LogInfo($"已删除{deletedCount}个不在12306官方数据中的车站（包含{stationsWithoutCode.Count}个无车站代码的记录）");
                }

                // 报告进度
                progressCallback?.Invoke(deletedCount, deletedCount);

                return (deletedCount, deletedStationNames);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"同步数据失败: {ex.Message}", ex);
                throw new Exception($"同步数据失败: {ex.Message}", ex);
            }
        }
    }
}