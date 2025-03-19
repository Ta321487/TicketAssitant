using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using LiveCharts;
using LiveCharts.Wpf;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 仪表盘视图模型，负责管理仪表盘数据
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly ConfigurationService _configurationService;
        private readonly DispatcherTimer _timer;
        private List<TrainRideInfo> _allTickets; // 所有车票数据缓存
        
        private int _totalTickets;
        private int _currentRangeTickets;
        private string _mostFrequentDepartureStation;
        private string _lastDepartureStation;
        private DateTime _currentDateTime;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _selectedTimeRange = "今日";
        private ObservableCollection<MonthlyTicketData> _monthlyTicketData;
        private ObservableCollection<TicketTypeData> _ticketTypeData;
        private ObservableCollection<MonthlyExpenseData> _monthlyExpenseData;
        private ObservableCollection<RouteData> _topRouteData;
        private ObservableCollection<TrainRideInfo> _recentActivities;
        private bool _isLoading;
        private double _budgetAmount;
        private bool _setTimeRangeInProgress; // 标记是否正在通过按钮设置时间范围
        
        // 图表相关属性
        private SeriesCollection _monthlyTicketSeries;
        private string[] _monthlyTicketLabels;
        private Func<double, string> _monthlyTicketYFormatter;
        
        private SeriesCollection _ticketTypeSeries;
        
        private SeriesCollection _expenseSeries;
        private string[] _expenseLabels;
        private Func<double, string> _expenseYFormatter;
        
        private string _selectedTrendIndicator = "支出金额";
        private string _expenseYTitle = "金额";
        
        private bool _isFullScreen;
        private WindowState _previousWindowState = WindowState.Normal;
        
        // 保存窗口的位置和大小信息
        private double _savedLeft;
        private double _savedTop;
        private double _savedWidth;
        private double _savedHeight;
        
        private bool _showMonthlyTicketChart = true;
        private bool _showRecentActivitiesChart = true;
        private string _monthlyTicketChartMessage = "";
        private string _recentActivitiesMessage = "";
        
        private string _expenseChartMessage = "";
        private bool _showExpenseChart = true;
        
        /// <summary>
        /// 仪表盘视图模型，负责管理仪表盘数据
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="configurationService">配置服务</param>
        public DashboardViewModel(DatabaseService databaseService, ConfigurationService configurationService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            
            // 初始化所有车票数据缓存
            _allTickets = new List<TrainRideInfo>();
            
            // 从配置加载预算金额
            _budgetAmount = _configurationService.LoadBudgetAmountFromConfig();
            
            // 初始化时间
            _currentDateTime = DateTime.Now;
            
            // 初始化集合
            _monthlyTicketData = new ObservableCollection<MonthlyTicketData>();
            _ticketTypeData = new ObservableCollection<TicketTypeData>();
            _monthlyExpenseData = new ObservableCollection<MonthlyExpenseData>();
            _topRouteData = new ObservableCollection<RouteData>();
            _recentActivities = new ObservableCollection<TrainRideInfo>();
            
            // 添加初始的"暂无数据"项
            _topRouteData.Add(new RouteData
            {
                From = "暂无数据",
                To = "",
                Count = 0,
                TotalExpense = 0
            });
            
            _recentActivities.Add(new TrainRideInfo
            {
                TrainNo = "提示",
                DepartStation = "暂无数据",
                ArriveStation = "请添加车票记录",
                DepartDate = DateTime.Now
            });
            
            // 设置图表显示状态，确保自定义时间段也会显示
            _showMonthlyTicketChart = true;
            _showExpenseChart = true;
            _showRecentActivitiesChart = true;
            
            // 设置默认时间范围为今日
            SetTimeRange(_selectedTimeRange);
            
            // 更新Y轴标题
            UpdateExpenseYTitle();
            
            // 确保最近活动和常用线路TOP5始终显示
            _showRecentActivitiesChart = true;
            
            // 创建并启动计时器，每秒更新一次时间
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
            _timer.Start();
            
            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            TimeRangeCommand = new RelayCommand<string>(SetTimeRange);
            ShowTicketTypeDetailsCommand = new RelayCommand<TicketTypeData>(ShowTicketTypeDetails);
            SelectTrendIndicatorCommand = new RelayCommand<string>(SetTrendIndicator);
            ToggleFullScreenCommand = new RelayCommand(ToggleFullScreen);
            
            // 加载仪表盘数据
            LoadDashboardDataAsync();
        }
        
        /// <summary>
        /// 总车票数
        /// </summary>
        public int TotalTickets
        {
            get => _totalTickets;
            set
            {
                if (_totalTickets != value)
                {
                    _totalTickets = value;
                    OnPropertyChanged(nameof(TotalTickets));
                }
            }
        }
        
        /// <summary>
        /// 当前时间范围内的车票数
        /// </summary>
        public int CurrentRangeTickets
        {
            get => _currentRangeTickets;
            set
            {
                if (_currentRangeTickets != value)
                {
                    _currentRangeTickets = value;
                    OnPropertyChanged(nameof(CurrentRangeTickets));
                }
            }
        }
        
        /// <summary>
        /// 最经常出发的车站
        /// </summary>
        public string MostFrequentDepartureStation
        {
            get => _mostFrequentDepartureStation;
            set
            {
                if (_mostFrequentDepartureStation != value)
                {
                    _mostFrequentDepartureStation = value;
                    OnPropertyChanged(nameof(MostFrequentDepartureStation));
                }
            }
        }
        
        /// <summary>
        /// 最后一次出发的车站
        /// </summary>
        public string LastDepartureStation
        {
            get => _lastDepartureStation;
            set
            {
                if (_lastDepartureStation != value)
                {
                    _lastDepartureStation = value;
                    OnPropertyChanged(nameof(LastDepartureStation));
                }
            }
        }
        
        /// <summary>
        /// 当前系统时间
        /// </summary>
        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                if (_currentDateTime != value)
                {
                    _currentDateTime = value;
                    OnPropertyChanged(nameof(CurrentDateTime));
                    OnPropertyChanged(nameof(FormattedDate));
                    OnPropertyChanged(nameof(FormattedTime));
                }
            }
        }
        
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged(nameof(StartDate));
                    // 如果是通过日历控件手动设置日期，则更新SelectedTimeRange为自定义
                    if (!SetTimeRangeInProgress)
                    {
                        SelectedTimeRange = "自定义";
                        OnPropertyChanged(nameof(CurrentRangeText));
                        
                        // 确保图表显示
                        _showMonthlyTicketChart = true;
                        _showExpenseChart = true;
                        OnPropertyChanged(nameof(ShowMonthlyTicketChart));
                        OnPropertyChanged(nameof(ShowExpenseChart));
                        
                        // 清除提示信息
                        _monthlyTicketChartMessage = "";
                        _expenseChartMessage = "";
                        OnPropertyChanged(nameof(MonthlyTicketChartMessage));
                        OnPropertyChanged(nameof(ExpenseChartMessage));
                        
                        // 检查开始日期是否大于结束日期
                        if (_startDate > _endDate)
                        {
                            // 显示警告对话框
                            Utils.MessageBoxHelper.ShowWarning("开始日期不能大于结束日期，请重新选择！", "日期范围错误");
                            
                            // 将开始日期重置为结束日期
                            _startDate = _endDate;
                            OnPropertyChanged(nameof(StartDate));
                        }
                    }
                    RefreshDataAsync();
                }
            }
        }
        
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged(nameof(EndDate));
                    // 如果是通过日历控件手动设置日期，则更新SelectedTimeRange为自定义
                    if (!SetTimeRangeInProgress)
                    {
                        SelectedTimeRange = "自定义";
                        OnPropertyChanged(nameof(CurrentRangeText));
                        
                        // 确保图表显示
                        _showMonthlyTicketChart = true;
                        _showExpenseChart = true;
                        OnPropertyChanged(nameof(ShowMonthlyTicketChart));
                        OnPropertyChanged(nameof(ShowExpenseChart));
                        
                        // 清除提示信息
                        _monthlyTicketChartMessage = "";
                        _expenseChartMessage = "";
                        OnPropertyChanged(nameof(MonthlyTicketChartMessage));
                        OnPropertyChanged(nameof(ExpenseChartMessage));
                        
                        // 检查结束日期是否小于开始日期
                        if (_endDate < _startDate)
                        {
                            // 显示警告对话框
                            MessageBoxHelper.ShowWarning("结束日期不能小于开始日期，请重新选择！", "日期范围错误");
                            
                            // 将结束日期重置为开始日期
                            _endDate = _startDate;
                            OnPropertyChanged(nameof(EndDate));
                        }
                    }
                    RefreshDataAsync();
                }
            }
        }
        
        /// <summary>
        /// 选中的时间范围
        /// </summary>
        public string SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                if (_selectedTimeRange != value)
                {
                    _selectedTimeRange = value;
                    OnPropertyChanged(nameof(SelectedTimeRange));
                }
            }
        }
        
        /// <summary>
        /// 月度车票数据
        /// </summary>
        public ObservableCollection<MonthlyTicketData> MonthlyTicketData
        {
            get => _monthlyTicketData;
            set
            {
                if (_monthlyTicketData != value)
                {
                    _monthlyTicketData = value;
                    OnPropertyChanged(nameof(MonthlyTicketData));
                }
            }
        }
        
        /// <summary>
        /// 车票类型数据
        /// </summary>
        public ObservableCollection<TicketTypeData> TicketTypeData
        {
            get => _ticketTypeData;
            set
            {
                if (_ticketTypeData != value)
                {
                    _ticketTypeData = value;
                    OnPropertyChanged(nameof(TicketTypeData));
                }
            }
        }
        
        /// <summary>
        /// 月度支出数据
        /// </summary>
        public ObservableCollection<MonthlyExpenseData> MonthlyExpenseData
        {
            get => _monthlyExpenseData;
            set
            {
                if (_monthlyExpenseData != value)
                {
                    _monthlyExpenseData = value;
                    OnPropertyChanged(nameof(MonthlyExpenseData));
                }
            }
        }
        
        /// <summary>
        /// 热门路线数据
        /// </summary>
        public ObservableCollection<RouteData> TopRouteData
        {
            get => _topRouteData;
            set
            {
                if (_topRouteData != value)
                {
                    _topRouteData = value;
                    OnPropertyChanged(nameof(TopRouteData));
                }
            }
        }
        
        /// <summary>
        /// 最近活动
        /// </summary>
        public ObservableCollection<TrainRideInfo> RecentActivities
        {
            get => _recentActivities;
            set
            {
                if (_recentActivities != value)
                {
                    _recentActivities = value;
                    OnPropertyChanged(nameof(RecentActivities));
                }
            }
        }
        
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        
        /// <summary>
        /// 预算金额
        /// </summary>
        public double BudgetAmount
        {
            get => _budgetAmount;
            set
            {
                if (_budgetAmount != value)
                {
                    _budgetAmount = value;
                    OnPropertyChanged(nameof(BudgetAmount));
                    // 当预算变更时，更新支出图表
                    UpdateExpenseChart();
                }
            }
        }
        
        /// <summary>
        /// 月度车票数据图表
        /// </summary>
        public SeriesCollection MonthlyTicketSeries
        {
            get => _monthlyTicketSeries;
            set
            {
                if (_monthlyTicketSeries != value)
                {
                    _monthlyTicketSeries = value;
                    OnPropertyChanged(nameof(MonthlyTicketSeries));
                }
            }
        }
        
        /// <summary>
        /// 月度车票标签
        /// </summary>
        public string[] MonthlyTicketLabels
        {
            get => _monthlyTicketLabels;
            set
            {
                if (_monthlyTicketLabels != value)
                {
                    _monthlyTicketLabels = value;
                    OnPropertyChanged(nameof(MonthlyTicketLabels));
                }
            }
        }
        
        /// <summary>
        /// 月度车票Y轴格式化器
        /// </summary>
        public Func<double, string> MonthlyTicketYFormatter
        {
            get => _monthlyTicketYFormatter;
            set
            {
                if (_monthlyTicketYFormatter != value)
                {
                    _monthlyTicketYFormatter = value;
                    OnPropertyChanged(nameof(MonthlyTicketYFormatter));
                }
            }
        }
        
        /// <summary>
        /// 车票类型数据图表
        /// </summary>
        public SeriesCollection TicketTypeSeries
        {
            get => _ticketTypeSeries;
            set
            {
                if (_ticketTypeSeries != value)
                {
                    _ticketTypeSeries = value;
                    OnPropertyChanged(nameof(TicketTypeSeries));
                }
            }
        }
        
        /// <summary>
        /// 月度支出数据图表
        /// </summary>
        public SeriesCollection ExpenseSeries
        {
            get => _expenseSeries;
            set
            {
                if (_expenseSeries != value)
                {
                    _expenseSeries = value;
                    OnPropertyChanged(nameof(ExpenseSeries));
                }
            }
        }
        
        /// <summary>
        /// 月度支出标签
        /// </summary>
        public string[] ExpenseLabels
        {
            get => _expenseLabels;
            set
            {
                if (_expenseLabels != value)
                {
                    _expenseLabels = value;
                    OnPropertyChanged(nameof(ExpenseLabels));
                }
            }
        }
        
        /// <summary>
        /// 月度支出Y轴格式化器
        /// </summary>
        public Func<double, string> ExpenseYFormatter
        {
            get => _expenseYFormatter;
            set
            {
                if (_expenseYFormatter != value)
                {
                    _expenseYFormatter = value;
                    OnPropertyChanged(nameof(ExpenseYFormatter));
                }
            }
        }
        
        /// <summary>
        /// 格式化的日期（年月日）
        /// </summary>
        public string FormattedDate => CurrentDateTime.ToString("yyyy年MM月dd日");
        
        /// <summary>
        /// 格式化的时间（时分秒）
        /// </summary>
        public string FormattedTime => CurrentDateTime.ToString("HH:mm:ss");
        
        /// <summary>
        /// 当前时间范围的显示文本
        /// </summary>
        public string CurrentRangeText
        {
            get
            {
                switch (SelectedTimeRange)
                {
                    case "今日":
                        return "今日车票";
                    case "本周":
                        return "本周车票";
                    case "本月":
                        return "本月车票";
                    case "本年":
                        return "本年车票";
                    case "自定义":
                        return "自定义时段车票";
                    default:
                        return "时段车票";
                }
            }
        }
        
        /// <summary>
        /// 是否有车票类型数据
        /// </summary>
        public bool HasTicketTypeData => TicketTypeData != null && TicketTypeData.Any();
        
        /// <summary>
        /// 是否有费用数据
        /// </summary>
        public bool HasExpenseData => MonthlyExpenseData != null && MonthlyExpenseData.Any();
        
        /// <summary>
        /// 是否有热门路线数据
        /// </summary>
        public bool HasTopRouteData => TopRouteData != null && TopRouteData.Any();
        
        /// <summary>
        /// 是否有最近活动数据
        /// </summary>
        public bool HasRecentActivities => RecentActivities != null && RecentActivities.Any();
        
        /// <summary>
        /// 选中的趋势指标
        /// </summary>
        public string SelectedTrendIndicator
        {
            get => _selectedTrendIndicator;
            set
            {
                if (_selectedTrendIndicator != value)
                {
                    _selectedTrendIndicator = value;
                    OnPropertyChanged(nameof(SelectedTrendIndicator));
                    UpdateExpenseYTitle();
                    RefreshDataAsync();
                }
            }
        }
        
        /// <summary>
        /// 费用Y轴标题
        /// </summary>
        public string ExpenseYTitle
        {
            get => _expenseYTitle;
            set
            {
                if (_expenseYTitle != value)
                {
                    _expenseYTitle = value;
                    OnPropertyChanged(nameof(ExpenseYTitle));
                }
            }
        }
        
        /// <summary>
        /// 费用X轴标题
        /// </summary>
        public string ExpenseXTitle
        {
            get
            {
                switch (SelectedTimeRange)
                {
                    case "今日":
                        return "小时";
                    case "本周":
                    case "本月":
                        return "日期";
                    case "本年":
                        return "月份";
                    case "自定义":
                        // 根据时间跨度选择合适的标题
                        TimeSpan span = EndDate - StartDate;
                        if (span.TotalDays <= 1)
                            return "小时";
                        else if (span.TotalDays <= 31)
                            return "日期";
                        else
                            return "月份";
                    default:
                        return "时间";
                }
            }
        }
        
        /// <summary>
        /// 车票使用趋势X轴标题
        /// </summary>
        public string MonthlyTicketXTitle
        {
            get
            {
                // 使用与ExpenseXTitle相同的逻辑
                switch (SelectedTimeRange)
                {
                    case "今日":
                        return "小时";
                    case "本周":
                    case "本月":
                        return "日期";
                    case "本年":
                        return "月份";
                    case "自定义":
                        // 根据时间跨度选择合适的标题
                        TimeSpan span = EndDate - StartDate;
                        if (span.TotalDays <= 1)
                            return "小时";
                        else if (span.TotalDays <= 31)
                            return "日期";
                        else
                            return "月份";
                    default:
                        return "时间";
                }
            }
        }
        
        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set
            {
                if (_isFullScreen != value)
                {
                    _isFullScreen = value;
                    OnPropertyChanged(nameof(IsFullScreen));
                }
            }
        }
        
        /// <summary>
        /// 之前的窗口状态
        /// </summary>
        public WindowState PreviousWindowState
        {
            get => _previousWindowState;
            set
            {
                if (_previousWindowState != value)
                {
                    _previousWindowState = value;
                    OnPropertyChanged(nameof(PreviousWindowState));
                }
            }
        }
        
        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }
        
        /// <summary>
        /// 时间范围命令
        /// </summary>
        public ICommand TimeRangeCommand { get; }
        
        /// <summary>
        /// 显示车票类型详情命令
        /// </summary>
        public ICommand ShowTicketTypeDetailsCommand { get; }
        
        /// <summary>
        /// 趋势指标选择命令
        /// </summary>
        public ICommand SelectTrendIndicatorCommand { get; private set; }
        
        /// <summary>
        /// 切换全屏命令
        /// </summary>
        public ICommand ToggleFullScreenCommand { get; }
        
        /// <summary>
        /// 费用图表消息
        /// </summary>
        public string ExpenseChartMessage
        {
            get => _expenseChartMessage;
            set
            {
                if (_expenseChartMessage != value)
                {
                    _expenseChartMessage = value;
                    OnPropertyChanged(nameof(ExpenseChartMessage));
                }
            }
        }
        
        /// <summary>
        /// 是否显示费用图表
        /// </summary>
        public bool ShowExpenseChart
        {
            get => _showExpenseChart;
            set
            {
                if (_showExpenseChart != value)
                {
                    _showExpenseChart = value;
                    OnPropertyChanged(nameof(ShowExpenseChart));
                }
            }
        }
        
        /// <summary>
        /// 获取是否正在设置时间范围
        /// </summary>
        public bool SetTimeRangeInProgress => _setTimeRangeInProgress;

        /// <summary>
        /// 是否显示车票使用趋势图表
        /// </summary>
        public bool ShowMonthlyTicketChart => _showMonthlyTicketChart;

        /// <summary>
        /// 是否显示最近活动图表
        /// </summary>
        public bool ShowRecentActivitiesChart => _showRecentActivitiesChart;

        /// <summary>
        /// 月度车票图表消息
        /// </summary>
        public string MonthlyTicketChartMessage => _monthlyTicketChartMessage;

        /// <summary>
        /// 最近活动图表消息
        /// </summary>
        public string RecentActivitiesMessage => _recentActivitiesMessage;
        
        /// <summary>
        /// 设置时间范围
        /// </summary>
        /// <param name="range">时间范围</param>
        private void SetTimeRange(string range)
        {
            if (string.IsNullOrEmpty(range))
                return;
                
            SelectedTimeRange = range;
            
            _setTimeRangeInProgress = true; // 标记开始设置时间范围
            
            switch (range)
            {
                case "今日":
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                    // 设置车票使用趋势和费用支出分析的提示信息
                    _showMonthlyTicketChart = false;
                    _monthlyTicketChartMessage = "当前时间维度无法反映使用趋势，请选择\"本周\"、\"本月\"、\"本年\"或\"自定义时间\"查看使用趋势。";
                    OnPropertyChanged(nameof(ShowMonthlyTicketChart));
                    OnPropertyChanged(nameof(MonthlyTicketChartMessage));
                    
                    _showExpenseChart = false;
                    _expenseChartMessage = "当前时间维度无法反映支出趋势，请选择\"本周\"、\"本月\"、\"本年\"或\"自定义时间\"查看支出分析。";
                    OnPropertyChanged(nameof(ShowExpenseChart));
                    OnPropertyChanged(nameof(ExpenseChartMessage));
                    break;
                case "本周":
                    StartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    EndDate = StartDate.AddDays(7).AddSeconds(-1);
                    break;
                case "本月":
                    StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    EndDate = StartDate.AddMonths(1).AddSeconds(-1);
                    break;
                case "本年":
                    StartDate = new DateTime(DateTime.Today.Year, 1, 1);
                    EndDate = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);
                    break;
                default:
                    // 自定义时间范围
                    if (range == "自定义")
                    {
                        if (StartDate > EndDate)
                        {
                            // 如果开始日期大于结束日期，则显示警告并交换日期
                            Utils.MessageBoxHelper.ShowWarning("开始日期不能大于结束日期，系统已自动调整！", "日期范围错误");
                            var temp = StartDate;
                            StartDate = EndDate;
                            EndDate = temp;
                        }
                        
                        // 检查时间跨度是否过大（超过20年）
                        TimeSpan timeSpan = EndDate - StartDate;
                        if (timeSpan.TotalDays > 365 * 20)
                        {
                            Utils.MessageBoxHelper.ShowWarning("选择的时间范围过大（超过20年），可能影响显示效果，建议调整时间范围。", "时间范围过大");
                        }
                    }
                    break;
            }

            _setTimeRangeInProgress = false; // 标记结束设置时间范围
            
            // 更新图表显示状态
            if (range != "今日") {
                // 车票使用趋势在本周、本月、本年和自定义时间范围下显示
                _showMonthlyTicketChart = true;
                OnPropertyChanged(nameof(ShowMonthlyTicketChart));
                
                // 费用支出分析在本周、本月、本年和自定义时间范围下显示
                _showExpenseChart = true;
                OnPropertyChanged(nameof(ShowExpenseChart));
                
                // 清除任何可能存在的消息提示
                _monthlyTicketChartMessage = "";
                _expenseChartMessage = "";
                OnPropertyChanged(nameof(MonthlyTicketChartMessage));
                OnPropertyChanged(nameof(ExpenseChartMessage));
            }
            
            // 最近活动始终显示，不受时间范围约束
            _showRecentActivitiesChart = true;
            OnPropertyChanged(nameof(ShowRecentActivitiesChart));

            OnPropertyChanged(nameof(CurrentRangeText));
            OnPropertyChanged(nameof(ExpenseXTitle));
            OnPropertyChanged(nameof(MonthlyTicketXTitle));
            
            // 确保在时间范围变更后刷新数据
            RefreshDataAsync();
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"时间范围已变更为：{range}，开始日期：{StartDate:yyyy-MM-dd}，结束日期：{EndDate:yyyy-MM-dd}");
        }
        
        /// <summary>
        /// 显示车票类型详情
        /// </summary>
        /// <param name="ticketType">车票类型数据</param>
        private void ShowTicketTypeDetails(TicketTypeData ticketType)
        {
            if (ticketType == null)
                return;
                
            // 这里可以实现显示详情的逻辑，例如弹出对话框等
            System.Diagnostics.Debug.WriteLine($"显示车票类型详情: {ticketType.TypeName}, 数量: {ticketType.Count}");
        }
        
        /// <summary>
        /// 加载仪表盘数据
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                
                // 清空所有集合
                MonthlyTicketData.Clear();
                TicketTypeData.Clear();
                MonthlyExpenseData.Clear();
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"开始刷新数据，当前时间范围：{SelectedTimeRange}，开始日期：{StartDate:yyyy-MM-dd}，结束日期：{EndDate:yyyy-MM-dd}");
                
                // 重新加载所有车票数据
                _allTickets = await _databaseService.GetAllTrainRideInfosAsync();
                
                // 更新总票数
                TotalTickets = _allTickets.Count;
                
                // 更新当前范围内的票数
                CurrentRangeTickets = _allTickets.Count(t => t.DepartDate.HasValue && 
                                                         t.DepartDate.Value >= StartDate && 
                                                         t.DepartDate.Value <= EndDate);
                
                // 更新最常用出发站
                var departStationGroups = _allTickets
                    .Where(t => !string.IsNullOrEmpty(t.DepartStation))
                    .GroupBy(t => t.DepartStation)
                    .OrderByDescending(g => g.Count());
                
                MostFrequentDepartureStation = departStationGroups.Any() ? departStationGroups.First().Key : "无数据";
                
                // 更新最近出发站
                var lastTicket = _allTickets
                    .Where(t => t.DepartDate.HasValue && !string.IsNullOrEmpty(t.DepartStation))
                    .OrderByDescending(t => t.DepartDate)
                    .FirstOrDefault();
                
                LastDepartureStation = lastTicket != null ? lastTicket.DepartStation : "无数据";
                
                // 加载图表数据
                await LoadChartDataAsync(_allTickets);
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"数据刷新完成，总车票数：{TotalTickets}，当前范围车票数：{CurrentRangeTickets}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新数据时出错: {ex.Message}");
                // 可以在这里添加错误提示逻辑
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 加载图表数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private async Task LoadChartDataAsync(List<TrainRideInfo> tickets)
        {
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"开始加载图表数据，当前时间范围：{SelectedTimeRange}");
            
            // 先加载热门路线数据和最近活动（使用所有车票数据）
            LoadTopRouteData(tickets);
            LoadRecentActivities(tickets);
            
            // 根据时间范围筛选车票数据（当前时间范围）
            var filteredTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                        t.DepartDate.Value >= StartDate && 
                                        t.DepartDate.Value <= EndDate).ToList();
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"筛选后的当前时间范围车票数据：{filteredTickets.Count}条，时间范围：{StartDate:yyyy-MM-dd} 至 {EndDate:yyyy-MM-dd}");
            
            // 对比时间范围（去年同期或其他对比时间）
            DateTime comparisonStartDate = DateTime.MinValue;
            DateTime comparisonEndDate = DateTime.MinValue;
            
            // 确定对比时间范围
            switch (SelectedTimeRange)
            {
                case "本周":
                    comparisonStartDate = StartDate.AddDays(-7);
                    comparisonEndDate = EndDate.AddDays(-7);
                    break;
                case "本月":
                    comparisonStartDate = StartDate.AddMonths(-1);
                    comparisonEndDate = EndDate.AddMonths(-1);
                    break;
                case "本年":
                    comparisonStartDate = StartDate.AddYears(-1);
                    comparisonEndDate = EndDate.AddYears(-1);
                    break;
                case "自定义":
                    // 根据时间跨度选择合适的对比方式
                    TimeSpan span = EndDate - StartDate;
                    if (span.TotalDays <= 7)
                    {
                        // 一周内，对比上周同期
                        comparisonStartDate = StartDate.AddDays(-7);
                        comparisonEndDate = EndDate.AddDays(-7);
                    }
                    else if (span.TotalDays <= 31)
                    {
                        // 一个月内，对比上月同期
                        comparisonStartDate = StartDate.AddMonths(-1);
                        comparisonEndDate = EndDate.AddMonths(-1);
                    }
                    else if (span.TotalDays <= 366) // 考虑闰年
                    {
                        // 一年内，对比去年同期
                        comparisonStartDate = StartDate.AddYears(-1);
                        comparisonEndDate = EndDate.AddYears(-1);
                    }
                    else
                    {
                        // 多年数据，比较同样跨度的上一时间段
                        int yearSpan = EndDate.Year - StartDate.Year;
                        comparisonStartDate = StartDate.AddYears(-yearSpan);
                        comparisonEndDate = EndDate.AddYears(-yearSpan);
                    }
                    break;
                default:
                    // 默认为去年同期
                    comparisonStartDate = StartDate.AddYears(-1);
                    comparisonEndDate = EndDate.AddYears(-1);
                    break;
            }
            
            // 根据对比时间范围筛选车票数据
            var comparisonTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                          t.DepartDate.Value >= comparisonStartDate && 
                                          t.DepartDate.Value <= comparisonEndDate).ToList();
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"筛选后的对比时间范围车票数据：{comparisonTickets.Count}条，对比时间范围：{comparisonStartDate:yyyy-MM-dd} 至 {comparisonEndDate:yyyy-MM-dd}");
            
            // 加载月度车票数据（传入当前和对比时间段的数据）
            LoadMonthlyTicketData(filteredTickets, comparisonTickets);
            
            // 加载车票类型数据（使用筛选后的数据）
            LoadTicketTypeData(filteredTickets);
            
            // 加载月度支出数据
            LoadMonthlyExpenseData(filteredTickets);
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"图表数据加载完成，月度车票数据：{MonthlyTicketData.Count}条，月度支出数据：{MonthlyExpenseData.Count}条");
        }
        
        /// <summary>
        /// 加载月度车票数据
        /// </summary>
        /// <param name="tickets">当前时间范围车票数据</param>
        /// <param name="comparisonTickets">对比时间范围车票数据</param>
        private void LoadMonthlyTicketData(List<TrainRideInfo> tickets, List<TrainRideInfo> comparisonTickets)
        {
            // 清空集合
            MonthlyTicketData.Clear();
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"开始加载月度车票数据，当前时间范围：{SelectedTimeRange}，开始日期：{StartDate:yyyy-MM-dd}，结束日期：{EndDate:yyyy-MM-dd}");
            
            // 只在本周、本月、本年、自定义时间范围显示车票使用趋势
            if (SelectedTimeRange == "今日")
            {
                // 在今日时间范围下，不显示图表
                // 提示信息已在SetTimeRange方法中设置
                if (MonthlyTicketSeries != null && MonthlyTicketSeries.Count >= 2)
                {
                    ((LineSeries)MonthlyTicketSeries[0]).Values = new ChartValues<int> { 0 };
                    ((LineSeries)MonthlyTicketSeries[1]).Values = new ChartValues<int> { 0 };
                    MonthlyTicketLabels = new[] { "" };
                    
                    // 更新图表标题
                    ((LineSeries)MonthlyTicketSeries[0]).Title = "当前";
                    ((LineSeries)MonthlyTicketSeries[1]).Title = "对比";
                }
                System.Diagnostics.Debug.WriteLine($"车票使用趋势：今日时间范围下不显示图表");
                return;
            }
            
            // 显示图表
            // 图表显示状态已在SetTimeRange方法或属性设置器中设置
            
            DateTime startDate, endDate;
            string format;
            Func<DateTime, DateTime> getNextStep;
            
            // 根据选择的时间范围确定开始日期、结束日期和日期格式
            switch (SelectedTimeRange)
            {
                case "本周":
                    // 本周数据按天显示
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    endDate = startDate.AddDays(7).AddSeconds(-1);
                    format = "MM/dd";
                    getNextStep = date => date.AddDays(1);
                    break;
                case "本月":
                    // 本月数据按天显示，但可能需要适当跳过一些天
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = startDate.AddMonths(1).AddSeconds(-1);
                    format = "MM/dd";
                    // 如果天数超过15天，则每3天显示一次
                    int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                    getNextStep = daysInMonth > 15 ? (date => date.AddDays(3)) : (date => date.AddDays(1));
                    break;
                case "本年":
                    // 本年数据按月显示
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    endDate = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);
                    format = "yyyy/MM";
                    getNextStep = date => date.AddMonths(1);
                    break;
                case "自定义":
                    // 自定义时间范围，根据时间跨度选择合适的显示方式
                    startDate = StartDate;
                    endDate = EndDate;
                    
                    // 计算时间跨度
                    TimeSpan span = endDate - startDate;
                    
                    if (span.TotalDays <= 1)
                    {
                        // 一天内按小时显示
                        format = "HH:00";
                        getNextStep = date => date.AddHours(1);
                    }
                    else if (span.TotalDays <= 31)
                    {
                        // 一个月内按天显示
                        format = "MM/dd";
                        // 如果天数超过15天，则每3天显示一次
                        getNextStep = span.TotalDays > 15 ? (date => date.AddDays(3)) : (date => date.AddDays(1));
                    }
                    else if (span.TotalDays <= 365)
                    {
                        // 一年内按月显示
                        format = "yyyy/MM";
                        getNextStep = date => date.AddMonths(1);
                    }
                    else
                    {
                        // 跨越多年的情况
                        // 检查是否跨越了超过5年
                        int yearDiff = endDate.Year - startDate.Year;
                        if (yearDiff >= 5)
                        {
                            // 如果跨越了5年或以上，按年显示
                            format = "yyyy年";
                            getNextStep = date => date.AddYears(1);
                        }
                        else
                        {
                            // 否则按季度显示
                            format = "yyyy/MM";
                            getNextStep = date => date.AddMonths(3);
                        }
                    }
                    break;
                default:
                    // 默认显示最近6个月
                    startDate = DateTime.Today.AddMonths(-6);
                    endDate = DateTime.Today;
                    format = "yyyy/MM";
                    getNextStep = date => date.AddMonths(1);
                    break;
            }
            
            // 按时间间隔统计
            for (DateTime date = startDate; date <= endDate; date = getNextStep(date))
            {
                DateTime periodStart = date;
                DateTime periodEnd = getNextStep(date).AddSeconds(-1);
                
                // 如果结束日期超过了总的结束日期，则使用总的结束日期
                if (periodEnd > endDate)
                    periodEnd = endDate;
                
                // 统计当前时间段内的车票数量
                int count = tickets.Count(t => t.DepartDate.HasValue && 
                                          t.DepartDate.Value >= periodStart && 
                                          t.DepartDate.Value <= periodEnd);
                
                // 计算对比时间段
                DateTime comparisonPeriodStart;
                DateTime comparisonPeriodEnd;
                
                switch (SelectedTimeRange)
                {
                    case "本周":
                        // 上周同期
                        comparisonPeriodStart = periodStart.AddDays(-7);
                        comparisonPeriodEnd = periodEnd.AddDays(-7);
                        break;
                    case "本月":
                        // 上月同期
                        comparisonPeriodStart = periodStart.AddMonths(-1);
                        comparisonPeriodEnd = periodEnd.AddMonths(-1);
                        break;
                    case "本年":
                        // 去年同期
                        comparisonPeriodStart = periodStart.AddYears(-1);
                        comparisonPeriodEnd = periodEnd.AddYears(-1);
                        break;
                    case "自定义":
                        // 自定义时间范围，根据时间跨度选择合适的对比方式
                        TimeSpan span = endDate - startDate;
                        
                        if (span.TotalDays <= 7)
                        {
                            // 一周内，对比上周同期
                            comparisonPeriodStart = periodStart.AddDays(-7);
                            comparisonPeriodEnd = periodEnd.AddDays(-7);
                        }
                        else if (span.TotalDays <= 31)
                        {
                            // 一个月内，对比上月同期
                            comparisonPeriodStart = periodStart.AddMonths(-1);
                            comparisonPeriodEnd = periodEnd.AddMonths(-1);
                        }
                        else if (span.TotalDays <= 366) // 考虑闰年
                        {
                            // 一年内，对比去年同期
                            comparisonPeriodStart = periodStart.AddYears(-1);
                            comparisonPeriodEnd = periodEnd.AddYears(-1);
                        }
                        else
                        {
                            // 对于跨越多年的情况，我们需要特殊处理
                            // 如果是按年统计，对比的就是前一年的数据
                            if (format == "yyyy年")
                            {
                                comparisonPeriodStart = periodStart.AddYears(-1);
                                comparisonPeriodEnd = periodEnd.AddYears(-1);
                            }
                            else
                            {
                                // 否则对比去年同期
                                comparisonPeriodStart = periodStart.AddYears(-1);
                                comparisonPeriodEnd = periodEnd.AddYears(-1);
                            }
                        }
                        break;
                    default:
                        // 默认为去年同期
                        comparisonPeriodStart = periodStart.AddYears(-1);
                        comparisonPeriodEnd = periodEnd.AddYears(-1);
                        break;
                }
                
                // 使用传入的对比数据计算对比时间段内的车票数量
                int comparisonCount = comparisonTickets.Count(t => t.DepartDate.HasValue && 
                                                  t.DepartDate.Value >= comparisonPeriodStart && 
                                                  t.DepartDate.Value <= comparisonPeriodEnd);
                
                // 记录调试信息
                if (count > 0 || comparisonCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"时间段 {periodStart:yyyy-MM-dd} 至 {periodEnd:yyyy-MM-dd}: 当前={count}, 对比={comparisonCount}");
                }
                
                // 上一时间段
                var lastPeriodStart = getNextStep(date.AddDays(-getNextStep(date).Subtract(date).TotalDays * 2));
                var lastPeriodEnd = getNextStep(lastPeriodStart).AddSeconds(-1);
                var lastPeriodCount = tickets.Count(t => t.DepartDate.HasValue && 
                                                     t.DepartDate.Value >= lastPeriodStart && 
                                                     t.DepartDate.Value <= lastPeriodEnd);
                
                var monthData = new MonthlyTicketData
                {
                    Month = date.ToString(format),
                    Count = count,
                    LastYearCount = comparisonCount,
                    MonthOnMonthGrowth = lastPeriodCount > 0 ? (count - lastPeriodCount) * 100.0 / lastPeriodCount : 0,
                    YearOnYearGrowth = comparisonCount > 0 ? (count - comparisonCount) * 100.0 / comparisonCount : 0
                };
                
                MonthlyTicketData.Add(monthData);
            }
            
            // 更新图表数据
            if (MonthlyTicketSeries != null && MonthlyTicketSeries.Count >= 2)
            {
                var currentYearValues = new ChartValues<int>();
                var lastYearValues = new ChartValues<int>();
                var labels = new string[MonthlyTicketData.Count];
                
                for (int i = 0; i < MonthlyTicketData.Count; i++)
                {
                    var data = MonthlyTicketData[i];
                    currentYearValues.Add(data.Count);
                    lastYearValues.Add(data.LastYearCount);
                    labels[i] = data.Month;
                }
                
                ((LineSeries)MonthlyTicketSeries[0]).Values = currentYearValues;
                ((LineSeries)MonthlyTicketSeries[1]).Values = lastYearValues;
                MonthlyTicketLabels = labels;
                
                // 更新图表标题
                switch (SelectedTimeRange)
                {
                    case "本周":
                        ((LineSeries)MonthlyTicketSeries[0]).Title = "本周";
                        ((LineSeries)MonthlyTicketSeries[1]).Title = "上周";
                        break;
                    case "本月":
                        ((LineSeries)MonthlyTicketSeries[0]).Title = "本月";
                        ((LineSeries)MonthlyTicketSeries[1]).Title = "上月";
                        break;
                    case "本年":
                        ((LineSeries)MonthlyTicketSeries[0]).Title = "今年";
                        ((LineSeries)MonthlyTicketSeries[1]).Title = "去年";
                        break;
                    case "自定义":
                        // 对于自定义时间范围，根据跨度设置更具体的标题
                        TimeSpan span = EndDate - StartDate;
                        if (span.TotalDays > 365 * 2)
                        {
                            ((LineSeries)MonthlyTicketSeries[0]).Title = $"{StartDate.Year}-{EndDate.Year}";
                            ((LineSeries)MonthlyTicketSeries[1]).Title = $"{StartDate.Year-1}-{EndDate.Year-1}";
                        }
                        else if (span.TotalDays > 365)
                        {
                            ((LineSeries)MonthlyTicketSeries[0]).Title = $"{StartDate.Year}/{EndDate.Year}";
                            ((LineSeries)MonthlyTicketSeries[1]).Title = $"{StartDate.Year-1}/{EndDate.Year-1}";
                        }
                        else if (span.TotalDays > 30)
                        {
                            ((LineSeries)MonthlyTicketSeries[0]).Title = $"{StartDate:yyyy/MM}-{EndDate:yyyy/MM}";
                            ((LineSeries)MonthlyTicketSeries[1]).Title = "去年同期";
                        }
                        else
                        {
                            ((LineSeries)MonthlyTicketSeries[0]).Title = "当前";
                            ((LineSeries)MonthlyTicketSeries[1]).Title = "对比";
                        }
                        break;
                    default:
                        ((LineSeries)MonthlyTicketSeries[0]).Title = "当前";
                        ((LineSeries)MonthlyTicketSeries[1]).Title = "对比";
                        break;
                }
            }
        }
        
        /// <summary>
        /// 加载车票类型数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadTicketTypeData(List<TrainRideInfo> tickets)
        {
            TicketTypeData.Clear();
            OnPropertyChanged(nameof(HasTicketTypeData));
            
            // 定义车票类型判断函数
            bool IsHighSpeedTrain(string trainNo)
            {
                if (string.IsNullOrEmpty(trainNo))
                    return false;
                    
                return trainNo.StartsWith("G") || trainNo.StartsWith("C") || trainNo.StartsWith("D");
            }
            
            bool IsRegularTrain(string trainNo)
            {
                if (string.IsNullOrEmpty(trainNo))
                    return false;
                    
                return trainNo.StartsWith("Z") || trainNo.StartsWith("T") || trainNo.StartsWith("K") || 
                       trainNo.StartsWith("L") || trainNo.StartsWith("S") || char.IsDigit(trainNo[0]);
            }
            
            // 高铁/动车数量
            var highSpeedCount = tickets.Count(t => IsHighSpeedTrain(t.TrainNo));
            
            // 普速车数量
            var regularCount = tickets.Count(t => IsRegularTrain(t.TrainNo));
            
            // 其他类型数量
            var otherCount = tickets.Count - highSpeedCount - regularCount;
            
            // 定义饼图颜色
            var colors = new[]
            {
                Color.FromRgb(124, 77, 255),  // 主色调紫色 #7C4DFF
                Color.FromRgb(0, 176, 255),   // 天蓝色 #00B0FF
                Color.FromRgb(156, 100, 255), // 浅紫色 #9C64FF
                Color.FromRgb(94, 53, 177),   // 深紫色 #5E35B1
                Color.FromRgb(3, 169, 244),   // 蓝色 #03A9F4
                Color.FromRgb(179, 136, 255)  // 淡紫色 #B388FF
            };
            
            // 添加数据
            if (highSpeedCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "高铁/动车",
                    Count = highSpeedCount,
                    Percentage = tickets.Count > 0 ? highSpeedCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(colors[0])
                });
            }
            
            if (regularCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "普速车",
                    Count = regularCount,
                    Percentage = tickets.Count > 0 ? regularCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(colors[1])
                });
            }
            
            if (otherCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "其他",
                    Count = otherCount,
                    Percentage = tickets.Count > 0 ? otherCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(colors[2])
                });
            }
            
            // 更新饼图数据
            if (TicketTypeSeries != null)
            {
                TicketTypeSeries.Clear();
                foreach (var data in TicketTypeData)
                {
                    TicketTypeSeries.Add(new PieSeries
                    {
                        Title = $"{data.TypeName}",
                        Values = new ChartValues<int> { data.Count },
                        DataLabels = false,
                        LabelPoint = chartPoint => $"{data.TypeName}\n{chartPoint.Y}张 ({(int)data.Percentage}%)",
                        Fill = data.Color
                    });
                }
            }
            
            OnPropertyChanged(nameof(HasTicketTypeData));
        }
        
        /// <summary>
        /// 加载月度支出数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadMonthlyExpenseData(List<TrainRideInfo> tickets)
        {
            // 清空集合
            MonthlyExpenseData.Clear();
            
            // 记录调试信息
            System.Diagnostics.Debug.WriteLine($"开始加载月度支出数据，当前时间范围：{SelectedTimeRange}，开始日期：{StartDate:yyyy-MM-dd}，结束日期：{EndDate:yyyy-MM-dd}");
            
            // 检查是否有数据
            bool hasData = tickets != null && tickets.Any(t => t.Money > 0);
            System.Diagnostics.Debug.WriteLine($"费用支出分析：总共有 {tickets?.Count ?? 0} 条车票数据，其中有费用的车票数量：{tickets?.Count(t => t.Money > 0) ?? 0}");
            
            // 只在本周、本月、本年、自定义时间范围显示支出趋势
            if (SelectedTimeRange == "今日")
            {
                // 在今日时间范围下，不显示图表
                // 提示信息已在SetTimeRange方法中设置
                if (ExpenseSeries != null && ExpenseSeries.Count >= 1)
                {
                    ((ColumnSeries)ExpenseSeries[0]).Values = new ChartValues<double> { 0 };
                    ExpenseLabels = new[] { "" };
                }
                System.Diagnostics.Debug.WriteLine($"费用支出分析：今日时间范围下不显示图表");
                return;
            }
            
            // 显示图表
            // 图表显示状态已在SetTimeRange方法或属性设置器中设置
            
            DateTime startDate, endDate;
            string format;
            Func<DateTime, DateTime> getNextStep;
            
            // 根据选择的时间范围确定开始日期、结束日期和日期格式
            switch (SelectedTimeRange)
            {
                case "本周":
                    // 本周数据按天显示
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    endDate = startDate.AddDays(7).AddSeconds(-1);
                    format = "MM/dd";
                    getNextStep = date => date.AddDays(1);
                    break;
                case "本月":
                    // 本月数据按天显示，但可能需要适当跳过一些天
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = startDate.AddMonths(1).AddSeconds(-1);
                    format = "MM/dd";
                    // 如果天数超过15天，则每3天显示一次
                    int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                    getNextStep = daysInMonth > 15 ? (date => date.AddDays(3)) : (date => date.AddDays(1));
                    break;
                case "本年":
                    // 本年数据按月显示
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    endDate = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);
                    format = "yyyy/MM";
                    getNextStep = date => date.AddMonths(1);
                    break;
                case "自定义":
                    // 自定义时间范围，根据时间跨度选择合适的显示方式
                    startDate = StartDate;
                    endDate = EndDate;
                    
                    // 计算时间跨度
                    TimeSpan span = endDate - startDate;
                    
                    if (span.TotalDays <= 1)
                    {
                        // 一天内按小时显示
                        format = "HH:00";
                        getNextStep = date => date.AddHours(1);
                    }
                    else if (span.TotalDays <= 31)
                    {
                        // 一个月内按天显示
                        format = "MM/dd";
                        // 如果天数超过15天，则每3天显示一次
                        getNextStep = span.TotalDays > 15 ? (date => date.AddDays(3)) : (date => date.AddDays(1));
                    }
                    else if (span.TotalDays <= 365)
                    {
                        // 一年内按月显示
                        format = "yyyy/MM";
                        getNextStep = date => date.AddMonths(1);
                    }
                    else
                    {
                        // 超过一年按季度显示
                        format = "yyyy/MM";
                        getNextStep = date => date.AddMonths(3);
                    }
                    break;
                default:
                    // 默认显示最近6个月
                    startDate = DateTime.Today.AddMonths(-6);
                    endDate = DateTime.Today;
                    format = "yyyy/MM";
                    getNextStep = date => date.AddMonths(1);
                    break;
            }
            
            // 如果没有数据，设置HasExpenseData为false
            if (!hasData)
            {
                System.Diagnostics.Debug.WriteLine($"费用支出分析：没有任何费用数据");
                // 清空集合，确保HasExpenseData返回false
                MonthlyExpenseData.Clear();
                OnPropertyChanged(nameof(HasExpenseData));
                return;
            }
            
            // 检查当前时间范围内是否有数据
            var ticketsInRange = tickets.Where(t => t.DepartDate.HasValue && 
                                              t.DepartDate.Value >= startDate && 
                                              t.DepartDate.Value <= endDate).ToList();
            
            System.Diagnostics.Debug.WriteLine($"费用支出分析：当前时间范围内有 {ticketsInRange.Count} 条数据");
            System.Diagnostics.Debug.WriteLine($"费用支出分析：时间范围 {startDate:yyyy-MM-dd} 到 {endDate:yyyy-MM-dd}");
            
            if (!ticketsInRange.Any())
            {
                System.Diagnostics.Debug.WriteLine($"费用支出分析：当前时间范围内没有数据");
                // 清空集合，确保HasExpenseData返回false
                MonthlyExpenseData.Clear();
                OnPropertyChanged(nameof(HasExpenseData));
                return;
            }
            
            // 按时间间隔统计
            for (DateTime date = startDate; date <= endDate; date = getNextStep(date))
            {
                DateTime periodStart = date;
                DateTime periodEnd = getNextStep(date).AddSeconds(-1);
                
                // 如果结束日期超过了总的结束日期，则使用总的结束日期
                if (periodEnd > endDate)
                    periodEnd = endDate;
                
                // 统计当前时间段内的车票数量和支出
                var ticketsInPeriod = tickets.Where(t => t.DepartDate.HasValue && 
                                                   t.DepartDate.Value >= periodStart && 
                                                   t.DepartDate.Value <= periodEnd).ToList();
                
                int count = ticketsInPeriod.Count;
                decimal expense = ticketsInPeriod.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                decimal avgPrice = count > 0 ? expense / count : 0;
                
                System.Diagnostics.Debug.WriteLine($"费用支出分析：时间段 {periodStart:yyyy-MM-dd} 到 {periodEnd:yyyy-MM-dd}，有 {count} 条数据，总支出 {expense}，平均票价 {avgPrice}");
                
                // 即使没有数据，也添加一个记录，确保图表显示完整
                if (count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"费用支出分析：时间段 {periodStart:yyyy-MM-dd} 到 {periodEnd:yyyy-MM-dd} 没有数据，添加空记录");
                }
                
                // 计算同比数据
                DateTime comparisonPeriodStart = DateTime.MinValue;
                DateTime comparisonPeriodEnd = DateTime.MinValue;
                
                if (SelectedTimeRange == "本周")
                {
                    // 上周同期
                    comparisonPeriodStart = periodStart.AddDays(-7);
                    comparisonPeriodEnd = periodEnd.AddDays(-7);
                }
                else if (SelectedTimeRange == "本月")
                {
                    // 上月同期
                    comparisonPeriodStart = periodStart.AddMonths(-1);
                    comparisonPeriodEnd = periodEnd.AddMonths(-1);
                }
                else if (SelectedTimeRange == "本年")
                {
                    // 去年同期
                    comparisonPeriodStart = periodStart.AddYears(-1);
                    comparisonPeriodEnd = periodEnd.AddYears(-1);
                }
                else if (SelectedTimeRange == "自定义")
                {
                    // 自定义时间范围，根据时间跨度选择合适的对比方式
                    TimeSpan span = endDate - startDate;
                    
                    if (span.TotalDays <= 7)
                    {
                        // 一周内，对比上周同期
                        comparisonPeriodStart = periodStart.AddDays(-7);
                        comparisonPeriodEnd = periodEnd.AddDays(-7);
                    }
                    else if (span.TotalDays <= 31)
                    {
                        // 一个月内，对比上月同期
                        comparisonPeriodStart = periodStart.AddMonths(-1);
                        comparisonPeriodEnd = periodEnd.AddMonths(-1);
                    }
                    else
                    {
                        // 超过一个月，对比去年同期
                        comparisonPeriodStart = periodStart.AddYears(-1);
                        comparisonPeriodEnd = periodEnd.AddYears(-1);
                    }
                }
                
                var comparisonTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                     t.DepartDate.Value >= comparisonPeriodStart && 
                                                     t.DepartDate.Value <= comparisonPeriodEnd).ToList();
                
                decimal comparisonExpense = comparisonTickets.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                
                // 上一时间段
                var lastPeriodStart = getNextStep(date.AddDays(-getNextStep(date).Subtract(date).TotalDays * 2));
                var lastPeriodEnd = getNextStep(lastPeriodStart).AddSeconds(-1);
                var lastPeriodTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                      t.DepartDate.Value >= lastPeriodStart && 
                                                      t.DepartDate.Value <= lastPeriodEnd).ToList();
                
                decimal lastPeriodExpense = lastPeriodTickets.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                
                // 计算增长率
                double yearOnYearGrowth = comparisonExpense > 0 ? (double)((expense - comparisonExpense) * 100 / comparisonExpense) : 0;
                double monthOnMonthGrowth = lastPeriodExpense > 0 ? (double)((expense - lastPeriodExpense) * 100 / lastPeriodExpense) : 0;
                
                // 添加数据
                MonthlyExpenseData.Add(new MonthlyExpenseData
                {
                    Month = date.ToString(format),
                    Expense = expense,
                    Budget = BudgetAmount,
                    AvgPrice = avgPrice,
                    YearOnYearGrowth = yearOnYearGrowth,
                    MonthOnMonthGrowth = monthOnMonthGrowth,
                    TicketCount = count
                });
            }
            
            // 更新图表
            UpdateExpenseChart();
            
            System.Diagnostics.Debug.WriteLine($"费用支出分析：共生成 {MonthlyExpenseData.Count} 条数据");
            
            OnPropertyChanged(nameof(HasExpenseData));
        }
        
        /// <summary>
        /// 更新支出图表
        /// </summary>
        private void UpdateExpenseChart()
        {
            System.Diagnostics.Debug.WriteLine($"开始更新支出图表，当前趋势指标：{SelectedTrendIndicator}");
            
            if (MonthlyExpenseData == null || !MonthlyExpenseData.Any())
            {
                System.Diagnostics.Debug.WriteLine($"支出图表更新：没有月度支出数据，清空图表");
                ExpenseSeries = new SeriesCollection();
                ExpenseLabels = new string[0];
                OnPropertyChanged(nameof(ExpenseSeries));
                OnPropertyChanged(nameof(ExpenseLabels));
                return;
            }

            System.Diagnostics.Debug.WriteLine($"支出图表更新：有 {MonthlyExpenseData.Count} 条月度支出数据");
            
            var orderedData = MonthlyExpenseData.ToList();
            
            // 根据不同的时间范围使用不同的排序逻辑
            try
            {
                // 尝试按照yyyy/MM格式排序
                if (orderedData.All(d => d.Month.Contains("/")))
                {
                    if (orderedData[0].Month.Length >= 7 && orderedData[0].Month.Contains("/"))
                    {
                        // 可能是yyyy/MM格式
                        System.Diagnostics.Debug.WriteLine($"支出图表更新：使用yyyy/MM格式排序");
                        orderedData = orderedData.OrderBy(d => d.Month).ToList();
                    }
                    else
                    {
                        // 可能是MM/dd格式
                        System.Diagnostics.Debug.WriteLine($"支出图表更新：使用MM/dd格式排序");
                        orderedData = orderedData.OrderBy(d => 
                        {
                            var parts = d.Month.Split('/');
                            if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int day))
                            {
                                return new DateTime(DateTime.Now.Year, month, day);
                            }
                            return DateTime.MinValue;
                        }).ToList();
                    }
                }
                else if (orderedData.All(d => d.Month.Contains(":")))
                {
                    // 可能是HH:00格式
                    System.Diagnostics.Debug.WriteLine($"支出图表更新：使用HH:00格式排序");
                    orderedData = orderedData.OrderBy(d => 
                    {
                        var parts = d.Month.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int hour))
                        {
                            return hour;
                        }
                        return -1;
                    }).ToList();
                }
                else
                {
                    // 其他格式，直接按字符串排序
                    System.Diagnostics.Debug.WriteLine($"支出图表更新：使用默认字符串排序");
                    orderedData = orderedData.OrderBy(d => d.Month).ToList();
                }
            }
            catch (Exception ex)
            {
                // 如果排序出错，使用原始顺序
                System.Diagnostics.Debug.WriteLine($"排序费用数据时出错，使用原始顺序: {ex.Message}");
            }
                
            ExpenseLabels = orderedData.Select(d => d.Month).ToArray();
            
            var newSeries = new SeriesCollection();
            
            // 根据当前主题选择适当的文本颜色
            var textColor = IsDarkMode ? Colors.White : Colors.Black;
            
            switch (SelectedTrendIndicator)
            {
                case "支出金额":
                    var expenseData = orderedData.Select(d => (double)d.Expense).ToList();
                    var budgetLine = Enumerable.Repeat(BudgetAmount, expenseData.Count).ToList();
                    
                    newSeries.Add(new ColumnSeries
                    {
                        Title = "实际支出",
                        Values = new ChartValues<double>(expenseData),
                        Fill = new SolidColorBrush(Color.FromRgb(124, 77, 255)), // #7C4DFF
                        DataLabels = true,
                        Foreground = new SolidColorBrush(textColor),
                        LabelPoint = point => $"¥{point.Y:N0}"
                    });
                    
                    newSeries.Add(new LineSeries
                    {
                        Title = "预算线",
                        Values = new ChartValues<double>(budgetLine),
                        Stroke = new SolidColorBrush(Color.FromRgb(255, 82, 82)), // #FF5252
                        PointGeometry = null,
                        LineSmoothness = 0
                    });
                    
                    ExpenseYFormatter = value => $"¥{value:N0}";
                    break;
                    
                case "平均票价":
                    var avgPriceData = orderedData.Select(d => (double)d.AvgPrice).ToList();
                    
                    newSeries.Add(new ColumnSeries
                    {
                        Title = "平均票价",
                        Values = new ChartValues<double>(avgPriceData),
                        Fill = new SolidColorBrush(Color.FromRgb(0, 176, 255)), // #00B0FF
                        DataLabels = true,
                        Foreground = new SolidColorBrush(textColor),
                        LabelPoint = point => $"¥{point.Y:N2}"
                    });
                    
                    ExpenseYFormatter = value => $"¥{value:N2}";
                    break;
                    
                case "同比增长":
                    var yearGrowthData = orderedData.Select(d => d.YearOnYearGrowth).ToList();
                    
                    newSeries.Add(new LineSeries
                    {
                        Title = "同比增长",
                        Values = new ChartValues<double>(yearGrowthData),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 124, 77, 255)),
                        Stroke = new SolidColorBrush(Color.FromRgb(124, 77, 255)),
                        DataLabels = true,
                        Foreground = new SolidColorBrush(textColor),
                        LabelPoint = point => $"{point.Y:N1}%"
                    });
                    
                    ExpenseYFormatter = value => $"{value:N1}%";
                    break;
                    
                case "环比增长":
                    var monthGrowthData = orderedData.Select(d => d.MonthOnMonthGrowth).ToList();
                    
                    newSeries.Add(new LineSeries
                    {
                        Title = "环比增长",
                        Values = new ChartValues<double>(monthGrowthData),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 124, 77, 255)),
                        Stroke = new SolidColorBrush(Color.FromRgb(124, 77, 255)),
                        DataLabels = true,
                        Foreground = new SolidColorBrush(textColor),
                        LabelPoint = point => $"{point.Y:N1}%"
                    });
                    
                    ExpenseYFormatter = value => $"{value:N1}%";
                    break;
            }
            
            ExpenseSeries = newSeries;
            
            // 触发属性变更通知
            OnPropertyChanged(nameof(ExpenseSeries));
            OnPropertyChanged(nameof(ExpenseLabels));
            OnPropertyChanged(nameof(ExpenseYFormatter));
            
            System.Diagnostics.Debug.WriteLine($"支出图表更新完成，X轴标签数量：{ExpenseLabels.Length}，数据系列数量：{ExpenseSeries.Count}");
        }
        
        /// <summary>
        /// 加载热门路线数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadTopRouteData(List<TrainRideInfo> tickets)
        {
            TopRouteData.Clear();
            
            try
            {
                // 使用所有车票数据，不受时间筛选影响
                var routeGroups = _allTickets
                    .Where(t => !string.IsNullOrEmpty(t.DepartStation) && 
                               !string.IsNullOrEmpty(t.ArriveStation))
                    .GroupBy(t => new { From = t.DepartStation, To = t.ArriveStation })
                    .Select(g => new
                    {
                        From = g.Key.From,
                        To = g.Key.To,
                        Count = g.Count(),
                        TotalExpense = g.Where(t => t.Money.HasValue).Sum(t => t.Money.Value)
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(5)
                    .ToList(); // 确保立即执行查询
                
                // 添加数据
                foreach (var route in routeGroups)
                {
                    TopRouteData.Add(new RouteData
                    {
                        From = route.From,
                        To = route.To,
                        Count = route.Count,
                        TotalExpense = route.TotalExpense
                    });
                }
                
                // 如果没有数据，添加提示信息
                if (!TopRouteData.Any())
                {
                    TopRouteData.Add(new RouteData
                    {
                        From = "暂无数据",
                        To = "",
                        Count = 0,
                        TotalExpense = 0
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载热门路线数据时出错: {ex.Message}");
                
                // 出错时添加提示信息
                TopRouteData.Add(new RouteData
                {
                    From = "数据加载出错",
                    To = "",
                    Count = 0,
                    TotalExpense = 0
                });
            }
            
            OnPropertyChanged(nameof(HasTopRouteData));
        }
        
        /// <summary>
        /// 加载最近活动
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadRecentActivities(List<TrainRideInfo> tickets)
        {
            RecentActivities.Clear();
            
            // 始终显示最近活动，不受时间范围约束
            _showRecentActivitiesChart = true;
            _recentActivitiesMessage = "";
            OnPropertyChanged(nameof(ShowRecentActivitiesChart));
            OnPropertyChanged(nameof(RecentActivitiesMessage));
            
            // 获取最近10条记录
            var recentTickets = tickets.Where(t => t.DepartDate.HasValue)
                                     .OrderByDescending(t => t.DepartDate)
                                     .Take(10)
                                     .ToList(); // 确保立即执行查询
            
            // 添加数据
            foreach (var ticket in recentTickets)
            {
                RecentActivities.Add(ticket);
            }
            
            // 如果没有数据，显示提示
            if (!RecentActivities.Any())
            {
                var tipTicket = new TrainRideInfo
                {
                    TrainNo = "提示",
                    DepartStation = "暂无数据",
                    ArriveStation = "请添加车票记录",
                    DepartDate = DateTime.Now
                };
                RecentActivities.Add(tipTicket);
            }
        }
        
        /// <summary>
        /// 刷新仪表盘数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"开始刷新数据，当前时间范围：{SelectedTimeRange}，开始日期：{StartDate:yyyy-MM-dd}，结束日期：{EndDate:yyyy-MM-dd}");
                
                // 调用加载仪表盘数据方法
                await LoadDashboardDataAsync();
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"数据刷新完成，总车票数：{TotalTickets}，当前范围车票数：{CurrentRangeTickets}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新数据时出错: {ex.Message}");
                // 可以在这里添加错误提示逻辑
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 停止计时器
            _timer.Stop();
        }
        
        /// <summary>
        /// 设置趋势指标
        /// </summary>
        /// <param name="indicator">趋势指标</param>
        private void SetTrendIndicator(string indicator)
        {
            if (SelectedTrendIndicator == indicator)
                return;
                
            SelectedTrendIndicator = indicator;
            
            // 更新Y轴标题
            UpdateExpenseYTitle();
            
            // 更新图表
            UpdateExpenseChart();
        }
        
        /// <summary>
        /// 更新费用Y轴标题
        /// </summary>
        private void UpdateExpenseYTitle()
        {
            ExpenseYTitle = SelectedTrendIndicator == "支出金额" || SelectedTrendIndicator == "平均票价" ? "金额" : "百分比";
        }
        
        /// <summary>
        /// 切换全屏状态
        /// </summary>
        private void ToggleFullScreen()
        {
            try
            {
                // 获取主窗口引用
                Window mainWindow = Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    Console.WriteLine("无法获取主窗口引用");
                    return;
                }
                
                // 切换全屏状态
                IsFullScreen = !IsFullScreen;
                
                if (IsFullScreen)
                {
                    // 进入全屏模式
                    
                    // 保存当前窗口状态（如果不是最小化）
                    if (mainWindow.WindowState != WindowState.Minimized)
                    {
                        PreviousWindowState = mainWindow.WindowState;
                    }
                    
                    // 保存当前窗口位置和大小
                    _savedLeft = mainWindow.RestoreBounds.Left;
                    _savedTop = mainWindow.RestoreBounds.Top;
                    _savedWidth = mainWindow.RestoreBounds.Width;
                    _savedHeight = mainWindow.RestoreBounds.Height;
                    
                    Console.WriteLine($"进入全屏模式：保存之前的窗口状态 {PreviousWindowState}，位置：({_savedLeft}, {_savedTop})，大小：{_savedWidth}x{_savedHeight}");
                    
                    // 设置为全屏模式（无标题栏，最大化）
                    mainWindow.WindowStyle = WindowStyle.None; // 无边框
                    mainWindow.ResizeMode = ResizeMode.NoResize; // 禁止调整大小
                    mainWindow.Topmost = true; // 置顶显示
                    
                    // 获取屏幕尺寸
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    // 设置窗口位置和大小为整个屏幕
                    mainWindow.WindowState = WindowState.Normal; // 先设为普通状态
                    mainWindow.Left = 0;
                    mainWindow.Top = 0;
                    mainWindow.Width = screenWidth;
                    mainWindow.Height = screenHeight;
                    
                    Console.WriteLine($"设置窗口为完全全屏模式：宽度={screenWidth}, 高度={screenHeight}");
                }
                else
                {
                    // 退出全屏模式
                    Console.WriteLine($"退出全屏模式：恢复窗口状态为 {PreviousWindowState}，位置：({_savedLeft}, {_savedTop})，大小：{_savedWidth}x{_savedHeight}");
                    
                    // 恢复窗口样式和状态
                    mainWindow.WindowStyle = WindowStyle.SingleBorderWindow; // 显示标题栏
                    mainWindow.ResizeMode = ResizeMode.CanResize; // 允许调整大小
                    mainWindow.Topmost = false; // 取消置顶
                    
                    // 根据之前的窗口状态决定如何恢复
                    if (PreviousWindowState == WindowState.Maximized)
                    {
                        // 如果之前是最大化状态，直接设置为最大化，避免闪烁
                        mainWindow.WindowState = WindowState.Maximized;
                        Console.WriteLine("直接恢复到最大化状态，避免闪烁");
                    }
                    else
                    {
                        // 如果之前不是最大化状态，恢复到之前的位置和大小
                        mainWindow.WindowState = WindowState.Normal; // 先设为普通状态
                        
                        // 使用 Dispatcher 延迟设置窗口位置和大小，确保 WindowStyle 更改已应用
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                mainWindow.Left = _savedLeft;
                                mainWindow.Top = _savedTop;
                                mainWindow.Width = _savedWidth;
                                mainWindow.Height = _savedHeight;
                                
                                Console.WriteLine($"完成恢复窗口位置和大小：({mainWindow.Left}, {mainWindow.Top}), {mainWindow.Width}x{mainWindow.Height}, 状态: {mainWindow.WindowState}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"恢复窗口位置和大小时出错: {ex.Message}");
                            }
                        }), DispatcherPriority.Render);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ToggleFullScreen异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 重写OnPropertyChanged方法，在IsDarkMode属性变化时更新图表
        /// </summary>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            
            // 当主题变化时更新图表
            if (propertyName == nameof(IsDarkMode))
            {
                UpdateExpenseChart();
                
                // 更新车票类型图表
                if (TicketTypeData != null && TicketTypeData.Any())
                {
                    // 通知视图更新车票类型图表
                    OnPropertyChanged(nameof(TicketTypeSeries));
                }
            }
        }
    }
    
    /// <summary>
    /// 月度车票数据
    /// </summary>
    public class MonthlyTicketData
    {
        /// <summary>
        /// 月份
        /// </summary>
        public string Month { get; set; }
        
        /// <summary>
        /// 车票数量
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 去年同期数量
        /// </summary>
        public int LastYearCount { get; set; }
        
        /// <summary>
        /// 环比增长率
        /// </summary>
        public double MonthOnMonthGrowth { get; set; }
        
        /// <summary>
        /// 同比增长率
        /// </summary>
        public double YearOnYearGrowth { get; set; }
    }
    
    /// <summary>
    /// 车票类型数据
    /// </summary>
    public class TicketTypeData
    {
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }
        
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 百分比
        /// </summary>
        public double Percentage { get; set; }
        
        /// <summary>
        /// 颜色
        /// </summary>
        public Brush Color { get; set; }
    }
    
    /// <summary>
    /// 月度支出数据
    /// </summary>
    public class MonthlyExpenseData
    {
        /// <summary>
        /// 月份
        /// </summary>
        public string Month { get; set; }
        
        /// <summary>
        /// 支出金额
        /// </summary>
        public decimal Expense { get; set; }
        
        /// <summary>
        /// 预算金额
        /// </summary>
        public double Budget { get; set; }
        
        /// <summary>
        /// 平均票价
        /// </summary>
        public decimal AvgPrice { get; set; }
        
        /// <summary>
        /// 同比增长率
        /// </summary>
        public double YearOnYearGrowth { get; set; }
        
        /// <summary>
        /// 环比增长率
        /// </summary>
        public double MonthOnMonthGrowth { get; set; }
        
        /// <summary>
        /// 车票数量
        /// </summary>
        public int TicketCount { get; set; }
    }
    
    /// <summary>
    /// 路线数据
    /// </summary>
    public class RouteData
    {
        /// <summary>
        /// 出发站
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// 到达站
        /// </summary>
        public string To { get; set; }
        
        /// <summary>
        /// 次数
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 总支出
        /// </summary>
        public decimal TotalExpense { get; set; }
        
        /// <summary>
        /// 路线名称
        /// </summary>
        public string RouteName => $"{From} → {To}";
    }
} 