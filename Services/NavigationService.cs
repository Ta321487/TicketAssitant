using System;
using System.Windows;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.Services
{
    /// <summary>
    /// 导航服务，负责管理应用程序的页面导航
    /// </summary>
    public class NavigationService
    {
        /// <summary>
        /// 显示登录窗口并关闭主窗口
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void NavigateToLogin(string databaseName = "")
        {
            try
            {
                // 获取当前主窗口
                var mainWindow = Application.Current.MainWindow;
                
                // 创建并显示登录窗口
                var loginWindow = new LoginWindow();
                
                // 设置数据库名称
                if (!string.IsNullOrEmpty(databaseName))
                {
                    loginWindow.SetDatabaseName(databaseName);
                }
                
                // 设置登录窗口为主窗口
                Application.Current.MainWindow = loginWindow;
                
                // 显示登录窗口
                loginWindow.Show();
                
                // 关闭当前主窗口
                mainWindow?.Close();
                
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 记录日志
                LogHelper.LogInfo("已返回登录界面");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"返回登录界面时出错: {ex.Message}");
                LogHelper.LogError($"返回登录界面时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开添加车票窗口
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <returns>是否成功添加车票</returns>
        public bool OpenAddTicketWindow(DatabaseService databaseService)
        {
            try
            {
                var addTicketWindow = new AddTicketWindow(databaseService);
                
                // 确保主窗口已初始化并且可见
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    addTicketWindow.Owner = Application.Current.MainWindow;
                    addTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // 如果主窗口不可用，使用CenterScreen
                    addTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                
                // 显示窗口
                bool? result = addTicketWindow.ShowDialog();
                
                // 返回是否成功添加车票
                return result == true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开添加车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开添加车票窗口时出错: {ex.Message}");
                return false;
            }
        }
    }
} 