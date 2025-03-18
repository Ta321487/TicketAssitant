using System.Windows.Input;
using TA_WPF.Services;
using TA_WPF.Utils;

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
        
        private bool _showWelcome = true;
        private bool _showSettings = false;
        private bool _showQueryAllTickets = false;
        private bool _showDashboardView = false;
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
                
                // 初始化车票视图模型，传入this作为MainViewModel引用
                _ticketViewModel = new TicketViewModel(_databaseService, _navigationService, new PaginationViewModel(), this);
                
                // 初始化仪表盘视图模型
                _dashboardViewModel = new DashboardViewModel(_databaseService, _configurationService); 
                
                // 初始化命令
                ShowHomeCommand = new RelayCommand(ShowHome);
                TicketListCommand = new RelayCommand(async () => await QueryAllAsync());
                ShowDashboardCommand = new RelayCommand(ShowDashboard);
                
                // 检查必要的表是否存在
                _databaseCheckService.CheckRequiredTablesAsync();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化主视图模型时出错: {ex.Message}");
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
        /// 查询所有车票
        /// </summary>
        private async Task QueryAllAsync()
        {
            ShowQueryAllTickets = true;
            await _queryAllTicketsViewModel.QueryAllAsync();
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