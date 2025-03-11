using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TA_WPF.Utils;
using TA_WPF.Views;

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
                            
                            // 记录日志
                            LogHelper.LogInfo($"从配置文件加载字体大小: {fontSize}pt");
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
                        
                        LogHelper.LogInfo($"使用默认字体大小: {defaultFontSize}pt");
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
                                
                                LogHelper.LogInfo($"已同步字体大小设置: {configFontSize}pt");
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
                _loginWindow.Show();

                // 监听登录窗口的关闭事件
                _loginWindow.Closed += (s, args) =>
                {
                    // 如果登录不成功，关闭应用程序
                    if (!_loginWindow.LoginSuccessful)
                    {
                        Shutdown();
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
    }
}
