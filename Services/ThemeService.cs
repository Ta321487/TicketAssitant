using System.Configuration;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Utils;
using System.ComponentModel;
using Theme = MaterialDesignThemes.Wpf.Theme;

namespace TA_WPF.Services
{
    /// <summary>
    /// 主题服务，负责管理应用程序的主题设置
    /// </summary>
    public class ThemeService
    {
        private static ThemeService _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取 ThemeService 的单例实例
        /// </summary>
        public static ThemeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ThemeService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，防止外部创建实例
        /// </summary>
        private ThemeService()
        {
        }

        /// <summary>
        /// 主题变更事件
        /// </summary>
        public event EventHandler<bool> ThemeChanged;

        /// <summary>
        /// 应用主题设置
        /// </summary>
        /// <param name="isDarkMode">是否为深色模式</param>
        public void ApplyTheme(bool isDarkMode)
        {
            try
            {
                // 获取当前资源字典
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                
                // 如果当前主题状态与要设置的状态相同，则不做任何改变
                if (theme.GetBaseTheme() == (isDarkMode ? BaseTheme.Dark : BaseTheme.Light))
                {
                    return;
                }
                
                // 设置深色/浅色模式
                theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);
                
                // 应用主题
                paletteHelper.SetTheme(theme);
                
                // 更新资源字典中的BaseTheme
                if (Application.Current?.Resources != null)
                {
                    // 更新MaterialDesignTheme的BaseTheme
                    var bundledTheme = Application.Current.Resources.MergedDictionaries
                        .OfType<MaterialDesignThemes.Wpf.BundledTheme>()
                        .FirstOrDefault();
                    
                    if (bundledTheme != null)
                    {
                        bundledTheme.BaseTheme = isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light;
                        Console.WriteLine($"已更新BundledTheme的BaseTheme为: {bundledTheme.BaseTheme}");
                    }
                    else
                    {
                        Console.WriteLine("警告: 未找到BundledTheme");
                    }
                    
                    // 更新Theme.Dark和Theme.Light资源 - 确保这些值立即更新
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                    
                    // 更新选择相关资源
                    if (isDarkMode)
                    {
                        // 深色模式下调整选择资源
                        if (Application.Current.Resources.Contains("MaterialDesignSelection") && 
                            Application.Current.Resources.Contains("MaterialDesignSelectionForeground"))
                        {
                            // 深色模式下使用更明显的选择背景和前景色
                            Application.Current.Resources["MaterialDesignSelection"] = new SolidColorBrush(Color.FromArgb(100, 124, 77, 255)); // #7C4DFF with 0.4 opacity
                            Application.Current.Resources["MaterialDesignSelectionForeground"] = new SolidColorBrush(Colors.White);
                        }
                    }
                    else
                    {
                        // 浅色模式下恢复默认选择资源
                        if (Application.Current.Resources.Contains("MaterialDesignSelection") && 
                            Application.Current.Resources.Contains("MaterialDesignSelectionForeground"))
                        {
                            // 重新创建默认的选择资源
                            Application.Current.Resources["MaterialDesignSelection"] = new SolidColorBrush(Color.FromArgb(77, 124, 77, 255)); // #7C4DFF with 0.3 opacity
                            Application.Current.Resources["MaterialDesignSelectionForeground"] = new SolidColorBrush(Colors.Black);
                        }
                    }
                    
                    // 更新全局颜色资源
                    if (isDarkMode)
                    {
                        // 深色模式下稍微调亮主色调
                        Application.Current.Resources["PrimaryHueLightBrush"] = new SolidColorBrush(Color.FromRgb(156, 100, 255)); // #9C64FF
                        Application.Current.Resources["PrimaryHueMidBrush"] = new SolidColorBrush(Color.FromRgb(124, 77, 255));   // #7C4DFF
                        Application.Current.Resources["PrimaryHueDarkBrush"] = new SolidColorBrush(Color.FromRgb(94, 53, 177));   // #5E35B1
                        
                        Application.Current.Resources["GlobalAccentBrush"] = new SolidColorBrush(Color.FromRgb(124, 77, 255));    // #7C4DFF
                        Application.Current.Resources["GlobalAccentLightBrush"] = new SolidColorBrush(Color.FromRgb(156, 100, 255)); // #9C64FF
                        Application.Current.Resources["GlobalAccentDarkBrush"] = new SolidColorBrush(Color.FromRgb(94, 53, 177));  // #5E35B1
                        
                        // 确保深色模式下文本颜色为白色
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignBodyLight"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                        
                        // 设置DataGrid背景色为黑色
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // #212121
                        Application.Current.Resources["MaterialDesignAlternatingRowBackground"] = new SolidColorBrush(Color.FromRgb(55, 55, 55)); // 调整为更明显的深灰色
                    }
                    else
                    {
                        // 浅色模式下恢复默认文本颜色
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // #212121
                        Application.Current.Resources["MaterialDesignBodyLight"] = new SolidColorBrush(Color.FromRgb(117, 117, 117)); // #757575
                        
                        // 恢复默认背景色
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignAlternatingRowBackground"] = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // #F5F5F5
                    }
                    
                    // 应用主题到所有打开的窗口
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != null && window.IsLoaded)
                        {
                            // 更新窗口的ThemeAssist.Theme属性
                            MaterialDesignThemes.Wpf.ThemeAssist.SetTheme(window, isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light);
                            
                            // 如果窗口的DataContext实现了INotifyPropertyChanged接口，尝试更新其IsDarkMode属性
                            if (window.DataContext is INotifyPropertyChanged viewModel)
                            {
                                // 使用反射查找并设置IsDarkMode属性
                                var property = viewModel.GetType().GetProperty("IsDarkMode");
                                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                                {
                                    property.SetValue(viewModel, isDarkMode);
                                }
                            }
                            
                            // 强制刷新窗口
                            window.UpdateLayout();
                        }
                    }
                    
