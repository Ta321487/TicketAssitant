using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using LiveCharts;
using LiveCharts.Wpf;

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
        
        // 图表相关属性
        private SeriesCollection _monthlyTicketSeries;
        private string[] _monthlyTicketLabels;
        private Func<double, string> _monthlyTicketYFormatter;
        
        private SeriesCollection _ticketTypeSeries;
        
        private SeriesCollection _expenseSeries;
        private string[] _expenseLabels;
        private Func<double, string> _expenseYFormatter;
        
        private string _selectedAnalysisType = "支出趋势";
        private string _selectedTrendIndicator = "支出金额";
        private string _selectedStructureDimension = "座位类型";
        private SeriesCollection _expenseStructureSeries;
        private bool _hasExpenseStructureData;
        private string _expenseYTitle = "金额";
        
        private bool _isFullScreen;
        private WindowState _previousWindowState = WindowState.Normal;
        
        // 保存窗口的位置和大小信息
        private double _savedLeft;
        private double _savedTop;
        private double _savedWidth;
        private double _savedHeight;
        
        /// <summary>
        /// 构造函数
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
            
            // 设置默认时间范围为今日
            SetTimeRange(_selectedTimeRange);
            
            // 设置默认分析类型和指标
            _selectedAnalysisType = "支出趋势";
            _selectedTrendIndicator = "支出金额";
            _selectedStructureDimension = "座位类型";
            
            // 更新Y轴标题
            UpdateExpenseYTitle();
            
            // 创建并启动计时器，每秒更新一次时间
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
            _timer.Start();
            
            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            TimeRangeCommand = new RelayCommand<string>(SetTimeRange);
            ShowTicketTypeDetailsCommand = new RelayCommand<TicketTypeData>(ShowTicketTypeDetails);
            AnalysisTypeCommand = new RelayCommand<string>(SetAnalysisType);
            SelectTrendIndicatorCommand = new RelayCommand<string>(SetTrendIndicator);
            SelectStructureDimensionCommand = new RelayCommand<string>(SetStructureDimension);
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
        /// 选中的分析类型
        /// </summary>
        public string SelectedAnalysisType
        {
            get => _selectedAnalysisType;
            set
            {
                if (_selectedAnalysisType != value)
                {
                    _selectedAnalysisType = value;
                    OnPropertyChanged(nameof(SelectedAnalysisType));
                    OnPropertyChanged(nameof(IsTrendAnalysis));
                    OnPropertyChanged(nameof(IsStructureAnalysis));
                }
            }
        }
        
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
        /// 选中的结构维度
        /// </summary>
        public string SelectedStructureDimension
        {
            get => _selectedStructureDimension;
            set
            {
                if (_selectedStructureDimension != value)
                {
                    _selectedStructureDimension = value;
                    OnPropertyChanged(nameof(SelectedStructureDimension));
                    RefreshDataAsync();
                }
            }
        }
        
        /// <summary>
        /// 是否为趋势分析
        /// </summary>
        public bool IsTrendAnalysis => SelectedAnalysisType == "支出趋势";
        
        /// <summary>
        /// 是否为结构分析
        /// </summary>
        public bool IsStructureAnalysis => SelectedAnalysisType == "消费结构";
        
        /// <summary>
        /// 费用支出结构数据图表
        /// </summary>
        public SeriesCollection ExpenseStructureSeries
        {
            get => _expenseStructureSeries;
            set
            {
                if (_expenseStructureSeries != value)
                {
                    _expenseStructureSeries = value;
                    OnPropertyChanged(nameof(ExpenseStructureSeries));
                }
            }
        }
        
        /// <summary>
        /// 是否有费用结构数据
        /// </summary>
        public bool HasExpenseStructureData
        {
            get => _hasExpenseStructureData;
            set
            {
                if (_hasExpenseStructureData != value)
                {
                    _hasExpenseStructureData = value;
                    OnPropertyChanged(nameof(HasExpenseStructureData));
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
        /// 分析类型命令
        /// </summary>
        public ICommand AnalysisTypeCommand { get; private set; }
        
        /// <summary>
        /// 趋势指标选择命令
        /// </summary>
        public ICommand SelectTrendIndicatorCommand { get; private set; }
        
        /// <summary>
        /// 结构维度选择命令
        /// </summary>
        public ICommand SelectStructureDimensionCommand { get; private set; }
        
        /// <summary>
        /// 切换全屏命令
        /// </summary>
        public ICommand ToggleFullScreenCommand { get; }
        
        /// <summary>
        /// 设置时间范围
        /// </summary>
        /// <param name="range">时间范围</param>
        private void SetTimeRange(string range)
        {
            if (string.IsNullOrEmpty(range))
                return;
                
            SelectedTimeRange = range;
            
            switch (range)
            {
                case "今日":
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
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
                    StartDate = DateTime.Today.AddMonths(-6);
                    EndDate = DateTime.Today;
                    break;
            }

            OnPropertyChanged(nameof(CurrentRangeText));
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
                
                // 从数据库加载所有车票数据
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
                
                // 加载最近活动
                LoadRecentActivities(_allTickets);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载仪表盘数据时出错: {ex.Message}");
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
            // 筛选时间范围内的车票
            var filteredTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                   t.DepartDate.Value >= StartDate && 
                                                   t.DepartDate.Value <= EndDate).ToList();
            
            // 加载月度车票数据
            LoadMonthlyTicketData(filteredTickets);
            
            // 加载车票类型数据
            LoadTicketTypeData(filteredTickets);
            
            // 加载月度支出数据
            LoadMonthlyExpenseData(filteredTickets);
            
            // 加载热门路线数据
            LoadTopRouteData(filteredTickets);
        }
        
        /// <summary>
        /// 加载月度车票数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadMonthlyTicketData(List<TrainRideInfo> tickets)
        {
            MonthlyTicketData.Clear();
            
            // 获取最近6个月的数据
            var endMonth = DateTime.Today;
            var startMonth = endMonth.AddMonths(-5);
            
            // 按月份分组统计
            for (var date = startMonth; date <= endMonth; date = date.AddMonths(1))
            {
                var year = date.Year;
                var month = date.Month;
                
                // 当月车票数
                var count = tickets.Count(t => t.DepartDate.HasValue && 
                                             t.DepartDate.Value.Year == year && 
                                             t.DepartDate.Value.Month == month);
                
                // 去年同期车票数
                var lastYearCount = tickets.Count(t => t.DepartDate.HasValue && 
                                                    t.DepartDate.Value.Year == year - 1 && 
                                                    t.DepartDate.Value.Month == month);
                
                // 环比（与上个月相比）
                var lastMonth = month == 1 ? 12 : month - 1;
                var lastMonthYear = month == 1 ? year - 1 : year;
                var lastMonthCount = tickets.Count(t => t.DepartDate.HasValue && 
                                                     t.DepartDate.Value.Year == lastMonthYear && 
                                                     t.DepartDate.Value.Month == lastMonth);
                
                var monthData = new MonthlyTicketData
                {
                    Month = $"{year}/{month}",
                    Count = count,
                    LastYearCount = lastYearCount,
                    MonthOnMonthGrowth = lastMonthCount > 0 ? (count - lastMonthCount) * 100.0 / lastMonthCount : 0,
                    YearOnYearGrowth = lastYearCount > 0 ? (count - lastYearCount) * 100.0 / lastYearCount : 0
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
            MonthlyExpenseData.Clear();
            
            // 获取最近12个月的数据
            var endMonth = DateTime.Today;
            var startMonth = endMonth.AddMonths(-11);
            
            // 按月份分组统计
            for (var date = startMonth; date <= endMonth; date = date.AddMonths(1))
            {
                var year = date.Year;
                var month = date.Month;
                
                // 当月车票
                var monthTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                   t.DepartDate.Value.Year == year && 
                                                   t.DepartDate.Value.Month == month).ToList();
                
                // 当月支出
                var expense = monthTickets.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                
                // 当月平均票价
                var avgPrice = monthTickets.Count > 0 && monthTickets.Any(t => t.Money.HasValue) 
                    ? monthTickets.Where(t => t.Money.HasValue).Average(t => t.Money.Value) 
                    : 0;
                
                // 去年同期支出
                var lastYearTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                      t.DepartDate.Value.Year == year - 1 && 
                                                      t.DepartDate.Value.Month == month).ToList();
                var lastYearExpense = lastYearTickets.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                
                // 计算同比增长
                var yearOnYearGrowth = lastYearExpense > 0 
                    ? ((double)expense / (double)lastYearExpense - 1) * 100 
                    : 0;
                
                // 上月支出
                var lastMonth = date.AddMonths(-1);
                var lastMonthTickets = tickets.Where(t => t.DepartDate.HasValue && 
                                                      t.DepartDate.Value.Year == lastMonth.Year && 
                                                      t.DepartDate.Value.Month == lastMonth.Month).ToList();
                var lastMonthExpense = lastMonthTickets.Where(t => t.Money.HasValue).Sum(t => t.Money.Value);
                
                // 计算环比增长
                var monthOnMonthGrowth = lastMonthExpense > 0 
                    ? ((double)expense / (double)lastMonthExpense - 1) * 100 
                    : 0;
                
                MonthlyExpenseData.Add(new MonthlyExpenseData
                {
                    Month = $"{year}/{month:D2}",
                    Expense = expense,
                    Budget = BudgetAmount,
                    AvgPrice = avgPrice,
                    YearOnYearGrowth = yearOnYearGrowth,
                    MonthOnMonthGrowth = monthOnMonthGrowth,
                    TicketCount = monthTickets.Count
                });
            }
            
            OnPropertyChanged(nameof(HasExpenseData));
            
            // 更新图表
            UpdateExpenseChart();
        }
        
        /// <summary>
        /// 加载消费结构数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadExpenseStructureData(List<TrainRideInfo> tickets)
        {
            // 筛选有效票
            var validTickets = tickets.Where(t => t.Money.HasValue && 
                                               t.DepartDate.HasValue && 
                                               t.DepartDate.Value >= StartDate && 
                                               t.DepartDate.Value <= EndDate).ToList();
            
            HasExpenseStructureData = validTickets.Any();
            
            if (!HasExpenseStructureData)
                return;
                
            // 定义饼图颜色
            var colors = new[]
            {
                Color.FromRgb(124, 77, 255),   // 主色调紫色 #7C4DFF
                Color.FromRgb(0, 176, 255),    // 天蓝色 #00B0FF
                Color.FromRgb(156, 100, 255),  // 浅紫色 #9C64FF
                Color.FromRgb(94, 53, 177),    // 深紫色 #5E35B1
                Color.FromRgb(3, 169, 244),    // 蓝色 #03A9F4
                Color.FromRgb(179, 136, 255),  // 淡紫色 #B388FF
                Color.FromRgb(0, 137, 123),    // 青色 #00897B
                Color.FromRgb(255, 87, 34),    // 橙色 #FF5722
                Color.FromRgb(255, 193, 7),    // 琥珀色 #FFC107
                Color.FromRgb(139, 195, 74)    // 浅绿色 #8BC34A
            };
            
            ExpenseStructureSeries = new SeriesCollection();
            
            // 按座位类型分组
            var seatGroups = validTickets
                .GroupBy(t => string.IsNullOrEmpty(t.SeatType) ? "未知" : t.SeatType)
                .Select(g => new
                {
                    SeatType = g.Key,
                    TotalExpense = g.Sum(t => t.Money.Value),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.TotalExpense)
                .ToList();
            
            // 计算总支出
            var totalExpense = seatGroups.Sum(g => g.TotalExpense);
            
            // 添加饼图数据
            for (int i = 0; i < seatGroups.Count; i++)
            {
                var group = seatGroups[i];
                var percentage = totalExpense > 0 ? group.TotalExpense * 100 / totalExpense : 0;
                
                ExpenseStructureSeries.Add(new PieSeries
                {
                    Title = group.SeatType,
                    Values = new ChartValues<decimal> { group.TotalExpense },
                    DataLabels = false,
                    LabelPoint = chartPoint => $"{group.SeatType}\n¥{group.TotalExpense:N2} ({(int)percentage}%)",
                    Fill = new SolidColorBrush(colors[i % colors.Length])
                });
            }
        }
        
        /// <summary>
        /// 更新支出图表
        /// </summary>
        private void UpdateExpenseChart()
        {
            if (MonthlyExpenseData == null || !MonthlyExpenseData.Any())
            {
                ExpenseSeries = new SeriesCollection();
                ExpenseLabels = new string[0];
                OnPropertyChanged(nameof(ExpenseSeries));
                OnPropertyChanged(nameof(ExpenseLabels));
                return;
            }

            var orderedData = MonthlyExpenseData
                .OrderBy(d => DateTime.ParseExact(d.Month, "yyyy/MM", null))
                .ToList();
                
            ExpenseLabels = orderedData.Select(d => d.Month).ToArray();
            
            var newSeries = new SeriesCollection();
            
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
                    
                case "同比增长":
                    var yearGrowthData = orderedData.Select(d => d.YearOnYearGrowth).ToList();
                    
                    newSeries.Add(new LineSeries
                    {
                        Title = "同比增长",
                        Values = new ChartValues<double>(yearGrowthData),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 124, 77, 255)),
                        Stroke = new SolidColorBrush(Color.FromRgb(124, 77, 255)),
                        DataLabels = true,
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
        }
        
        /// <summary>
        /// 加载热门路线数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadTopRouteData(List<TrainRideInfo> tickets)
        {
            TopRouteData.Clear();
            OnPropertyChanged(nameof(HasTopRouteData));
            
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
                    .Take(5);
                
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载热门路线数据时出错: {ex.Message}");
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
            
            // 获取最近10条记录
            var recentTickets = tickets.Where(t => t.DepartDate.HasValue)
                                     .OrderByDescending(t => t.DepartDate)
                                     .Take(10)
                                     .ToList();
            
            // 添加数据
            foreach (var ticket in recentTickets)
            {
                RecentActivities.Add(ticket);
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
                await LoadDashboardDataAsync();
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
        /// 设置分析类型
        /// </summary>
        /// <param name="type">分析类型</param>
        private void SetAnalysisType(string type)
        {
            if (SelectedAnalysisType == type)
                return;
                
            SelectedAnalysisType = type;
            OnPropertyChanged(nameof(IsTrendAnalysis));
            OnPropertyChanged(nameof(IsStructureAnalysis));
            
            // 更新图表
            if (IsTrendAnalysis)
            {
                UpdateExpenseChart();
            }
            else if (IsStructureAnalysis)
            {
                // 加载消费结构数据
                Task.Run(async () => {
                    var tickets = await _databaseService.GetAllTrainRideInfosAsync();
                    LoadExpenseStructureData(tickets);
                });
            }
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
        /// 设置结构维度
        /// </summary>
        /// <param name="dimension">结构维度</param>
        private void SetStructureDimension(string dimension)
        {
            if (SelectedStructureDimension == dimension)
                return;
                
            SelectedStructureDimension = dimension;
            
            // 更新消费结构数据
            Task.Run(async () => {
                var tickets = await _databaseService.GetAllTrainRideInfosAsync();
                LoadExpenseStructureData(tickets);
            });
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