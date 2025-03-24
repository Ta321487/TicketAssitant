using System.Configuration;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Utils;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
                string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 动态获取可能的配置文件名称
                string[] configNames = Directory.GetFiles(baseDir, "*.config")
                    .Where(file => !file.EndsWith(".vshost.exe.config")) // 排除VS主机配置
                    .ToArray();

                Console.WriteLine($"可执行文件路径: {exePath}");
                Console.WriteLine($"应用程序名称: {exeName}");
                Console.WriteLine($"应用程序基础目录: {baseDir}");
                Console.WriteLine($"找到的配置文件数量: {configNames.Length}");

                foreach (var config in configNames)
                {
                    Console.WriteLine($"找到配置文件: {config}");
                }

                LogHelper.LogInfo($"可执行文件路径: {exePath}");
                LogHelper.LogInfo($"应用程序名称: {exeName}");
                LogHelper.LogInfo($"应用程序基础目录: {baseDir}");

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

                Console.WriteLine($"程序dll配置文件存在: {dllConfigExists} - {dllConfigPath}");
                Console.WriteLine($"程序exe配置文件存在: {exeConfigExists} - {exeConfigPath}");
                Console.WriteLine($"已知dll配置文件存在: {knownDllExists} - {knownDllConfig}");
                Console.WriteLine($"已知exe配置文件存在: {knownExeExists} - {knownExeConfig}");

                LogHelper.LogInfo($"程序dll配置文件存在: {dllConfigExists} - {dllConfigPath}");
                LogHelper.LogInfo($"程序exe配置文件存在: {exeConfigExists} - {exeConfigPath}");
                LogHelper.LogInfo($"已知dll配置文件存在: {knownDllExists} - {knownDllConfig}");
                LogHelper.LogInfo($"已知exe配置文件存在: {knownExeExists} - {knownExeConfig}");

                // 尝试直接修改配置文件
                string configToUse = null;

                // 按优先级选择配置文件
                if (dllConfigExists)
                {
                    configToUse = dllConfigPath;
                }
                else if (exeConfigExists)
                {
                    configToUse = exeConfigPath;
                }
                else if (knownDllExists)
                {
                    configToUse = knownDllConfig;
                }
                else if (knownExeExists)
                {
                    configToUse = knownExeConfig;
                }
                else if (configNames.Length > 0)
                {
                    // 如果没有找到预期的配置文件，但目录中存在其他配置文件，使用第一个
                    configToUse = configNames[0];
                }

                if (configToUse != null)
                {
                    Console.WriteLine($"将使用配置文件: {configToUse}");
                    LogHelper.LogInfo($"将使用配置文件: {configToUse}");

                    try
                    {
                        // 使用XmlDocument直接操作XML配置文件
                        var xmlDoc = new System.Xml.XmlDocument();
                        xmlDoc.Load(configToUse);

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
                                Console.WriteLine($"更新现有IsDarkMode设置为: {isDarkMode.ToString().ToLower()}");
                                LogHelper.LogInfo($"更新现有IsDarkMode设置为: {isDarkMode.ToString().ToLower()}");
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

                                Console.WriteLine($"创建新的IsDarkMode设置: {isDarkMode.ToString().ToLower()}");
                                LogHelper.LogInfo($"创建新的IsDarkMode设置: {isDarkMode.ToString().ToLower()}");
                            }

                            // 保存XML文档
                            xmlDoc.Save(configToUse);
                            Console.WriteLine($"已保存配置文件: {configToUse}");
                            LogHelper.LogInfo($"已保存配置文件: {configToUse}");
                        }
                        else
                        {
                            Console.WriteLine("未找到appSettings节点");
                            LogHelper.LogWarning("未找到appSettings节点");
                        }

                        // 刷新配置
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"直接操作配置文件时出错: {ex.Message}");
                        Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                        LogHelper.LogError($"直接操作配置文件时出错: {ex.Message}");
                        LogHelper.LogError($"异常堆栈: {ex.StackTrace}");
                    }
                }

                // 同时更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                }

                Console.WriteLine($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
                LogHelper.LogInfo($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
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
                string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 动态获取可能的配置文件名称
                string[] configNames = Directory.GetFiles(baseDir, "*.config")
                    .Where(file => !file.EndsWith(".vshost.exe.config")) // 排除VS主机配置
                    .ToArray();

                Console.WriteLine($"加载配置 - 可执行文件路径: {exePath}");
                Console.WriteLine($"加载配置 - 应用程序名称: {exeName}");
                Console.WriteLine($"加载配置 - 应用程序基础目录: {baseDir}");
                Console.WriteLine($"加载配置 - 找到的配置文件数量: {configNames.Length}");

                foreach (var config in configNames)
                {
                    Console.WriteLine($"加载配置 - 找到配置文件: {config}");
                }

                LogHelper.LogInfo($"加载配置 - 可执行文件路径: {exePath}");
                LogHelper.LogInfo($"加载配置 - 应用程序名称: {exeName}");
                LogHelper.LogInfo($"加载配置 - 应用程序基础目录: {baseDir}");

                // 首先尝试dll.config和exe.config
                string dllConfigPath = System.IO.Path.Combine(baseDir, exeName + ".dll.config");
                string exeConfigPath = System.IO.Path.Combine(baseDir, exeName + ".exe.config");

                // 也尝试已知可能的应用程序名称
                string knownDllConfig = System.IO.Path.Combine(baseDir, "车票标记时光：旅程归档.dll.config");
                string knownExeConfig = System.IO.Path.Combine(baseDir, "车票标记时光：旅程归档.exe.config");

                // 检查文件是否存在
                bool dllConfigExists = System.IO.File.Exists(dllConfigPath);
                bool exeConfigExists = System.IO.File.Exists(exeConfigPath);
                bool knownDllExists = System.IO.File.Exists(knownDllConfig);
                bool knownExeExists = System.IO.File.Exists(knownExeConfig);

                Console.WriteLine($"加载配置 - 程序dll配置文件存在: {dllConfigExists} - {dllConfigPath}");
                Console.WriteLine($"加载配置 - 程序exe配置文件存在: {exeConfigExists} - {exeConfigPath}");
                Console.WriteLine($"加载配置 - 已知dll配置文件存在: {knownDllExists} - {knownDllConfig}");
                Console.WriteLine($"加载配置 - 已知exe配置文件存在: {knownExeExists} - {knownExeConfig}");

                LogHelper.LogInfo($"加载配置 - 程序dll配置文件存在: {dllConfigExists} - {dllConfigPath}");
                LogHelper.LogInfo($"加载配置 - 程序exe配置文件存在: {exeConfigExists} - {exeConfigPath}");
                LogHelper.LogInfo($"加载配置 - 已知dll配置文件存在: {knownDllExists} - {knownDllConfig}");
                LogHelper.LogInfo($"加载配置 - 已知exe配置文件存在: {knownExeExists} - {knownExeConfig}");

                // 尝试直接读取配置文件
                string configToUse = null;

                // 按优先级选择配置文件
                if (dllConfigExists)
                {
                    configToUse = dllConfigPath;
                }
                else if (exeConfigExists)
                {
                    configToUse = exeConfigPath;
                }
                else if (knownDllExists)
                {
                    configToUse = knownDllConfig;
                }
                else if (knownExeExists)
                {
                    configToUse = knownExeConfig;
                }
                else if (configNames.Length > 0)
                {
                    // 如果没有找到预期的配置文件，但目录中存在其他配置文件，使用第一个
                    configToUse = configNames[0];
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
                            // 使用XmlDocument直接读取
                            var xmlDoc = new System.Xml.XmlDocument();
                            xmlDoc.Load(configToUse);

                            // 查找IsDarkMode设置
                            var isDarkModeNode = xmlDoc.SelectSingleNode("//add[@key='IsDarkMode']");
                            if (isDarkModeNode != null && isDarkModeNode.Attributes["value"] != null)
                            {
                                string value = isDarkModeNode.Attributes["value"].Value.ToLower();
                                Console.WriteLine($"加载配置 - 从配置文件读取到IsDarkMode值: {value}");
                                LogHelper.LogInfo($"加载配置 - 从配置文件读取到IsDarkMode值: {value}");

                                if (bool.TryParse(value, out bool isDarkMode))
                                {
                                    // 同时更新资源字典中的主题标志
                                    if (Application.Current?.Resources != null)
                                    {
                                        Application.Current.Resources["Theme.Dark"] = isDarkMode;
                                        Application.Current.Resources["Theme.Light"] = !isDarkMode;
                                    }

                                    Console.WriteLine($"加载配置 - 已从配置文件读取主题设置: {(isDarkMode ? "深色" : "浅色")}");
                                    LogHelper.LogInfo($"加载配置 - 已从配置文件读取主题设置: {(isDarkMode ? "深色" : "浅色")}");
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