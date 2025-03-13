using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
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
        private readonly DispatcherTimer _timer;
        
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
        private double _budgetAmount = 2000;
        
        // 图表相关属性
        private SeriesCollection _monthlyTicketSeries;
        private string[] _monthlyTicketLabels;
        private Func<double, string> _monthlyTicketYFormatter;
        
        private SeriesCollection _ticketTypeSeries;
        
        private SeriesCollection _expenseSeries;
        private string[] _expenseLabels;
        private Func<double, string> _expenseYFormatter;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public DashboardViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
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
            
            // 创建并启动计时器，每秒更新一次时间
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
            _timer.Start();
            
            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            TimeRangeCommand = new RelayCommand<string>(SetTimeRange);
            ShowTicketTypeDetailsCommand = new RelayCommand<TicketTypeData>(ShowTicketTypeDetails);
            
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
                
                // 获取所有车票数据
                var allTickets = await _databaseService.GetAllTrainRideInfosAsync();
                
                // 计算总车票数
                TotalTickets = allTickets.Count;
                
                // 计算当前时间范围内的车票数
                CurrentRangeTickets = allTickets.Count(t => t.DepartDate >= StartDate && t.DepartDate <= EndDate);
                
                // 计算最经常出发的车站
                if (allTickets.Any())
                {
                    var stationGroups = allTickets
                        .Where(t => !string.IsNullOrEmpty(t.DepartStation))
                        .GroupBy(t => t.DepartStation)
                        .OrderByDescending(g => g.Count());
                    
                    MostFrequentDepartureStation = stationGroups.FirstOrDefault()?.Key ?? "无数据";
                    
                    // 获取最后一次出发的车站（按出发日期排序）
                    var lastTicket = allTickets
                        .Where(t => t.DepartDate.HasValue && !string.IsNullOrEmpty(t.DepartStation))
                        .OrderByDescending(t => t.DepartDate)
                        .FirstOrDefault();
                    
                    LastDepartureStation = lastTicket?.DepartStation ?? "无数据";
                    
                    // 加载图表数据
                    await LoadChartDataAsync(allTickets);
                    
                    // 加载最近活动
                    LoadRecentActivities(allTickets);
                }
                else
                {
                    MostFrequentDepartureStation = "无数据";
                    LastDepartureStation = "无数据";
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                System.Diagnostics.Debug.WriteLine($"加载仪表盘数据时出错: {ex.Message}");
                MostFrequentDepartureStation = "加载失败";
                LastDepartureStation = "加载失败";
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
            
            // 添加数据
            if (highSpeedCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "高铁/动车",
                    Count = highSpeedCount,
                    Percentage = tickets.Count > 0 ? highSpeedCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(Colors.Red)
                });
            }
            
            if (regularCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "普速车",
                    Count = regularCount,
                    Percentage = tickets.Count > 0 ? regularCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(Colors.Green)
                });
            }
            
            if (otherCount > 0)
            {
                TicketTypeData.Add(new TicketTypeData
                {
                    TypeName = "其他",
                    Count = otherCount,
                    Percentage = tickets.Count > 0 ? otherCount * 100.0 / tickets.Count : 0,
                    Color = new SolidColorBrush(Colors.Gray)
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
                        Title = $"{data.TypeName} ({data.Count})",
                        Values = new ChartValues<int> { data.Count },
                        DataLabels = true,
                        LabelPoint = chartPoint => $"{data.TypeName}: {chartPoint.Y} ({data.Percentage:F1}%)",
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
            OnPropertyChanged(nameof(HasExpenseData));
            
            // 获取最近12个月的数据
            var endMonth = DateTime.Today;
            var startMonth = endMonth.AddMonths(-11);
            
            // 按月份分组统计
            for (var date = startMonth; date <= endMonth; date = date.AddMonths(1))
            {
                var year = date.Year;
                var month = date.Month;
                
                // 当月支出
                var expense = tickets.Where(t => t.DepartDate.HasValue && 
                                              t.DepartDate.Value.Year == year && 
                                              t.DepartDate.Value.Month == month && 
                                              t.Money.HasValue)
                                   .Sum(t => t.Money.Value);
                
                var monthData = new MonthlyExpenseData
                {
                    Month = $"{year}/{month}",
                    Expense = expense,
                    Budget = BudgetAmount
                };
                
                MonthlyExpenseData.Add(monthData);
            }
            
            // 更新支出图表
            UpdateExpenseChart();
            
            OnPropertyChanged(nameof(HasExpenseData));
        }
        
        /// <summary>
        /// 加载热门路线数据
        /// </summary>
        /// <param name="tickets">车票数据</param>
        private void LoadTopRouteData(List<TrainRideInfo> tickets)
        {
            TopRouteData.Clear();
            OnPropertyChanged(nameof(HasTopRouteData));
            
            // 按路线分组统计
            var routeGroups = tickets.Where(t => !string.IsNullOrEmpty(t.DepartStation) && 
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
            await LoadDashboardDataAsync();
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
        /// 更新支出图表
        /// </summary>
        private void UpdateExpenseChart()
        {
            if (MonthlyExpenseData == null || !MonthlyExpenseData.Any() || ExpenseSeries == null || ExpenseSeries.Count < 2)
                return;

            var expenseData = MonthlyExpenseData
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Expense)
                .ToList();

            var budgetLine = Enumerable.Repeat(BudgetAmount, expenseData.Count()).ToList();

            ((ChartValues<decimal>)ExpenseSeries[0].Values).Clear();
            ((ChartValues<decimal>)ExpenseSeries[0].Values).AddRange(expenseData);

            ((ChartValues<double>)ExpenseSeries[1].Values).Clear();
            ((ChartValues<double>)ExpenseSeries[1].Values).AddRange(budgetLine);
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