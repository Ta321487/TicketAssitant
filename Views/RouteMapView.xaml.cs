using Microsoft.Web.WebView2.Core;
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
    public partial class RouteMapView : UserControl, IDisposable
    {
        private RouteMapViewModel _viewModel;
        private bool _isWebViewInitialized = false;
        private bool _isDisposed = false;

        public RouteMapView()
        {
            InitializeComponent();

            // 注册加载事件
            this.Loaded += RouteMapView_Loaded;
            this.Unloaded += RouteMapView_Unloaded;
            this.DataContextChanged += RouteMapView_DataContextChanged;

            // 注册键盘事件，用于开发阶段的F12调试
            this.KeyDown += RouteMapView_KeyDown;
            this.Focusable = true;
            this.Focus();

            // 注册视图可见性变化事件
            this.IsVisibleChanged += RouteMapView_IsVisibleChanged;
            
            // 注册大小变化事件，确保WebView2适应容器大小
            this.SizeChanged += RouteMapView_SizeChanged;
        }

        /// <summary>
        /// 处理视图可见性变化事件
        /// </summary>
        private void RouteMapView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible)
            {
                if (isVisible)
                {
                    // 视图变为可见时，重新初始化WebView
                    Debug.WriteLine("路线地图视图变为可见，准备初始化WebView");

                    // 在视图变为可见时，如果WebView未初始化或已被清理，则重新初始化
                    if (!_isWebViewInitialized || MapWebView.Source?.AbsoluteUri == "about:blank")
                    {
                        Debug.WriteLine("WebView需要重新初始化");
                        _isWebViewInitialized = false; // 重置初始化标记，确保完全重新初始化
                        InitializeWebView();
                    }
                    else if (_isWebViewInitialized && MapWebView?.CoreWebView2 != null && _viewModel != null)
                    {
                        // 如果WebView已初始化但可能需要刷新数据
                        Debug.WriteLine("WebView已初始化，尝试刷新地图数据");
                        // 优化：使用低优先级调度刷新，避免阻塞UI线程
                        Dispatcher.BeginInvoke(new Action(async () =>
                        {
                            try
                            {
                                // 确保应用了正确的主题
                                await MapWebView.CoreWebView2.ExecuteScriptAsync($"setMapTheme({(_viewModel.IsDarkMode ? "true" : "false")});");
                                Debug.WriteLine($"重新应用地图主题: {(_viewModel.IsDarkMode ? "深色" : "浅色")}");

                                // 改为异步刷新地图数据，减少UI线程阻塞
                                await Task.Delay(300); // 添加短暂延迟，确保UI响应
                                await _viewModel.RefreshMapDataAsync(MapWebView.CoreWebView2);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"尝试刷新地图数据时出错: {ex.Message}");
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                else
                {
                    // 视图变为不可见时，只执行轻量级的资源释放
                    Debug.WriteLine("路线地图视图变为不可见，执行轻量级资源释放");
                    LightReleaseWebViewResources();
                }
            }
        }

        /// <summary>
        /// 处理视图卸载事件
        /// </summary>
        private void RouteMapView_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("路线地图视图被卸载，释放WebView资源");
            ReleaseWebViewResources();

            // 注销事件处理器，防止内存泄漏
            this.Loaded -= RouteMapView_Loaded;
            this.Unloaded -= RouteMapView_Unloaded;
            this.DataContextChanged -= RouteMapView_DataContextChanged;
            this.KeyDown -= RouteMapView_KeyDown;
            this.IsVisibleChanged -= RouteMapView_IsVisibleChanged;
            this.SizeChanged -= RouteMapView_SizeChanged;
        }

        /// <summary>
        /// 轻量级释放WebView资源，只做必要的清理以便于后续重新使用
        /// </summary>
        private void LightReleaseWebViewResources()
        {
            try
            {
                if (_isWebViewInitialized && MapWebView?.CoreWebView2 != null)
                {
                    // 暂停地图动画和渲染，但不完全清空页面
                    MapWebView.CoreWebView2.ExecuteScriptAsync("if(loca && loca.animate) { loca.animate.pause(); }");
                    
                    // 优化：主动清理部分资源
                    MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        // 清理不再需要的对象
                        if (geoData && geoData.length > 100) { 
                            geoData = geoData.slice(0, 0); 
                        }
                        // 降低渲染分辨率以节省资源
                        if (map && map.getStatus) {
                            map.setStatus({
                                showLabel: false,
                                showIndoorMap: false
                            });
                        }");

                    Debug.WriteLine("已暂停地图渲染，执行轻量级资源释放");
                    LogHelper.LogInfo("执行轻量级WebView资源释放以节省内存");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"轻量级释放WebView资源时出错: {ex.Message}");
                LogHelper.LogError("轻量级释放WebView资源时出错", ex);
            }
        }

        /// <summary>
        /// 释放WebView资源
        /// </summary>
        private void ReleaseWebViewResources()
        {
            try
            {
                if (_isWebViewInitialized && MapWebView != null)
                {
                    // 移除WebMessage事件处理器
                    if (MapWebView.CoreWebView2 != null)
                    {
                        // 先在JavaScript端清理资源
                        try
                        {
                            MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                                // 清理地图资源
                                if (map) {
                                    try {
                                        // 清除所有事件监听
                                        map.clearEvents();
                                        // 销毁地图实例
                                        map.destroy();
                                        map = null;
                                    } catch(e) {
                                        console.error('清理map出错:', e);
                                    }
                                }
                                
                                // 清理Loca资源
                                if (loca) {
                                    try {
                                        // 停止所有动画
                                        if (loca.animate) loca.animate.stop();
                                        // 销毁所有图层
                                        loca.dispose();
                                        loca = null;
                                    } catch(e) {
                                        console.error('清理loca出错:', e);
                                    }
                                }
                                
                                // 清理标记点
                                if (markers && markers.length > 0) {
                                    markers = [];
                                }
                                
                                // 清理数据
                                if (geoData && geoData.length > 0) {
                                    geoData = [];
                                }
                                
                                // 清理批处理数据
                                if (window.batchedData && window.batchedData.length > 0) {
                                    window.batchedData = [];
                                }
                                
                                // 强制垃圾回收
                                if (window.gc) {
                                    window.gc();
                                }
                            ");
                        }
                        catch (Exception jsEx)
                        {
                            Debug.WriteLine($"清理JavaScript资源时出错: {jsEx.Message}");
                        }
                        
                        // 移除事件处理器
                        MapWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                        
                        // 移除注入的宿主对象
                        try
                        {
                            // 先调用ReleaseParent方法释放循环引用
                            MapWebView.CoreWebView2.ExecuteScriptAsync("if(chrome.webview.hostObjects.hostObject) chrome.webview.hostObjects.hostObject.releaseParent();");
                            // 等待执行完成
                            Task.Delay(50).Wait();
                            // 清除宿主对象引用
                            MapWebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.hostObjects.hostObject = undefined;");
                            MapWebView.CoreWebView2.RemoveHostObjectFromScript("hostObject");
                        }
                        catch (Exception hostEx)
                        {
                            Debug.WriteLine($"移除宿主对象时出错: {hostEx.Message}");
                        }
                    }

                    // 导航到空白页面帮助释放资源
                    MapWebView.NavigateToString("about:blank");
                    
                    // 重要：显式移除CoreWebView2的所有事件处理
                    if (MapWebView.CoreWebView2 != null)
                    {
                        // 使用反射移除所有事件处理器 - 更彻底的方式
                        try
                        {
                            var eventFields = typeof(CoreWebView2).GetFields(
                                System.Reflection.BindingFlags.Instance | 
                                System.Reflection.BindingFlags.NonPublic);
                                
                            foreach (var field in eventFields)
                            {
                                if (field.FieldType.IsSubclassOf(typeof(MulticastDelegate)))
                                {
                                    field.SetValue(MapWebView.CoreWebView2, null);
                                }
                            }
                        }
                        catch (Exception reflectionEx)
                        {
                            Debug.WriteLine($"使用反射清除事件处理器时出错: {reflectionEx.Message}");
                        }
                    }

                    // 通知ViewModel完全清理资源
                    if (_viewModel != null)
                    {
                        _viewModel.CompleteCleanupWebViewResources();
                    }

                    // 标记为未初始化，以便下次重新初始化
                    _isWebViewInitialized = false;

                    // 强制垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    Debug.WriteLine("WebView资源已完全释放");
                    LogHelper.LogInfo("WebView资源已完全释放以节省内存");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放WebView资源时出错: {ex.Message}");
                LogHelper.LogError("释放WebView资源时出错", ex);
            }
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
                Dispatcher.BeginInvoke(new Action(() =>
                {
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
                if (_isWebViewInitialized || _isDisposed)
                {
                    return;
                }

                // 先标记为已初始化，防止重复调用
                _isWebViewInitialized = true;

                // 取消订阅Loaded事件，防止重复初始化
                this.Loaded -= RouteMapView_Loaded;

                // 创建WebView2环境选项
                var options = new CoreWebView2EnvironmentOptions("--disable-web-security --disable-gpu-vsync --disable-accelerated-animations --disable-features=CalculateNativeWinOcclusion");

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

                    // 优化：配置WebView2内存和性能设置
                    MapWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                    MapWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                    MapWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                    MapWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                    MapWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                    MapWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

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
                    MapWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false; // 禁用右键菜单提高性能
                    MapWebView.CoreWebView2.Settings.AreDevToolsEnabled = true; // 在开发阶段启用开发者工具

                    // 优化：配置内存限制
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        if (window.performance && window.performance.memory) {
                            console.log('内存使用情况:', window.performance.memory.usedJSHeapSize / 1048576, 'MB');
                        }
                    ");

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

                    // 优化：减少不必要的事件监听
                    MapWebView.CoreWebView2.HistoryChanged -= MapWebView_CoreWebView2HistoryChanged;

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
        
        // 占位符事件处理器，用于优化中的禁用操作，实际代码中可能并不存在
        private void MapWebView_CoreWebView2HistoryChanged(object sender, object e)
        {
            // 这只是一个占位符，用于禁用事件
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

                    // 注入CSS样式确保内容不会溢出
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        const style = document.createElement('style');
                        style.textContent = `
                            html, body, #container {
                                width: 100% !important;
                                height: 100% !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                overflow: hidden !important;
                            }
                            .amap-logo, .amap-copyright {
                                z-index: 100 !important;
                            }
                        `;
                        document.head.appendChild(style);
                    ");

                    // 初始化地图前优化性能设置
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        // 优化渲染性能
                        if (typeof AMap !== 'undefined') {
                            AMap.Util.retina = false; // 禁用高分辨率显示
                        }
                    ");

                    // 初始化地图
                    await _viewModel.InitializeMapAsync(MapWebView.CoreWebView2);

                    // 注入控制台日志捕获脚本
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        console.defaultLog = console.log.bind(console);
                        console.log = function(message) {
                            // 只记录重要日志，提高性能
                            if (typeof message === 'string' && 
                                (message.includes('error') || 
                                 message.includes('加载') || 
                                 message.includes('初始化'))) {
                                console.defaultLog(message);
                                window.chrome.webview.postMessage('LOG: ' + message);
                            }
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
                    // 优化：在刷新前检测CPU和内存负载
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        // 检查是否需要释放资源
                        if (window.performance && window.performance.memory && 
                            window.performance.memory.usedJSHeapSize > 50 * 1024 * 1024) {
                            // 内存超过50MB，执行清理
                            if (geoData && geoData.length > 0) {
                                console.log('释放地图数据内存');
                                geoData = [];
                            }
                            // 手动触发垃圾回收
                            if (typeof gc === 'function') {
                                gc();
                            }
                        }
                    ");
                    
                    // 将useJavaScriptRefresh设置为true，表示由JavaScript端负责处理加载指示器
                    await _viewModel.RefreshMapDataAsync(MapWebView.CoreWebView2, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新地图数据时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// IDisposable接口实现，释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // 释放托管资源
                ReleaseWebViewResources();

                // 注销所有事件处理器
                this.Loaded -= RouteMapView_Loaded;
                this.Unloaded -= RouteMapView_Unloaded;
                this.DataContextChanged -= RouteMapView_DataContextChanged;
                this.KeyDown -= RouteMapView_KeyDown;
                this.IsVisibleChanged -= RouteMapView_IsVisibleChanged;
                this.SizeChanged -= RouteMapView_SizeChanged;
            }

            // 释放非托管资源

            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~RouteMapView()
        {
            Dispose(false);
        }

        /// <summary>
        /// 处理大小变化事件，确保WebView2内容适应容器大小
        /// </summary>
        private void RouteMapView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (_isWebViewInitialized && MapWebView?.CoreWebView2 != null)
                {
                    // 注入CSS以确保内容适应新的容器大小
                    MapWebView.CoreWebView2.ExecuteScriptAsync(@"
                        const style = document.createElement('style');
                        style.textContent = `
                            html, body, #container {
                                width: 100% !important;
                                height: 100% !important;
                                margin: 0 !important;
                                padding: 0 !important;
                                overflow: hidden !important;
                            }
                        `;
                        document.head.appendChild(style);
                    ");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理大小变化事件时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// WebView2和WPF之间的通信对象
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WebViewHostObject
    {
        private RouteMapView _parent;

        public WebViewHostObject(RouteMapView parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// JavaScript可以调用此方法来刷新地图数据
        /// </summary>
        public void RefreshMap()
        {
            // 检查父对象是否有效
            if (_parent == null)
            {
                Debug.WriteLine("父对象已被清理，无法刷新地图");
                return;
            }
            
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
        
        /// <summary>
        /// 释放父对象引用，避免循环引用
        /// </summary>
        public void ReleaseParent()
        {
            Debug.WriteLine("释放WebViewHostObject中的父对象引用");
            _parent = null;
        }
    }
}