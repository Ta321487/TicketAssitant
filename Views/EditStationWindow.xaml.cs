using System;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// EditStationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditStationWindow : Window
    {
        private readonly EditStationViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        /// <param name="stationToEdit">要编辑的车站信息</param>
        /// <param name="refreshCallback">刷新回调</param>
        public EditStationWindow(DatabaseService databaseService, StationSearchService stationSearchService, StationInfo stationToEdit, Action refreshCallback)
        {
            InitializeComponent();

            // 初始化ViewModel
            _viewModel = new EditStationViewModel(databaseService, stationSearchService, stationToEdit, refreshCallback);
            
            // 设置DataContext
            DataContext = _viewModel;

            // 订阅关闭窗口事件
            _viewModel.CloseWindow += (s, e) => Close();

            // 设置所有者窗口
            if (Application.Current?.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }
        }
    }
} 