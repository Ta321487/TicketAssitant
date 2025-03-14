using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Utils;
using System.ComponentModel;

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
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["IsDarkMode"] == null)
                {
                    config.AppSettings.Settings.Add("IsDarkMode", isDarkMode.ToString());
                }
                else
                {
                    config.AppSettings.Settings["IsDarkMode"].Value = isDarkMode.ToString();
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                
                // 同时更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                }
                
                Console.WriteLine($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存主题设置时出错: {ex.Message}");
                LogHelper.LogError($"保存主题设置时出错: {ex.Message}");
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
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["IsDarkMode"] != null)
                {
                    if (bool.TryParse(config.AppSettings.Settings["IsDarkMode"].Value, out bool isDarkMode))
                    {
                        // 同时更新资源字典中的主题标志
                        if (Application.Current?.Resources != null)
                        {
                            Application.Current.Resources["Theme.Dark"] = isDarkMode;
                            Application.Current.Resources["Theme.Light"] = !isDarkMode;
                        }
                        
                        Console.WriteLine($"已从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                        return isDarkMode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载主题设置时出错: {ex.Message}");
                LogHelper.LogError($"加载主题设置时出错: {ex.Message}");
            }
            
            // 如果配置文件中没有主题设置或加载失败，检查当前主题
            bool currentTheme = IsDarkThemeActive();
            
            // 同时更新资源字典中的主题标志
            if (Application.Current?.Resources != null)
            {
                Application.Current.Resources["Theme.Dark"] = currentTheme;
                Application.Current.Resources["Theme.Light"] = !currentTheme;
            }
            
            return currentTheme;
        }

        /// <summary>
        /// 检查当前是否为深色主题
        /// </summary>
        /// <returns>是否为深色主题</returns>
        public bool IsDarkThemeActive()
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark;
        }
    }
} 