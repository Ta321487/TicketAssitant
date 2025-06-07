using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 高德地图距离计算服务，用于计算两点之间的驾车距离
    /// </summary>
    public class DistanceCalculationService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configurationService;
        private const string ApiUrl = "https://restapi.amap.com/v3/distance";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        public DistanceCalculationService(ConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置超时时间为10秒
        }

        /// <summary>
        /// 计算两点之间的驾车距离
        /// </summary>
        /// <param name="originLon">起点经度</param>
        /// <param name="originLat">起点纬度</param>
        /// <param name="destLon">终点经度</param>
        /// <param name="destLat">终点纬度</param>
        /// <returns>距离（单位：公里）</returns>
        public async Task<decimal> CalculateDistanceAsync(string originLon, string originLat, string destLon, string destLat)
        {
            if (string.IsNullOrWhiteSpace(originLon) || string.IsNullOrWhiteSpace(originLat) ||
                string.IsNullOrWhiteSpace(destLon) || string.IsNullOrWhiteSpace(destLat))
            {
                throw new ArgumentException("经纬度参数不能为空");
            }

            try
            {
                // 从配置服务获取API key
                string apiKey = _configurationService.GetSettingValue("AmapWebServiceKey");

                // 检查API key是否存在
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new Exception("未配置高德地图API密钥，请在系统设置中添加相关信息");
                }

                // 构建起点和终点坐标字符串
                string origins = $"{originLon},{originLat}";
                string destination = $"{destLon},{destLat}";

                // 构建查询参数
                var parameters = new Dictionary<string, string>
                {
                    { "key", apiKey },
                    { "origins", origins },
                    { "destination", destination },
                    { "type", "1" }, // 1=驾车距离
                    { "output", "json" }
                };

                // 构建完整URL
                string queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                string fullUrl = $"{ApiUrl}?{queryString}";

                // 发送请求
                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                // 读取响应内容
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"高德地图距离计算API响应: {responseContent}");

                // 解析响应内容
                return ParseDistanceResponse(responseContent);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"计算距离时出错: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 计算多点之间的驾车距离
        /// </summary>
        /// <param name="origins">起点列表，格式为经度,纬度|经度,纬度...</param>
        /// <param name="destination">终点，格式为经度,纬度</param>
        /// <returns>距离列表（单位：公里）</returns>
        public async Task<List<decimal>> CalculateMultipleDistancesAsync(string origins, string destination)
        {
            if (string.IsNullOrWhiteSpace(origins) || string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("起点或终点参数不能为空");
            }

            try
            {
                // 从配置服务获取API key
                string apiKey = _configurationService.GetSettingValue("AmapWebServiceKey");

                // 检查API key是否存在
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new Exception("未配置高德地图API密钥，请在系统设置中添加相关信息");
                }

                // 构建查询参数
                var parameters = new Dictionary<string, string>
                {
                    { "key", apiKey },
                    { "origins", origins },
                    { "destination", destination },
                    { "type", "1" }, // 1=驾车距离
                    { "output", "json" }
                };

                // 构建完整URL
                string queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                string fullUrl = $"{ApiUrl}?{queryString}";

                // 发送请求
                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                // 读取响应内容
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"高德地图多点距离计算API响应: {responseContent}");

                // 解析响应内容
                return ParseMultipleDistancesResponse(responseContent);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"计算多点距离时出错: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 解析距离计算API响应
        /// </summary>
        /// <param name="response">API响应内容</param>
        /// <returns>距离（单位：公里）</returns>
        private decimal ParseDistanceResponse(string response)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    JsonElement root = doc.RootElement;

                    // 获取状态码和信息
                    string status = root.GetProperty("status").GetString();
                    string info = root.GetProperty("info").GetString();
                    string infocode = root.GetProperty("infocode").GetString();

                    // 检查API是否成功
                    if (status != "1" || infocode != "10000")
                    {
                        // 处理API错误
                        HandleApiError(info, infocode);
                    }

                    // 获取计算结果
                    if (root.TryGetProperty("results", out JsonElement results) && results.GetArrayLength() > 0)
                    {
                        JsonElement firstResult = results[0];
                        if (firstResult.TryGetProperty("distance", out JsonElement distance))
                        {
                            // 将返回的距离（米）转换为公里
                            if (decimal.TryParse(distance.GetString(), out decimal distanceInMeters))
                            {
                                return Math.Round(distanceInMeters / 1000, 2); // 转换为公里并保留两位小数
                            }
                        }
                    }

                    throw new Exception("无法从API响应中解析距离信息");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"解析距离计算API响应时出错: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 解析多点距离计算API响应
        /// </summary>
        /// <param name="response">API响应内容</param>
        /// <returns>距离列表（单位：公里）</returns>
        private List<decimal> ParseMultipleDistancesResponse(string response)
        {
            try
            {
                var distances = new List<decimal>();

                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    JsonElement root = doc.RootElement;

                    // 获取状态码和信息
                    string status = root.GetProperty("status").GetString();
                    string info = root.GetProperty("info").GetString();
                    string infocode = root.GetProperty("infocode").GetString();

                    // 检查API是否成功
                    if (status != "1" || infocode != "10000")
                    {
                        // 处理API错误
                        HandleApiError(info, infocode);
                    }

                    // 获取计算结果
                    if (root.TryGetProperty("results", out JsonElement results))
                    {
                        foreach (JsonElement result in results.EnumerateArray())
                        {
                            if (result.TryGetProperty("distance", out JsonElement distance))
                            {
                                // 将返回的距离（米）转换为公里
                                if (decimal.TryParse(distance.GetString(), out decimal distanceInMeters))
                                {
                                    distances.Add(Math.Round(distanceInMeters / 1000, 2)); // 转换为公里并保留两位小数
                                }
                                else
                                {
                                    distances.Add(0); // 解析失败时添加0
                                }
                            }
                            else
                            {
                                distances.Add(0); // 无距离信息时添加0
                            }
                        }
                    }

                    return distances;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"解析多点距离计算API响应时出错: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 处理API错误
        /// </summary>
        /// <param name="info">错误信息</param>
        /// <param name="infocode">错误码</param>
        private void HandleApiError(string info, string infocode)
        {
            // 根据错误码提供更友好的错误消息
            string errorMessage;
            switch (infocode)
            {
                case "10001":
                    errorMessage = "高德地图API密钥不正确或已过期，请在系统设置中更新有效的API密钥。";
                    break;
                case "10002":
                    errorMessage = "没有权限使用该服务，请确认您的高德地图API密钥已开通此项服务权限。";
                    break;
                case "10003":
                    errorMessage = "访问已超出日访问量限制，请检查您的API密钥是否为企业版。";
                    break;
                case "10004":
                    errorMessage = "单位时间内访问次数超出限制，请控制访问频率。";
                    break;
                case "10005":
                    errorMessage = "IP白名单出错，请在高德开放平台检查您的IP白名单设置。";
                    break;
                case "10006":
                    errorMessage = "绑定域名无效，请在高德开放平台检查您的域名设置。";
                    break;
                case "10008":
                    errorMessage = "IP访问超限，请在高德开放平台检查您的IP访问限制。";
                    break;
                case "10009":
                    errorMessage = "API密钥类型错误：您可能在Web服务中使用了Web端的密钥，或在Web端使用了Web服务的密钥。请在系统设置中检查并正确填写不同类型的API密钥。";
                    break;
                case "20000":
                    errorMessage = "请求参数非法，请检查坐标信息是否正确。";
                    break;
                case "20001":
                    errorMessage = "缺少必填参数，请检查坐标信息是否完整。";
                    break;
                case "20800":
                    errorMessage = "查询地点不在中国陆地范围内，暂不支持海外地点查询。";
                    break;
                case "40000":
                    errorMessage = "API密钥余额已耗尽，请在高德开放平台充值后再试。";
                    break;
                default:
                    errorMessage = $"API错误 ({infocode}): {info}";
                    break;
            }

            LogHelper.LogError($"距离计算API错误: {errorMessage}");
            throw new Exception(errorMessage);
        }
    }
}