                    // 触发全局主题更新
                    FrameworkElement.StyleProperty.OverrideMetadata(
                        typeof(Window),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
                }
                
                Console.WriteLine($"已应用{(isDarkMode ? "深色" : "浅色")}主题");
                
                // 保存主题设置到配置文件
                SaveThemeToConfig(isDarkMode);
                
                // 触发主题变更事件
                ThemeChanged?.Invoke(this, isDarkMode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用主题时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 保存主题设置到配置文件
        /// </summary>
        /// <param name="isDarkMode">是否为深色模式</param>
        private void SaveThemeToConfig(bool isDarkMode)
        {
            try
            {
                // 获取可执行文件的配置文件路径 - 使用多种方式尝试
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string configPath1 = exePath + ".config";
                string configPath2 = System.IO.Path.ChangeExtension(exePath, ".config");
                string configPath3 = AppDomain.CurrentDomain.BaseDirectory + "TA_WPF.dll.config";
                
                Console.WriteLine($"可执行文件路径: {exePath}");
                Console.WriteLine($"配置文件路径1: {configPath1}");
                Console.WriteLine($"配置文件路径2: {configPath2}");
                Console.WriteLine($"配置文件路径3: {configPath3}");
                
                LogHelper.LogInfo($"可执行文件路径: {exePath}");
                LogHelper.LogInfo($"配置文件路径1: {configPath1}");
                LogHelper.LogInfo($"配置文件路径2: {configPath2}");
                LogHelper.LogInfo($"配置文件路径3: {configPath3}");
                
                // 检查文件是否存在
                Console.WriteLine($"配置文件1存在: {System.IO.File.Exists(configPath1)}");
                Console.WriteLine($"配置文件2存在: {System.IO.File.Exists(configPath2)}");
                Console.WriteLine($"配置文件3存在: {System.IO.File.Exists(configPath3)}");
                
                LogHelper.LogInfo($"配置文件1存在: {System.IO.File.Exists(configPath1)}");
                LogHelper.LogInfo($"配置文件2存在: {System.IO.File.Exists(configPath2)}");
                LogHelper.LogInfo($"配置文件3存在: {System.IO.File.Exists(configPath3)}");
                
                // 尝试直接修改配置文件
                string configToUse = null;
                if (System.IO.File.Exists(configPath3))
                {
                    configToUse = configPath3;
                }
                else if (System.IO.File.Exists(configPath2))
                {
                    configToUse = configPath2;
                }
                else if (System.IO.File.Exists(configPath1))
                {
                    configToUse = configPath1;
                }
                
                if (configToUse != null)
                {
                    Console.WriteLine($"将使用配置文件: {configToUse}");
                    LogHelper.LogInfo($"将使用配置文件: {configToUse}");
                    
                    try
                    {
                        // 读取配置文件内容
                        string configContent = System.IO.File.ReadAllText(configToUse);
                        Console.WriteLine($"配置文件内容: {configContent}");
                        LogHelper.LogInfo($"配置文件内容长度: {configContent.Length}字节");
                        
                        // 检查是否包含IsDarkMode设置
                        bool containsIsDarkMode = configContent.Contains("IsDarkMode");
                        Console.WriteLine($"配置文件包含IsDarkMode: {containsIsDarkMode}");
                        LogHelper.LogInfo($"配置文件包含IsDarkMode: {containsIsDarkMode}");
                        
                        // 使用ConfigurationManager修改配置
                        var config = ConfigurationManager.OpenExeConfiguration(configToUse);
                        Console.WriteLine($"打开的配置文件: {config.FilePath}");
                        LogHelper.LogInfo($"打开的配置文件: {config.FilePath}");
                        
                        if (config.AppSettings.Settings["IsDarkMode"] == null)
                        {
                            Console.WriteLine("配置文件中不存在IsDarkMode设置，正在添加...");
                            LogHelper.LogInfo("配置文件中不存在IsDarkMode设置，正在添加...");
                            config.AppSettings.Settings.Add("IsDarkMode", isDarkMode.ToString().ToLower());
                        }
                        else
                        {
                            Console.WriteLine($"更新配置文件中的IsDarkMode设置，从 {config.AppSettings.Settings["IsDarkMode"].Value} 到 {isDarkMode.ToString().ToLower()}");
                            LogHelper.LogInfo($"更新配置文件中的IsDarkMode设置，从 {config.AppSettings.Settings["IsDarkMode"].Value} 到 {isDarkMode.ToString().ToLower()}");
                            config.AppSettings.Settings["IsDarkMode"].Value = isDarkMode.ToString().ToLower();
                        }
                        
                        // 保存配置文件
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                        
                        // 验证配置是否已保存
                        string newContent = System.IO.File.ReadAllText(configToUse);
                        Console.WriteLine($"保存后的配置文件内容长度: {newContent.Length}字节");
                        LogHelper.LogInfo($"保存后的配置文件内容长度: {newContent.Length}字节");
                        
                        // 检查是否包含IsDarkMode设置
                        bool newContainsIsDarkMode = newContent.Contains("IsDarkMode");
                        Console.WriteLine($"保存后的配置文件包含IsDarkMode: {newContainsIsDarkMode}");
                        LogHelper.LogInfo($"保存后的配置文件包含IsDarkMode: {newContainsIsDarkMode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"直接操作配置文件时出错: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                        LogHelper.LogError($"直接操作配置文件时出错: {ex.Message}");
                        LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
                    }
                }
                
                // 使用标准方式修改配置
                var standardConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Console.WriteLine($"标准方式打开的配置文件: {standardConfig.FilePath}");
                LogHelper.LogInfo($"标准方式打开的配置文件: {standardConfig.FilePath}");
                
                if (standardConfig.AppSettings.Settings["IsDarkMode"] == null)
                {
                    Console.WriteLine("标准配置文件中不存在IsDarkMode设置，正在添加...");
                    LogHelper.LogInfo("标准配置文件中不存在IsDarkMode设置，正在添加...");
                    standardConfig.AppSettings.Settings.Add("IsDarkMode", isDarkMode.ToString().ToLower());
                }
                else
                {
                    Console.WriteLine($"更新标准配置文件中的IsDarkMode设置，从 {standardConfig.AppSettings.Settings["IsDarkMode"].Value} 到 {isDarkMode.ToString().ToLower()}");
                    LogHelper.LogInfo($"更新标准配置文件中的IsDarkMode设置，从 {standardConfig.AppSettings.Settings["IsDarkMode"].Value} 到 {isDarkMode.ToString().ToLower()}");
                    standardConfig.AppSettings.Settings["IsDarkMode"].Value = isDarkMode.ToString().ToLower();
                }
                
                // 保存配置文件
                standardConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // 同时更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                }
                
                Console.WriteLine($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
                LogHelper.LogInfo($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
                
                // 验证配置是否已保存
                var verifyConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (verifyConfig.AppSettings.Settings["IsDarkMode"] != null)
                {
                    string savedValue = verifyConfig.AppSettings.Settings["IsDarkMode"].Value;
                    Console.WriteLine($"验证配置文件中的IsDarkMode值: {savedValue}");
                    LogHelper.LogInfo($"验证配置文件中的IsDarkMode值: {savedValue}");
                }
                else
                {
                    Console.WriteLine("验证失败：配置文件中不存在IsDarkMode设置");
                    LogHelper.LogWarning("验证失败：配置文件中不存在IsDarkMode设置");
                }
                
                // 尝试直接写入配置文件
                try
                {
                    if (configToUse != null)
                    {
                        string content = System.IO.File.ReadAllText(configToUse);
                        if (!content.Contains("IsDarkMode"))
                        {
                            // 如果配置文件中不存在IsDarkMode设置，添加它
                            int appSettingsEndIndex = content.IndexOf("</appSettings>");
                            if (appSettingsEndIndex > 0)
                            {
                                string newContent = content.Substring(0, appSettingsEndIndex) +
                                                   $"    <add key=\"IsDarkMode\" value=\"{isDarkMode.ToString().ToLower()}\" />\r\n" +
                                                   content.Substring(appSettingsEndIndex);
                                System.IO.File.WriteAllText(configToUse, newContent);
                                Console.WriteLine($"已直接写入IsDarkMode设置到配置文件: {configToUse}");
                                LogHelper.LogInfo($"已直接写入IsDarkMode设置到配置文件: {configToUse}");
                            }
                        }
                        else
                        {
                            // 如果配置文件中已存在IsDarkMode设置，更新它
                            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("key=\"IsDarkMode\"\\s+value=\"(true|false|True|False)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            string newContent = regex.Replace(content, $"key=\"IsDarkMode\" value=\"{isDarkMode.ToString().ToLower()}\"");
                            System.IO.File.WriteAllText(configToUse, newContent);
                            Console.WriteLine($"已直接更新IsDarkMode设置在配置文件: {configToUse}");
                            LogHelper.LogInfo($"已直接更新IsDarkMode设置在配置文件: {configToUse}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"直接写入配置文件时出错: {ex.Message}");
                    Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                    LogHelper.LogError($"直接写入配置文件时出错: {ex.Message}");
                    LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存主题设置时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                LogHelper.LogError($"保存主题设置时出错: {ex.Message}");
                LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 从配置文件加载主题设置
        /// </summary>
        /// <returns>是否为深色模式</returns>
        public bool LoadThemeFromConfig()
        {
            try
            {
                // 获取可执行文件的配置文件路径 - 使用多种方式尝试
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string configPath1 = exePath + ".config";
                string configPath2 = System.IO.Path.ChangeExtension(exePath, ".config");
                string configPath3 = AppDomain.CurrentDomain.BaseDirectory + "TA_WPF.dll.config";
                
                Console.WriteLine($"加载配置 - 可执行文件路径: {exePath}");
                Console.WriteLine($"加载配置 - 配置文件路径1: {configPath1}");
                Console.WriteLine($"加载配置 - 配置文件路径2: {configPath2}");
                Console.WriteLine($"加载配置 - 配置文件路径3: {configPath3}");
                
                LogHelper.LogInfo($"加载配置 - 可执行文件路径: {exePath}");
                LogHelper.LogInfo($"加载配置 - 配置文件路径1: {configPath1}");
                LogHelper.LogInfo($"加载配置 - 配置文件路径2: {configPath2}");
                LogHelper.LogInfo($"加载配置 - 配置文件路径3: {configPath3}");
                
                // 检查文件是否存在
                Console.WriteLine($"加载配置 - 配置文件1存在: {System.IO.File.Exists(configPath1)}");
                Console.WriteLine($"加载配置 - 配置文件2存在: {System.IO.File.Exists(configPath2)}");
                Console.WriteLine($"加载配置 - 配置文件3存在: {System.IO.File.Exists(configPath3)}");
                
                LogHelper.LogInfo($"加载配置 - 配置文件1存在: {System.IO.File.Exists(configPath1)}");
                LogHelper.LogInfo($"加载配置 - 配置文件2存在: {System.IO.File.Exists(configPath2)}");
                LogHelper.LogInfo($"加载配置 - 配置文件3存在: {System.IO.File.Exists(configPath3)}");
                
                // 尝试直接读取配置文件
                string configToUse = null;
                if (System.IO.File.Exists(configPath3))
                {
                    configToUse = configPath3;
                }
                else if (System.IO.File.Exists(configPath2))
                {
                    configToUse = configPath2;
                }
                else if (System.IO.File.Exists(configPath1))
                {
                    configToUse = configPath1;
                }
                
                if (configToUse != null)
                {
                    Console.WriteLine($"加载配置 - 将使用配置文件: {configToUse}");
                    LogHelper.LogInfo($"加载配置 - 将使用配置文件: {configToUse}");
                    
                    try
                    {
                        // 读取配置文件内容
                        string configContent = System.IO.File.ReadAllText(configToUse);
                        Console.WriteLine($"加载配置 - 配置文件内容长度: {configContent.Length}字节");
                        LogHelper.LogInfo($"加载配置 - 配置文件内容长度: {configContent.Length}字节");
                        
                        // 检查是否包含IsDarkMode设置
                        bool containsIsDarkMode = configContent.Contains("IsDarkMode");
                        Console.WriteLine($"加载配置 - 配置文件包含IsDarkMode: {containsIsDarkMode}");
                        LogHelper.LogInfo($"加载配置 - 配置文件包含IsDarkMode: {containsIsDarkMode}");
                        
                        if (containsIsDarkMode)
                        {
                            // 尝试使用正则表达式提取值
                            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("key=\"IsDarkMode\"\\s+value=\"(true|false|True|False)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            var match = regex.Match(configContent);
                            if (match.Success && match.Groups.Count > 1)
                            {
                                string value = match.Groups[1].Value.ToLower(); // 转换为小写
                                Console.WriteLine($"加载配置 - 从配置文件直接提取的IsDarkMode值: {value}");
                                LogHelper.LogInfo($"加载配置 - 从配置文件直接提取的IsDarkMode值: {value}");
                                
                                if (bool.TryParse(value, out bool isDarkMode))
                                {
                                    // 同时更新资源字典中的主题标志
                                    if (Application.Current?.Resources != null)
                                    {
                                        Application.Current.Resources["Theme.Dark"] = isDarkMode;
                                        Application.Current.Resources["Theme.Light"] = !isDarkMode;
                                    }
                                    
                                    Console.WriteLine($"加载配置 - 已从配置文件直接提取主题设置: {(isDarkMode ? "深色" : "浅色")}");
                                    LogHelper.LogInfo($"加载配置 - 已从配置文件直接提取主题设置: {(isDarkMode ? "深色" : "浅色")}");
                                    return isDarkMode;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"加载配置 - 直接读取配置文件时出错: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                        LogHelper.LogError($"加载配置 - 直接读取配置文件时出错: {ex.Message}");
                        LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
                    }
                }
                
                // 使用标准方式读取配置
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Console.WriteLine($"加载配置 - 标准方式打开的配置文件: {config.FilePath}");
                LogHelper.LogInfo($"加载配置 - 标准方式打开的配置文件: {config.FilePath}");
                
                if (config.AppSettings.Settings["IsDarkMode"] != null)
                {
                    string configValue = config.AppSettings.Settings["IsDarkMode"].Value;
                    Console.WriteLine($"加载配置 - 从配置文件读取到的IsDarkMode值: {configValue}");
                    LogHelper.LogInfo($"加载配置 - 从配置文件读取到的IsDarkMode值: {configValue}");
                    
                    // 转换为小写，确保能够正确解析
                    configValue = configValue.ToLower();
                    
                    if (bool.TryParse(configValue, out bool isDarkMode))
                    {
                        // 同时更新资源字典中的主题标志
                        if (Application.Current?.Resources != null)
                        {
                            Application.Current.Resources["Theme.Dark"] = isDarkMode;
                            Application.Current.Resources["Theme.Light"] = !isDarkMode;
                        }
                        
                        Console.WriteLine($"加载配置 - 已从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                        LogHelper.LogInfo($"加载配置 - 已从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                        return isDarkMode;
                    }
                    else
                    {
                        Console.WriteLine($"加载配置 - 无法解析配置文件中的IsDarkMode值: {configValue}");
                        LogHelper.LogWarning($"加载配置 - 无法解析配置文件中的IsDarkMode值: {configValue}");
                    }
                }
                else
                {
                    Console.WriteLine("加载配置 - 配置文件中不存在IsDarkMode设置，将创建默认设置");
                    LogHelper.LogWarning("加载配置 - 配置文件中不存在IsDarkMode设置，将创建默认设置");
                    
                    // 如果配置文件中没有IsDarkMode设置，创建一个默认设置（浅色模式）
                    SaveThemeToConfig(false);
                    
                    // 再次尝试读取
                    var retryConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (retryConfig.AppSettings.Settings["IsDarkMode"] != null)
                    {
                        string retryValue = retryConfig.AppSettings.Settings["IsDarkMode"].Value;
                        Console.WriteLine($"加载配置 - 重试读取到的IsDarkMode值: {retryValue}");
                        LogHelper.LogInfo($"加载配置 - 重试读取到的IsDarkMode值: {retryValue}");
                        
                        if (bool.TryParse(retryValue, out bool isDarkMode))
                        {
                            return isDarkMode;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载主题设置时出错: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                LogHelper.LogError($"加载主题设置时出错: {ex.Message}");
                LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
            }
            
            // 如果配置文件中没有主题设置或加载失败，检查当前主题
            bool currentTheme = IsDarkThemeActive();
            
            // 同时更新资源字典中的主题标志
            if (Application.Current?.Resources != null)
            {
                Application.Current.Resources["Theme.Dark"] = currentTheme;
                Application.Current.Resources["Theme.Light"] = !currentTheme;
            }
            
            Console.WriteLine($"加载配置 - 使用当前活动主题: {(currentTheme ? "深色" : "浅色")}");
            LogHelper.LogInfo($"加载配置 - 使用当前活动主题: {(currentTheme ? "深色" : "浅色")}");
            
            // 保存当前主题设置到配置文件
            SaveThemeToConfig(currentTheme);
            
            return currentTheme;
        }

        /// <summary>
        /// 检查当前是否为深色主题
        /// </summary>
        /// <returns>是否为深色主题</returns>
        public bool IsDarkThemeActive()
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                bool isDark = theme.GetBaseTheme() == BaseTheme.Dark;
                
                Console.WriteLine($"当前活动主题检测: {(isDark ? "深色" : "浅色")}");
                LogHelper.LogInfo($"当前活动主题检测: {(isDark ? "深色" : "浅色")}");
                
                return isDark;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测当前主题时出错: {ex.Message}");
                LogHelper.LogError($"检测当前主题时出错: {ex.Message}");
                
                // 默认返回false（浅色主题）
                return false;
            }
        }
    }
} 