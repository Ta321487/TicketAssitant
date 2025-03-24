using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using TA_WPF.Utils;
using TA_WPF.Views;
using System.Linq;
using System.IO;
using System.Xml;

namespace TA_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindow? _mainWindow;
        private LoginWindow? _loginWindow;

        // COM初始化常量
        private const int COINIT_APARTMENTTHREADED = 0x2;
        private const int COINIT_DISABLE_OLE1DDE = 0x4;

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr pvReserved, int dwCoInit);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                
                // 添加全局异常处理
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                
                // 初始化COM - 使用try-catch包裹
                try
                {
                    CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"COM初始化失败: {ex.Message}");
                    LogHelper.LogError($"COM初始化失败: {ex.Message}");
                    // 继续执行，不要因COM初始化失败而中断应用程序启动
                }
                
                // 设置应用程序的区域设置为中国
                Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                
                // 确保所有新线程也使用相同的区域设置
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN");
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("zh-CN");
                
                // 初始化主题服务并应用主题设置
                try
                {
                    var themeService = Services.ThemeService.Instance;
                    
                    // 从当前运行的可执行文件配置中获取主题设置
                    bool isDarkMode = themeService.LoadThemeFromConfig();
                    
                    Console.WriteLine($"应用程序启动时加载的主题设置: {(isDarkMode ? "深色" : "浅色")}");
                    LogHelper.LogInfo($"应用程序启动时加载的主题设置: {(isDarkMode ? "深色" : "浅色")}");
                    
                    // 确保资源字典中的主题标志被正确设置
                    if (Resources != null)
                    {
                        Resources["Theme.Dark"] = isDarkMode;
                        Resources["Theme.Light"] = !isDarkMode;
                        
                        // 获取BundledTheme
                        var bundledTheme = Resources.MergedDictionaries
                            .OfType<MaterialDesignThemes.Wpf.BundledTheme>()
                            .FirstOrDefault();
                        
                        if (bundledTheme != null)
                        {
                            // 设置基本主题
                            bundledTheme.BaseTheme = isDarkMode ? 
                                MaterialDesignThemes.Wpf.BaseTheme.Dark : 
                                MaterialDesignThemes.Wpf.BaseTheme.Light;
                            
                            Console.WriteLine($"已更新BundledTheme的BaseTheme为: {bundledTheme.BaseTheme}");
                            LogHelper.LogInfo($"已更新BundledTheme的BaseTheme为: {bundledTheme.BaseTheme}");
                        }
                    }
                    
                    // 应用主题
                    themeService.ApplyTheme(isDarkMode);
                    
                    // 验证主题是否已正确应用
                    bool verifyIsDarkMode = themeService.IsDarkThemeActive();
                    Console.WriteLine($"应用程序启动时验证主题设置: {(verifyIsDarkMode ? "深色" : "浅色")}");
                    LogHelper.LogInfo($"应用程序启动时验证主题设置: {(verifyIsDarkMode ? "深色" : "浅色")}");
                    
                    if (isDarkMode != verifyIsDarkMode)
                    {
                        Console.WriteLine($"警告: 主题设置验证失败，重新应用主题");
                        LogHelper.LogWarning($"警告: 主题设置验证失败，重新应用主题");
                        themeService.ApplyTheme(isDarkMode);
                    }
                    
                    LogHelper.LogInfo($"已应用{(isDarkMode ? "深色" : "浅色")}主题");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"初始化主题服务时出错: {ex.Message}");
                    LogHelper.LogError($"初始化主题服务时出错: {ex.Message}");
                }
                
                // 从配置文件加载字体大小设置
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["FontSize"] != null)
                    {
                        if (double.TryParse(config.AppSettings.Settings["FontSize"].Value, out double fontSize))
                        {
                            // 确保字体大小不小于最小可读值
                            if (fontSize < 12)
                            {
                                fontSize = 12;
                                // 更新配置文件中的值
                                config.AppSettings.Settings["FontSize"].Value = fontSize.ToString(CultureInfo.InvariantCulture);
                                config.Save(ConfigurationSaveMode.Modified);
                                ConfigurationManager.RefreshSection("appSettings");
                            }
                            
                            // 设置字体大小
                            Resources["MaterialDesignFontSize"] = fontSize;
                            Resources["MaterialDesignSubtitle1FontSize"] = fontSize + 2;
                            Resources["MaterialDesignSubtitle2FontSize"] = fontSize + 1;
                            Resources["MaterialDesignHeadline6FontSize"] = fontSize + 4;
                            Resources["MaterialDesignHeadline5FontSize"] = fontSize + 6;
                            
                            // 设置自定义字体大小资源 - 使用动态缩放因子
                            double scaleFactor = Math.Min(3.0, Math.Max(1.5, 4.0 - fontSize / 10.0)); // 字体越大，缩放因子越小
                            Resources["LargeFontSize"] = fontSize * scaleFactor;
                            Resources["MediumLargeFontSize"] = fontSize * (scaleFactor * 0.7);
                            Resources["MediumFontSize"] = fontSize * (scaleFactor * 0.5);
                            
                            // 记录日志
                            LogHelper.LogInfo($"从配置文件加载字体大小: {fontSize}pt，缩放因子: {scaleFactor}");
                        }
                    }
                    else
                    {
                        // 如果配置文件中没有字体大小设置，添加默认值
                        double defaultFontSize = 13;
                        if (config.AppSettings.Settings["FontSize"] == null)
                        {
                            config.AppSettings.Settings.Add("FontSize", defaultFontSize.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            config.AppSettings.Settings["FontSize"].Value = defaultFontSize.ToString(CultureInfo.InvariantCulture);
                        }
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                        
                        // 设置默认字体大小
                        Resources["MaterialDesignFontSize"] = defaultFontSize;
                        Resources["MaterialDesignSubtitle1FontSize"] = defaultFontSize + 2;
                        Resources["MaterialDesignSubtitle2FontSize"] = defaultFontSize + 1;
                        Resources["MaterialDesignHeadline6FontSize"] = defaultFontSize + 4;
                        Resources["MaterialDesignHeadline5FontSize"] = defaultFontSize + 6;
                        
                        // 设置自定义字体大小资源 - 使用动态缩放因子
                        double scaleFactor = Math.Min(3.0, Math.Max(1.5, 4.0 - defaultFontSize / 10.0)); // 字体越大，缩放因子越小
                        Resources["LargeFontSize"] = defaultFontSize * scaleFactor;
                        Resources["MediumLargeFontSize"] = defaultFontSize * (scaleFactor * 0.7);
                        Resources["MediumFontSize"] = defaultFontSize * (scaleFactor * 0.5);
                        
                        LogHelper.LogInfo($"使用默认字体大小: {defaultFontSize}pt，缩放因子: {scaleFactor}");
                    }
                    
                    // 确保App.xaml中的资源字典与配置文件一致
                    double currentFontSize = (double)Resources["MaterialDesignFontSize"];
                    if (config.AppSettings.Settings["FontSize"] != null)
                    {
                        if (double.TryParse(config.AppSettings.Settings["FontSize"].Value, out double configFontSize))
                        {
                            if (Math.Abs(currentFontSize - configFontSize) > 0.01)
                            {
                                // 如果资源字典中的值与配置文件不一致，更新资源字典
                                Resources["MaterialDesignFontSize"] = configFontSize;
                                Resources["MaterialDesignSubtitle1FontSize"] = configFontSize + 2;
                                Resources["MaterialDesignSubtitle2FontSize"] = configFontSize + 1;
                                Resources["MaterialDesignHeadline6FontSize"] = configFontSize + 4;
                                Resources["MaterialDesignHeadline5FontSize"] = configFontSize + 6;
                                
                                // 设置自定义字体大小资源 - 使用动态缩放因子
                                double scaleFactor = Math.Min(3.0, Math.Max(1.5, 4.0 - configFontSize / 10.0)); // 字体越大，缩放因子越小
                                Resources["LargeFontSize"] = configFontSize * scaleFactor;
                                Resources["MediumLargeFontSize"] = configFontSize * (scaleFactor * 0.7);
                                Resources["MediumFontSize"] = configFontSize * (scaleFactor * 0.5);
                                
                                LogHelper.LogInfo($"已同步字体大小设置: {configFontSize}pt，缩放因子: {scaleFactor}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载字体大小设置时出错: {ex.Message}");
                    LogHelper.LogError($"加载字体大小设置时出错: {ex.Message}");
                }

                // 创建并显示登录窗口
                _loginWindow = new LoginWindow();
                
                // 设置登录窗口为主窗口
                Current.MainWindow = _loginWindow;
                
                // 显示登录窗口
                _loginWindow.Show();

                // 监听登录窗口的关闭事件
                _loginWindow.Closed += (s, args) =>
                {
                    // 如果登录不成功，关闭应用程序
                    if (!_loginWindow.LoginSuccessful)
                    {
                        Shutdown();
                    }
                    else
                    {
                        // 登录成功后，将_loginWindow设为null，以便垃圾回收
                        _loginWindow = null;
                        
                        // 强制垃圾回收
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用程序启动时出错: {ex.Message}");
                LogHelper.LogError($"应用程序启动时出错: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 确保在退出时保存当前主题设置
                var themeService = Services.ThemeService.Instance;
                bool isDarkMode = themeService.IsDarkThemeActive();
                
                Console.WriteLine($"应用程序退出时检测到的主题: {(isDarkMode ? "深色" : "浅色")}");
                LogHelper.LogInfo($"应用程序退出时检测到的主题: {(isDarkMode ? "深色" : "浅色")}");
                
                // 保存主题设置
                themeService.ApplyTheme(isDarkMode); // 这会触发保存配置
                
                // 强制保存一次主题设置
                try
                {
                    // 获取可执行文件的配置文件路径
                    string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? 
                                    System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    
                    // 动态获取可能的配置文件名称
                    string[] configNames = Directory.GetFiles(baseDir, "*.config")
                        .Where(file => !file.EndsWith(".vshost.exe.config")) // 排除VS主机配置
                        .ToArray();
                    
                    Console.WriteLine($"应用程序退出时 - 找到的配置文件数量: {configNames.Length}");
                    LogHelper.LogInfo($"应用程序退出时 - 找到的配置文件数量: {configNames.Length}");
                    
                    foreach (var configFile in configNames)
                    {
                        Console.WriteLine($"应用程序退出时 - 找到配置文件: {configFile}");
                    }
                    
                    // 首先尝试dll.config和exe.config
                    string dllConfigPath = System.IO.Path.Combine(baseDir, exeName + ".dll.config");
                    string exeConfigPath = System.IO.Path.Combine(baseDir, exeName + ".exe.config");
                    
                    // 也尝试已知可能的应用程序名称
                    string knownDllConfig = System.IO.Path.Combine(baseDir, "以车票标记时光：旅程归档.dll.config");
                    string knownExeConfig = System.IO.Path.Combine(baseDir, "以车票标记时光：旅程归档.exe.config");
                    
                    // 检查文件是否存在
                    bool dllConfigExists = System.IO.File.Exists(dllConfigPath);
                    bool exeConfigExists = System.IO.File.Exists(exeConfigPath);
                    bool knownDllExists = System.IO.File.Exists(knownDllConfig);
                    bool knownExeExists = System.IO.File.Exists(knownExeConfig);
                    
                    // 选择要使用的配置文件
                    string configPath = null;
                    if (dllConfigExists)
                    {
                        configPath = dllConfigPath;
                    }
                    else if (exeConfigExists)
                    {
                        configPath = exeConfigPath;
                    }
                    else if (knownDllExists)
                    {
                        configPath = knownDllConfig;
                    }
                    else if (knownExeExists)
                    {
                        configPath = knownExeConfig;
                    }
                    else if (configNames.Length > 0)
                    {
                        // 如果没有找到预期的配置文件，但目录中存在其他配置文件，使用第一个
                        configPath = configNames[0];
                    }
                    
                    Console.WriteLine($"应用程序退出时使用的配置文件路径: {configPath}");
                    LogHelper.LogInfo($"应用程序退出时使用的配置文件路径: {configPath}");
                    
                    if (configPath != null && System.IO.File.Exists(configPath))
                    {
                        // 使用XmlDocument直接操作XML配置文件
                        var xmlDoc = new System.Xml.XmlDocument();
                        xmlDoc.Load(configPath);
                        
                        // 获取appSettings节点
                        var appSettingsNode = xmlDoc.SelectSingleNode("//appSettings");
                        if (appSettingsNode != null)
                        {
                            // 查找IsDarkMode设置
                            var isDarkModeNode = appSettingsNode.SelectSingleNode("//add[@key='IsDarkMode']");
                            
                            if (isDarkModeNode != null)
                            {
                                // 更新已存在的IsDarkMode设置
                                isDarkModeNode.Attributes["value"].Value = isDarkMode.ToString().ToLower();
                                Console.WriteLine($"应用程序退出时更新现有IsDarkMode设置为: {isDarkMode.ToString().ToLower()}");
                                LogHelper.LogInfo($"应用程序退出时更新现有IsDarkMode设置为: {isDarkMode.ToString().ToLower()}");
                            }
                            else
                            {
                                // 创建新的IsDarkMode设置
                                var newNode = xmlDoc.CreateElement("add");
                                var keyAttr = xmlDoc.CreateAttribute("key");
                                keyAttr.Value = "IsDarkMode";
                                var valueAttr = xmlDoc.CreateAttribute("value");
                                valueAttr.Value = isDarkMode.ToString().ToLower();
                                
                                newNode.Attributes.Append(keyAttr);
                                newNode.Attributes.Append(valueAttr);
                                appSettingsNode.AppendChild(newNode);
                                
                                Console.WriteLine($"应用程序退出时创建新的IsDarkMode设置: {isDarkMode.ToString().ToLower()}");
                                LogHelper.LogInfo($"应用程序退出时创建新的IsDarkMode设置: {isDarkMode.ToString().ToLower()}");
                            }
                            
                            // 保存XML文档
                            xmlDoc.Save(configPath);
                            Console.WriteLine($"应用程序退出时已保存配置文件: {configPath}");
                            LogHelper.LogInfo($"应用程序退出时已保存配置文件: {configPath}");
                            
                            // 刷新配置
                            ConfigurationManager.RefreshSection("appSettings");
                        }
                        else
                        {
                            Console.WriteLine("应用程序退出时未找到appSettings节点");
                            LogHelper.LogWarning("应用程序退出时未找到appSettings节点");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"应用程序退出时配置文件不存在或无法确定配置文件");
                        LogHelper.LogWarning($"应用程序退出时配置文件不存在或无法确定配置文件");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"应用程序退出时直接操作配置文件出错: {ex.Message}");
                    Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                    LogHelper.LogError($"应用程序退出时直接操作配置文件出错: {ex.Message}");
                    LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
                }
                
                // 验证配置是否已保存
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["IsDarkMode"] != null)
                {
                    string savedValue = config.AppSettings.Settings["IsDarkMode"].Value;
                    Console.WriteLine($"应用程序退出时验证配置文件中的IsDarkMode值: {savedValue}");
                    LogHelper.LogInfo($"应用程序退出时验证配置文件中的IsDarkMode值: {savedValue}");
                }
                else
                {
                    Console.WriteLine("应用程序退出时验证失败：配置文件中不存在IsDarkMode设置");
                    LogHelper.LogWarning("应用程序退出时验证失败：配置文件中不存在IsDarkMode设置");
                }
                
                Console.WriteLine($"应用程序退出时保存主题设置: {(isDarkMode ? "深色" : "浅色")}");
                LogHelper.LogInfo($"应用程序退出时保存主题设置: {(isDarkMode ? "深色" : "浅色")}");
                
                // 重置StationCheckService的状态
                Services.StationCheckService.Instance.ResetIgnoreStationCheck();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"退出时保存主题设置出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                LogHelper.LogError($"退出时保存主题设置出错: {ex.Message}");
                LogHelper.LogError($"退出时保存主题设置出错: {ex.StackTrace}");
            }
            
            // 释放COM
            CoUninitialize();
            
            base.OnExit(e);
        }

        // 处理AppDomain未处理的异常
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LogHelper.LogError($"未处理的AppDomain异常: {exception?.Message}", exception);
            MessageBox.Show($"程序发生严重错误: {exception?.Message}\n\n请联系开发人员并提供日志文件。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 处理UI线程未处理的异常
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 特殊处理窗口Owner属性相关的异常
            if (e.Exception.Message.Contains("Owner") || e.Exception.Message.Contains("Window"))
            {
                LogHelper.LogError($"窗口操作异常: {e.Exception.Message}", e.Exception);
                e.Handled = true; // 标记为已处理，防止应用程序崩溃
                return;
            }
            
            LogHelper.LogError($"未处理的UI线程异常: {e.Exception.Message}", e.Exception);
            MessageBox.Show($"程序发生错误: {e.Exception.Message}\n\n请联系开发人员并提供日志文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // 标记为已处理，防止应用程序崩溃
        }

        // 处理Task未观察到的异常
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogHelper.LogError($"未观察到的Task异常: {e.Exception.Message}", e.Exception);
            e.SetObserved(); // 标记为已观察，防止应用程序崩溃
        }

        /// <summary>
        /// 同步字体大小设置
        /// </summary>
        /// <param name="configFontSize"></param>
        public void SyncFontSizeSettings(double configFontSize)
        {
            Resources["MaterialDesignFontSize"] = configFontSize;
            Resources["MaterialDesignSubtitle1FontSize"] = configFontSize + 2;
            Resources["MaterialDesignSubtitle2FontSize"] = configFontSize + 1;
            Resources["MaterialDesignHeadline6FontSize"] = configFontSize + 4;
            Resources["MaterialDesignHeadline5FontSize"] = configFontSize + 6;

            // 设置自定义字体大小资源 - 使用动态缩放因子
            double scaleFactor = Math.Min(3.0, Math.Max(1.5, 4.0 - configFontSize / 10.0)); // 字体越大，缩放因子越小
            Resources["LargeFontSize"] = configFontSize * scaleFactor;
            Resources["MediumLargeFontSize"] = configFontSize * (scaleFactor * 0.7);
            Resources["MediumFontSize"] = configFontSize * (scaleFactor * 0.5);

            LogHelper.LogInfo($"同步字体大小设置: {configFontSize}pt，缩放因子: {scaleFactor}");
        }
    }
}
