using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly HttpClient _httpClient;
        private const string Station12306Url = "https://www.12306.cn/index/script/core/common/station_name_new_v10079.js";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public StationImportService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 设置超时时间为30秒
        }

        /// <summary>
        /// 从12306获取车站数据
        /// </summary>
        /// <returns>车站数据内容</returns>
        public async Task<string> FetchStationDataAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(Station12306Url);
                return response;
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
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    // 提取车站名和编码
                    string stationName = parts[1];
                    string stationCode = parts.Length > 2 ? parts[2] : "";
                    string stationPinyin = parts.Length > 3 ? parts[3] : "";
                    string stationPinyinAbbr = parts.Length > 4 ? parts[4] : "";
                    
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
        /// <returns>导入统计信息</returns>
        public async Task<(int total, int imported, int skipped, List<string> newStations, List<int> importedIds)> ImportStationsAsync(
            List<StationInfo> stations, 
            Action<int, int> progressCallback)
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
                var existingStationNames = new HashSet<string>(
                    existingStations.Select(s => StationNameHelper.RemoveStationSuffix(s.StationName)));
                
                // 逐个导入车站
                for (int i = 0; i < stations.Count; i++)
                {
                    var station = stations[i];
                    string stationNameWithoutSuffix = StationNameHelper.RemoveStationSuffix(station.StationName);
                    
                    // 检查车站是否已存在
                    if (existingStationNames.Contains(stationNameWithoutSuffix))
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
                            var insertedStation = await _databaseService.GetStationByNameAsync(station.StationName);
                            if (insertedStation != null)
                            {
                                importedIds.Add(insertedStation.Id);
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