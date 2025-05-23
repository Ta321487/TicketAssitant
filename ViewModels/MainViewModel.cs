using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 主视图模型，负责管理主窗口的数据和操作
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ConfigurationService _configurationService;
        private readonly UIService _uiService;
        private readonly NavigationService _navigationService;
        private readonly DatabaseCheckService _databaseCheckService;

        private readonly TicketViewModel _ticketViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly QueryAllTicketsViewModel _queryAllTicketsViewModel;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly QueryAllStationsViewModel _queryAllStationsViewModel;
        private readonly QueryAllCollectionsViewModel _queryAllCollectionsViewModel;

        private bool _showWelcome = true;
        private bool _showSettings = false;
        private bool _showQueryAllTickets = false;
        private bool _showDashboardView = false;
        private bool _showQueryAllStations = false;
        private bool _showQueryAllCollections = false;
        private string _connectionString;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public MainViewModel(string connectionString)
        {
            try
            {
                _connectionString = connectionString;

                // 初始化服务
                _databaseService = new DatabaseService(connectionString);
                _configurationService = new ConfigurationService();
                _uiService = new UIService();
                _navigationService = new NavigationService();
                _databaseCheckService = new DatabaseCheckService(_databaseService);

                // 初始化设置视图模型
                _settingsViewModel = new SettingsViewModel(_configurationService, _uiService, _navigationService, _databaseService, connectionString);

                // 初始化车票中心视图模型
                _queryAllTicketsViewModel = new QueryAllTicketsViewModel(_databaseService, new PaginationViewModel(), this);

                // 初始化车站中心视图模型
                _queryAllStationsViewModel = new QueryAllStationsViewModel(_databaseService, new PaginationViewModel(), this);

                // 初始化车票收藏夹视图模型
                _queryAllCollectionsViewModel = new QueryAllCollectionsViewModel(_databaseService, new PaginationViewModel(), this);

                // 初始化车票视图模型，传入this作为MainViewModel引用
                _ticketViewModel = new TicketViewModel(_databaseService, _navigationService, new PaginationViewModel(), this);

                // 初始化仪表盘视图模型
                _dashboardViewModel = new DashboardViewModel(_databaseService, _configurationService);

                // 初始化命令
                ShowHomeCommand = new RelayCommand(ShowHome);
                TicketListCommand = new RelayCommand(async () => await QueryAllAsync());
                ShowDashboardCommand = new RelayCommand(ShowDashboard);
                StationListCommand = new RelayCommand(async () => await QueryAllStationsAsync());
                CollectionListCommand = new RelayCommand(async () => await QueryAllCollectionsAsync());

                // 新增添加车票相关命令
                OcrTicketCommand = new RelayCommand(ShowOcrTicketFeatureNotAvailable);
                Import12306TicketCommand = new RelayCommand(Show12306ImportFeatureNotAvailable);

                // 检测必要的表是否存在
                _databaseCheckService.CheckRequiredTablesAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化主视图模型时出错: {ex.Message}");
                LogHelper.LogSystemError("MainViewModel", "初始化主视图模型时出错", ex);
            }
        }

        /// <summary>
        /// 车票视图模型
        /// </summary>
        public TicketViewModel TicketViewModel => _ticketViewModel;

        /// <summary>
        /// 设置视图模型
        /// </summary>
        public SettingsViewModel SettingsViewModel => _settingsViewModel;

        /// <summary>
        /// 车票中心视图模型
        /// </summary>
        public QueryAllTicketsViewModel QueryAllTicketsViewModel => _queryAllTicketsViewModel;

        /// <summary>
        /// 车站中心视图模型
        /// </summary>
        public QueryAllStationsViewModel QueryAllStationsViewModel => _queryAllStationsViewModel;

        /// <summary>
        /// 车票收藏夹视图模型
        /// </summary>
        public QueryAllCollectionsViewModel QueryAllCollectionsViewModel => _queryAllCollectionsViewModel;

        /// <summary>
        /// 仪表盘视图模型
        /// </summary>
        public DashboardViewModel DashboardViewModel => _dashboardViewModel;

        /// <summary>
        /// 是否显示欢迎页
        /// </summary>
        public bool ShowWelcome
        {
            get => _showWelcome;
            set
            {
                if (_showWelcome != value)
                {
                    _showWelcome = value;
                    OnPropertyChanged(nameof(ShowWelcome));

                    // 如果显示欢迎页，则隐藏其他页面
                    if (value)
                    {
                        ShowSettings = false;
                        ShowQueryAllTickets = false;
                        ShowDashboardView = false;
                        ShowQueryAllStations = false;
                        ShowQueryAllCollections = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示设置页
        /// </summary>
        public bool ShowSettings
        {
            get => _showSettings;
            set
            {
                if (_showSettings != value)
                {
                    _showSettings = value;
                    OnPropertyChanged(nameof(ShowSettings));

                    // 如果显示设置页，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowQueryAllTickets = false;
                        ShowDashboardView = false;
                        ShowQueryAllStations = false;
                        ShowQueryAllCollections = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示车票中心页面
        /// </summary>
        public bool ShowQueryAllTickets
        {
            get => _showQueryAllTickets;
            set
            {
                if (_showQueryAllTickets != value)
                {
                    _showQueryAllTickets = value;
                    OnPropertyChanged(nameof(ShowQueryAllTickets));

                    // 如果显示车票中心页面，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                        ShowDashboardView = false;
                        ShowQueryAllStations = false;
                        ShowQueryAllCollections = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示仪表盘页面
        /// </summary>
        public bool ShowDashboardView
        {
            get => _showDashboardView;
            set
            {
                if (_showDashboardView != value)
                {
                    _showDashboardView = value;
                    OnPropertyChanged(nameof(ShowDashboardView));

                    // 如果显示仪表盘页面，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                        ShowQueryAllTickets = false;
                        ShowQueryAllStations = false;
                        ShowQueryAllCollections = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示车站中心页面
        /// </summary>
        public bool ShowQueryAllStations
        {
            get => _showQueryAllStations;
            set
            {
                if (_showQueryAllStations != value)
                {
                    _showQueryAllStations = value;
                    OnPropertyChanged(nameof(ShowQueryAllStations));

                    // 如果显示车站中心页面，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                        ShowQueryAllTickets = false;
                        ShowDashboardView = false;
                        ShowQueryAllCollections = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示车票收藏夹页面
        /// </summary>
        public bool ShowQueryAllCollections
        {
            get => _showQueryAllCollections;
            set
            {
                if (_showQueryAllCollections != value)
                {
                    _showQueryAllCollections = value;
                    OnPropertyChanged(nameof(ShowQueryAllCollections));

                    // 如果显示车票收藏夹页，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                        ShowQueryAllTickets = false;
                        ShowDashboardView = false;
                        ShowQueryAllStations = false;
                    }
                }
            }
        }

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _uiService.CalculateDataGridRowHeight(_settingsViewModel.FontSize);

        /// <summary>
        /// 数据表格表头字体大小
        /// </summary>
        public double DataGridHeaderFontSize => _uiService.CalculateDataGridHeaderFontSize(_settingsViewModel.FontSize);

        /// <summary>
        /// 数据表格单元格字体大小
        /// </summary>
        public double DataGridCellFontSize => _uiService.CalculateDataGridCellFontSize(_settingsViewModel.FontSize);

        /// <summary>
        /// 车票中心命令
        /// </summary>
        public ICommand TicketListCommand { get; }

        /// <summary>
        /// 显示首页命令
        /// </summary>
        public ICommand ShowHomeCommand { get; }

        /// <summary>
        /// 显示仪表盘命令
        /// </summary>
        public ICommand ShowDashboardCommand { get; }

        /// <summary>
        /// OCR识别车票命令
        /// </summary>
        public ICommand OcrTicketCommand { get; }

        /// <summary>
        /// 从12306导入车票命令
        /// </summary>
        public ICommand Import12306TicketCommand { get; }

        /// <summary>
        /// 车站中心命令
        /// </summary>
        public ICommand StationListCommand { get; }

        /// <summary>
        /// 车票收藏夹命令
        /// </summary>
        public ICommand CollectionListCommand { get; }

        /// <summary>
        /// 修改连接命令
        /// </summary>
        public ICommand ModifyConnectionCommand => _settingsViewModel.ModifyConnectionCommand;

        /// <summary>
        /// 更新连接命令
        /// </summary>
        public ICommand UpdateConnectionCommand => _settingsViewModel.UpdateConnectionCommand;

        /// <summary>
        /// 更新数据库命令
        /// </summary>
        public ICommand UpdateDatabaseCommand => _settingsViewModel.UpdateDatabaseCommand;

        /// <summary>
        /// 导出日志命令
        /// </summary>
        public ICommand ExportLogCommand => _settingsViewModel.ExportLogCommand;

        /// <summary>
        /// 是否为深色模式
        /// </summary>
        public override bool IsDarkMode
        {
            get => base.IsDarkMode;
            set
            {
                if (base.IsDarkMode != value)
                {
                    base.IsDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                }
            }
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize
        {
            get => _settingsViewModel.FontSize;
            set => _settingsViewModel.FontSize = value;
        }

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress
        {
            get => _settingsViewModel.ServerAddress;
            set => _settingsViewModel.ServerAddress = value;
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get => _settingsViewModel.Username;
            set => _settingsViewModel.Username = value;
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get => _settingsViewModel.Password;
            set => _settingsViewModel.Password = value;
        }

        /// <summary>
        /// 显示密码
        /// </summary>
        public string DisplayPassword => _settingsViewModel.DisplayPassword;

        /// <summary>
        /// 是否显示密码
        /// </summary>
        public bool ShowPassword
        {
            get => _settingsViewModel.ShowPassword;
            set => _settingsViewModel.ShowPassword = value;
        }

        /// <summary>
        /// 新的连接字符串
        /// </summary>
        public string NewConnectionString
        {
            get => _settingsViewModel.NewConnectionString;
            set => _settingsViewModel.NewConnectionString = value;
        }

        /// <summary>
        /// 新的数据库名称
        /// </summary>
        public string NewDatabaseName
        {
            get => _settingsViewModel.NewDatabaseName;
            set => _settingsViewModel.NewDatabaseName = value;
        }

        /// <summary>
        /// 是否暂停字体大小更新
        /// </summary>
        public bool SuspendFontSizeUpdate
        {
            get => _settingsViewModel.SuspendFontSizeUpdate;
            set => _settingsViewModel.SuspendFontSizeUpdate = value;
        }

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// 查询所有车票
        /// </summary>
        private async Task QueryAllAsync()
        {
            ShowQueryAllTickets = true;
            await _queryAllTicketsViewModel.QueryAllAsync();
        }

        /// <summary>
        /// 查询所有车站
        /// </summary>
        private async Task QueryAllStationsAsync()
        {
            try
            {
                ShowQueryAllStations = true;
                await _queryAllStationsViewModel.QueryAllAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"显示车站中心页面时出错: {ex.Message}");
                LogHelper.LogError($"显示车站中心页面时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询所有车票收藏夹
        /// </summary>
        public async Task QueryAllCollectionsAsync()
        {
            try
            {
                ShowQueryAllCollections = true;
                await _queryAllCollectionsViewModel.QueryAllAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"显示车票收藏夹页面时出错: {ex.Message}");
                LogHelper.LogError($"显示车票收藏夹页面时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示首页
        /// </summary>
        private void ShowHome()
        {
            ShowWelcome = true;
        }

        /// <summary>
        /// 显示仪表盘
        /// </summary>
        private void ShowDashboard()
        {
            ShowDashboardView = true;
        }

        /// <summary>
        /// 显示OCR识别车票功能
        /// </summary>
        private void ShowOcrTicketFeatureNotAvailable()
        {
            // 不再显示未开通提示，而是打开OCR识别车票窗口
            try
            {
                _navigationService.OpenOcrTicketWindow(this);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开OCR识别车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开OCR识别车票窗口时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示12306导入车票功能暂未开通提示
        /// </summary>
        private void Show12306ImportFeatureNotAvailable()
        {
            try
            {
                // 使用导航服务打开PDF导入窗口
                _navigationService.OpenPdfImportWindow(this);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开12306 PDF导入车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开12306 PDF导入车票窗口时出错: {ex.Message}");
            }
        }
    }
}