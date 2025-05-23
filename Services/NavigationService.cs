using System.Windows;
using TA_WPF.Models;
using TA_WPF.Utils;
using TA_WPF.ViewModels;
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
        /// <param name="mainViewModel">主视图模型</param>
        /// <returns>是否成功添加车票</returns>
        public bool OpenAddTicketWindow(DatabaseService databaseService, MainViewModel mainViewModel)
        {
            try
            {
                var addTicketWindow = new AddTicketWindow(databaseService, mainViewModel);

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

        /// <summary>
        /// 打开修改车票窗口
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        /// <param name="ticket">要修改的车票</param>
        /// <returns>是否成功修改车票</returns>
        public bool OpenEditTicketWindow(DatabaseService databaseService, MainViewModel mainViewModel, TrainRideInfo ticket)
        {
            try
            {
                var editTicketWindow = new EditTicketWindow(databaseService, mainViewModel, ticket);

                // 确保主窗口已初始化并且可见
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    editTicketWindow.Owner = Application.Current.MainWindow;
                    editTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // 如果主窗口不可用，使用CenterScreen
                    editTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // 显示窗口
                bool? result = editTicketWindow.ShowDialog();

                // 返回是否成功修改车票
                return result == true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开修改车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开修改车票窗口时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开OCR识别车票窗口
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        /// <returns>是否成功识别车票</returns>
        public bool OpenOcrTicketWindow(MainViewModel mainViewModel)
        {
            try
            {
                var ocrTicketWindow = new Views.OcrTicketWindow(mainViewModel);

                // 确保主窗口已初始化并且可见
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    ocrTicketWindow.Owner = Application.Current.MainWindow;
                    ocrTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // 如果主窗口不可用，使用CenterScreen
                    ocrTicketWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // 显示窗口
                bool? result = ocrTicketWindow.ShowDialog();

                // 返回是否成功识别车票
                return result == true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开OCR识别车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开OCR识别车票窗口时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开12306 PDF导入车票窗口
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        /// <returns>是否成功导入车票</returns>
        public bool OpenPdfImportWindow(MainViewModel mainViewModel)
        {
            try
            {
                // 创建必要的服务实例
                var databaseService = new DatabaseService(mainViewModel.ConnectionString);
                var stationSearchService = new StationSearchService(databaseService);
                var pdfImportService = new PdfImportService(databaseService, stationSearchService);

                // 创建并配置窗口
                var pdfImportWindow = new PdfImportWindow(mainViewModel, pdfImportService, stationSearchService);

                // 确保主窗口已初始化并且可见
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    pdfImportWindow.Owner = Application.Current.MainWindow;
                    pdfImportWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // 如果主窗口不可用，使用CenterScreen
                    pdfImportWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // 显示窗口
                bool? result = pdfImportWindow.ShowDialog();

                // 返回是否成功导入车票
                return result == true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开12306 PDF导入车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开12306 PDF导入车票窗口时出错: {ex.Message}");
                return false;
            }
        }
    }
}