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
        /// 应用主题设置到指定窗口
        /// </summary>
        /// <param name="window">要应用主题的窗口</param>
        /// <param name="isDarkMode">是否为深色模式</param>
        /// <param name="themeIcon">主题图标(如果有)</param>
        /// <param name="mainCard">主卡片(如果有)</param>
        public void ApplyThemeToWindow(Window window, bool isDarkMode, PackIcon themeIcon = null, Card mainCard = null)
        {
            try
            {
                if (window == null)
                    return;
            
                // 显式设置窗口的ThemeAssist.Theme属性
                MaterialDesignThemes.Wpf.ThemeAssist.SetTheme(window,
                    isDarkMode ? MaterialDesignThemes.Wpf.BaseTheme.Dark : MaterialDesignThemes.Wpf.BaseTheme.Light);

                // 获取当前主题
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                // 强制设置主题基础类型
                theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);
                paletteHelper.SetTheme(theme);

                // 更新主题图标 - 深色模式显示太阳图标，浅色模式显示月亮图标
                if (themeIcon != null)
                {
                    themeIcon.Kind = isDarkMode ? PackIconKind.WeatherSunny : PackIconKind.WeatherNight;
                }

                // 强制刷新窗口和所有控件
                window.UpdateLayout();

                // 强制刷新主卡片背景
                if (mainCard != null)
                {
                    mainCard.Background = window.Background;
                    mainCard.UpdateLayout();
                }

                System.Diagnostics.Debug.WriteLine($"窗口已设置为{(isDarkMode ? "深色" : "浅色")}主题");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题到窗口时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

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
                        System.Diagnostics.Debug.WriteLine($"已更新BundledTheme的BaseTheme为: {bundledTheme.BaseTheme}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("警告: 未找到BundledTheme");
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

                System.Diagnostics.Debug.WriteLine($"已应用{(isDarkMode ? "深色" : "浅色")}主题");

                // 保存主题设置到配置文件
                SaveThemeToConfig(isDarkMode);

                // 触发主题变更事件
                ThemeChanged?.Invoke(this, isDarkMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
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
                // 使用ConfigUtils保存主题设置
                ConfigUtils.SaveBoolValue("IsDarkMode", isDarkMode);
                
                // 同时更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                }

                System.Diagnostics.Debug.WriteLine($"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
                LogHelper.LogSystem("主题", $"已保存主题设置: {(isDarkMode ? "深色" : "浅色")}，值为: {isDarkMode.ToString().ToLower()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存主题设置时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                LogHelper.LogSystem("主题", $"保存主题设置时出错: {ex.Message}");
                LogHelper.LogSystem("主题", $"异常堆栈: {ex.StackTrace}");
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
                // 使用ConfigUtils获取主题设置
                bool isDarkMode = ConfigUtils.GetBoolValue("IsDarkMode", false);
                
                // 同时更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = isDarkMode;
                    Application.Current.Resources["Theme.Light"] = !isDarkMode;
                }
                
                System.Diagnostics.Debug.WriteLine($"从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                LogHelper.LogSystem("主题", $"从配置文件加载主题设置: {(isDarkMode ? "深色" : "浅色")}");
                
                return isDarkMode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主题设置时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                LogHelper.LogSystem("主题", $"加载主题设置时出错: {ex.Message}");
                LogHelper.LogSystem("主题", $"异常堆栈: {ex.StackTrace}");
                
                // 如果加载失败，检查当前主题
                bool currentTheme = IsDarkThemeActive();
                
                // 更新资源字典中的主题标志
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Theme.Dark"] = currentTheme;
                    Application.Current.Resources["Theme.Light"] = !currentTheme;
                }
                
                // 保存当前主题设置到配置文件
                SaveThemeToConfig(currentTheme);
                
                return currentTheme;
            }
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

                System.Diagnostics.Debug.WriteLine($"当前活动主题检测: {(isDark ? "深色" : "浅色")}");
                LogHelper.LogSystem("主题", $"当前活动主题检测: {(isDark ? "深色" : "浅色")}");

                return isDark;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检测当前主题时出错: {ex.Message}");
                LogHelper.LogSystem("主题", $"检测当前主题时出错: {ex.Message}");

                // 默认返回false（浅色主题）
                return false;
            }
        }
    }
}