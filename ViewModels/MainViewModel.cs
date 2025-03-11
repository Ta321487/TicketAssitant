using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;
using System.Globalization;
using System.Windows.Threading;
using System.Linq;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 主视图模型，负责管理主窗口的数据和操作
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly ThemeService _themeService;
        private readonly ConfigurationService _configurationService;
        private readonly UIService _uiService;
        private readonly NavigationService _navigationService;
        private readonly DatabaseCheckService _databaseCheckService;
        
        private readonly PaginationViewModel _paginationViewModel;
        private readonly TicketViewModel _ticketViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        
        private bool _showWelcome = true;
        private bool _showSettings = false;
        private bool _showDataGrid = false;
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
                _themeService = new ThemeService();
                _configurationService = new ConfigurationService();
                _uiService = new UIService();
                _navigationService = new NavigationService();
                _databaseCheckService = new DatabaseCheckService(_databaseService);
                
                // 初始化分页视图模型
                _paginationViewModel = new PaginationViewModel();
                
                // 订阅分页视图模型的属性变更事件
                _paginationViewModel.PropertyChanged += (sender, e) =>
                {
                    // 当分页相关属性变更时，通知UI更新
                    if (e.PropertyName == nameof(PaginationViewModel.CurrentPage) ||
                        e.PropertyName == nameof(PaginationViewModel.TotalPages) ||
                        e.PropertyName == nameof(PaginationViewModel.TotalItems) ||
                        e.PropertyName == nameof(PaginationViewModel.PageSize) ||
                        e.PropertyName == nameof(PaginationViewModel.CanNavigateToFirstPage) ||
                        e.PropertyName == nameof(PaginationViewModel.CanNavigateToPreviousPage) ||
                        e.PropertyName == nameof(PaginationViewModel.CanNavigateToNextPage) ||
                        e.PropertyName == nameof(PaginationViewModel.CanNavigateToLastPage))
                    {
                        OnPropertyChanged(e.PropertyName);
                    }
                };
                
                // 初始化设置视图模型
                _settingsViewModel = new SettingsViewModel(_themeService, _configurationService, _uiService, _navigationService, _databaseService, connectionString);
                
                // 初始化车票视图模型，传入this作为MainViewModel引用
                _ticketViewModel = new TicketViewModel(_databaseService, _navigationService, _paginationViewModel, this);
                
                // 初始化命令
                ShowHomeCommand = new RelayCommand(ShowHome);
                
                // 从配置文件加载主题设置
                bool isDarkMode = _themeService.LoadThemeFromConfig();
                
                // 应用主题
                _themeService.ApplyTheme(isDarkMode);
                
                // 检查必要的表是否存在
                _databaseCheckService.CheckRequiredTablesAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化主视图模型时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页视图模型
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        /// <summary>
        /// 车票视图模型
        /// </summary>
        public TicketViewModel TicketViewModel => _ticketViewModel;

        /// <summary>
        /// 设置视图模型
        /// </summary>
        public SettingsViewModel SettingsViewModel => _settingsViewModel;

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
                        ShowDataGrid = false;
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
                        ShowDataGrid = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否显示数据表格
        /// </summary>
        public bool ShowDataGrid
        {
            get => _showDataGrid;
            set
            {
                if (_showDataGrid != value)
                {
                    _showDataGrid = value;
                    OnPropertyChanged(nameof(ShowDataGrid));
                    
                    // 如果显示数据表格，则隐藏其他页面
                    if (value)
                    {
                        ShowWelcome = false;
                        ShowSettings = false;
                    }
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _paginationViewModel.IsLoading;
            set => _paginationViewModel.IsLoading = value;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _paginationViewModel.CurrentPage;
            set => _paginationViewModel.CurrentPage = value;
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _paginationViewModel.TotalPages;

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalItems => _paginationViewModel.TotalItems;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize
        {
            get => _paginationViewModel.PageSize;
            set => _paginationViewModel.PageSize = value;
        }

        /// <summary>
        /// 页大小选项
        /// </summary>
        public int[] PageSizeOptions => _paginationViewModel.PageSizeOptions;

        /// <summary>
        /// 是否可以导航到首页
        /// </summary>
        public bool CanNavigateToFirstPage => _paginationViewModel.CanNavigateToFirstPage;

        /// <summary>
        /// 是否可以导航到上一页
        /// </summary>
        public bool CanNavigateToPreviousPage => _paginationViewModel.CanNavigateToPreviousPage;

        /// <summary>
        /// 是否可以导航到下一页
        /// </summary>
        public bool CanNavigateToNextPage => _paginationViewModel.CanNavigateToNextPage;

        /// <summary>
        /// 是否可以导航到末页
        /// </summary>
        public bool CanNavigateToLastPage => _paginationViewModel.CanNavigateToLastPage;

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
        /// 车票数据
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<Models.TrainRideInfo> TrainRideInfos => _paginationViewModel.Items;

        /// <summary>
        /// 首页命令
        /// </summary>
        public ICommand FirstPageCommand => _paginationViewModel.FirstPageCommand;

        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand => _paginationViewModel.PreviousPageCommand;

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand => _paginationViewModel.NextPageCommand;

        /// <summary>
        /// 末页命令
        /// </summary>
        public ICommand LastPageCommand => _paginationViewModel.LastPageCommand;

        /// <summary>
        /// 加载数据命令
        /// </summary>
        public ICommand LoadDataCommand => _ticketViewModel.LoadDataCommand;

        /// <summary>
        /// 添加车票命令
        /// </summary>
        public ICommand AddTicketCommand => _ticketViewModel.AddTicketCommand;

        /// <summary>
        /// 查询所有车票命令
        /// </summary>
        public ICommand QueryAllCommand => _ticketViewModel.QueryAllCommand;

        /// <summary>
        /// 显示首页命令
        /// </summary>
        public ICommand ShowHomeCommand { get; }

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
        public bool IsDarkMode
        {
            get => _settingsViewModel.IsDarkMode;
            set => _settingsViewModel.IsDarkMode = value;
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
        /// 应用主题设置
        /// </summary>
        /// <param name="isDarkMode">是否为深色模式</param>
        public void ApplyTheme(bool isDarkMode)
        {
            _themeService.ApplyTheme(isDarkMode);
        }

        /// <summary>
        /// 显示首页
        /// </summary>
        private void ShowHome()
        {
            ShowWelcome = true;
            ShowSettings = false;
            ShowDataGrid = false;
            
            // 重置分页
            _paginationViewModel.Reset();
        }
        
        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 简单的命令实现
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter) => _execute();
    }

    // 支持泛型参数的命令实现
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter || parameter == null && default(T) == null)
            {
                return _canExecute == null || _canExecute((T)parameter!);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter || parameter == null && default(T) == null)
            {
                _execute((T)parameter!);
            }
        }
    }
} 