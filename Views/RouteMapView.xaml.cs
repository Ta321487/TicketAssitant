using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// RouteMapView.xaml 的交互逻辑
    /// </summary>
    public partial class RouteMapView : UserControl
    {
        private RouteMapViewModel _viewModel;
        private bool _isWebViewInitialized = false;

        public RouteMapView()
        {
            InitializeComponent();
            
            // 注册加载事件
            this.Loaded += RouteMapView_Loaded;
            this.DataContextChanged += RouteMapView_DataContextChanged;
            
            // 注册键盘事件，用于开发阶段的F12调试
            this.KeyDown += RouteMapView_KeyDown;
            this.Focusable = true;
            this.Focus();
        }
        
        /// <summary>
        /// 处理键盘事件，支持F12打开开发者工具
        /// </summary>
        private void RouteMapView_KeyDown(object sender, KeyEventArgs e)
        {
            // 监听F12键，用于打开开发者工具
            if (e.Key == Key.F12)
            {
                Debug.WriteLine("F12按下，打开开发者工具");
                
                if (_isWebViewInitialized && MapWebView?.CoreWebView2 != null)
                {
                    // 打开开发者工具
                    MapWebView.CoreWebView2.OpenDevToolsWindow();
                    e.Handled = true;
                }
            }
        }

        private void RouteMapView_Loaded(object sender, RoutedEventArgs e)
        {
            // 避免重复初始化
            if (_isWebViewInitialized)
            {
                return;
            }
            
            // 确保DataContext有效
            if (DataContext is RouteMapViewModel viewModel)
            {
                _viewModel = viewModel;
                
                // 初始化WebView2
                InitializeWebView();
            }
            else
            {
                // 延迟初始化，等待DataContext设置完成
                Dispatcher.BeginInvoke(new Action(() => {
                    if (DataContext is RouteMapViewModel delayedViewModel)
                    {
                        _viewModel = delayedViewModel;
                        InitializeWebView();
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void RouteMapView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is RouteMapViewModel viewModel)
            {
                _viewModel = viewModel;
            }
        }

        /// <summary>
        /// 初始化WebView2控件
        /// </summary>
        private async void InitializeWebView()
        {
            try
            {
                if (_isWebViewInitialized)
                {
                    return;
                }

                // 先标记为已初始化，防止重复调用
                _isWebViewInitialized = true;
                
                // 取消订阅Loaded事件，防止重复初始化
                this.Loaded -= RouteMapView_Loaded;

                // 创建WebView2环境选项
                var options = new CoreWebView2EnvironmentOptions();
                
                // 设置WebView2数据文件夹 - 使用程序目录下的WebView2Data文件夹
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string appDataPath = Path.Combine(baseDirectory, "WebView2Data");
                
                // 确保文件夹存在
                Directory.CreateDirectory(appDataPath);
                
                try
                {
                    // 初始化WebView2环境
                    var environment = await CoreWebView2Environment.CreateAsync(null, appDataPath, options);
                    
                    // 初始化WebView2控件 - 使用await防止其他地方同时初始化
                    await MapWebView.EnsureCoreWebView2Async(environment);
                    
                    // 确保HTML文件夹存在
                    string htmlFolderPath = Path.Combine(baseDirectory, "Assets", "html");
                    if (!Directory.Exists(htmlFolderPath))
                    {
                        Directory.CreateDirectory(htmlFolderPath);
                        Debug.WriteLine($"创建目录: {htmlFolderPath}");
                    }
                    
                    // 检查HTML文件是否存在
                    string htmlFilePath = Path.Combine(htmlFolderPath, "RouteMap.html");
                    if (!File.Exists(htmlFilePath) && _viewModel != null)
                    {
                        // 从嵌入资源创建HTML文件
                        CreateHtmlFile(htmlFilePath);
                    }
                    
                    // 获取HTML文件路径
                    if (_viewModel != null)
                    {
                        string mapFilePath = _viewModel.GetMapHtmlFilePath();
                        
                        if (!string.IsNullOrEmpty(mapFilePath) && File.Exists(mapFilePath))
                        {
                            // 加载HTML文件
                            MapWebView.CoreWebView2.Navigate(new Uri(mapFilePath).AbsoluteUri);
                        }
                        else
                        {
                            // 显示错误面板
                            MapErrorPanel.Visibility = Visibility.Visible;
                            Debug.WriteLine($"HTML文件不存在: {mapFilePath}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("ViewModel为空，无法获取地图HTML路径");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WebView2初始化失败: {ex.Message}");
                    MapErrorPanel.Visibility = Visibility.Visible;
                    
                    // 重置初始化状态，以便可以重试
                    _isWebViewInitialized = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化WebView2时出错: {ex.Message}");
                MessageBoxHelper.ShowError($"初始化地图控件时发生错误: {ex.Message}", "错误");
                
                // 显示错误面板
                MapErrorPanel.Visibility = Visibility.Visible;
                
                // 重置初始化状态，以便可以重试
                _isWebViewInitialized = false;
            }
        }
        
        /// <summary>
        /// 创建HTML文件
        /// </summary>
        private void CreateHtmlFile(string filePath)
        {
            try
            {
                // 创建HTML内容（简化版本，仅作为示例）
                string htmlContent = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>行程路线地图</title>
    <style>
        html, body {
            margin: 0;
            padding: 0;
            height: 100%;
            width: 100%;
            overflow: hidden;
        }
        
        #container {
            width: 100%;
            height: 100%;
            position: relative;
        }
        
        .legend {
            position: absolute;
            bottom: 20px;
            left: 20px;
            padding: 10px;
            background-color: rgba(255, 255, 255, 0.8);
            border-radius: 4px;
            box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
            font-size: 12px;
            z-index: 100;
        }
    </style>
</head>
<body>
    <div id=""container"">地图加载中...</div>
    <script>
        // 地图初始化代码将由WebView2通过JavaScript注入
        function setAmapKeys(webKey, securityKey) {
            console.log('API密钥已设置');
        }
        
        function loadRouteData(routeData) {
            console.log('加载路线数据');
        }
    </script>
</body>
</html>";

                // 写入HTML文件
                File.WriteAllText(filePath, htmlContent);
                Debug.WriteLine($"创建HTML文件: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建HTML文件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// WebView2控件初始化完成事件
        /// </summary>
        private async void MapWebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            try
            {
                if (e.IsSuccess)
                {
                    // 配置WebView2
                    MapWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true; // 在开发阶段启用右键菜单
                    MapWebView.CoreWebView2.Settings.AreDevToolsEnabled = true; // 在开发阶段启用开发者工具
                    
                    // 设置WebView2与WPF之间的通信
                    MapWebView.CoreWebView2.AddHostObjectToScript("hostObject", new WebViewHostObject(this));
                    
                    // 引用本地HTML文件的脚本资源
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string htmlFolderPath = Path.Combine(baseDirectory, "Assets", "html");
                    
                    // 配置虚拟主机文件夹映射
                    MapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "local.resources", htmlFolderPath, CoreWebView2HostResourceAccessKind.Allow);
                        
                    // 添加控制台消息处理
                    MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                    
                    // 注释：在开发阶段，可以通过F12打开开发者工具
                    Debug.WriteLine("WebView2初始化成功，可以按F12打开开发者工具");
                }
                else
                {
                    Debug.WriteLine($"WebView2初始化失败: {e.InitializationException?.Message}");
                    // 显示错误面板
                    MapErrorPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2初始化完成事件处理时出错: {ex.Message}");
                // 显示错误面板
                MapErrorPanel.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// 处理WebView2发送的消息
        /// </summary>
        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                Debug.WriteLine($"从WebView收到消息: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理WebView消息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// WebView2导航完成事件
        /// </summary>
        private async void MapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (e.IsSuccess)
                {
                    // 隐藏错误面板
                    MapErrorPanel.Visibility = Visibility.Collapsed;
                    
                    // 初始化地图
                    await _viewModel.InitializeMapAsync(MapWebView.CoreWebView2);
                    
                    // 注入控制台日志捕获脚本
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        console.defaultLog = console.log.bind(console);
                        console.log = function(message) {
                            console.defaultLog(message);
                            window.chrome.webview.postMessage('LOG: ' + message);
                        };
                        
                        console.defaultError = console.error.bind(console);
                        console.error = function(message) {
                            console.defaultError(message);
                            window.chrome.webview.postMessage('ERROR: ' + message);
                        };
                    ");
                }
                else
                {
                    Debug.WriteLine($"WebView2导航失败: {e.WebErrorStatus}");
                    // 显示错误面板
                    MapErrorPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2导航完成事件处理时出错: {ex.Message}");
                // 显示错误面板
                MapErrorPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 刷新地图数据
        /// </summary>
        public async void RefreshMap()
        {
            try
            {
                if (_viewModel != null && _isWebViewInitialized && MapWebView.CoreWebView2 != null)
                {
                    // 将useJavaScriptRefresh设置为true，表示由JavaScript端负责处理加载指示器
                    await _viewModel.RefreshMapDataAsync(MapWebView.CoreWebView2, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新地图数据时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// WebView2和WPF之间的通信对象
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WebViewHostObject
    {
        private readonly RouteMapView _parent;

        public WebViewHostObject(RouteMapView parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// JavaScript可以调用此方法来刷新地图数据
        /// </summary>
        public void RefreshMap()
        {
            // 在UI线程上执行
            _parent.Dispatcher.Invoke(() => _parent.RefreshMap());
        }

        /// <summary>
        /// JavaScript可以调用此方法记录消息到日志
        /// </summary>
        public void LogMessage(string message)
        {
            Debug.WriteLine($"WebView消息: {message}");
        }
    }
} 