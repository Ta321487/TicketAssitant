using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class RouteMapViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ConfigurationService _configurationService;
        private bool _isMapInitialized;
        private bool _isLoading;
        private string _amapWebKey;
        private string _amapSecurityKey;
        private CoreWebView2 _currentWebView;
        private bool _isRefreshing;

        public RouteMapViewModel(DatabaseService databaseService, ConfigurationService configurationService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            
            // 从配置中加载高德地图API密钥
            _amapWebKey = _configurationService.GetSettingValue("AmapWebKey") ?? "";
            _amapSecurityKey = _configurationService.GetSettingValue("AmapSecurityKey") ?? "";

            RefreshMapCommand = new RelayCommand(async () => await RefreshMapDataAsync());
            
            // 订阅ConfigurationService的事件，在API密钥更新时刷新地图
            ConfigurationService.ApiKeyUpdated += OnApiKeyUpdated;
        }
        
        /// <summary>
        /// 处理API密钥更新事件
        /// </summary>
        private async void OnApiKeyUpdated(object sender, ApiKeyEventArgs e)
        {
            try
            {
                // 检查是否是地图相关的API密钥
                if (e.KeyName == "AmapWebKey" || e.KeyName == "AmapSecurityKey")
                {
                    Debug.WriteLine($"地图API密钥已更新: {e.KeyName}");
                    
                    // 更新本地存储的密钥
                    if (e.KeyName == "AmapWebKey")
                    {
                        _amapWebKey = e.Value ?? "";
                    }
                    else if (e.KeyName == "AmapSecurityKey")
                    {
                        _amapSecurityKey = e.Value ?? "";
                    }
                    
                    // 如果地图已初始化，则更新地图API密钥
                    if (_isMapInitialized && _currentWebView != null)
                    {
                        await UpdateMapApiKeysAsync(_currentWebView);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理API密钥更新事件时出错: {ex.Message}", ex);
                Debug.WriteLine($"处理API密钥更新事件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新地图API密钥
        /// </summary>
        public async Task UpdateMapApiKeysAsync(CoreWebView2 webView)
        {
            try
            {
                if (webView != null)
                {
                    // 显示加载状态
                    IsLoading = true;
                    
                    // 设置高德地图API密钥
                    await webView.ExecuteScriptAsync($"setAmapKeys('{_amapWebKey}', '{_amapSecurityKey}');");
                    Debug.WriteLine("地图API密钥已更新");
                    
                    // 等待一段时间确保地图初始化完成
                    await Task.Delay(1000);
                    
                    // 刷新地图数据
                    await RefreshMapDataAsync(webView);
                    
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新地图API密钥时出错: {ex.Message}", ex);
                Debug.WriteLine($"更新地图API密钥时出错: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 地图是否已初始化
        /// </summary>
        public bool IsMapInitialized
        {
            get => _isMapInitialized;
            private set
            {
                if (_isMapInitialized != value)
                {
                    _isMapInitialized = value;
                    OnPropertyChanged(nameof(IsMapInitialized));
                }
            }
        }

        /// <summary>
        /// 是否正在加载数据
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
        /// 刷新地图数据命令
        /// </summary>
        public ICommand RefreshMapCommand { get; }

        /// <summary>
        /// 初始化地图
        /// </summary>
        public async Task InitializeMapAsync(CoreWebView2 webView)
        {
            try
            {
                if (webView != null)
                {
                    // 保存WebView2引用，以便后续更新API密钥
                    _currentWebView = webView;
                    
                    // 设置高德地图API密钥
                    await webView.ExecuteScriptAsync($"setAmapKeys('{_amapWebKey}', '{_amapSecurityKey}');");
                    IsMapInitialized = true;
                    
                    // 加载地图数据
                    await RefreshMapDataAsync(webView);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"初始化地图时出错: {ex.Message}", ex);
                Debug.WriteLine($"初始化地图时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 刷新地图数据
        /// </summary>
        public async Task RefreshMapDataAsync()
        {
            if (_isRefreshing)
            {
                Debug.WriteLine("地图数据刷新正在进行中，跳过本次刷新请求");
                return;
            }

            try
            {
                _isRefreshing = true;
                IsLoading = true;
                
                // 如果WebView2引用可用，则刷新地图数据
                if (_currentWebView != null)
                {
                    await RefreshMapDataAsync(_currentWebView).ConfigureAwait(false);
                }
                else
                {
                    // 需要在UI线程执行WebView相关操作
                    await Task.Delay(100).ConfigureAwait(false); // 确保UI更新
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新地图数据时出错: {ex.Message}", ex);
                Debug.WriteLine($"刷新地图数据时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _isRefreshing = false;
            }
        }
        
        /// <summary>
        /// 刷新地图数据（直接传入WebView）
        /// </summary>
        public async Task RefreshMapDataAsync(CoreWebView2 webView)
        {
            if (webView == null)
            {
                Debug.WriteLine("WebView为空，无法刷新地图数据");
                return;
            }

            // 防止重复刷新
            if (_isRefreshing && IsLoading)
            {
                Debug.WriteLine("地图数据刷新正在进行中，跳过本次刷新请求");
                return;
            }

            try
            {
                _isRefreshing = true;
                IsLoading = true;
                LogHelper.LogInfo("开始刷新地图数据");
                Debug.WriteLine("开始刷新地图数据");

                // 1. 从数据库获取行程数据 (可以在后台线程执行)
                var routeData = await GetRouteDataAsync().ConfigureAwait(false);
                
                // 记录获取到的数据
                Debug.WriteLine($"获取到{routeData.Count}条路线数据");
                
                // 如果获取的数据量非常少，添加更详细的日志
                if (routeData.Count < 10)
                {
                    Debug.WriteLine($"注意：路线数据数量较少({routeData.Count}条)，可能是因为站点信息不完整");
                    Debug.WriteLine("请确保数据库中的车站表包含完整的经纬度信息");
                    LogHelper.LogWarning($"路线数据数量较少({routeData.Count}条)，请检查车站经纬度信息完整性");
                }
                
                // 2. 将数据转换为前端可使用的JSON格式 (可以在后台线程执行)
                string jsonData = ConvertToJson(routeData);
                
                // 记录没有数据的情况
                if (routeData.Count == 0)
                {
                    Debug.WriteLine("没有可用的路线数据，地图将不显示任何路线");
                    LogHelper.LogWarning("没有可用的路线数据，请检查数据库中车站经纬度信息的完整性");
                }
                
                // 3. 所有WebView2操作必须回到UI线程执行
                // 使用Application.Current.Dispatcher确保在UI线程上执行WebView操作
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        // 检查地图是否初始化
                        string checkScript = "typeof map !== 'undefined' && map !== null";
                        string result = await webView.ExecuteScriptAsync(checkScript);
                        bool isMapInitialized = result.Trim().ToLower() == "true";
                        
                        if (!isMapInitialized && !string.IsNullOrEmpty(_amapWebKey))
                        {
                            // 如果地图未初始化但有API密钥，先重新设置API密钥
                            Debug.WriteLine("地图未初始化，重新设置API密钥");
                            await webView.ExecuteScriptAsync($"setAmapKeys('{_amapWebKey}', '{_amapSecurityKey}');");
                            
                            // 等待地图初始化
                            await Task.Delay(1000);
                        }
                        
                        // 加载路线数据
                        await webView.ExecuteScriptAsync($"loadRouteData({jsonData});");
                        LogHelper.LogInfo($"地图数据已刷新，加载了{routeData.Count}条路线");
                        Debug.WriteLine($"地图数据已刷新，加载了{routeData.Count}条路线");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError($"在UI线程执行WebView操作时出错: {ex.Message}", ex);
                        Debug.WriteLine($"在UI线程执行WebView操作时出错: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新地图数据时出错: {ex.Message}", ex);
                Debug.WriteLine($"刷新地图数据时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// 从数据库获取路线数据
        /// </summary>
        private async Task<List<RouteMapData>> GetRouteDataAsync()
        {
            try
            {
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                
                var result = new List<RouteMapData>();
                
                // 获取所有车票
                var tickets = await _databaseService.GetAllTrainRideInfosAsync().ConfigureAwait(false);
                LogHelper.LogInfo($"从数据库获取到{tickets.Count}条车票记录");
                Debug.WriteLine($"从数据库获取到{tickets.Count}条车票记录，耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                if (tickets.Count == 0)
                {
                    return result;
                }
                
                // 收集所有出发站和到达站的名称
                var allStationNames = new HashSet<string>();
                foreach (var ticket in tickets)
                {
                    if (!string.IsNullOrWhiteSpace(ticket.DepartStation))
                    {
                        allStationNames.Add(ticket.DepartStation);
                    }
                    if (!string.IsNullOrWhiteSpace(ticket.ArriveStation))
                    {
                        allStationNames.Add(ticket.ArriveStation);
                    }
                }
                
                // 创建站点信息查询任务字典
                var stationQueryTasks = new Dictionary<string, Task<StationInfo>>();
                
                // 创建站名和站点信息的映射字典
                var stationCache = new Dictionary<string, StationInfo>();
                
                Debug.WriteLine($"需要查询{allStationNames.Count}个站点信息");
                
                // 创建所有站点的查询任务
                foreach (var stationName in allStationNames)
                {
                    // 避免重复查询相同站名
                    if (!stationQueryTasks.ContainsKey(stationName))
                    {
                        stationQueryTasks[stationName] = _databaseService.GetStationByNameAsync(stationName);
                    }
                }
                
                // 等待所有查询任务完成
                await Task.WhenAll(stationQueryTasks.Values).ConfigureAwait(false);
                
                // 将查询结果填充到缓存
                foreach (var kvp in stationQueryTasks)
                {
                    var stationName = kvp.Key;
                    var stationTask = kvp.Value;
                    
                    var station = await stationTask;
                    if (station != null)
                    {
                        stationCache[stationName] = station;
                    }
                }
                
                Debug.WriteLine($"站点信息查询完成，成功获取{stationCache.Count}/{allStationNames.Count}个站点，耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                int missingDepartStationCount = 0;
                int missingArriveStationCount = 0;
                int missingDepartCoordinatesCount = 0;
                int missingArriveCoordinatesCount = 0;
                
                // 处理车票数据，获取起始站和终点站的经纬度信息
                foreach (var ticket in tickets)
                {
                    // 从缓存中获取站点信息
                    StationInfo departStation = null;
                    StationInfo arriveStation = null;
                    
                    if (!string.IsNullOrWhiteSpace(ticket.DepartStation))
                    {
                        stationCache.TryGetValue(ticket.DepartStation, out departStation);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(ticket.ArriveStation))
                    {
                        stationCache.TryGetValue(ticket.ArriveStation, out arriveStation);
                    }
                    
                    // 检查站点信息是否存在
                    if (departStation == null)
                    {
                        missingDepartStationCount++;
                        Debug.WriteLine($"车票 {ticket.TrainNo} ({ticket.DepartStation}-{ticket.ArriveStation}) 的出发站信息在数据库中不存在");
                        continue;
                    }
                    
                    if (arriveStation == null)
                    {
                        missingArriveStationCount++;
                        Debug.WriteLine($"车票 {ticket.TrainNo} ({ticket.DepartStation}-{ticket.ArriveStation}) 的到达站信息在数据库中不存在");
                        continue;
                    }
                    
                    // 检查经纬度信息是否完整
                    bool hasDepartCoordinates = !string.IsNullOrEmpty(departStation.Longitude) && !string.IsNullOrEmpty(departStation.Latitude);
                    bool hasArriveCoordinates = !string.IsNullOrEmpty(arriveStation.Longitude) && !string.IsNullOrEmpty(arriveStation.Latitude);
                    
                    if (!hasDepartCoordinates)
                    {
                        missingDepartCoordinatesCount++;
                        Debug.WriteLine($"车票 {ticket.TrainNo} 的出发站 {ticket.DepartStation} 缺少经纬度信息");
                    }
                    
                    if (!hasArriveCoordinates)
                    {
                        missingArriveCoordinatesCount++;
                        Debug.WriteLine($"车票 {ticket.TrainNo} 的到达站 {ticket.ArriveStation} 缺少经纬度信息");
                    }
                    
                    // 确保两个站点都有经纬度信息
                    if (hasDepartCoordinates && hasArriveCoordinates)
                    {
                        // 创建路线数据对象
                        var routeData = new RouteMapData
                        {
                            DepartStation = departStation.StationName,
                            DepartLongitude = double.Parse(departStation.Longitude),
                            DepartLatitude = double.Parse(departStation.Latitude),
                            ArriveStation = arriveStation.StationName,
                            ArriveLongitude = double.Parse(arriveStation.Longitude),
                            ArriveLatitude = double.Parse(arriveStation.Latitude),
                            TrainNo = ticket.TrainNo,
                            DepartDate = ticket.DepartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                            Money = ticket.Money ?? 0
                        };
                        
                        result.Add(routeData);
                    }
                }
                
                // 记录过滤信息
                int validCount = result.Count;
                int totalCount = tickets.Count;
                int filteredCount = totalCount - validCount;
                
                stopwatch.Stop();
                Debug.WriteLine($"路线数据获取总耗时: {stopwatch.ElapsedMilliseconds}ms");
                Debug.WriteLine($"路线数据过滤统计: 总车票数={totalCount}, 有效数据={validCount}, 被过滤={filteredCount}");
                Debug.WriteLine($"过滤原因: 出发站不存在={missingDepartStationCount}, 到达站不存在={missingArriveStationCount}");
                Debug.WriteLine($"过滤原因: 出发站缺少经纬度={missingDepartCoordinatesCount}, 到达站缺少经纬度={missingArriveCoordinatesCount}");
                
                LogHelper.LogInfo($"路线数据过滤统计: 总车票数={totalCount}, 有效数据={validCount}, 被过滤={filteredCount}, 总耗时: {stopwatch.ElapsedMilliseconds}ms");
                
                return result;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取路线数据时出错: {ex.Message}", ex);
                Debug.WriteLine($"获取路线数据时出错: {ex.Message}");
                return new List<RouteMapData>();
            }
        }
        
        /// <summary>
        /// 将路线数据转换为JSON字符串
        /// </summary>
        private string ConvertToJson(List<RouteMapData> routeData)
        {
            try
            {
                if (routeData == null || routeData.Count == 0)
                {
                    return "[]";
                }
                
                // 创建JSON数组
                var json = new System.Text.StringBuilder();
                json.Append("[");
                
                for (int i = 0; i < routeData.Count; i++)
                {
                    var data = routeData[i];
                    
                    // 添加单个路线数据
                    json.Append("{");
                    json.Append($"\"departStation\":\"{data.DepartStation}\",");
                    json.Append($"\"departLongitude\":{data.DepartLongitude},");
                    json.Append($"\"departLatitude\":{data.DepartLatitude},");
                    json.Append($"\"arriveStation\":\"{data.ArriveStation}\",");
                    json.Append($"\"arriveLongitude\":{data.ArriveLongitude},");
                    json.Append($"\"arriveLatitude\":{data.ArriveLatitude},");
                    json.Append($"\"trainNo\":\"{data.TrainNo}\",");
                    json.Append($"\"departDate\":\"{data.DepartDate}\",");
                    json.Append($"\"money\":{data.Money}");
                    json.Append("}");
                    
                    // 除最后一个元素外，添加逗号
                    if (i < routeData.Count - 1)
                    {
                        json.Append(",");
                    }
                }
                
                json.Append("]");
                return json.ToString();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"转换JSON数据时出错: {ex.Message}", ex);
                Debug.WriteLine($"转换JSON数据时出错: {ex.Message}");
                return "[]";
            }
        }
        
        /// <summary>
        /// 获取地图HTML文件路径
        /// </summary>
        public string GetMapHtmlFilePath()
        {
            try
            {
                // 构建HTML文件的完整路径
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string htmlFolderPath = Path.Combine(baseDirectory, "Assets", "html");
                string htmlFilePath = Path.Combine(htmlFolderPath, "RouteMap.html");
                
                // 先确保目录存在
                if (!Directory.Exists(htmlFolderPath))
                {
                    Directory.CreateDirectory(htmlFolderPath);
                    LogHelper.LogInfo($"创建地图HTML目录: {htmlFolderPath}");
                }
                
                // 确保文件存在
                if (File.Exists(htmlFilePath))
                {
                    return htmlFilePath;
                }
                else
                {
                    // 尝试从原始项目目录复制文件（如果存在）
                    string sourceFilePath = Path.Combine(
                        Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? "", 
                        "Assets", "html", "RouteMap.html");
                    
                    if (File.Exists(sourceFilePath))
                    {
                        // 复制文件
                        File.Copy(sourceFilePath, htmlFilePath, true);
                        LogHelper.LogInfo($"复制地图HTML文件: {sourceFilePath} -> {htmlFilePath}");
                        return htmlFilePath;
                    }
                    
                    LogHelper.LogError($"地图HTML文件不存在: {htmlFilePath}");
                    Debug.WriteLine($"地图HTML文件不存在: {htmlFilePath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"获取地图HTML文件路径时出错: {ex.Message}", ex);
                Debug.WriteLine($"获取地图HTML文件路径时出错: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 析构函数，取消事件订阅
        /// </summary>
        ~RouteMapViewModel()
        {
            // 取消订阅事件，避免内存泄漏
            ConfigurationService.ApiKeyUpdated -= OnApiKeyUpdated;
        }
    }

    /// <summary>
    /// 路线地图数据模型
    /// </summary>
    public class RouteMapData
    {
        /// <summary>
        /// 出发车站名称
        /// </summary>
        public string DepartStation { get; set; }
        
        /// <summary>
        /// 出发车站经度
        /// </summary>
        public double DepartLongitude { get; set; }
        
        /// <summary>
        /// 出发车站纬度
        /// </summary>
        public double DepartLatitude { get; set; }
        
        /// <summary>
        /// 到达车站名称
        /// </summary>
        public string ArriveStation { get; set; }
        
        /// <summary>
        /// 到达车站经度
        /// </summary>
        public double ArriveLongitude { get; set; }
        
        /// <summary>
        /// 到达车站纬度
        /// </summary>
        public double ArriveLatitude { get; set; }
        
        /// <summary>
        /// 车次
        /// </summary>
        public string TrainNo { get; set; }
        
        /// <summary>
        /// 出发日期
        /// </summary>
        public string DepartDate { get; set; }
        
        /// <summary>
        /// 票价
        /// </summary>
        public decimal Money { get; set; }
    }
} 