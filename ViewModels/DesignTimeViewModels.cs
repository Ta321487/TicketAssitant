using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TA_WPF.Models;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 设计时使用的 ViewModel 类，用于在设计视图中显示数据
    /// </summary>
    public class DesignTimeQueryAllTicketsViewModel : INotifyPropertyChanged
    {
        // 静态实例，确保设计时数据上下文的稳定性
        public static readonly DesignTimeQueryAllTicketsViewModel Instance = new DesignTimeQueryAllTicketsViewModel();
        
        private ObservableCollection<TrainRideInfo> _trainRideInfos;
        private int _currentPage = 1;
        private int _totalPages = 10;
        private int _totalItems = 100;
        private bool _isLoading = false;
        private int _pageSize = 10;
        private ObservableCollection<int> _pageSizeOptions;
        private int _dataGridRowHeight = 40;

        public DesignTimeQueryAllTicketsViewModel()
        {
            // 初始化设计时数据
            _trainRideInfos = new ObservableCollection<TrainRideInfo>
            {
                new TrainRideInfo
                {
                    TicketNumber = "E123456789",
                    CheckInLocation = "1号检票口",
                    DepartStation = "北京",
                    DepartStationPinyin = "BEIJING",
                    TrainNo = "G123",
                    ArriveStation = "上海",
                    ArriveStationPinyin = "SHANGHAI",
                    DepartDate = DateTime.Now,
                    DepartTime = TimeSpan.FromHours(10),
                    CoachNo = "01",
                    SeatNo = "01A",
                    Money = 553.5m,
                    SeatType = "一等座",
                    AdditionalInfo = "无",
                    TicketPurpose = "乘车",
                    Hint = "请提前到达车站"
                },
                new TrainRideInfo
                {
                    TicketNumber = "E987654321",
                    CheckInLocation = "2号检票口",
                    DepartStation = "上海",
                    DepartStationPinyin = "SHANGHAI",
                    TrainNo = "G321",
                    ArriveStation = "北京",
                    ArriveStationPinyin = "BEIJING",
                    DepartDate = DateTime.Now.AddDays(1),
                    DepartTime = TimeSpan.FromHours(14),
                    CoachNo = "02",
                    SeatNo = "02B",
                    Money = 553.5m,
                    SeatType = "二等座",
                    AdditionalInfo = "无",
                    TicketPurpose = "乘车",
                    Hint = "请提前到达车站"
                }
            };
            
            // 初始化页大小选项
            _pageSizeOptions = new ObservableCollection<int> { 10, 20, 50 };
        }

        public ObservableCollection<TrainRideInfo> TrainRideInfos
        {
            get => _trainRideInfos;
            set
            {
                _trainRideInfos = value;
                OnPropertyChanged();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                _pageSize = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<int> PageSizeOptions
        {
            get => _pageSizeOptions;
            set
            {
                _pageSizeOptions = value;
                OnPropertyChanged();
            }
        }

        public int DataGridRowHeight
        {
            get => _dataGridRowHeight;
            set
            {
                _dataGridRowHeight = value;
                OnPropertyChanged();
            }
        }

        public bool CanNavigateToFirstPage => CurrentPage > 1;
        public bool CanNavigateToPreviousPage => CurrentPage > 1;
        public bool CanNavigateToNextPage => CurrentPage < TotalPages;
        public bool CanNavigateToLastPage => CurrentPage < TotalPages;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 设计时使用的高级查询ViewModel类，用于在设计视图中显示数据
    /// </summary>
    public class DesignTimeAdvancedQueryTicketViewModel : INotifyPropertyChanged
    {
        // 静态实例，确保设计时数据上下文的稳定性
        public static readonly DesignTimeAdvancedQueryTicketViewModel Instance = new DesignTimeAdvancedQueryTicketViewModel();
        
        private bool _isQueryPanelVisible = true;
        private string _trainNumberFilter = "1234";
        private string _selectedTrainPrefix = "G";
        private List<string> _trainPrefixes = new List<string> { "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字" };
        private DepartStationItem _selectedDepartStation;
        private YearOption? _selectedYearOption;
        private List<YearOption> _yearOptions = new();
        private ObservableCollection<DepartStationItem> _departStations = new();
        private bool _isAndCondition = true;
        private bool _isOrCondition;
        private bool _hasActiveFilters = true;
        private string _departStationSearchText = "北京";
        private ObservableCollection<StationInfo> _departStationSuggestions = new();
        
        public DesignTimeAdvancedQueryTicketViewModel()
        {
            // 初始化年份选项
            int currentYear = DateTime.Now.Year;
            _selectedDepartStation = new DepartStationItem("北京");
            
            // 初始化年份选项
            _yearOptions = new List<YearOption>
            {
                new YearOption(null, "不筛选"),
                new YearOption(currentYear, $"{currentYear}年"),
                new YearOption(currentYear - 1, $"{currentYear - 1}年"),
                new YearOption(currentYear - 2, $"{currentYear - 2}年"),
                new YearOption(currentYear - 3, $"{currentYear - 3}年"),
                new YearOption(null, "自定义年份", true)
            };
            _selectedYearOption = _yearOptions[1]; // 选择当前年
            
            // 初始化出发站列表
            _departStations = new ObservableCollection<DepartStationItem>
            {
                new DepartStationItem("北京"),
                new DepartStationItem("上海"),
                new DepartStationItem("广州"),
                new DepartStationItem("深圳")
            };
            
            // 初始化站点建议列表
            _departStationSuggestions = new ObservableCollection<StationInfo>
            {
                new StationInfo { StationName = "北京站", StationPinyin = "BEIJINGZHAN" },
                new StationInfo { StationName = "北京西站", StationPinyin = "BEIJINGXIZHAN" },
                new StationInfo { StationName = "北京南站", StationPinyin = "BEIJINGNANZHAN" }
            };
        }
        
        public bool IsQueryPanelVisible
        {
            get => _isQueryPanelVisible;
            set
            {
                _isQueryPanelVisible = value;
                OnPropertyChanged();
            }
        }
        
        public string TrainNumberFilter
        {
            get => _trainNumberFilter;
            set
            {
                _trainNumberFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QueryButtonText));
            }
        }
        
        public string SelectedTrainPrefix
        {
            get => _selectedTrainPrefix;
            set
            {
                _selectedTrainPrefix = value;
                OnPropertyChanged();
            }
        }
        
        public List<string> TrainPrefixes
        {
            get => _trainPrefixes;
            set
            {
                _trainPrefixes = value;
                OnPropertyChanged();
            }
        }
        
        public DepartStationItem SelectedDepartStation
        {
            get => _selectedDepartStation;
            set
            {
                _selectedDepartStation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QueryButtonText));
            }
        }
        
        public YearOption? SelectedYearOption
        {
            get => _selectedYearOption;
            set
            {
                _selectedYearOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QueryButtonText));
            }
        }
        
        public List<YearOption> YearOptions
        {
            get => _yearOptions;
            set
            {
                _yearOptions = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<DepartStationItem> DepartStations
        {
            get => _departStations;
            set
            {
                _departStations = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<StationInfo> DepartStationSuggestions
        {
            get => _departStationSuggestions;
            set
            {
                _departStationSuggestions = value;
                OnPropertyChanged();
            }
        }
        
        public string DepartStationSearchText
        {
            get => _departStationSearchText;
            set
            {
                _departStationSearchText = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsDepartStationDropdownOpen { get; set; } = false;
        
        public bool IsAndCondition
        {
            get => _isAndCondition;
            set
            {
                _isAndCondition = value;
                _isOrCondition = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOrCondition));
            }
        }
        
        public bool IsOrCondition
        {
            get => _isOrCondition;
            set
            {
                _isOrCondition = value;
                _isAndCondition = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAndCondition));
            }
        }
        
        public bool HasActiveFilters
        {
            get => _hasActiveFilters;
            set
            {
                _hasActiveFilters = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QueryButtonText));
            }
        }
        
        public string QueryButtonText
        {
            get => HasActiveFilters ? "查询" : "查询全部";
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 