using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TA_WPF.Models;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 高德地图地理编码服务，用于获取地理位置信息
    /// </summary>
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configurationService;
        private const string ApiUrl = "https://restapi.amap.com/v3/geocode/geo";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configurationService">配置服务</param>
        public GeocodingService(ConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置超时时间为10秒
        }

        /// <summary>
        /// 根据地址获取地理编码信息
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns>地理编码结果列表</returns>
        public async Task<List<GeocodeResult>> GetGeocodingAsync(string address)
        {
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
                    { "address", address }
                };

                // 构建URL
                string url = $"{ApiUrl}?{string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))}";

                // 发送请求
                string response = await _httpClient.GetStringAsync(url);

                // 解析JSON响应
                return ParseGeocodingResponse(response);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取地理编码信息失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 解析地理编码响应
        /// </summary>
        /// <param name="response">API响应内容</param>
        /// <returns>地理编码结果列表</returns>
        private List<GeocodeResult> ParseGeocodingResponse(string response)
        {
            try
            {
                var results = new List<GeocodeResult>();
                var jsonDocument = JsonDocument.Parse(response);
                var root = jsonDocument.RootElement;

                // 检查API响应状态
                string status = root.GetProperty("status").GetString();
                if (status != "1")
                {
                    string infocode = root.GetProperty("infocode").GetString();
                    string info = root.GetProperty("info").GetString();
                    
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
                        case "20000":
                            errorMessage = "请求参数非法，请检查地址信息是否正确。";
                            break;
                        case "20001":
                            errorMessage = "缺少必填参数，请检查地址信息是否完整。";
                            break;
                        case "20011":
                            errorMessage = "查询坐标在海外，但没有海外地图权限。请使用国内坐标或升级API密钥。";
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
                    
                    LogHelper.LogError($"地理编码API错误: {errorMessage}");
                    throw new Exception(errorMessage);
                }

                // 获取结果计数
                int count = 0;
                if (root.TryGetProperty("count", out var countProperty))
                {
                    // 尝试直接获取Int32，如果失败则获取字符串并转换
                    if (countProperty.ValueKind == JsonValueKind.Number)
                    {
                        count = countProperty.GetInt32();
                    }
                    else
                    {
                        // 获取为字符串然后转换为整数
                        string countStr = countProperty.GetString();
                        if (!int.TryParse(countStr, out count))
                        {
                            count = 0; // 转换失败时默认为0
                            LogHelper.LogWarning($"无法解析count值: {countStr}");
                        }
                    }
                }
                
                if (count == 0)
                {
                    return results;
                }

                // 解析地理编码结果
                var geocodesArray = root.GetProperty("geocodes");
                foreach (var geocode in geocodesArray.EnumerateArray())
                {
                    var result = new GeocodeResult
                    {
                        FormattedAddress = geocode.GetProperty("formatted_address").GetString()
                    };

                    // 获取省份
                    if (geocode.TryGetProperty("province", out var provinceProperty))
                    {
                        result.Province = provinceProperty.GetString();
                    }

                    // 获取城市
                    if (geocode.TryGetProperty("city", out var cityProperty) && 
                        !cityProperty.ValueKind.Equals(JsonValueKind.Array))
                    {
                        result.City = cityProperty.GetString();
                    }

                    // 获取区县
                    if (geocode.TryGetProperty("district", out var districtProperty) && 
                        !districtProperty.ValueKind.Equals(JsonValueKind.Array))
                    {
                        result.District = districtProperty.GetString();
                    }

                    // 获取位置
                    if (geocode.TryGetProperty("location", out var locationProperty))
                    {
                        string location = locationProperty.GetString();
                        string[] coordinates = location.Split(',');
                        if (coordinates.Length == 2)
                        {
                            result.Longitude = coordinates[0];
                            result.Latitude = coordinates[1];
                        }
                    }

                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"解析地理编码响应失败: {ex.Message}", ex);
                throw;
            }
        }
    }
} 