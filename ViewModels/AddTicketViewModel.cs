using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; // 添加用于调试输出
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class AddTicketViewModel : INotifyPropertyChanged
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
        // 添加重置表单操作标志
        private bool _isResetting = false;
        // 添加用于取消校验任务的取消令牌源
        private CancellationTokenSource _validationCancellationTokenSource;

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        public event EventHandler CloseWindow;

        /// <summary>
        /// 文本框聚焦事件
        /// </summary>
        public event EventHandler<TextBoxFocusEventArgs> FocusTextBox;

        /// <summary>
        /// 触发窗口关闭事件
        /// </summary>
        protected virtual void OnCloseWindow()
        {
            CloseWindow?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 触发文本框聚焦事件
        /// </summary>
        protected virtual void OnFocusTextBox(string tag)
        {
            FocusTextBox?.Invoke(this, new TextBoxFocusEventArgs(tag));
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

        // 支付渠道
        private bool _isAlipayPayment;
        private bool _isWeChatPayment;
        private bool _isABCPayment;
        private bool _isCCBPayment;
        private bool _isICBCPayment;
        private bool _isCMBPayment;
        private bool _isPSBCPayment;
        private bool _isBOCPayment;
        private bool _isCOMMPayment;

        // 车站数据
        private ObservableCollection<StationInfo> _stations;

        // 车站搜索相关属性
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private ObservableCollection<StationInfo> _arriveStationSuggestions;
        private bool _isDepartStationDropdownOpen;
        private bool _isArriveStationDropdownOpen;
        private string _departStationSearchText;
        private string _arriveStationSearchText;
        private double _dataGridHeaderFontSize = 15; // 表头字体大小
        private double _dataGridRowHeight = 40;     // 行高
        private double _dataGridCellFontSize = 14;  // 单元格字体大小

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

                // 初始化取消令牌源
                _validationCancellationTokenSource = new CancellationTokenSource();

                // 初始化命令
                SaveCommand = new RelayCommand(SaveTicket, CanSaveTicket);
                ResetCommand = new RelayCommand(PrepareReset);
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
                    "",
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
                _selectedSeatType = SeatTypes.FirstOrDefault() ?? "新空调硬座";
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

                // 初始化字体大小
                InitializeFontSizes();

                // 初始化完成后，设置初始化标志为false
                _isInitializing = false;

                // 应用支付渠道互斥逻辑
                if (IsAlipayPayment)
                {
                    HandlePaymentChannelMutualExclusion("Alipay");
                }
                else if (IsWeChatPayment) 
                {
                    HandlePaymentChannelMutualExclusion("WeChat");
                }
                else if (IsABCPayment)
                {
                    HandlePaymentChannelMutualExclusion("ABC");
                }
                else if (IsCCBPayment)
                {
                    HandlePaymentChannelMutualExclusion("CCB");
                }
                else if (IsICBCPayment)
                {
                    HandlePaymentChannelMutualExclusion("ICBC");
                }
                else if (IsCMBPayment) {
                    HandlePaymentChannelMutualExclusion("CMB");
                }
                else if (IsPSBCPayment)
                {
                    HandlePaymentChannelMutualExclusion("PSBC");
                }
                else if (IsBOCPayment)
                {
                    HandlePaymentChannelMutualExclusion("BOC");
                }
                else if (IsCOMMPayment){
                    HandlePaymentChannelMutualExclusion("COMM");
                }

                // 确保学生票和儿童票互斥逻辑正确应用
                if (IsStudentTicket)
                {
                    IsChildTicket = false;
                }
                else if (IsChildTicket)
                {
                    IsStudentTicket = false;
                }
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
                    Debug.WriteLine($"[DepartStation] 正在设置出发站: '{value}'");
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
                    Debug.WriteLine($"[ArriveStation] 正在设置到达站: '{value}'");
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
            set => HandleStationSearchTextChanged(value, ref _departStationSearchText, ref _departStation, nameof(DepartStationSearchText), nameof(DepartStation), true, ref _isUpdatingDepartStation);
        }

        public string ArriveStationSearchText
        {
            get => _arriveStationSearchText;
            set => HandleStationSearchTextChanged(value, ref _arriveStationSearchText, ref _arriveStation, nameof(ArriveStationSearchText), nameof(ArriveStation), false, ref _isUpdatingArriveStation);
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

                    // 学生票和儿童票互斥关系
                    if (!_isInitializing && value)
                    {
                        IsChildTicket = false; // 如果选择了学生票，取消儿童票选择
                    }

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

                    // 学生票和儿童票互斥关系
                    if (!_isInitializing && value)
                    {
                        IsStudentTicket = false; // 如果选择了儿童票，取消学生票选择
                    }

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        // 支付渠道属性
        public bool IsAlipayPayment
        {
            get => _isAlipayPayment;
            set
            {
                if (_isAlipayPayment != value)
                {
                    _isAlipayPayment = value;
                    OnPropertyChanged(nameof(IsAlipayPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("Alipay");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        public bool IsWeChatPayment
        {
            get => _isWeChatPayment;
            set
            {
                if (_isWeChatPayment != value)
                {
                    _isWeChatPayment = value;
                    OnPropertyChanged(nameof(IsWeChatPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("WeChat");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }

        // 控制支付渠道启用/禁用状态的属性
        private bool _isAlipayPaymentEnabled = true;
        public bool IsAlipayPaymentEnabled
        {
            get => _isAlipayPaymentEnabled;
            set
            {
                if (_isAlipayPaymentEnabled != value)
                {
                    _isAlipayPaymentEnabled = value;
                    OnPropertyChanged(nameof(IsAlipayPaymentEnabled));
                }
            }
        }

        private bool _isWeChatPaymentEnabled = true;
        public bool IsWeChatPaymentEnabled
        {
            get => _isWeChatPaymentEnabled;
            set
            {
                if (_isWeChatPaymentEnabled != value)
                {
                    _isWeChatPaymentEnabled = value;
                    OnPropertyChanged(nameof(IsWeChatPaymentEnabled));
                }
            }
        }

        public bool IsABCPayment
        {
            get => _isABCPayment;
            set
            {
                if (_isABCPayment != value)
                {
                    _isABCPayment = value;
                    OnPropertyChanged(nameof(IsABCPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("ABC");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsCCBPayment
        {
            get => _isCCBPayment;
            set
            {
                if (_isCCBPayment != value)
                {
                    _isCCBPayment = value;
                    OnPropertyChanged(nameof(IsCCBPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("CCB");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsICBCPayment
        {
            get => _isICBCPayment;
            set
            {
                if (_isICBCPayment != value)
                {
                    _isICBCPayment = value;
                    OnPropertyChanged(nameof(IsICBCPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("ICBC");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsCMBPayment
        {
            get => _isCMBPayment;
            set
            {
                if (_isCMBPayment != value)
                {
                    _isCMBPayment = value;
                    OnPropertyChanged(nameof(IsCMBPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("CMB");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsPSBCPayment
        {
            get => _isPSBCPayment;
            set
            {
                if (_isPSBCPayment != value)
                {
                    _isPSBCPayment = value;
                    OnPropertyChanged(nameof(IsPSBCPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("PSBC");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsBOCPayment
        {
            get => _isBOCPayment;
            set
            {
                if (_isBOCPayment != value)
                {
                    _isBOCPayment = value;
                    OnPropertyChanged(nameof(IsBOCPayment));

                    // 使用提取的互斥逻辑方法
                    HandlePaymentChannelMutualExclusion("BOC");

                    if (!_isInitializing) _isFormModified = true;
                }
            }
        }
        public bool IsCOMMPayment
        {
            get => _isCOMMPayment;
            set
            {
                if(_isCOMMPayment != value)
                {
                    _isCOMMPayment = value;
                    OnPropertyChanged(nameof(IsCOMMPayment));
                    HandlePaymentChannelMutualExclusion("COMM");
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
        /// 创建车票信息对象
        /// </summary>
        /// <returns>填充好数据的TrainRideInfo对象</returns>
        private TrainRideInfo CreateTicketInfo()
        {
            return new TrainRideInfo
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
        }

        /// <summary>
        /// 验证表单
        /// </summary>
        /// <returns>是否验证通过</returns>
        public virtual bool ValidateForm()
        {
            // 创建一个新的TrainRideInfo对象用于验证
            var ticket = CreateTicketInfo();

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
                // 创建一个错误消息列表，用于保存所有验证错误
                _validationErrors.Clear();
                StringBuilder errorMessages = new StringBuilder();

                // 创建TrainRideInfo对象用于验证和保存
                var ticket = CreateTicketInfo();

                // 1. 收集所有必填项错误，但不立即返回
                bool isBasicValidationPassed = FormValidationHelper.ValidateTicketForm(ticket, _validationErrors);

                // 2. 验证车站信息 (使用新的统一方法，收集错误信息但不显示警告)
                bool departHasError = false;
                bool arriveHasError = false;

                // 准备异步验证任务
                var departValidationTask = TryValidateAndSetStationInfoAsync(DepartStation?.Replace("站", "").Trim(), true, showWarning: false, errorMessages: errorMessages);
                var arriveValidationTask = TryValidateAndSetStationInfoAsync(ArriveStation?.Replace("站", "").Trim(), false, showWarning: false, errorMessages: errorMessages);
                
                // 并行等待验证结果
                await Task.WhenAll(departValidationTask, arriveValidationTask);
                
                var (departStatus, _) = departValidationTask.Result;
                var (arriveStatus, _) = arriveValidationTask.Result;
                
                // 检查验证状态 (0=成功, 1=不存在, 2=不完整)
                // 注意：TryValidateAndSetStationInfoAsync 内部已经将错误信息添加到 errorMessages
                // 只需要检查状态码是否为0即可判断是否有新增错误
                departHasError = departStatus != 0;
                arriveHasError = arriveStatus != 0;

                // 4. 组合所有错误信息
                // Check if basic validation failed OR if any station validation produced errors (collected in errorMessages)
                if (!isBasicValidationPassed || errorMessages.Length > 0 || _validationErrors.Count > 0)
                {
                    // 构建完整的错误信息
                    StringBuilder fullErrorMessage = new StringBuilder("请修正以下错误：\n");

                    // 先添加必填项错误 (来自 FormValidationHelper)
                    foreach (var error in _validationErrors)
                    {
                        fullErrorMessage.AppendLine($"- {error}");
                    }

                    // 如果有必填项错误和其他错误，添加一个分隔行
                    if (_validationErrors.Count > 0 && errorMessages.Length > 0)
                    {
                        fullErrorMessage.AppendLine();
                    }

                    // 添加车站匹配/完整性错误 (来自 TryValidateAndSetStationInfoAsync)
                    if (errorMessages.Length > 0)
                    {
                        // 移除最后一个换行符（如果存在）
                        string stationErrors = errorMessages.ToString().TrimEnd('\r', '\n');
                        fullErrorMessage.Append(stationErrors);
                    }

                    MessageBoxHelper.ShowWarning(fullErrorMessage.ToString(), "保存验证");
                    return;
                }

                // 5. 所有验证都通过，更新车票对象的格式化字段 (确保使用最新的验证过的代码和拼音)
                ticket.DepartStation = DepartStation + "站";
                ticket.ArriveStation = ArriveStation + "站";
                ticket.DepartStationCode = DepartStationCode; // 使用ViewModel中已更新的Code
                ticket.ArriveStationCode = ArriveStationCode; // 使用ViewModel中已更新的Code
                ticket.DepartStationPinyin = DepartStationPinyin; // 使用ViewModel中已更新的Pinyin
                ticket.ArriveStationPinyin = ArriveStationPinyin; // 使用ViewModel中已更新的Pinyin

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
        /// 准备重置表单，在执行实际重置前设置标志并取消所有校验任务
        /// </summary>
        private void PrepareReset()
        {
            try
            {
                // 先设置重置标志为true
                _isResetting = true;

                // 取消所有正在进行的校验任务
                if (_validationCancellationTokenSource != null)
                {
                    _validationCancellationTokenSource.Cancel();
                    _validationCancellationTokenSource.Dispose();
                    _validationCancellationTokenSource = new CancellationTokenSource();
                }

                // 直接同步执行重置操作，不再使用异步方式
                ResetForm();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"准备重置表单时出错: {ex.Message}", ex);
                // 确保标志被重置
                _isResetting = false;
            }
        }

        /// <summary>
        /// 重置表单
        /// </summary>
        public virtual void ResetForm()
        {
            try
            {
                Debug.WriteLine("开始重置表单...");

                // 重置所有属性
                TicketNumber = string.Empty;
                CheckInLocation = string.Empty;

                DepartStationSearchText = string.Empty;
                ArriveStationSearchText = string.Empty;

                DepartStation = string.Empty;
                ArriveStation = string.Empty;

                DepartStationPinyin = string.Empty;
                ArriveStationPinyin = string.Empty;

                DepartStationCode = string.Empty;
                ArriveStationCode = string.Empty;

                Money = 0;

                DepartDate = DateTime.Today;
                DepartHour = 0;
                DepartMinute = 0;

                SelectedTrainType = null;
                TrainNumber = string.Empty;
                CoachNo = string.Empty;
                IsExtraCoach = false;
                SeatNo = string.Empty;
                IsNoSeat = false;

                SelectedSeatType = null;
                SeatPositions.Clear();
                SelectedSeatPosition = null;

                SelectedAdditionalInfo = null;
                SelectedTicketPurpose = null;

                SelectedHint = null;
                CustomHint = null;

                IsStudentTicket = false;
                IsDiscountTicket = false;
                IsOnlineTicket = false;
                IsChildTicket = false;

                IsAlipayPayment = false;
                IsWeChatPayment = false;
                IsABCPayment = false;
                IsCCBPayment = false;
                IsICBCPayment = false;
                IsCMBPayment = false;
                IsPSBCPayment = false;
                IsBOCPayment = false;
                IsCOMMPayment = false;

                SelectedTicketModificationType = null;

                // 重置表单修改状态
                _isFormModified = false;

                // 重置验证错误列表
                _validationErrors.Clear();

                // 根据默认座位类型更新座位位置选项
                UpdateSeatPositions();

                // 将焦点设置到取票号输入框
                OnFocusTextBox("TicketNumber");

                // 使用Dispatcher确保焦点设置在UI更新后执行
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OnFocusTextBox("TicketNumber");
                }, DispatcherPriority.Render);

                Debug.WriteLine("表单重置完成，焦点已设置到取票号输入框");

                // 重置完成，设置重置标志为false
                _isResetting = false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("重置表单时出错", ex);
                MessageBoxHelper.ShowError("重置表单时出错: " + ex.Message);
                _isResetting = false;
            }
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

        private async void SelectStation(StationInfo station, bool isDepartStation)
        {
            if (station == null)
                return;

            // 获取选择的车站名称 (不含'站')
            string stationName = station.StationName?.Replace("站", "") ?? string.Empty;

            // 关闭下拉框
            if (isDepartStation)
            {
                IsDepartStationDropdownOpen = false;
                
                // 设置出发站文本 (标记为正在更新，避免触发重复搜索)
                _isUpdatingDepartStation = true;
                DepartStationSearchText = stationName;
                DepartStation = stationName; 
                _isUpdatingDepartStation = false;
                
                // 使用新方法更新代码和拼音 (不需要显示警告，因为刚选择)
                await TryValidateAndSetStationInfoAsync(stationName, true, showWarning: false);
            }
            else
            {
                IsArriveStationDropdownOpen = false;
                
                // 设置到达站文本 (标记为正在更新，避免触发重复搜索)
                _isUpdatingArriveStation = true;
                ArriveStationSearchText = stationName;
                ArriveStation = stationName; 
                _isUpdatingArriveStation = false;
                
                 // 使用新方法更新代码和拼音 (不需要显示警告，因为刚选择)
                await TryValidateAndSetStationInfoAsync(stationName, false, showWarning: false);
            }

            // 注意: 原 CheckStationInfoCompleteness 的逻辑已包含在 TryValidateAndSetStationInfoAsync 中，
            // 且 showWarning 设置为 false，因此这里不需要额外调用。
        }

        private void SelectDepartStation(StationInfo station)
        {
            SelectStation(station, true);
        }

        private void SelectArriveStation(StationInfo station)
        {
            SelectStation(station, false);
        }

        /// <summary>
        /// 检查车站信息的完整性
        /// </summary>
        /// <param name="station">车站信息对象</param>
        /// <param name="stationName">车站名称</param>
        /// <param name="isDepartStation">是否为出发站</param>
        /// <returns>返回是否完整 (true=完整, false=不完整)</returns>
        private bool CheckStationCompleteness(StationInfo station, string stationName, bool isDepartStation)
        {
            if (station == null) return false;
            
            // 检测车站信息是否完整
            bool hasStationCode = !string.IsNullOrWhiteSpace(station.StationCode);
            bool hasStationPinyin = !string.IsNullOrWhiteSpace(station.StationPinyin);
            
            if (!hasStationCode || !hasStationPinyin)
            {
                // 构建缺失信息列表
                List<string> missingInfo = new List<string>();
                if (!hasStationCode) missingInfo.Add("station_code");
                if (!hasStationPinyin) missingInfo.Add("station_pinyin");

                string displayName = station.StationName?.Replace("站", "") ?? stationName;
                string missingItems = string.Join("、", missingInfo);

                // 弹出警告
                MessageBoxHelper.ShowWarning($"车站【{displayName}站】信息不完整，缺少：{missingItems}，请在车站中心中完善该车站信息", "车站信息不完整");

                // 将焦点设回文本框
                OnFocusTextBox(isDepartStation ? "Depart" : "Arrive");
                
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 检测站点信息完整性
        /// </summary>
        /// <param name="station">站点信息</param>
        /// <param name="isDepartStation">是否为出发站</param>
        private void CheckStationInfoCompleteness(StationInfo station, bool isDepartStation)
        {
            try
            {
                if (station == null) return;

                // 确保不是用户点击了命令按钮
                if (Mouse.DirectlyOver is Button button)
                {
                    // 如果当前鼠标悬停在命令按钮上，则不进行校验
                    if (button.Command != null &&
                        (button.Command == SaveCommand || button.Command == ResetCommand ||
                         button.Name == "MinimizeButton" || button.Name == "CloseButton"))
                    {
                        return;
                    }
                }

                // 使用提取的公共方法检查车站信息完整性
                CheckStationCompleteness(station, station.StationName, isDepartStation);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"检测站点信息完整性时出错: {ex.Message}", ex);
            }
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

        /// <summary>
        /// 处理车站搜索文本框内容变化
        /// </summary>
        private void HandleStationSearchTextChanged(string newValue, ref string backingFieldSearchText, ref string backingFieldStationName, string propertyNameSearchText, string propertyNameStationName, bool isDepart, ref bool isUpdatingFlag)
        {
            if (backingFieldSearchText != newValue)
            {
                backingFieldSearchText = newValue;
                OnPropertyChanged(propertyNameSearchText);

                // 如果是通过选择项更新的，不触发搜索
                if (!isUpdatingFlag)
                {
                    // 移除"站"字后搜索
                    string searchText = newValue?.Replace("站", "").Trim() ?? string.Empty;
                    SearchStations(searchText, isDepart);

                    // 同步更新 DepartStation / ArriveStation 属性
                    if (backingFieldStationName != newValue)
                    {
                        backingFieldStationName = newValue;
                        OnPropertyChanged(propertyNameStationName);
                        // 清除关联的代码和拼音，因为用户正在输入新站名
                        if (isDepart)
                        {
                            DepartStationCode = string.Empty;
                            DepartStationPinyin = string.Empty;
                        }
                        else
                        {
                            ArriveStationCode = string.Empty;
                            ArriveStationPinyin = string.Empty;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理站点输入框失去焦点事件
        /// </summary>
        /// <param name="isDepartStation">是否为出发站</param>
        public virtual async void OnStationLostFocus(bool isDepartStation)
        {
            try
            {
                // 快速检测是否正在重置，优先级最高
                if (_isResetting)
                {
                    return;
                }

                // 检测鼠标是否在重置按钮上等逻辑保持不变...
                if (Mouse.DirectlyOver is FrameworkElement element)
                {
                    Button button = FindVisualParent<Button>(element);
                    if (button != null && (button.Command == ResetCommand || button.Name == "CloseButton")) // 增加CloseButton判断
                    {
                        _isResetting = (button.Command == ResetCommand); // 只有Reset才设置标志
                        return;
                    }
                }
                if (Mouse.DirectlyOver is Button clickedButton && 
                    (clickedButton.Command == SaveCommand || clickedButton.Name == "MinimizeButton" ))
                {
                    return;
                }
                 // 检测是否在下拉框上操作或下拉框刚关闭
                if (isDepartStation && IsDepartStationDropdownOpen) return;
                if (!isDepartStation && IsArriveStationDropdownOpen) return;
                // 添加检测：如果焦点目标是下拉列表项，也暂时不校验
                if (FocusManager.GetFocusedElement(Application.Current.MainWindow) is ListBoxItem) return;
                

                string stationName = isDepartStation ? DepartStation?.Replace("站", "").Trim() : ArriveStation?.Replace("站", "").Trim();
                string currentCode = isDepartStation ? DepartStationCode : ArriveStationCode;
                string currentPinyin = isDepartStation ? DepartStationPinyin : ArriveStationPinyin;

                // 如果站名、代码、拼音都为空，则不校验
                if (string.IsNullOrWhiteSpace(stationName) && string.IsNullOrWhiteSpace(currentCode) && string.IsNullOrWhiteSpace(currentPinyin))
                {
                     Debug.WriteLine($"[OnStationLostFocus] {(isDepartStation ? "Depart" : "Arrive")} station info is all empty, skipping validation.");
                    return;
                }
                

                // 获取当前的取消令牌
                var cancellationToken = _validationCancellationTokenSource.Token;

                // 使用新的统一方法进行验证和信息设置，并显示警告
                var validationTask = TryValidateAndSetStationInfoAsync(stationName, isDepartStation, showWarning: true);

                // 不需要 ContinueWith 了，因为 TryValidateAndSetStationInfoAsync 内部处理了UI更新和错误显示
                await validationTask; // 等待异步验证完成

                // 检测任务是否已取消 (虽然上面已经return，但以防万一)
                if (cancellationToken.IsCancellationRequested || _isResetting)
                {
                    Debug.WriteLine($"[OnStationLostFocus] Validation for {(isDepartStation ? "Depart" : "Arrive")} cancelled or form resetting.");
                    return;
                }
                
                Debug.WriteLine($"[OnStationLostFocus] Validation process completed for {(isDepartStation ? "Depart" : "Arrive")} station: {stationName}.");

            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[OnStationLostFocus] Operation cancelled for {(isDepartStation ? "Depart" : "Arrive")} station.");
                // 忽略取消的操作
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理站点输入框失去焦点事件时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 查找可视元素的父元素
        /// </summary>
        /// <typeparam name="T">要查找的父元素类型</typeparam>
        /// <param name="child">子元素</param>
        /// <returns>找到的父元素，如果没有则返回null</returns>
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        private void HandlePaymentChannelMutualExclusion(string selectedChannel)
        {
            if (_isInitializing) return;

            // 定义银行渠道列表
            var bankChannels = new List<string> { "ABC", "CCB", "ICBC", "CMB", "PSBC", "BOC", "COMM" };

            // 支付宝和微信的互斥逻辑
            if (selectedChannel == "Alipay")
            {
                if (IsAlipayPayment)
                {
                    // 确保不触发连锁反应导致栈溢出
                    if (IsWeChatPayment)
                    {
                        _isWeChatPayment = false;
                        OnPropertyChanged(nameof(IsWeChatPayment));
                    }
                    IsWeChatPaymentEnabled = false;
                }
                else
                {
                    IsWeChatPaymentEnabled = true;
                }
            }
            else if (selectedChannel == "WeChat")
            {
                if (IsWeChatPayment)
                {
                    // 确保不触发连锁反应导致栈溢出
                    if (IsAlipayPayment)
                    {
                        _isAlipayPayment = false;
                        OnPropertyChanged(nameof(IsAlipayPayment));
                    }
                    IsAlipayPaymentEnabled = false;
                }
                else
                {
                    IsAlipayPaymentEnabled = true;
                }
            }
            // 银行渠道的互斥逻辑
            else if (bankChannels.Contains(selectedChannel))
            {
                // 获取当前选中的银行属性值
                bool isSelected = GetBankPaymentStatus(selectedChannel);

                // 如果当前银行被选中，则取消其他所有银行的选择
                if (isSelected)
                {
                    // 遍历所有银行渠道
                    foreach (var bank in bankChannels)
                    {
                        // 如果不是当前选中的银行，并且该银行当前是选中状态，则取消选择
                        if (bank != selectedChannel && GetBankPaymentStatus(bank))
                        {
                            SetBankPaymentStatus(bank, false);
                        }
                    }
                }
            }
        }

        // 辅助方法：根据渠道名称获取银行支付状态
        private bool GetBankPaymentStatus(string channel)
        {
            switch (channel)
            {
                case "ABC": return IsABCPayment;
                case "CCB": return IsCCBPayment;
                case "ICBC": return IsICBCPayment;
                case "CMB": return IsCMBPayment;
                case "PSBC": return IsPSBCPayment;
                case "BOC": return IsBOCPayment;
                case "COMM": return IsCOMMPayment;
                default: return false;
            }
        }

        // 辅助方法：根据渠道名称设置银行支付状态
        private void SetBankPaymentStatus(string channel, bool status)
        {
            switch (channel)
            {
                case "ABC":
                    // 直接修改字段避免递归调用setter
                    if (_isABCPayment != status) { _isABCPayment = status; OnPropertyChanged(nameof(IsABCPayment)); }
                    break;
                case "CCB":
                    if (_isCCBPayment != status) { _isCCBPayment = status; OnPropertyChanged(nameof(IsCCBPayment)); }
                    break;
                case "ICBC":
                    if (_isICBCPayment != status) { _isICBCPayment = status; OnPropertyChanged(nameof(IsICBCPayment)); }
                    break;
                case "CMB":
                    if (_isCMBPayment != status) { _isCMBPayment = status; OnPropertyChanged(nameof(IsCMBPayment)); }
                    break;
                case "PSBC":
                    if (_isPSBCPayment != status) { _isPSBCPayment = status; OnPropertyChanged(nameof(IsPSBCPayment)); }
                    break;
                case "BOC":
                    if (_isBOCPayment != status) { _isBOCPayment = status; OnPropertyChanged(nameof(IsBOCPayment)); }
                    break;
                case "COMM":
                    if (_isCOMMPayment != status) { _isCOMMPayment = status; OnPropertyChanged(nameof(IsCOMMPayment)); }
                    break;
            }
        }

        private bool ValidateStationInfo(string stationName, string stationCode, string stationPinyin, 
                                       bool isDepart, StringBuilder errorMessages)
        {
            if (string.IsNullOrWhiteSpace(stationName)) return true;
            
            bool hasError = false;
            
            // 通过站名查找站点信息
            var stationByName = _stationSearchService.Stations
                .FirstOrDefault(s => s.StationName == stationName ||
                                   s.StationName == stationName + "站" ||
                                   s.StationName?.Replace("站", "") == stationName);
                                   
            // 通过代码查找站点信息
            var stationByCode = !string.IsNullOrWhiteSpace(stationCode) ?
                _stationSearchService.Stations.FirstOrDefault(s => s.StationCode == stationCode) : null;
                
            string stationType = isDepart ? "出发站" : "到达站";
            
            // 如果站名能找到，但代码不匹配或为空
            if (stationByName != null)
            {
                // 检测代码是否匹配或为空
                if (string.IsNullOrWhiteSpace(stationCode))
                {
                    // 将代码为空的情况添加到验证错误
                    string errorMsg = $"未填写{stationType}代码";
                    if (!_validationErrors.Any(e => e.Contains(errorMsg)))
                    {
                        _validationErrors.Add(errorMsg);
                    }
                }
                else if (stationByCode == null || stationByName.Id != stationByCode.Id)
                {
                    // 代码不匹配
                    hasError = true;
                    errorMessages.AppendLine($"{stationType}【{stationName}】的代码错误：");
                    errorMessages.AppendLine($"- 当前填写的代码【{stationCode}】与车站不匹配");
                    errorMessages.AppendLine($"- 正确的代码应为：【{stationByName.StationCode}】");
                    errorMessages.AppendLine();
                }

                // 检测拼音是否匹配或为空
                if (string.IsNullOrWhiteSpace(stationPinyin))
                {
                    // 将拼音为空的情况添加到验证错误
                    string errorMsg = $"未填写{stationType}拼音";
                    if (!_validationErrors.Any(e => e.Contains(errorMsg)))
                    {
                        _validationErrors.Add(errorMsg);
                    }
                }
                else if (stationPinyin != stationByName.StationPinyin)
                {
                    // 拼音不匹配
                    hasError = true;
                    errorMessages.AppendLine($"{stationType}【{stationName}】的拼音错误：");
                    errorMessages.AppendLine($"- 当前填写的拼音【{stationPinyin}】与车站记录不匹配");
                    errorMessages.AppendLine($"- 正确的拼音应为：【{stationByName.StationPinyin}】");
                    errorMessages.AppendLine();
                }
            }
            // 如果无法通过站名找到匹配的车站
            else if (stationByName == null && stationByCode == null)
            {
                hasError = true;
                errorMessages.AppendLine($"{stationType}【{stationName}】在车站中心不存在，请先添加该车站信息。");
                errorMessages.AppendLine();
            }
            
            return !hasError;
        }

        // --- Start of New Helper Methods ---

        /// <summary>
        /// 尝试验证并设置车站信息（代码、拼音）。
        /// </summary>
        /// <param name="stationName">要验证的车站名称（不含'站'）。</param>
        /// <param name="isDepart">是否为出发站。</param>
        /// <param name="station">如果找到并验证通过，输出找到的车站信息。</param>
        /// <param name="errorMessages">用于收集验证错误的StringBuilder，如果为null则不收集。</param>
        /// <param name="showWarning">是否在信息不完整时显示MessageBox警告。</param>
        /// <returns>返回验证结果：0=成功, 1=车站不存在, 2=车站信息不完整。</returns>
        private async Task<(int Status, StationInfo Station)> TryValidateAndSetStationInfoAsync(string stationName, bool isDepart, bool showWarning = true, StringBuilder errorMessages = null)
        {
            if (string.IsNullOrWhiteSpace(stationName))
            {
                // 如果站名为空，清除相关信息并返回成功（不进行验证）
                if (isDepart)
                {
                    DepartStationPinyin = string.Empty;
                    DepartStationCode = string.Empty;
                }
                else
                {
                    ArriveStationPinyin = string.Empty;
                    ArriveStationCode = string.Empty;
                }
                return (0, null);
            }

            // 确保StationSearchService已初始化
            if (!_stationSearchService.IsInitialized)
            {
                await _stationSearchService.InitializeAsync();
            }

            // 使用StationSearchService进行验证
            var (status, station) = await _stationSearchService.ValidateStationCompleteAsync(stationName);
            string stationType = isDepart ? "出发站" : "到达站";

            switch (status)
            {
                case 0: // 验证通过
                    // 更新代码和拼音
                    if (isDepart)
                    {
                        DepartStationPinyin = station.StationPinyin ?? string.Empty;
                        DepartStationCode = station.StationCode ?? string.Empty;
                    }
                    else
                    {
                        ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                        ArriveStationCode = station.StationCode ?? string.Empty;
                    }
                    break;

                case 1: // 车站不存在
                    string notExistMsg = $"{stationType}【{stationName}】在车站中心不存在，请先添加该车站信息。";
                    if (errorMessages != null)
                    {
                        errorMessages.AppendLine(notExistMsg);
                        errorMessages.AppendLine();
                    }
                    else if (showWarning)
                    {
                        if (Stations.Count == 0)
                        {
                             MessageBoxHelper.ShowWarning("车站表为空，请在车站中心中添加一些车站再来添加车票", "车站信息不完整");
                        }
                        else
                        {
                             MessageBoxHelper.ShowWarning($"车站表内不存在车站【{stationName}】，请确认是否输入错误或者在车站中心中添加该车站信息", "车站不存在");
                        }
                        OnFocusTextBox(isDepart ? "Depart" : "Arrive");
                    }
                    break;

                case 2: // 车站信息不完整
                    bool hasCode = !string.IsNullOrWhiteSpace(station.StationCode);
                    bool hasPinyin = !string.IsNullOrWhiteSpace(station.StationPinyin);
                    List<string> missingInfo = new List<string>();
                    if (!hasCode) missingInfo.Add("station_code");
                    if (!hasPinyin) missingInfo.Add("station_pinyin");
                    string missingItems = string.Join("、", missingInfo);
                    string incompleteMsg = $"{stationType}【{stationName}站】信息不完整，缺少：{missingItems}，请在车站中心中完善该车站信息。";

                    if (errorMessages != null)
                    {
                        errorMessages.AppendLine(incompleteMsg);
                        errorMessages.AppendLine();
                    }
                    else if (showWarning)
                    {
                        MessageBoxHelper.ShowWarning(incompleteMsg, "车站信息不完整");
                        OnFocusTextBox(isDepart ? "Depart" : "Arrive");
                    }
                    // 即使信息不完整，也尝试填充已知信息
                    if (isDepart)
                    {
                         DepartStationPinyin = station.StationPinyin ?? string.Empty;
                         DepartStationCode = station.StationCode ?? string.Empty;
                    }
                    else
                    {
                         ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                         ArriveStationCode = station.StationCode ?? string.Empty;
                    }
                    break;
            }
            return (status, station);
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
        /// 检测表单是否有未保存的修改
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


        private int GetTicketTypeFlags()
        {
            int flags = 0;
            if (IsStudentTicket) flags |= (int)TicketTypeFlags.StudentTicket;
            if (IsDiscountTicket) flags |= (int)TicketTypeFlags.DiscountTicket;
            if (IsOnlineTicket) flags |= (int)TicketTypeFlags.OnlineTicket;
            if (IsChildTicket) flags |= (int)TicketTypeFlags.ChildTicket;
            return flags;
        }

        private int GetPaymentChannelFlags()
        {
            int flags = 0;
            if (IsAlipayPayment) flags |= (int)PaymentChannelFlags.Alipay;
            if (IsWeChatPayment) flags |= (int)PaymentChannelFlags.WeChat;
            if (IsABCPayment) flags |= (int)PaymentChannelFlags.ABC;
            if (IsCCBPayment) flags |= (int)PaymentChannelFlags.CCB;
            if (IsICBCPayment) flags |= (int)PaymentChannelFlags.ICBC;
            if (IsCMBPayment) flags |= (int)PaymentChannelFlags.CMB;
            if (IsPSBCPayment) flags |= (int)PaymentChannelFlags.PSBC;
            if (IsBOCPayment) flags |= (int)PaymentChannelFlags.BOC;
            if (IsCOMMPayment) flags |= (int)PaymentChannelFlags.COMM;
            return flags;
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
            // 使用计时器定期检测字体大小变化
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500); // 每500毫秒检测一次
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

    // 文本框焦点事件参数
    public class TextBoxFocusEventArgs : EventArgs
    {
        public string TextBoxTag { get; }

        public TextBoxFocusEventArgs(string textBoxTag)
        {
            TextBoxTag = textBoxTag;
        }
    }
}