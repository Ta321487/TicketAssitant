using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Utils;

namespace TA_WPF.Services
{
    /// <summary>
    /// 主题服务，负责管理应用程序的主题设置
    /// </summary>
    public class ThemeService
    {
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
                    }
                    
                    // 更新Theme.Dark和Theme.Light资源
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
                            
                            // 刷新窗口
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
                        Console.WriteLine($"已从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                        return isDarkMode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载主题设置时出错: {ex.Message}");
            }
            
            // 如果配置文件中没有主题设置或加载失败，检查当前主题
            return IsDarkThemeActive();
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