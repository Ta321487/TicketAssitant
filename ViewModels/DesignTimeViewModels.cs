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
                    TrainNo = "G456",
                    ArriveStation = "广州",
                    ArriveStationPinyin = "GUANGZHOU",
                    DepartDate = DateTime.Now.AddDays(1),
                    DepartTime = TimeSpan.FromHours(14),
                    CoachNo = "02",
                    SeatNo = "05B",
                    Money = 463.0m,
                    SeatType = "二等座",
                    AdditionalInfo = "无",
                    TicketPurpose = "乘车",
                    Hint = "请提前到达车站"
                }
            };

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 