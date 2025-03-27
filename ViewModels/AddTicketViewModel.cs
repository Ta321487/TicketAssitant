using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class AddTicketViewModel : PaymentChannelViewModel
    {
        protected readonly DatabaseService _databaseService;
        protected readonly StationSearchService _stationSearchService;
        protected readonly MainViewModel _mainViewModel;
        protected readonly List<string> _validationErrors = new List<string>();

        // 添加字体大小变化监听
        private readonly FontSizeChangeListener _fontSizeChangeListener;
        
        // 添加表单修改状态跟踪
        private bool _isFormModified = false;
        private bool _isInitializing = true;
        private bool _needToRefreshData = false;

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        public event EventHandler CloseWindow;

        /// <summary>
        /// 触发窗口关闭事件
        /// </summary>
        protected virtual void OnCloseWindow()
        {
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        // 基本信息
        private string _ticketNumber;
        private string _checkInLocation;
        private string _departStation;
        private string _arriveStation;
        private string _departStationPinyin;
        private string _arriveStationPinyin;
        private decimal _money;
        private string _departStationCode;
        private string _arriveStationCode;

        // 日期和时间
        private DateTime _departDate = DateTime.Today;
        private int _departHour;
        private int _departMinute;

        // 车次信息
        private string _selectedTrainType;
        private string _trainNumber;
        private string _coachNo;
        private bool _isExtraCoach;
        private string _seatNo;
        private bool _isNoSeat;
        private string _selectedSeatPosition;

        // 座位类型
        private string _selectedSeatType;
        private ObservableCollection<string> _seatPositions;

        // 附加信息
        private string _selectedAdditionalInfo;
        private string _selectedTicketPurpose;
        private string? _selectedHint;
        private string? _customHint;
        private string? _selectedTicketModificationType;
        private ObservableCollection<string> _ticketModificationTypes;

        // 票种类型
        private bool _isStudentTicket;
        private bool _isDiscountTicket;
        private bool _isOnlineTicket;
        private bool _isChildTicket;

        // 车站数据
        private ObservableCollection<StationInfo> _stations;

        // 车站搜索相关属性
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private ObservableCollection<StationInfo> _arriveStationSuggestions;
        private bool _isDepartStationDropdownOpen;
        private bool _isArriveStationDropdownOpen;
        private string _departStationSearchText;
        private string _arriveStationSearchText;
        private double _dataGridHeaderFontSize = 14; // 默认表头字体大小
        private double _dataGridRowHeight = 40; // 默认行高
        private double _dataGridCellFontSize = 13; // 默认单元格字体大小

        private bool _isUpdatingDepartStation = false;
        private bool _isUpdatingArriveStation = false;

        public AddTicketViewModel(DatabaseService databaseService, MainViewModel mainViewModel)
        {
            try
            {
                _databaseService = databaseService;
                _mainViewModel = mainViewModel;
                
                // 初始化站点搜索服务
                _stationSearchService = new StationSearchService(databaseService);
                
                // 初始化字体大小监听器
                _fontSizeChangeListener = new FontSizeChangeListener();
                _fontSizeChangeListener.FontSizeChanged += OnFontSizeChanged;
                
                // 初始化命令
                SaveCommand = new RelayCommand(SaveTicket, CanSaveTicket);
                ResetCommand = new RelayCommand(ResetForm);
                CustomHintCommand = new RelayCommand(ShowCustomHintDialog);
                SelectDepartStationCommand = new RelayCommand<StationInfo>(SelectDepartStation);
                SelectArriveStationCommand = new RelayCommand<StationInfo>(SelectArriveStation);
                
                // 初始化下拉框选项
                TrainTypes = new ObservableCollection<string> { "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字" };
                HourOptions = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
                MinuteOptions = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();
                
                SeatTypes = new ObservableCollection<string> 
                { 
                    "新空调硬座", "软座", "新空调硬卧", "新空调软卧", 
                    "商务座", "一等座", "二等座", "硬卧代硬座" 
                };
                
                // 初始化座位位置集合
                SeatPositions = new ObservableCollection<string>();
                
                AdditionalInfoOptions = new ObservableCollection<string> { "", "限乘当日当次车", "退票费" };
                
                TicketPurposeOptions = new ObservableCollection<string> { "", "仅供报销使用" };
                
                HintOptions = new ObservableCollection<string> 
                {
                    "报销凭证 遗失不补|退票改签时须交回车站",
                    "买票请到12306 发货请到95306|中国铁路祝您旅途愉快",
                    "欢度国庆 祝福祖国|中国铁路祝您旅途愉快",
                    "奋斗百年路 启航新征程|热烈庆祝中国共产党成立100周年",
                    "锦州银行欢迎您",
                    "中国铁路沈阳局集团公司|团体订票电话024-12306",
                    "自定义"
                };
                
                // 初始化车站搜索相关集合
                DepartStationSuggestions = new ObservableCollection<StationInfo>();
                ArriveStationSuggestions = new ObservableCollection<StationInfo>();
                
                // 初始化车票改签类型选项
                TicketModificationTypes = new ObservableCollection<string>
                {
                    "始发改签",
                    "变更到站"
                };
                
                // 初始化字符串属性为空字符串而不是null
                _ticketNumber = string.Empty;
                _checkInLocation = string.Empty;
                _departStation = string.Empty;
                _arriveStation = string.Empty;
                _departStationPinyin = string.Empty;
                _arriveStationPinyin = string.Empty;
                _departStationCode = string.Empty;
                _arriveStationCode = string.Empty;
                _selectedTrainType = TrainTypes.FirstOrDefault() ?? "T";
                _trainNumber = string.Empty;
                _coachNo = string.Empty;
                _seatNo = string.Empty;
                _selectedSeatPosition = SeatPositions.FirstOrDefault() ?? string.Empty;
                _selectedSeatType = SeatTypes.FirstOrDefault() ?? "新空硬座";
                _selectedAdditionalInfo = string.Empty;
                _selectedTicketPurpose = string.Empty;
                _selectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
                _customHint = string.Empty;
                _selectedTicketModificationType = null;
                _departStationSearchText = string.Empty;
                _arriveStationSearchText = string.Empty;
                
                // 初始化DataGrid相关属性
                _dataGridHeaderFontSize = 15; // 表头字体大小
                _dataGridRowHeight = 40;     // 行高
                _dataGridCellFontSize = 14;  // 单元格字体大小
                
                // 根据默认座位类型更新座位位置选项
                UpdateSeatPositions();
                
                // 加载车站数据
                LoadStationsAsync();
                
                // 确保初始化字体大小相关属性
                InitializeFontSizes();
                
                // 在支付渠道变更时标记表单已修改
                PaymentChannelChanged += (s, e) => 
                {
                    if (!_isInitializing) _isFormModified = true;
                };

                // 初始化完成
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化添加车票窗口时出错: {ex.Message}");
                LogHelper.LogError("初始化添加车票窗口时出错", ex);
            }
        }

        #region 属性

        public string TicketNumber
        {
            get => _ticketNumber;
            set
            {
                if (_ticketNumber != value)
                {
                    // 如果输入不为空，将首字母转换为大写
                    if (!string.IsNullOrEmpty(value))
                    {
                        _ticketNumber = FormValidationHelper.EnsureFirstLetterUpperCase(value);
                    }
                    else
                    {
                        _ticketNumber = value;
                    }
                    OnPropertyChanged(nameof(TicketNumber));
                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        public string CheckInLocation
        {
            get => _checkInLocation;
            set
            {
                if (_checkInLocation != value)
                {
                    _checkInLocation = value;
                    OnPropertyChanged(nameof(CheckInLocation));
                }
            }
        }

        public string DepartStation
        {
            get => _departStation;
            set
            {
                if (_departStation != value)
                {
                    _departStation = value;
                    OnPropertyChanged(nameof(DepartStation));
                    // 不在这里调用ValidateStationName，而是在输入完成后再检测
                }
            }
        }

        public string ArriveStation
        {
            get => _arriveStation;
            set
            {
                if (_arriveStation != value)
                {
                    _arriveStation = value;
                    OnPropertyChanged(nameof(ArriveStation));
                    // 不在这里调用ValidateStationName，而是在输入完成后再检测
                }
            }
        }

        public string DepartStationPinyin
        {
            get => _departStationPinyin;
            set
            {
                if (_departStationPinyin != value)
                {
                    _departStationPinyin = value;
                    OnPropertyChanged(nameof(DepartStationPinyin));
                }
            }
        }

        public string ArriveStationPinyin
        {
            get => _arriveStationPinyin;
            set
            {
                if (_arriveStationPinyin != value)
                {
                    _arriveStationPinyin = value;
                    OnPropertyChanged(nameof(ArriveStationPinyin));
                }
            }
        }

        public decimal Money
        {
            get => _money;
            set
            {
                if (_money != value)
                {
                    _money = value;
                    OnPropertyChanged(nameof(Money));
                }
            }
        }

        public string DepartStationCode
        {
            get => _departStationCode;
            set
            {
                if (_departStationCode != value)
                {
                    _departStationCode = value;
                    OnPropertyChanged(nameof(DepartStationCode));
                }
            }
        }

        public string ArriveStationCode
        {
            get => _arriveStationCode;
            set
            {
                if (_arriveStationCode != value)
                {
                    _arriveStationCode = value;
                    OnPropertyChanged(nameof(ArriveStationCode));
                }
            }
        }

        public DateTime DepartDate
        {
            get => _departDate;
            set
            {
                if (_departDate != value)
                {
                    _departDate = value;
                    OnPropertyChanged(nameof(DepartDate));
                }
            }
        }

        public int DepartHour
        {
            get => _departHour;
            set
            {
                if (_departHour != value)
                {
                    _departHour = value;
                    OnPropertyChanged(nameof(DepartHour));
                }
            }
        }

        public int DepartMinute
        {
            get => _departMinute;
            set
            {
                if (_departMinute != value)
                {
                    _departMinute = value;
                    OnPropertyChanged(nameof(DepartMinute));
                }
            }
        }

        public string SelectedTrainType
        {
            get => _selectedTrainType;
            set
            {
                if (_selectedTrainType != value)
                {
                    _selectedTrainType = value;
                    OnPropertyChanged(nameof(SelectedTrainType));
                }
            }
        }

        public string TrainNumber
        {
            get => _trainNumber;
            set
            {
                if (_trainNumber != value)
                {
                    // 如果选择了"纯数字"类型，验证输入是否为0000-9999的数字
                    if (SelectedTrainType == "纯数字")
                    {
                        // 如果输入为空，直接接受
                        if (string.IsNullOrEmpty(value))
                        {
                            _trainNumber = value;
                        }
                        // 如果输入不为空，验证是否为数字且在0-9999范围内
                        else if (int.TryParse(value, out int number) && number >= 0 && number <= 9999)
                        {
                            _trainNumber = value;
                        }
                        // 如果不符合条件，保持原值不变
                        else
                        {
                            // 不更新属性，直接返回
                            OnPropertyChanged(nameof(TrainNumber)); // 触发UI更新，恢复原值
                            return;
                        }
                    }
                    else
                    {
                        _trainNumber = value;
                    }
                    
                    OnPropertyChanged(nameof(TrainNumber));
                }
            }
        }

        public string CoachNo
        {
            get => _coachNo;
            set
            {
                if (_coachNo != value)
                {
                    _coachNo = value;
                    OnPropertyChanged(nameof(CoachNo));
                }
            }
        }

        public bool IsExtraCoach
        {
            get => _isExtraCoach;
            set
            {
                if (_isExtraCoach != value)
                {
                    _isExtraCoach = value;
                    OnPropertyChanged(nameof(IsExtraCoach));
                }
            }
        }

        public string SeatNo
        {
            get => _seatNo;
            set
            {
                if (_seatNo != value)
                {
                    _seatNo = value;
                    OnPropertyChanged(nameof(SeatNo));
                }
            }
        }

        public bool IsNoSeat
        {
            get => _isNoSeat;
            set
            {
                if (_isNoSeat != value)
                {
                    _isNoSeat = value;
                    OnPropertyChanged(nameof(IsNoSeat));
                    OnPropertyChanged(nameof(IsSeatInputEnabled));
                }
            }
        }

        public bool IsSeatInputEnabled => !IsNoSeat;

        public bool IsSeatPositionVisible => SeatPositions != null && SeatPositions.Count > 0 && SelectedSeatType != "新空调硬座";

        public string SelectedSeatPosition
        {
            get => _selectedSeatPosition;
            set
            {
                if (_selectedSeatPosition != value)
                {
                    _selectedSeatPosition = value;
                    OnPropertyChanged(nameof(SelectedSeatPosition));
                }
            }
        }

        public string SelectedSeatType
        {
            get => _selectedSeatType;
            set
            {
                if (_selectedSeatType != value)
                {
                    _selectedSeatType = value;
                    OnPropertyChanged(nameof(SelectedSeatType));
                    UpdateSeatPositions();
                }
            }
        }

        public ObservableCollection<string> SeatPositions
        {
            get => _seatPositions;
            set
            {
                if (_seatPositions != value)
                {
                    _seatPositions = value;
                    OnPropertyChanged(nameof(SeatPositions));
                }
            }
        }

        public string SelectedAdditionalInfo
        {
            get => _selectedAdditionalInfo;
            set
            {
                if (_selectedAdditionalInfo != value)
                {
                    _selectedAdditionalInfo = value;
                    OnPropertyChanged(nameof(SelectedAdditionalInfo));
                    UpdateTicketPurposeOptions();
                }
            }
        }

        public string SelectedTicketPurpose
        {
            get => _selectedTicketPurpose;
            set
            {
                if (_selectedTicketPurpose != value)
                {
                    _selectedTicketPurpose = value;
                    OnPropertyChanged(nameof(SelectedTicketPurpose));
                    UpdateAdditionalInfoOptions();
                }
            }
        }

        public string SelectedHint
        {
            get => _selectedHint;
            set
            {
                if (_selectedHint != value)
                {
                    _selectedHint = value;
                    OnPropertyChanged(nameof(SelectedHint));
                    
                    if (value == "自定义")
                    {
                        ShowCustomHintDialog();
                    }
                }
            }
        }

        public string CustomHint
        {
            get => _customHint;
            set
            {
                if (_customHint != value)
                {
                    _customHint = value;
                    OnPropertyChanged(nameof(CustomHint));
                }
            }
        }

        public ObservableCollection<StationInfo> Stations
        {
            get => _stations;
            set
            {
                if (_stations != value)
                {
                    _stations = value;
                    OnPropertyChanged(nameof(Stations));
                }
            }
        }

        // 车站搜索相关属性
        public ObservableCollection<StationInfo> DepartStationSuggestions
        {
            get => _departStationSuggestions;
            set
            {
                if (_departStationSuggestions != value)
                {
                    _departStationSuggestions = value;
                    OnPropertyChanged(nameof(DepartStationSuggestions));
                }
            }
        }

        public ObservableCollection<StationInfo> ArriveStationSuggestions
        {
            get => _arriveStationSuggestions;
            set
            {
                if (_arriveStationSuggestions != value)
                {
                    _arriveStationSuggestions = value;
                    OnPropertyChanged(nameof(ArriveStationSuggestions));
                }
            }
        }

        public bool IsDepartStationDropdownOpen
        {
            get => _isDepartStationDropdownOpen;
            set
            {
                if (_isDepartStationDropdownOpen != value)
                {
                    _isDepartStationDropdownOpen = value;
                    OnPropertyChanged(nameof(IsDepartStationDropdownOpen));
                }
            }
        }

        public bool IsArriveStationDropdownOpen
        {
            get => _isArriveStationDropdownOpen;
            set
            {
                if (_isArriveStationDropdownOpen != value)
                {
                    _isArriveStationDropdownOpen = value;
                    OnPropertyChanged(nameof(IsArriveStationDropdownOpen));
                }
            }
        }

        public string DepartStationSearchText
        {
            get => _departStationSearchText;
            set
            {
                if (_departStationSearchText != value)
                {
                    _departStationSearchText = value;
                    OnPropertyChanged(nameof(DepartStationSearchText));
                    
                    // 如果是通过选择项更新的，不触发搜索
                    if (!_isUpdatingDepartStation)
                    {
                        // 移除"站"字后搜索
                        string searchText = value?.Replace("站", "").Trim() ?? string.Empty;
                        SearchStations(searchText, true);
                        
                        // 同步更新DepartStation属性
                        DepartStation = value;
                    }
                }
            }
        }

        public string ArriveStationSearchText
        {
            get => _arriveStationSearchText;
            set
            {
                if (_arriveStationSearchText != value)
                {
                    _arriveStationSearchText = value;
                    OnPropertyChanged(nameof(ArriveStationSearchText));
                    
                    // 如果是通过选择项更新的，不触发搜索
                    if (!_isUpdatingArriveStation)
                    {
                        // 移除"站"字后搜索
                        string searchText = value?.Replace("站", "").Trim() ?? string.Empty;
                        SearchStations(searchText, false);
                        
                        // 同步更新ArriveStation属性
                        ArriveStation = value;
                    }
                }
            }
        }

        public double DataGridHeaderFontSize
        {
            get => _dataGridHeaderFontSize;
            set
            {
                if (_dataGridHeaderFontSize != value)
                {
                    _dataGridHeaderFontSize = value;
                    OnPropertyChanged(nameof(DataGridHeaderFontSize));
                }
            }
        }

        public double DataGridRowHeight
        {
            get => _dataGridRowHeight;
            set
            {
                if (_dataGridRowHeight != value)
                {
                    _dataGridRowHeight = value;
                    OnPropertyChanged(nameof(DataGridRowHeight));
                }
            }
        }

        public double DataGridCellFontSize
        {
            get => _dataGridCellFontSize;
            set
            {
                if (_dataGridCellFontSize != value)
                {
                    _dataGridCellFontSize = value;
                    OnPropertyChanged(nameof(DataGridCellFontSize));
                }
            }
        }

        public string? SelectedTicketModificationType
        {
            get => _selectedTicketModificationType;
            set
            {
                if (_selectedTicketModificationType != value)
                {
                    _selectedTicketModificationType = value;
                    OnPropertyChanged(nameof(SelectedTicketModificationType));
                }
            }
        }

        public ObservableCollection<string> TicketModificationTypes
        {
            get => _ticketModificationTypes;
            private set
            {
                _ticketModificationTypes = value;
                OnPropertyChanged(nameof(TicketModificationTypes));
            }
        }

        // 票种类型属性
        public bool IsStudentTicket
        {
            get => _isStudentTicket;
            set
            {
                if (_isStudentTicket != value)
                {
                    _isStudentTicket = value;
                    OnPropertyChanged(nameof(IsStudentTicket));
                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        public bool IsDiscountTicket
        {
            get => _isDiscountTicket;
            set
            {
                if (_isDiscountTicket != value)
                {
                    _isDiscountTicket = value;
                    OnPropertyChanged(nameof(IsDiscountTicket));
                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        public bool IsOnlineTicket
        {
            get => _isOnlineTicket;
            set
            {
                if (_isOnlineTicket != value)
                {
                    _isOnlineTicket = value;
                    OnPropertyChanged(nameof(IsOnlineTicket));
                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        public bool IsChildTicket
        {
            get => _isChildTicket;
            set
            {
                if (_isChildTicket != value)
                {
                    _isChildTicket = value;
                    OnPropertyChanged(nameof(IsChildTicket));
                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        #endregion

        #region 集合属性

        public ObservableCollection<string> TrainTypes { get; }
        public List<string> HourOptions { get; }
        public List<string> MinuteOptions { get; }
        public ObservableCollection<string> SeatTypes { get; }
        public ObservableCollection<string> AdditionalInfoOptions { get; private set; }
        public ObservableCollection<string> TicketPurposeOptions { get; private set; }
        public ObservableCollection<string> HintOptions { get; }

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CustomHintCommand { get; }
        public ICommand SelectDepartStationCommand { get; }
        public ICommand SelectArriveStationCommand { get; }

        #endregion

        #region 方法

        private async void LoadStationsAsync()
        {
            try
            {
                // 使用StationSearchService加载站点数据
                await _stationSearchService.LoadStationsAsync();
                Stations = _stationSearchService.Stations;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载车站数据时出错: {ex.Message}");
                LogHelper.LogError("加载车站数据时出错", ex);
            }
        }

        private void ValidateStationName(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return;
            }

            // 使用StationSearchService验证站点名称
            var station = _stationSearchService.ValidateStationName(stationName);
            if (station != null)
            {
                UpdateStationInfo(station.StationName, isDepartStation);
                CheckStationInfoCompleteness(station, isDepartStation);
            }
        }

        private void UpdateStationInfo(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                return;
            }

            try
            {
                // 确保站点数据已加载
                if (Stations == null || Stations.Count == 0)
                {
                    return;
                }

                // 在站点列表中查找匹配的站点
                var station = Stations.FirstOrDefault(s => s.StationName == stationName);
                
                // 如果找不到完全匹配，则尝试查找部分匹配
                if (station == null)
                {
                    // 移除"站"后缀进行比较
                    string cleanName = stationName.Replace("站", "");
                    station = Stations.FirstOrDefault(s => 
                        s.StationName?.Replace("站", "") == cleanName ||
                        s.StationName == cleanName);
                }

                // 如果找到匹配的站点，更新站点信息
                if (station != null)
                {
                    CheckStationInfoCompleteness(station, isDepartStation);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"更新站点信息时出错: {stationName}", ex);
            }
        }

        protected virtual void UpdateSeatPositions()
        {
            if (SeatPositions == null)
            {
                SeatPositions = new ObservableCollection<string>();
            }
            else
            {
                SeatPositions.Clear();
            }

            // 根据座位类型设置可选的座位位置
            if (!string.IsNullOrEmpty(_selectedSeatType))
            {
                switch (_selectedSeatType)
                {
                    case "商务座":
                    case "一等座":
                    case "二等座":
                        // A-F 座位位置
                        SeatPositions.Add("A");
                        SeatPositions.Add("B");
                        SeatPositions.Add("C");
                        SeatPositions.Add("D");
                        SeatPositions.Add("F");
                        break;
                    case "新空调硬卧":
                    case "新空调软卧":
                        // 上中下卧铺位置
                        SeatPositions.Add("上");
                        SeatPositions.Add("中");
                        SeatPositions.Add("下");
                        break;
                    case "软座":
                    case "硬卧代硬座":
                        // 这些类型没有特定位置，只有座位号
                        break;
                    default:
                        // 默认不显示位置选项
                        break;
                }
            }

            // 通知UI更新
            OnPropertyChanged(nameof(SeatPositions));
            OnPropertyChanged(nameof(IsSeatPositionVisible));
        }

        private void UpdateTicketPurposeOptions()
        {
            if (SelectedAdditionalInfo == "限乘当日当次车")
            {
                // 移除"仅供报销使用"选项
                if (TicketPurposeOptions.Contains("仅供报销使用"))
                {
                    TicketPurposeOptions.Remove("仅供报销使用");
                    if (SelectedTicketPurpose == "仅供报销使用")
                        SelectedTicketPurpose = "";
                }
            }
            else
            {
                // 添加"仅供报销使用"选项
                if (!TicketPurposeOptions.Contains("仅供报销使用"))
                {
                    TicketPurposeOptions.Add("仅供报销使用");
                }
            }
        }

        private void UpdateAdditionalInfoOptions()
        {
            if (SelectedTicketPurpose == "仅供报销使用")
            {
                // 移除"限乘当日当次车"选项
                if (AdditionalInfoOptions.Contains("限乘当日当次车"))
                {
                    AdditionalInfoOptions.Remove("限乘当日当次车");
                    if (SelectedAdditionalInfo == "限乘当日当次车")
                        SelectedAdditionalInfo = "";
                }
            }
            else
            {
                // 添加"限乘当日当次车"选项
                if (!AdditionalInfoOptions.Contains("限乘当日当次车"))
                {
                    AdditionalInfoOptions.Add("限乘当日当次车");
                }
            }
        }

        private void ShowCustomHintDialog()
        {
            try
            {
                // 创建自定义提示对话框
                var dialog = new InputDialog("请输入你想展示在车票上的提示信息（车票上的虚线框区域）");
                
                // 如果已有自定义内容，则预填充
                if (!string.IsNullOrEmpty(CustomHint))
                {
                    dialog.ResponseText = CustomHint;
                }
                
                if (dialog.ShowDialog() == true)
                {
                    CustomHint = dialog.ResponseText;
                    
                    // 直接将当前选项替换为自定义内容
                    int customIndex = HintOptions.IndexOf("自定义");
                    if (customIndex >= 0)
                    {
                        // 确保"自定义"选项仍然存在
                        if (!HintOptions.Contains(CustomHint))
                        {
                            // 添加自定义内容作为新选项
                            HintOptions.Insert(customIndex, CustomHint);
                            
                            // 选择新添加的选项
                            SelectedHint = CustomHint;
                        }
                        else
                        {
                            // 如果已存在相同内容的选项，直接选择它
                            SelectedHint = CustomHint;
                        }
                    }
                }
                else
                {
                    // 用户取消，恢复选择
                    if (SelectedHint == "自定义")
                    {
                        SelectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"显示自定义提示对话框时出错: {ex.Message}");
                
                // 恢复选择
                if (SelectedHint == "自定义")
                {
                    SelectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// 验证表单
        /// </summary>
        /// <returns>是否验证通过</returns>
        public virtual bool ValidateForm()
        {
            // 创建一个新的TrainRideInfo对象用于验证
            var ticket = new TrainRideInfo
            {
                TicketNumber = TicketNumber,
                CheckInLocation = CheckInLocation,
                DepartStation = DepartStation,
                ArriveStation = ArriveStation,
                DepartStationPinyin = DepartStationPinyin,
                ArriveStationPinyin = ArriveStationPinyin,
                DepartStationCode = DepartStationCode,
                ArriveStationCode = ArriveStationCode,
                DepartDate = DepartDate,
                DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                TrainNo = FormValidationHelper.FormatTrainNo(SelectedTrainType, TrainNumber),
                CoachNo = IsExtraCoach ? $"加{CoachNo}车" : $"{CoachNo}车",
                SeatNo = FormValidationHelper.FormatSeatNo(IsNoSeat, SeatNo, SelectedSeatPosition),
                Money = Money,
                SeatType = SelectedSeatType,
                AdditionalInfo = SelectedAdditionalInfo,
                TicketPurpose = SelectedTicketPurpose,
                Hint = SelectedHint == "自定义" ? CustomHint : SelectedHint,
                TicketModificationType = SelectedTicketModificationType,
                TicketTypeFlags = GetTicketTypeFlags(),
                PaymentChannelFlags = GetPaymentChannelFlags()
            };

            // 使用表单验证工具类验证表单
            bool isValid = FormValidationHelper.ValidateTicketForm(ticket, _validationErrors);

            return isValid;
        }

        /// <summary>
        /// 判断是否可以保存车票
        /// </summary>
        /// <returns>是否可以保存</returns>
        private bool CanSaveTicket()
        {
            return true; // 简化处理，允许尝试保存，在保存前会进行验证
        }

        /// <summary>
        /// 保存车票
        /// </summary>
        protected virtual async void SaveTicket()
        {
            try
            {
                // 验证表单
                if (!ValidateForm())
                {
                    MessageBoxHelper.ShowWarning(FormValidationHelper.GetFormattedValidationErrors(_validationErrors));
                    return;
                }

                // 创建TrainRideInfo对象
                var ticket = new TrainRideInfo
                {
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation + "站",
                    ArriveStation = ArriveStation + "站",
                    DepartStationPinyin = DepartStationPinyin,
                    ArriveStationPinyin = ArriveStationPinyin,
                    DepartStationCode = DepartStationCode,
                    ArriveStationCode = ArriveStationCode,
                    DepartDate = DepartDate,
                    DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                    TrainNo = FormValidationHelper.FormatTrainNo(SelectedTrainType, TrainNumber),
                    CoachNo = IsExtraCoach ? $"加{CoachNo}车" : $"{CoachNo}车",
                    SeatNo = FormValidationHelper.FormatSeatNo(IsNoSeat, SeatNo, SelectedSeatPosition),
                    Money = Money,
                    SeatType = SelectedSeatType,
                    AdditionalInfo = SelectedAdditionalInfo,
                    TicketPurpose = SelectedTicketPurpose,
                    Hint = SelectedHint == "自定义" ? CustomHint : SelectedHint,
                    TicketModificationType = SelectedTicketModificationType,
                    TicketTypeFlags = GetTicketTypeFlags(),
                    PaymentChannelFlags = GetPaymentChannelFlags()
                };

                // 保存车票
                await _databaseService.AddTicketAsync(ticket);
                
                MessageBoxHelper.ShowInformation("车票已成功保存！", "成功");
                
                // 重置表单
                ResetForm();
                
                // 标记需要刷新数据
                _needToRefreshData = true;
                
                // 触发窗口关闭事件
                OnCloseWindow();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"保存车票时出错: {ex.Message}");
                LogHelper.LogTicketError("添加", "保存车票时出错", ex);
            }
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        private void ResetForm()
        {
            // 重置表单字段
            TicketNumber = string.Empty;
            CheckInLocation = string.Empty;
            DepartStation = string.Empty;
            ArriveStation = string.Empty;
            DepartStationPinyin = string.Empty;
            ArriveStationPinyin = string.Empty;
            DepartStationCode = string.Empty;
            ArriveStationCode = string.Empty;
            DepartDate = DateTime.Today;
            DepartHour = 0;
            DepartMinute = 0;
            SelectedTrainType = TrainTypes.FirstOrDefault() ?? "T";
            TrainNumber = string.Empty;
            CoachNo = string.Empty;
            IsExtraCoach = false;
            SeatNo = string.Empty;
            IsNoSeat = false;
            SelectedSeatType = SeatTypes.FirstOrDefault() ?? "新空硬座";
            SelectedSeatPosition = string.Empty;
            Money = 0m;
            SelectedAdditionalInfo = string.Empty;
            SelectedTicketPurpose = string.Empty;
            SelectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
            CustomHint = string.Empty;
            SelectedTicketModificationType = null;
            IsStudentTicket = false;
            IsDiscountTicket = false;
            IsOnlineTicket = false;
            IsChildTicket = false;
            
            // 重置搜索框文本
            DepartStationSearchText = string.Empty;
            ArriveStationSearchText = string.Empty;
            
            // 重置下拉列表状态
            IsDepartStationDropdownOpen = false;
            IsArriveStationDropdownOpen = false;
            
            // 清空建议列表
            DepartStationSuggestions.Clear();
            ArriveStationSuggestions.Clear();
            
            // 更新座位位置选项
            UpdateSeatPositions();
            
            // 重置表单修改状态
            ResetFormModifiedState();
        }

        /// <summary>
        /// 搜索车站
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="isDepartStation">是否为出发站</param>
        protected virtual async void SearchStations(string searchText, bool isDepartStation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    if (isDepartStation)
                    {
                        DepartStationSuggestions.Clear();
                        IsDepartStationDropdownOpen = false;
                    }
                    else
                    {
                        ArriveStationSuggestions.Clear();
                        IsArriveStationDropdownOpen = false;
                    }
                    return;
                }

                // 使用站点搜索服务搜索站点
                var suggestions = await _stationSearchService.SearchStationsAsync(searchText);
                
                if (isDepartStation)
                {
                    // 更新出发站建议列表
                    DepartStationSuggestions.Clear();
                    foreach (var station in suggestions)
                    {
                        DepartStationSuggestions.Add(station);
                    }
                    IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                }
                else
                {
                    // 更新到达站建议列表
                    ArriveStationSuggestions.Clear();
                    foreach (var station in suggestions)
                    {
                        ArriveStationSuggestions.Add(station);
                    }
                    IsArriveStationDropdownOpen = ArriveStationSuggestions.Count > 0;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索车站时出错: {ex.Message}", ex);
            }
        }

        private void SelectDepartStation(StationInfo station)
        {
            if (station == null)
                return;
                
            // 确保车站名称不包含"站"字
            string stationName = station.StationName?.Replace("站", "") ?? string.Empty;
            
            // 先关闭下拉框，防止触发搜索
            IsDepartStationDropdownOpen = false;
            
            // 更新属性
            DepartStation = stationName;
            
            // 暂时取消DepartStationSearchText的PropertyChanged事件触发
            _isUpdatingDepartStation = true;
            DepartStationSearchText = stationName;
            _isUpdatingDepartStation = false;
            
            // 直接更新站点信息，无需等待失去焦点
            DepartStationPinyin = station.StationPinyin ?? string.Empty;
            DepartStationCode = station.StationCode ?? string.Empty;
        }

        private void SelectArriveStation(StationInfo station)
        {
            if (station == null)
                return;
                
            // 确保车站名称不包含"站"字
            string stationName = station.StationName?.Replace("站", "") ?? string.Empty;
            
            // 先关闭下拉框，防止触发搜索
            IsArriveStationDropdownOpen = false;
            
            // 更新属性
            ArriveStation = stationName;
            
            // 暂时取消ArriveStationSearchText的PropertyChanged事件触发
            _isUpdatingArriveStation = true;
            ArriveStationSearchText = stationName;
            _isUpdatingArriveStation = false;
            
            // 直接更新站点信息，无需等待失去焦点
            ArriveStationPinyin = station.StationPinyin ?? string.Empty;
            ArriveStationCode = station.StationCode ?? string.Empty;
        }

        // 检查车站信息是否完整
        private void CheckStationInfoCompleteness(StationInfo station, bool isDepartStation)
        {
            if (station == null)
                return;
                
            // 如果已经设置忽略车站检查，则直接返回
            if (Services.StationCheckService.Instance.IgnoreStationCheck)
                return;
                
            string stationName = station.StationName?.Replace("站", "") ?? string.Empty;
            
            // 检查车站信息是否完整
            bool isPinyinMissing = string.IsNullOrEmpty(station.StationPinyin);
            bool isCodeMissing = string.IsNullOrEmpty(station.StationCode);
            
            if (isPinyinMissing || isCodeMissing)
            {
                string message = $"车站\"{stationName}站\"的信息不完整，";
                if (isPinyinMissing && isCodeMissing)
                    message += "缺少拼音和代码信息。";
                else if (isPinyinMissing)
                    message += "缺少拼音信息。";
                else
                    message += "缺少代码信息。";
                    
                message += "建议在车站管理中完善车站信息。";
                
                MessageBoxHelper.ShowWarning(message, "车站信息不完整");
            }
        }

        private void UpdateArriveStationSuggestions()
        {
            // 实现更新建议列表的逻辑
            // 这里可以根据需要调用SearchArriveStations方法来更新建议列表
        }

        // 添加字体大小变化处理方法
        private void OnFontSizeChanged(object sender, FontSizeChangedEventArgs e)
        {
            try
            {
                // 根据字体大小变化比例调整控件大小
                double ratio = e.NewSize / e.OldSize;
                
                // 更新DataGrid相关属性
                DataGridHeaderFontSize = Math.Max(14, DataGridHeaderFontSize * ratio);
                DataGridCellFontSize = Math.Max(13, DataGridCellFontSize * ratio);
                DataGridRowHeight = Math.Max(40, DataGridRowHeight * ratio);
                
                // 通知UI更新
                OnPropertyChanged(nameof(DataGridHeaderFontSize));
                OnPropertyChanged(nameof(DataGridCellFontSize));
                OnPropertyChanged(nameof(DataGridRowHeight));
            }
            catch (Exception ex)
            {
                LogHelper.LogSystemError("UI", "字体大小变化处理出错", ex);
            }
        }
        
        // 初始化字体大小相关属性
        private void InitializeFontSizes()
        {
            try
            {
                // 从配置或应用程序资源中获取字体大小设置
                var app = Application.Current;
                if (app != null && app.Resources.Contains("MaterialDesignFontSize"))
                {
                    var baseFontSize = Convert.ToDouble(app.Resources["MaterialDesignFontSize"]);
                    
                    // 设置默认值
                    DataGridHeaderFontSize = Math.Max(14, baseFontSize + 1);
                    DataGridCellFontSize = Math.Max(13, baseFontSize);
                    DataGridRowHeight = Math.Max(40, baseFontSize * 3);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogSystemError("UI", "初始化字体大小设置出错", ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // 当属性变更时，如果不是初始化阶段且不是IsFormModified属性本身，则标记表单已修改
            if (!_isInitializing && propertyName != nameof(IsFormModified))
            {
                IsFormModified = true;
            }
        }

        #endregion

        // 在析构函数中取消事件订阅
        ~AddTicketViewModel()
        {
            if (_fontSizeChangeListener != null)
            {
                _fontSizeChangeListener.FontSizeChanged -= OnFontSizeChanged;
            }
        }

        /// <summary>
        /// 表单是否已修改
        /// </summary>
        public bool IsFormModified
        {
            get => _isFormModified;
            protected set
            {
                if (_isFormModified != value)
                {
                    _isFormModified = value;
                    OnPropertyChanged(nameof(IsFormModified));
                }
            }
        }

        /// <summary>
        /// 检查表单是否有未保存的修改
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return IsFormModified;
        }

        /// <summary>
        /// 重置表单修改状态
        /// </summary>
        protected void ResetFormModifiedState()
        {
            IsFormModified = false;
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <returns>所有验证错误信息的字符串</returns>
        public string GetValidationErrors()
        {
            return string.Join("\n", _validationErrors);
        }

        private void ResetData()
        {
            // 重置所有字段
            ResetForm();
            
            // 根据默认座位类型更新座位位置选项
            UpdateSeatPositions();
            
            // 加载车站数据
            LoadStationsAsync();
            
            // 确保初始化字体大小相关属性
            InitializeFontSizes();
        }

        private string GetFormattedSeatNo()
        {
            if (IsNoSeat)
                return "无座";
            else if (SelectedSeatType == "新空调硬座")
                return SeatNo;
            else
                return $"{SeatNo}{SelectedSeatPosition}";
        }

        // 获取票种类型标志位
        private int GetTicketTypeFlags()
        {
            int flags = 0;
            if (IsStudentTicket) flags |= (int)TicketTypeFlags.StudentTicket;
            if (IsDiscountTicket) flags |= (int)TicketTypeFlags.DiscountTicket;
            if (IsOnlineTicket) flags |= (int)TicketTypeFlags.OnlineTicket;
            if (IsChildTicket) flags |= (int)TicketTypeFlags.ChildTicket;
            return flags;
        }

        /// <summary>
        /// 处理站点输入框失去焦点事件
        /// </summary>
        /// <param name="isDepartStation">是否为出发站</param>
        public virtual void OnStationLostFocus(bool isDepartStation)
        {
            try
            {
                string stationName = isDepartStation ? DepartStation : ArriveStation;
                if (!string.IsNullOrWhiteSpace(stationName))
                {
                    // 使用StationSearchService处理失去焦点事件
                    var station = _stationSearchService.HandleStationLostFocus(stationName, isDepartStation);
                    
                    if (station != null)
                    {
                        // 更新站点信息
                        if (isDepartStation)
                        {
                            // 更新出发站信息
                            DepartStationPinyin = station.StationPinyin ?? string.Empty;
                            DepartStationCode = station.StationCode ?? string.Empty;
                            
                            // 检查站点信息完整性，与原有逻辑保持一致
                            CheckStationInfoCompleteness(station, true);
                        }
                        else
                        {
                            // 更新到达站信息
                            ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                            ArriveStationCode = station.StationCode ?? string.Empty;
                            
                            // 检查站点信息完整性，与原有逻辑保持一致
                            CheckStationInfoCompleteness(station, false);
                        }
                    }
                    else
                    {
                        // 如果找不到匹配的站点，也执行原有的验证逻辑
                        ValidateStationName(stationName, isDepartStation);
                        UpdateStationInfo(stationName, isDepartStation);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理站点输入框失去焦点事件时出错: {ex.Message}", ex);
            }
        }
    }

    // 添加字体大小变化监听器类
    public class FontSizeChangeListener
    {
        private double _lastFontSize;
        
        public event EventHandler<FontSizeChangedEventArgs> FontSizeChanged;
        
        public FontSizeChangeListener()
        {
            // 获取初始字体大小
            _lastFontSize = GetCurrentFontSize();
            
            // 启动监听
            StartListening();
        }
        
        private void StartListening()
        {
            // 使用计时器定期检查字体大小变化
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500); // 每500毫秒检查一次
            timer.Tick += (s, e) => CheckFontSizeChange();
            timer.Start();
        }
        
        private void CheckFontSizeChange()
        {
            double currentFontSize = GetCurrentFontSize();
            
            // 如果字体大小发生变化，触发事件
            if (Math.Abs(currentFontSize - _lastFontSize) > 0.1) // 添加一个小的阈值避免浮点数比较问题
            {
                FontSizeChanged?.Invoke(this, new FontSizeChangedEventArgs(_lastFontSize, currentFontSize));
                _lastFontSize = currentFontSize;
            }
        }
        
        private double GetCurrentFontSize()
        {
            try
            {
                var app = Application.Current;
                if (app != null && app.Resources.Contains("MaterialDesignFontSize"))
                {
                    return Convert.ToDouble(app.Resources["MaterialDesignFontSize"]);
                }
            }
            catch { }
            
            return 13; // 默认字体大小
        }
    }

    // 字体大小变化事件参数
    public class FontSizeChangedEventArgs : EventArgs
    {
        public double OldSize { get; }
        public double NewSize { get; }
        
        public FontSizeChangedEventArgs(double oldSize, double newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }
    }
} 