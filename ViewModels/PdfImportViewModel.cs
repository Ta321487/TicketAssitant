using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Models;
using System.Linq;
using System.Collections.Generic;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// PDF导入视图模型
    /// </summary>
    public class PdfImportViewModel : BaseViewModel
    {
        private readonly PdfImportService _pdfImportService;
        private readonly MainViewModel _mainViewModel;
        private readonly StationSearchService _stationSearchService;
        
        private string _pdfContent = string.Empty;
        private string _selectedPdfPath = string.Empty;
        private bool _isLoading = false;
        private bool _isLoadingEnabled;
        private bool _isPaymentMethodEnabled;
        private bool _isExpandPanelEnabled;
        private string _noDataText = "暂无数据";

        // 表单相关私有字段
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
        private string _selectedHint;
        private string _customHint;
        private string _selectedTicketModificationType;

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

        // 车站搜索相关属性
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private ObservableCollection<StationInfo> _arriveStationSuggestions;
        private bool _isDepartStationDropdownOpen;
        private bool _isArriveStationDropdownOpen;
        private string _departStationSearchText;
        private string _arriveStationSearchText;
        private StationInfo _selectedDepartStation;
        private StationInfo _selectedArriveStation;

        // 用于临时禁止通知和互斥逻辑的标志
        private bool _suppressNotifications = false;
        private bool _ignoreSearchTextChange = false;

        // --- 添加用于字段解锁的属性 ---
        private bool _isQuestionButtonEnabled;
        private bool _isTicketNumberEnabled;
        private bool _isCheckInLocationEnabled;
        private bool _isDepartStationEnabled;
        private bool _isArriveStationEnabled;
        private bool _isDepartStationPinyinEnabled;
        private bool _isArriveStationPinyinEnabled;
        private bool _isMoneyEnabled;
        private bool _isDepartStationCodeEnabled;
        private bool _isArriveStationCodeEnabled;
        private bool _isDepartDateEnabled;
        private bool _isTrainTypeEnabled;
        private bool _isTrainNumberEnabled;
        private bool _isDepartTimeEnabled;
        private bool _isCoachNoEnabled;
        private bool _isExtraCoachEnabled;
        private bool _isSeatNoEnabled;
        private bool _isNoSeatEnabled;
        private bool _isSeatPositionEnabled;
        private bool _isSeatTypeEnabled;
        private bool _isAdditionalInfoEnabled;
        private bool _isTicketPurposeEnabled;
        private bool _isHintEnabled;
        private bool _isCustomHintEnabled;
        private bool _isTicketModificationTypeEnabled;
        private bool _isTicketTypeEnabled;

        /// <summary>
        /// 主视图模型，用于主题和字体大小绑定
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        /// <param name="pdfImportService">PDF导入服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        public PdfImportViewModel(MainViewModel mainViewModel, PdfImportService pdfImportService, StationSearchService stationSearchService)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _pdfImportService = pdfImportService ?? throw new ArgumentNullException(nameof(pdfImportService));
            _stationSearchService = stationSearchService ?? throw new ArgumentNullException(nameof(stationSearchService));

            // 初始化命令
            SelectPdfCommand = new RelayCommand(SelectPdfFile);
            ImportTicketCommand = new RelayCommand(ImportTicket, CanImportTicket);
            CancelCommand = new RelayCommand(Cancel);
            ToggleFieldCommand = new RelayCommand<string>(ToggleField);
            SelectDepartStationCommand = new RelayCommand<StationInfo>(SelectDepartStation);
            SelectArriveStationCommand = new RelayCommand<StationInfo>(SelectArriveStation);
            
            // 初始化表单相关集合
            TrainTypes = new ObservableCollection<string> { "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字" };
            HourOptions = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            MinuteOptions = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();

            SeatTypes = new ObservableCollection<string>
            {
                "新空调硬座", "软座", "新空调硬卧", "新空调软卧",
                "商务座", "特等座","一等座", "二等座", "硬卧代硬座"
            };

            // 初始化附加信息相关集合
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

            // 初始化车票改签类型选项
            TicketModificationTypes = new ObservableCollection<string>
            {
                "始发改签",
                "变更到站"
            };

            // 初始化座位位置集合
            SeatPositions = new ObservableCollection<string> { "A", "B", "C", "D", "F" };

            // 初始化车站搜索相关集合
            DepartStationSuggestions = new ObservableCollection<StationInfo>();
            ArriveStationSuggestions = new ObservableCollection<StationInfo>();

            // 初始化并加载车站数据
            Task.Run(async () => await _stationSearchService.InitializeAsync());
            
            // 重置字段启用状态
            ResetFormFieldsState();

            // 注册属性变更事件
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// 处理属性变更事件
        /// </summary>
        private async void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 防止无限递归（当我们在触发属性变更事件时执行操作导致再次触发相同事件）
            if (_suppressNotifications)
                return;

            // 当出发车站搜索文本变更时，进行搜索
            if (e.PropertyName == nameof(DepartStationSearchText) && !string.IsNullOrWhiteSpace(DepartStationSearchText))
            {
                // 如果设置了忽略标记，则跳过搜索
                if (_ignoreSearchTextChange)
                    return;
                
                try
                {
                    await SearchDepartStationsAsync(DepartStationSearchText);
                    
                    // 当搜索结果为空且不是输入初期时，显示站名不存在提示
                    if (DepartStationSuggestions.Count == 0 && DepartStationSearchText.Length > 1 && IsDepartStationEnabled)
                    {
                        // 使用MessageBoxHelper显示警告对话框
                        string normalizedStationName = StationNameHelper.RemoveStationSuffix(DepartStationSearchText);
                        MessageBoxHelper.ShowWarning($"出发车站【{normalizedStationName}】在车站中心不存在，请先添加该车站信息。");
                        DepartStationPinyin = string.Empty;
                        DepartStationCode = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"搜索出发车站时出错: {ex.Message}", ex);
                }
                
                // 如果用户已经启用了编辑模式，则尝试根据输入搜索并填充车站代码和拼音
                if (IsDepartStationEnabled && !string.IsNullOrWhiteSpace(DepartStationSearchText))
                {
                    DepartStation = DepartStationSearchText;
                }
            }
            
            // 当到达车站搜索文本变更时，进行搜索
            if (e.PropertyName == nameof(ArriveStationSearchText) && !string.IsNullOrWhiteSpace(ArriveStationSearchText))
            {
                // 如果设置了忽略标记，则跳过搜索
                if (_ignoreSearchTextChange)
                    return;
                
                try
                {
                    await SearchArriveStationsAsync(ArriveStationSearchText);
                    
                    // 当搜索结果为空且不是输入初期时，显示站名不存在提示
                    if (ArriveStationSuggestions.Count == 0 && ArriveStationSearchText.Length > 1 && IsArriveStationEnabled)
                    {
                        // 使用MessageBoxHelper显示警告对话框
                        string normalizedStationName = StationNameHelper.RemoveStationSuffix(ArriveStationSearchText);
                        MessageBoxHelper.ShowWarning($"到达车站【{normalizedStationName}】在车站中心不存在，请先添加该车站信息。");
                        ArriveStationPinyin = string.Empty;
                        ArriveStationCode = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"搜索到达车站时出错: {ex.Message}", ex);
                }
                
                // 如果用户已经启用了编辑模式，则尝试根据输入搜索并填充车站代码和拼音
                if (IsArriveStationEnabled && !string.IsNullOrWhiteSpace(ArriveStationSearchText))
                {
                    ArriveStation = ArriveStationSearchText;
                }
            }
        }

        /// <summary>
        /// PDF内容
        /// </summary>
        public string PdfContent
        {
            get => _pdfContent;
            set
            {
                if (_pdfContent != value)
                {
                    _pdfContent = value;
                    OnPropertyChanged(nameof(PdfContent));
                }
            }
        }

        /// <summary>
        /// 选中的PDF文件路径
        /// </summary>
        public string SelectedPdfPath
        {
            get => _selectedPdfPath;
            set
            {
                if (_selectedPdfPath != value)
                {
                    _selectedPdfPath = value;
                    OnPropertyChanged(nameof(SelectedPdfPath));
                    OnPropertyChanged(nameof(HasSelectedPdf));
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
        /// 是否已选择PDF文件
        /// </summary>
        public bool HasSelectedPdf => !string.IsNullOrEmpty(SelectedPdfPath);

        /// <summary>
        /// 选择PDF文件命令
        /// </summary>
        public ICommand SelectPdfCommand { get; }

        /// <summary>
        /// 导入车票命令
        /// </summary>
        public ICommand ImportTicketCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 切换字段编辑状态命令
        /// </summary>
        public ICommand ToggleFieldCommand { get; }

        /// <summary>
        /// 选择出发车站命令
        /// </summary>
        public ICommand SelectDepartStationCommand { get; }
        
        /// <summary>
        /// 选择到达车站命令
        /// </summary>
        public ICommand SelectArriveStationCommand { get; }

        #region 表单相关属性

        /// <summary>
        /// 取票号
        /// </summary>
        public string TicketNumber
        {
            get => _ticketNumber;
            set
            {
                if (_ticketNumber != value)
                {
                    if(!string.IsNullOrEmpty(value)){
                        _ticketNumber = FormValidationHelper.EnsureFirstLetterUpperCase(value);
                    }
                    else{
                        _ticketNumber = value;
                    }
                    OnPropertyChanged(nameof(TicketNumber));
                }
            }
        }

        /// <summary>
        /// 检票位置
        /// </summary>
        public string CheckInLocation
        {
            get => _checkInLocation;
            set
            {
                if (_checkInLocation != value)
                {
                    if(!string.IsNullOrEmpty(value)){
                        _checkInLocation = FormValidationHelper.EnsureFirstLetterUpperCase(value);
                    }
                    else{
                        _checkInLocation = value;
                    }
                    OnPropertyChanged(nameof(CheckInLocation));
                }
            }
        }

        /// <summary>
        /// 出发车站
        /// </summary>
        public string DepartStation
        {
            get => _departStation;
            set
            {
                if (_departStation != value)
                {
                    _departStation = value;
                    OnPropertyChanged(nameof(DepartStation));
                    
                    // 当手动输入站名时，尝试自动匹配站点代码和拼音
                    if (!string.IsNullOrEmpty(value))
                    {
                        SearchDepartStationAsync(value);
                    }
                }
            }
        }

        /// <summary>
        /// 到达车站
        /// </summary>
        public string ArriveStation
        {
            get => _arriveStation;
            set
            {
                if (_arriveStation != value)
                {
                    _arriveStation = value;
                    OnPropertyChanged(nameof(ArriveStation));
                    
                    // 当手动输入站名时，尝试自动匹配站点代码和拼音
                    if (!string.IsNullOrEmpty(value))
                    {
                        SearchArriveStationAsync(value);
                    }
                }
            }
        }

        /// <summary>
        /// 出发车站拼音
        /// </summary>
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

        /// <summary>
        /// 到达车站拼音
        /// </summary>
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

        /// <summary>
        /// 金额
        /// </summary>
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

        /// <summary>
        /// 出发车站代码
        /// </summary>
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

        /// <summary>
        /// 到达车站代码
        /// </summary>
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

        /// <summary>
        /// 出发日期
        /// </summary>
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

        /// <summary>
        /// 出发小时
        /// </summary>
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

        /// <summary>
        /// 出发分钟
        /// </summary>
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

        /// <summary>
        /// 选中的车型
        /// </summary>
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

        /// <summary>
        /// 车次号
        /// </summary>
        public string TrainNumber
        {
            get => _trainNumber;
            set
            {
                if (_trainNumber != value)
                {
                    _trainNumber = value;
                    OnPropertyChanged(nameof(TrainNumber));
                }
            }
        }

        /// <summary>
        /// 车厢号
        /// </summary>
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

        /// <summary>
        /// 是否加车
        /// </summary>
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

        /// <summary>
        /// 座位号
        /// </summary>
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

        /// <summary>
        /// 是否无座
        /// </summary>
        public bool IsNoSeat
        {
            get => _isNoSeat;
            set
            {
                if (_isNoSeat != value)
                {
                    _isNoSeat = value;
                    OnPropertyChanged(nameof(IsNoSeat));
                }
            }
        }

        /// <summary>
        /// 选中的座位位置
        /// </summary>
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

        /// <summary>
        /// 选中的座位类型
        /// </summary>
        public string SelectedSeatType
        {
            get => _selectedSeatType;
            set
            {
                if (_selectedSeatType != value)
                {
                    _selectedSeatType = value;
                    OnPropertyChanged(nameof(SelectedSeatType));
                }
            }
        }

        /// <summary>
        /// 选中的附加信息
        /// </summary>
        public string SelectedAdditionalInfo
        {
            get => _selectedAdditionalInfo;
            set
            {
                if (_selectedAdditionalInfo != value)
                {
                    _selectedAdditionalInfo = value;
                    OnPropertyChanged(nameof(SelectedAdditionalInfo));
                }
            }
        }

        /// <summary>
        /// 选中的车票用途
        /// </summary>
        public string SelectedTicketPurpose
        {
            get => _selectedTicketPurpose;
            set
            {
                if (_selectedTicketPurpose != value)
                {
                    _selectedTicketPurpose = value;
                    OnPropertyChanged(nameof(SelectedTicketPurpose));
                }
            }
        }

        /// <summary>
        /// 选中的提示信息
        /// </summary>
        public string SelectedHint
        {
            get => _selectedHint;
            set
            {
                if (_selectedHint != value)
                {
                    _selectedHint = value;
                    OnPropertyChanged(nameof(SelectedHint));
                }
            }
        }

        /// <summary>
        /// 自定义提示信息
        /// </summary>
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

        /// <summary>
        /// 选中的车票改签类型
        /// </summary>
        public string SelectedTicketModificationType
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

        /// <summary>
        /// 是否学生票
        /// </summary>
        public bool IsStudentTicket
        {
            get => _isStudentTicket;
            set
            {
                if (_isStudentTicket != value)
                {
                    _isStudentTicket = value;
                    OnPropertyChanged(nameof(IsStudentTicket));
                }
            }
        }

        /// <summary>
        /// 是否优惠票
        /// </summary>
        public bool IsDiscountTicket
        {
            get => _isDiscountTicket;
            set
            {
                if (_isDiscountTicket != value)
                {
                    _isDiscountTicket = value;
                    OnPropertyChanged(nameof(IsDiscountTicket));
                }
            }
        }

        /// <summary>
        /// 是否网络售票
        /// </summary>
        public bool IsOnlineTicket
        {
            get => _isOnlineTicket;
            set
            {
                if (_isOnlineTicket != value)
                {
                    _isOnlineTicket = value;
                    OnPropertyChanged(nameof(IsOnlineTicket));
                }
            }
        }

        /// <summary>
        /// 是否儿童票
        /// </summary>
        public bool IsChildTicket
        {
            get => _isChildTicket;
            set
            {
                if (_isChildTicket != value)
                {
                    _isChildTicket = value;
                    OnPropertyChanged(nameof(IsChildTicket));
                }
            }
        }

        /// <summary>
        /// 是否支付宝支付
        /// </summary>
        public bool IsAlipayPayment
        {
            get => _isAlipayPayment;
            set
            {
                if (_isAlipayPayment != value)
                {
                    _isAlipayPayment = value;
                    if (value && !_suppressNotifications)
                    {
                        IsWeChatPayment = false; // 互斥
                    }
                    OnPropertyChanged(nameof(IsAlipayPayment));
                }
            }
        }

        /// <summary>
        /// 是否微信支付
        /// </summary>
        public bool IsWeChatPayment
        {
            get => _isWeChatPayment;
            set
            {
                if (_isWeChatPayment != value)
                {
                    _isWeChatPayment = value;
                    if (value && !_suppressNotifications)
                    {
                        IsAlipayPayment = false; // 互斥
                    }
                    OnPropertyChanged(nameof(IsWeChatPayment));
                }
            }
        }

        /// <summary>
        /// 是否农业银行支付
        /// </summary>
        public bool IsABCPayment
        {
            get => _isABCPayment;
            set
            {
                if (_isABCPayment != value)
                {
                    _isABCPayment = value;
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.ABC);
                    OnPropertyChanged(nameof(IsABCPayment));
                }
            }
        }

        /// <summary>
        /// 是否建设银行支付
        /// </summary>
        public bool IsCCBPayment
        {
            get => _isCCBPayment;
            set
            {
                if (_isCCBPayment != value)
                {
                    _isCCBPayment = value;
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.CCB);
                    OnPropertyChanged(nameof(IsCCBPayment));
                }
            }
        }

        /// <summary>
        /// 是否工商银行支付
        /// </summary>
        public bool IsICBCPayment
        {
            get => _isICBCPayment;
            set
            {
                if (_isICBCPayment != value)
                {
                    _isICBCPayment = value;
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.ICBC);
                    OnPropertyChanged(nameof(IsICBCPayment));
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
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.CMB);
                    OnPropertyChanged(nameof(IsCMBPayment));
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
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.PSBC);
                    OnPropertyChanged(nameof(IsPSBCPayment));
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
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.BOC);
                    OnPropertyChanged(nameof(IsBOCPayment));
                }
            }
        }

        public bool IsCOMMPayment
        {
            get => _isCOMMPayment;
            set
            {
                if (_isCOMMPayment != value)
                {
                    _isCOMMPayment = value;
                    if (value && !_suppressNotifications) ClearOtherBankPayments(PaymentChannelFlags.COMM);
                    OnPropertyChanged(nameof(IsCOMMPayment));
                }
            }
        }

        /// <summary>
        /// 出发车站搜索文本
        /// </summary>
        public string DepartStationSearchText
        {
            get => _departStationSearchText;
            set
            {
                if (_departStationSearchText != value)
                {
                    _departStationSearchText = value;
                    OnPropertyChanged(nameof(DepartStationSearchText));
                }
            }
        }

        /// <summary>
        /// 到达车站搜索文本
        /// </summary>
        public string ArriveStationSearchText
        {
            get => _arriveStationSearchText;
            set
            {
                if (_arriveStationSearchText != value)
                {
                    _arriveStationSearchText = value;
                    OnPropertyChanged(nameof(ArriveStationSearchText));
                }
            }
        }

        /// <summary>
        /// 出发车站建议列表
        /// </summary>
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

        /// <summary>
        /// 到达车站建议列表
        /// </summary>
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

        /// <summary>
        /// 是否显示出发车站下拉列表
        /// </summary>
        public bool IsDepartStationDropdownOpen
        {
            get => _isDepartStationDropdownOpen;
            set => SetProperty(ref _isDepartStationDropdownOpen, value);
        }

        /// <summary>
        /// 是否显示到达车站下拉列表
        /// </summary>
        public bool IsArriveStationDropdownOpen
        {
            get => _isArriveStationDropdownOpen;
            set => SetProperty(ref _isArriveStationDropdownOpen, value);
        }

        /// <summary>
        /// 选中的出发车站
        /// </summary>
        public StationInfo SelectedDepartStation
        {
            get => _selectedDepartStation;
            set => SetProperty(ref _selectedDepartStation, value);
        }
        
        /// <summary>
        /// 选中的到达车站
        /// </summary>
        public StationInfo SelectedArriveStation
        {
            get => _selectedArriveStation;
            set => SetProperty(ref _selectedArriveStation, value);
        }

        /// <summary>
        /// 车型列表
        /// </summary>
        public ObservableCollection<string> TrainTypes { get; }

        /// <summary>
        /// 小时选项
        /// </summary>
        public System.Collections.Generic.List<string> HourOptions { get; }

        /// <summary>
        /// 分钟选项
        /// </summary>
        public System.Collections.Generic.List<string> MinuteOptions { get; }

        /// <summary>
        /// 座位类型列表
        /// </summary>
        public ObservableCollection<string> SeatTypes { get; }

        /// <summary>
        /// 座位位置列表
        /// </summary>
        public ObservableCollection<string> SeatPositions { get; }

        /// <summary>
        /// 附加信息选项
        /// </summary>
        public ObservableCollection<string> AdditionalInfoOptions { get; }

        /// <summary>
        /// 车票用途选项
        /// </summary>
        public ObservableCollection<string> TicketPurposeOptions { get; }

        /// <summary>
        /// 提示信息选项
        /// </summary>
        public ObservableCollection<string> HintOptions { get; }

        /// <summary>
        /// 车票改签类型选项
        /// </summary>
        public ObservableCollection<string> TicketModificationTypes { get; }

        #endregion

        #region 表单字段启用状态属性

        /// <summary>
        /// 问号按钮是否启用
        /// </summary>
        public bool IsQuestionButtonEnabled
        {
            get => _isQuestionButtonEnabled;
            set => SetProperty(ref _isQuestionButtonEnabled, value);
        }

        /// <summary>
        /// 取票号字段是否启用
        /// </summary>
        public bool IsTicketNumberEnabled
        {
            get => _isTicketNumberEnabled;
            set => SetProperty(ref _isTicketNumberEnabled, value);
        }

        /// <summary>
        /// 检票位置字段是否启用
        /// </summary>
        public bool IsCheckInLocationEnabled
        {
            get => _isCheckInLocationEnabled;
            set => SetProperty(ref _isCheckInLocationEnabled, value);
        }

        /// <summary>
        /// 出发车站字段是否启用
        /// </summary>
        public bool IsDepartStationEnabled
        {
            get => _isDepartStationEnabled;
            set
            {
                SetProperty(ref _isDepartStationEnabled, value);
                // 出发车站启用时，拼音和代码也应启用（如果需要用户输入）
                if (value)
                {
                    IsDepartStationPinyinEnabled = true;
                    IsDepartStationCodeEnabled = true;
                }
            }
        }

        /// <summary>
        /// 到达车站字段是否启用
        /// </summary>
        public bool IsArriveStationEnabled
        {
            get => _isArriveStationEnabled;
            set
            {
                SetProperty(ref _isArriveStationEnabled, value);
                // 到达车站启用时，拼音和代码也应启用（如果需要用户输入）
                if (value)
                {
                    IsArriveStationPinyinEnabled = true;
                    IsArriveStationCodeEnabled = true;
                }
            }
        }

        /// <summary>
        /// 出发车站拼音字段是否启用
        /// </summary>
        public bool IsDepartStationPinyinEnabled
        {
            get => _isDepartStationPinyinEnabled;
            set => SetProperty(ref _isDepartStationPinyinEnabled, value);
        }

        /// <summary>
        /// 到达车站拼音字段是否启用
        /// </summary>
        public bool IsArriveStationPinyinEnabled
        {
            get => _isArriveStationPinyinEnabled;
            set => SetProperty(ref _isArriveStationPinyinEnabled, value);
        }

        /// <summary>
        /// 金额字段是否启用
        /// </summary>
        public bool IsMoneyEnabled
        {
            get => _isMoneyEnabled;
            set => SetProperty(ref _isMoneyEnabled, value);
        }

        /// <summary>
        /// 出发车站代码字段是否启用
        /// </summary>
        public bool IsDepartStationCodeEnabled
        {
            get => _isDepartStationCodeEnabled;
            set => SetProperty(ref _isDepartStationCodeEnabled, value);
        }

        /// <summary>
        /// 到达车站代码字段是否启用
        /// </summary>
        public bool IsArriveStationCodeEnabled
        {
            get => _isArriveStationCodeEnabled;
            set => SetProperty(ref _isArriveStationCodeEnabled, value);
        }

        /// <summary>
        /// 出发日期字段是否启用
        /// </summary>
        public bool IsDepartDateEnabled
        {
            get => _isDepartDateEnabled;
            set => SetProperty(ref _isDepartDateEnabled, value);
        }

        /// <summary>
        /// 车型字段是否启用
        /// </summary>
        public bool IsTrainTypeEnabled
        {
            get => _isTrainTypeEnabled;
            set => SetProperty(ref _isTrainTypeEnabled, value);
        }

        /// <summary>
        /// 车次号字段是否启用
        /// </summary>
        public bool IsTrainNumberEnabled
        {
            get => _isTrainNumberEnabled;
            set => SetProperty(ref _isTrainNumberEnabled, value);
        }

        /// <summary>
        /// 出发时间字段是否启用
        /// </summary>
        public bool IsDepartTimeEnabled
        {
            get => _isDepartTimeEnabled;
            set => SetProperty(ref _isDepartTimeEnabled, value);
        }

        /// <summary>
        /// 车厢号字段是否启用
        /// </summary>
        public bool IsCoachNoEnabled
        {
            get => _isCoachNoEnabled;
            set
            {
                SetProperty(ref _isCoachNoEnabled, value);
                if (value) IsExtraCoachEnabled = true; // 车厢号启用时，加车也启用
            }
        }

        /// <summary>
        /// 是否加车字段是否启用
        /// </summary>
        public bool IsExtraCoachEnabled
        {
            get => _isExtraCoachEnabled;
            set => SetProperty(ref _isExtraCoachEnabled, value);
        }

        /// <summary>
        /// 座位号字段是否启用
        /// </summary>
        public bool IsSeatNoEnabled
        {
            get => _isSeatNoEnabled;
            set
            {
                SetProperty(ref _isSeatNoEnabled, value);
                if (value)
                {
                    IsNoSeatEnabled = true; // 座位号启用时，无座也启用
                    IsSeatPositionEnabled = true; // 座位号启用时，位置也启用
                    OnPropertyChanged(nameof(IsSeatInputEnabled)); // 更新依赖属性
                }
            }
        }

        /// <summary>
        /// 是否无座字段是否启用
        /// </summary>
        public bool IsNoSeatEnabled
        {
            get => _isNoSeatEnabled;
            set => SetProperty(ref _isNoSeatEnabled, value);
        }

        /// <summary>
        /// 座位位置字段是否启用
        /// </summary>
        public bool IsSeatPositionEnabled
        {
            get => _isSeatPositionEnabled;
            set => SetProperty(ref _isSeatPositionEnabled, value);
        }

        /// <summary>
        /// 座位类型字段是否启用
        /// </summary>
        public bool IsSeatTypeEnabled
        {
            get => _isSeatTypeEnabled;
            set => SetProperty(ref _isSeatTypeEnabled, value);
        }

        /// <summary>
        /// 附加信息字段是否启用
        /// </summary>
        public bool IsAdditionalInfoEnabled
        {
            get => _isAdditionalInfoEnabled;
            set => SetProperty(ref _isAdditionalInfoEnabled, value);
        }

        /// <summary>
        /// 车票用途字段是否启用
        /// </summary>
        public bool IsTicketPurposeEnabled
        {
            get => _isTicketPurposeEnabled;
            set => SetProperty(ref _isTicketPurposeEnabled, value);
        }

        /// <summary>
        /// 提示信息字段是否启用
        /// </summary>
        public bool IsHintEnabled
        {
            get => _isHintEnabled;
            set
            {
                SetProperty(ref _isHintEnabled, value);
                if (value) IsCustomHintEnabled = true; // 提示启用时，自定义提示也启用
            }
        }

        /// <summary>
        /// 自定义提示字段是否启用
        /// </summary>
        public bool IsCustomHintEnabled
        {
            get => _isCustomHintEnabled;
            set => SetProperty(ref _isCustomHintEnabled, value);
        }

        /// <summary>
        /// 车票改签类型字段是否启用
        /// </summary>
        public bool IsTicketModificationTypeEnabled
        {
            get => _isTicketModificationTypeEnabled;
            set => SetProperty(ref _isTicketModificationTypeEnabled, value);
        }

        /// <summary>
        /// 票种类型字段是否启用
        /// </summary>
        public bool IsTicketTypeEnabled
        {
            get => _isTicketTypeEnabled;
            set => SetProperty(ref _isTicketTypeEnabled, value);
        }

        /// <summary>
        /// 支付方式字段是否启用
        /// </summary>
        public bool IsPaymentMethodEnabled
        {
            get => _isPaymentMethodEnabled;
            set => SetProperty(ref _isPaymentMethodEnabled, value);
        }

        /// <summary>
        /// 座位号和位置输入是否启用（依赖于 IsSeatNoEnabled 和 IsNoSeat）
        /// </summary>
        public bool IsSeatInputEnabled => IsSeatNoEnabled && !IsNoSeat;

        /// <summary>
        /// 是否展开车票信息面板
        /// </summary>
        public bool IsExpandPanelEnabled
        {
            get => _isExpandPanelEnabled;
            set => SetProperty(ref _isExpandPanelEnabled, value);
        }

        /// <summary>
        /// 无数据时显示的文本
        /// </summary>
        public string NoDataText
        {
            get => _noDataText;
            set => SetProperty(ref _noDataText, value);
        }

        #endregion

        /// <summary>
        /// 选择PDF文件
        /// </summary>
        private async void SelectPdfFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF文件|*.pdf",
                Title = "选择PDF车票文件"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    IsLoading = true;
                    SelectedPdfPath = openFileDialog.FileName;
                    PdfContent = string.Empty; // 清空旧内容
                    ResetFormFieldsState(); // 重置字段状态

                    // 使用服务读取PDF内容
                    PdfContent = await _pdfImportService.LoadPdfContentAsync(SelectedPdfPath);

                    // 解析PDF内容并填充表单
                    await ParsePdfContentAsync(PdfContent);
                }
                catch (Exception ex)
                {
                    PdfContent = $"处理PDF文件时出错: {ex.Message}";
                    LogHelper.LogError($"处理PDF文件时出错: {ex.Message}");
                    ResetFormFieldsState(); // 出错时也重置
                    IsQuestionButtonEnabled = false; // 出错时禁用问号按钮
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 异步解析PDF内容
        /// </summary>
        /// <param name="content">PDF内容</param>
        private async Task ParsePdfContentAsync(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                IsQuestionButtonEnabled = false; // 无内容时禁用问号按钮
                return;
            }

            // 使用PdfImportService解析PDF内容
            var ticket = _pdfImportService.ParsePdfContent(content);
            if (ticket != null)
            {
                // 将解析结果填充到表单
                FillFormWithTicketInfo(ticket);
                IsQuestionButtonEnabled = true; // 填充成功后启用问号按钮
            }
            else
            {
                // 解析失败，可能是格式不支持等
                MessageBoxHelper.ShowWarning("无法从此PDF中解析出车票信息，请检查文件内容或手动输入。");
                ResetFormFieldsState(); // 重置字段状态
                IsQuestionButtonEnabled = false; // 解析失败时禁用问号按钮
            }
        }

        /// <summary>
        /// 使用车票信息填充表单
        /// </summary>
        /// <param name="ticket">车票信息</param>
        private void FillFormWithTicketInfo(TrainRideInfo ticket)
        {
            // 填充前先重置状态并禁用问号按钮，填充完成后再启用
            // ResetFormFieldsState(); // 在SelectPdfFile中已调用
            IsQuestionButtonEnabled = false;

            // 自动展开车票信息面板
            IsExpandPanelEnabled = true;

            using (SuppressNotifications())  // 使用SuppressNotifications避免触发下拉框
            {
                // 基本信息
                TicketNumber = ticket.TicketNumber;
                CheckInLocation = ticket.CheckInLocation; // 确保 CheckInLocation 在 TrainRideInfo 中存在
                DepartStation = ticket.DepartStation;
                ArriveStation = ticket.ArriveStation;
                DepartStationSearchText = ticket.DepartStation; // 更新搜索文本
                ArriveStationSearchText = ticket.ArriveStation; // 更新搜索文本
                DepartStationPinyin = ticket.DepartStationPinyin;
                ArriveStationPinyin = ticket.ArriveStationPinyin;
                DepartStationCode = ticket.DepartStationCode;
                ArriveStationCode = ticket.ArriveStationCode;
                Money = ticket.Money ?? 0;
            }
            
            // 解析车次号
            if (!string.IsNullOrEmpty(ticket.TrainNo))
            {
                // 尝试匹配开头的字母类型
                var trainTypeMatch = System.Text.RegularExpressions.Regex.Match(ticket.TrainNo, @"^([GCDZTKLSY])"); 

                if (trainTypeMatch.Success)
                {
                    // 如果匹配到字母类型
                    SelectedTrainType = trainTypeMatch.Groups[1].Value;
                    TrainNumber = ticket.TrainNo.Substring(1); // 号码是字母后面的部分
                }
                else
                {
                    // 如果没有匹配到字母类型，则认为是纯数字
                    SelectedTrainType = "纯数字"; // 确保 "纯数字" 是 TrainTypes 中的一个有效选项
                    TrainNumber = ticket.TrainNo; // <-- 修正：号码是完整的原始字符串
                }
            }

            // 日期和时间
            if (ticket.DepartDate.HasValue)
            {
                DepartDate = ticket.DepartDate.Value;
            }
            
            if (ticket.DepartTime.HasValue)
            {
                DepartHour = ticket.DepartTime.Value.Hours;
                DepartMinute = ticket.DepartTime.Value.Minutes;
            }

            // 车厢和座位
            if (!string.IsNullOrEmpty(ticket.CoachNo))
            {
                // 处理车厢号，例如"5车"提取成"5"
                var coachMatch = System.Text.RegularExpressions.Regex.Match(ticket.CoachNo, @"(\d+)");
                if (coachMatch.Success)
                {
                    CoachNo = coachMatch.Groups[1].Value;
                    IsExtraCoach = ticket.CoachNo.Contains("加");
                }
            }

            if (!string.IsNullOrEmpty(ticket.SeatNo))
            {
                // 处理座位号，例如"05A"提取成"05"和"A"
                var seatNoMatch = System.Text.RegularExpressions.Regex.Match(ticket.SeatNo, @"(\d+)([A-F])?");
                if (seatNoMatch.Success)
                {
                    SeatNo = seatNoMatch.Groups[1].Value;
                    if (seatNoMatch.Groups[2].Success)
                    {
                        SelectedSeatPosition = seatNoMatch.Groups[2].Value;
                    }
                }
                IsNoSeat = ticket.SeatNo.Contains("无座");
            }

            // 座位类型
            if (!string.IsNullOrEmpty(ticket.SeatType) && SeatTypes.Contains(ticket.SeatType))
            {
                SelectedSeatType = ticket.SeatType;
            }

            // 附加信息
            if (!string.IsNullOrEmpty(ticket.AdditionalInfo) && AdditionalInfoOptions.Contains(ticket.AdditionalInfo))
            {
                SelectedAdditionalInfo = ticket.AdditionalInfo;
            }

            if (!string.IsNullOrEmpty(ticket.TicketPurpose) && TicketPurposeOptions.Contains(ticket.TicketPurpose))
            {
                SelectedTicketPurpose = ticket.TicketPurpose;
            }

            if (!string.IsNullOrEmpty(ticket.Hint))
            {
                // 检查是否匹配预定义提示
                var matchedHint = HintOptions.FirstOrDefault(h => h != "自定义" && h == ticket.Hint);
                if (matchedHint != null)
                {
                    SelectedHint = matchedHint;
                }
                else
                {
                    SelectedHint = "自定义";
                    CustomHint = ticket.Hint;
                }
            }

            if (!string.IsNullOrEmpty(ticket.TicketModificationType) && TicketModificationTypes.Contains(ticket.TicketModificationType))
            {
                SelectedTicketModificationType = ticket.TicketModificationType;
            }

            // 填充票种类型复选框 (基于解析出的 Flags)
            IsStudentTicket = (ticket.TicketTypeFlags & (int)TicketTypeFlags.StudentTicket) != 0;
            IsChildTicket = (ticket.TicketTypeFlags & (int)TicketTypeFlags.ChildTicket) != 0;
            IsDiscountTicket = (ticket.TicketTypeFlags & (int)TicketTypeFlags.DiscountTicket) != 0;
            IsOnlineTicket = (ticket.TicketTypeFlags & (int)TicketTypeFlags.OnlineTicket) != 0;

            // 填充支付渠道复选框 (基于解析出的 Flags)
            using (SuppressNotifications())
            {
                IsAlipayPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.Alipay) != 0;
                IsWeChatPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.WeChat) != 0;
                IsABCPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.ABC) != 0;
                IsCCBPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.CCB) != 0;
                IsICBCPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.ICBC) != 0;
                IsCMBPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.CMB) != 0;
                IsPSBCPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.PSBC) != 0;
                IsBOCPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.BOC) != 0;
                IsCOMMPayment = (ticket.PaymentChannelFlags & (int)PaymentChannelFlags.COMM) != 0;
            }

            // 填充完成后启用问号按钮
            IsQuestionButtonEnabled = true;
        }

        /// <summary>
        /// 验证站名是否存在
        /// </summary>
        /// <param name="stationName">站名</param>
        /// <param name="isDepartStation">是否是出发车站</param>
        /// <returns>站名是否存在</returns>
        private bool ValidateStationName(string stationName, bool isDepartStation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stationName) || _stationSearchService == null)
                {
                    return false;
                }

                // 移除站名中可能的"站"字后缀再验证
                string normalizedStationName = StationNameHelper.RemoveStationSuffix(stationName);
                
                // 使用StationSearchService检测是否是有效站点
                var stationInfo = _stationSearchService.GetStationInfo(normalizedStationName);

                if (stationInfo != null)
                {
                    return true;
                }
                else
                {
                    // 显示警告消息
                    string stationType = isDepartStation ? "出发车站" : "到达车站";
                    MessageBoxHelper.ShowWarning($"{stationType}【{normalizedStationName}】在车站中心不存在，请先添加该车站信息。");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"验证站名时出错: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 导入车票
        /// </summary>
        private async void ImportTicket()
        {
            try
            {
                IsLoading = true;
                
                // 验证出发车站和到达车站是否存在
                string departStationName = DepartStation;
                string arriveStationName = ArriveStation;
                
                // 验证出发车站
                if (!ValidateStationName(departStationName, true))
                {
                    IsLoading = false;
                    return;
                }
                
                // 验证到达车站
                if (!ValidateStationName(arriveStationName, false))
                {
                    IsLoading = false;
                    return;
                }
                
                // 收集表单数据创建车票对象
                var ticket = CreateTicketFromForm();

                // 验证车站代码
                if (string.IsNullOrEmpty(ticket.DepartStationCode))
                {
                    MessageBoxHelper.ShowWarning($"车站【{ticket.DepartStation}】缺少车站代码信息，请在车站中心中完善该车站信息。");
                    IsLoading = false;
                    return;
                }

                if (string.IsNullOrEmpty(ticket.ArriveStationCode))
                {
                    MessageBoxHelper.ShowWarning($"车站【{ticket.ArriveStation}】缺少车站代码信息，请在车站中心中完善该车站信息。");
                    IsLoading = false;
                    return;
                }
                
                // 保存车票信息
                bool success = await _pdfImportService.SaveTicketAsync(ticket);
                
                if (success)
                {
                    IsLoading = false; // 在显示消息和关闭前设置 IsLoading = false
                    
                    // 刷新车票中心数据
                    await _mainViewModel.QueryAllTicketsViewModel.RefreshDataAsync();
                    
                    MessageBoxHelper.ShowInfo("车票导入成功");
                    
                    // 关闭窗口
                    Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this)?.Close();
                }
                else
                {
                    IsLoading = false; // 失败时也要设置
                    MessageBoxHelper.ShowError("导入车票失败");
                }
            }
            catch (Exception ex)
            {
                IsLoading = false; // 异常时也要设置
                MessageBoxHelper.ShowError($"导入车票时出错: {ex.Message}");
                LogHelper.LogError($"导入车票时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从表单创建车票对象
        /// </summary>
        /// <returns>车票对象</returns>
        private TrainRideInfo CreateTicketFromForm()
        {
            // 格式化车次号
            string trainNo = SelectedTrainType;
            if (SelectedTrainType != "纯数字")
            {
                trainNo = $"{SelectedTrainType}{TrainNumber}";
            }
            else
            {
                trainNo = TrainNumber;
            }

            // 格式化座位号
            string seatNo; 
            if (IsNoSeat)
            {
                seatNo = "无座";
            }
            else
            {
                seatNo = $"{SeatNo}{SelectedSeatPosition}";
            }
            
            // 格式化车厢号
            string coachNo;
            if (IsExtraCoach)
            {
                // 尝试去除前导零
                string coachNumTrimmed = CoachNo; // Default to original if parsing fails
                if (int.TryParse(CoachNo, out int coachNumInt))
                {
                    coachNumTrimmed = coachNumInt.ToString();
                }
                coachNo = $"加{coachNumTrimmed}车";
            }
            else
            {
                coachNo = $"{CoachNo}车";
            }
            
            // 提示信息处理
            string hint = SelectedHint;
            if (SelectedHint == "自定义")
            {
                hint = CustomHint;
            }

            // 确保车站名称以"站"结尾
            string departStation = Utils.StationNameHelper.EnsureStationSuffix(DepartStation);
            string arriveStation = Utils.StationNameHelper.EnsureStationSuffix(ArriveStation);

            return new TrainRideInfo
            {
                TicketNumber = TicketNumber,
                CheckInLocation = CheckInLocation,
                DepartStation = departStation,
                ArriveStation = arriveStation,
                DepartStationPinyin = DepartStationPinyin,
                ArriveStationPinyin = ArriveStationPinyin,
                DepartStationCode = DepartStationCode,
                ArriveStationCode = ArriveStationCode,
                DepartDate = DepartDate,
                DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                TrainNo = trainNo,
                CoachNo = coachNo, // Use the modified coachNo
                SeatNo = seatNo,   // Use the modified seatNo
                Money = Money,
                SeatType = SelectedSeatType,
                AdditionalInfo = SelectedAdditionalInfo,
                TicketPurpose = SelectedTicketPurpose,
                Hint = hint,
                TicketModificationType = SelectedTicketModificationType,
                TicketTypeFlags = GetTicketTypeFlags(),
                PaymentChannelFlags = GetPaymentChannelFlags()
            };
        }

        /// <summary>
        /// 获取票种类型标记
        /// </summary>
        /// <returns>票种类型标记</returns>
        private int GetTicketTypeFlags()
        {
            int flags = 0;
            if (IsStudentTicket) flags |= 1; // 学生票
            if (IsChildTicket) flags |= 2;   // 儿童票
            if (IsDiscountTicket) flags |= 4; // 优惠票
            if (IsOnlineTicket) flags |= 8;   // 网络票
            return flags;
        }

        /// <summary>
        /// 获取支付渠道标记
        /// </summary>
        /// <returns>支付渠道标记</returns>
        private int GetPaymentChannelFlags()
        {
            int flags = 0;
            if (IsAlipayPayment) flags |= 1; // 支付宝
            if (IsWeChatPayment) flags |= 2; // 微信
            if (IsABCPayment) flags |= 4;    // 农业银行
            if (IsCCBPayment) flags |= 8;    // 建设银行
            if (IsICBCPayment) flags |= 16;  // 工商银行
            if(IsCMBPayment) flags |= 32;    // 招商银行
            if(IsPSBCPayment) flags |= 64; // 邮储银行
            if(IsBOCPayment) flags |= 128;  // 中国银行
            if(IsCOMMPayment) flags |= 256;// 交通银行

            return flags;
        }

        /// <summary>
        /// 是否可以导入车票
        /// </summary>
        /// <returns>是否可以导入车票</returns>
        private bool CanImportTicket()
        {
            return HasSelectedPdf && !IsLoading;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            IsQuestionButtonEnabled = false; // 取消时禁用
            // 关闭窗口
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?.Close();
        }
        
        /// <summary>
        /// 搜索出发车站
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private async Task SearchDepartStationsAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 1)
                {
                    IsDepartStationDropdownOpen = false;
                    return;
                }

                // 确保StationSearchService已初始化
                if (_stationSearchService != null && !_stationSearchService.IsInitialized)
                {
                    await _stationSearchService.InitializeAsync();
                }

                // 移除可能的"站"后缀再执行搜索
                string normalizedSearchText = StationNameHelper.RemoveStationSuffix(searchText);
                var stations = await _stationSearchService.SearchStationsAsync(normalizedSearchText);

                // 使用Dispatcher在UI线程上更新集合
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 更新下拉列表和显示状态
                    DepartStationSuggestions = new ObservableCollection<StationInfo>(stations);
                    IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索出发车站时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索到达车站
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private async Task SearchArriveStationsAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 1)
                {
                    IsArriveStationDropdownOpen = false;
                    return;
                }

                // 确保StationSearchService已初始化
                if (_stationSearchService != null && !_stationSearchService.IsInitialized)
                {
                    await _stationSearchService.InitializeAsync();
                }

                // 移除可能的"站"后缀再执行搜索
                string normalizedSearchText = StationNameHelper.RemoveStationSuffix(searchText);
                var stations = await _stationSearchService.SearchStationsAsync(normalizedSearchText);

                // 使用Dispatcher在UI线程上更新集合
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 更新下拉列表和显示状态
                    ArriveStationSuggestions = new ObservableCollection<StationInfo>(stations);
                    IsArriveStationDropdownOpen = ArriveStationSuggestions.Count > 0;
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索到达车站时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 搜索出发车站并自动填充代码和拼音
        /// </summary>
        /// <param name="stationName">车站名称</param>
        private async void SearchDepartStationAsync(string stationName)
        {
            try
            {
                if (_stationSearchService != null && !string.IsNullOrEmpty(stationName))
                {
                    // 移除站名中可能的"站"后缀，再进行搜索
                    string normalizedStationName = StationNameHelper.RemoveStationSuffix(stationName);
                    var station = await _stationSearchService.GetClosestStationMatchAsync(normalizedStationName);
                    if (station != null)
                    {
                        DepartStationCode = station.StationCode;
                        DepartStationPinyin = station.StationPinyin;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索出发车站信息时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 搜索到达车站并自动填充代码和拼音
        /// </summary>
        /// <param name="stationName">车站名称</param>
        private async void SearchArriveStationAsync(string stationName)
        {
            try
            {
                if (_stationSearchService != null && !string.IsNullOrEmpty(stationName))
                {
                    // 移除站名中可能的"站"后缀，再进行搜索
                    string normalizedStationName = StationNameHelper.RemoveStationSuffix(stationName);
                    var station = await _stationSearchService.GetClosestStationMatchAsync(normalizedStationName);
                    if (station != null)
                    {
                        ArriveStationCode = station.StationCode;
                        ArriveStationPinyin = station.StationPinyin;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"搜索到达车站信息时出错: {ex.Message}");
            }
        }

        // 辅助方法，用于临时禁止通知，确保初始填充不触发互斥逻辑
        private IDisposable SuppressNotifications()
        {
            _suppressNotifications = true;
            return new DisposableAction(() => _suppressNotifications = false);
        }

        // 辅助方法，用于清除除当前选中银行外的其他银行支付方式
        private void ClearOtherBankPayments(PaymentChannelFlags selectedBank)
        {
            using (SuppressNotifications()) // 在清除操作期间也禁止通知
            {
                if (selectedBank != PaymentChannelFlags.ABC) IsABCPayment = false;
                if (selectedBank != PaymentChannelFlags.CCB) IsCCBPayment = false;
                if (selectedBank != PaymentChannelFlags.ICBC) IsICBCPayment = false;
                if (selectedBank != PaymentChannelFlags.CMB) IsCMBPayment = false;
                if (selectedBank != PaymentChannelFlags.PSBC) IsPSBCPayment = false;
                if (selectedBank != PaymentChannelFlags.BOC) IsBOCPayment = false;
                if (selectedBank != PaymentChannelFlags.COMM) IsCOMMPayment = false;
            }
        }

        // 辅助类，用于实现 using 语句块结束时恢复标志
        private class DisposableAction : IDisposable
        {
            private readonly Action _action;
            public DisposableAction(Action action)
            { _action = action; }
            public void Dispose()
            { _action?.Invoke(); }
        }

        // --- 添加字段解锁方法 ---

        /// <summary>
        /// 重置所有表单字段的启用状态为 False
        /// </summary>
        private void ResetFormFieldsState()
        {
            IsQuestionButtonEnabled = false; // 初始禁用问号按钮
            IsExpandPanelEnabled = false; // 重置时折叠面板

            IsTicketNumberEnabled = false;
            IsCheckInLocationEnabled = false;
            IsDepartStationEnabled = false;
            IsArriveStationEnabled = false;
            IsDepartStationPinyinEnabled = false;
            IsArriveStationPinyinEnabled = false;
            IsMoneyEnabled = false;
            IsDepartStationCodeEnabled = false;
            IsArriveStationCodeEnabled = false;
            IsDepartDateEnabled = false;
            IsTrainTypeEnabled = false;
            IsTrainNumberEnabled = false;
            IsDepartTimeEnabled = false;
            IsCoachNoEnabled = false;
            IsExtraCoachEnabled = false;
            IsSeatNoEnabled = false;
            IsNoSeatEnabled = false;
            IsSeatPositionEnabled = false;
            IsSeatTypeEnabled = false;
            IsAdditionalInfoEnabled = false;
            IsTicketPurposeEnabled = false;
            IsHintEnabled = false;
            IsCustomHintEnabled = false;
            IsTicketModificationTypeEnabled = false;
            IsTicketTypeEnabled = false;
            IsPaymentMethodEnabled = false;

            OnPropertyChanged(nameof(IsSeatInputEnabled)); // 更新依赖属性
        }

        /// <summary>
        /// 切换指定字段的编辑状态（设置为 True）
        /// </summary>
        /// <param name="fieldName">要切换的字段名称</param>
        private void ToggleField(string fieldName)
        {
            bool newState;
            switch (fieldName)
            {
                case "TicketNumber": IsTicketNumberEnabled = !IsTicketNumberEnabled; break;
                case "CheckInLocation": IsCheckInLocationEnabled = !IsCheckInLocationEnabled; break;
                case "DepartStation":
                    newState = !IsDepartStationEnabled; // 计算新状态
                    IsDepartStationEnabled = newState;
                    IsDepartStationPinyinEnabled = newState; // 同步相关字段
                    IsDepartStationCodeEnabled = newState;
                    
                    // 如果启用了编辑模式，则激活搜索下拉框功能
                    if (newState && !string.IsNullOrWhiteSpace(DepartStationSearchText))
                    {
                        DepartStationSearchText = DepartStation; // 保持一致性
                        // 异步调用搜索，显示下拉提示
                        Task.Run(async () => 
                        {
                            await SearchDepartStationsAsync(DepartStationSearchText);
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                            });
                        });
                    }
                    break;
                case "ArriveStation":
                    newState = !IsArriveStationEnabled;
                    IsArriveStationEnabled = newState;
                    IsArriveStationPinyinEnabled = newState;
                    IsArriveStationCodeEnabled = newState;
                    
                    // 如果启用了编辑模式，则激活搜索下拉框功能
                    if (newState && !string.IsNullOrWhiteSpace(ArriveStationSearchText))
                    {
                        ArriveStationSearchText = ArriveStation; // 保持一致性
                        // 异步调用搜索，显示下拉提示
                        Task.Run(async () => 
                        {
                            await SearchArriveStationsAsync(ArriveStationSearchText);
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                IsArriveStationDropdownOpen = ArriveStationSuggestions.Count > 0;
                            });
                        });
                    }
                    break;
                case "DepartStationPinyin": IsDepartStationPinyinEnabled = !IsDepartStationPinyinEnabled; break;
                case "ArriveStationPinyin": IsArriveStationPinyinEnabled = !IsArriveStationPinyinEnabled; break;
                case "Money": IsMoneyEnabled = !IsMoneyEnabled; break;
                case "DepartStationCode": IsDepartStationCodeEnabled = !IsDepartStationCodeEnabled; break;
                case "ArriveStationCode": IsArriveStationCodeEnabled = !IsArriveStationCodeEnabled; break;
                case "DepartDate": IsDepartDateEnabled = !IsDepartDateEnabled; break;
                case "TrainNumber": // 同时切换类型和编号
                    newState = !IsTrainTypeEnabled; // 以其中一个为基准
                    IsTrainTypeEnabled = newState;
                    IsTrainNumberEnabled = newState;
                    break;
                case "DepartTime": IsDepartTimeEnabled = !IsDepartTimeEnabled; break;
                case "CoachNo": // 同时切换车厢号和加车
                    newState = !IsCoachNoEnabled;
                    IsCoachNoEnabled = newState;
                    IsExtraCoachEnabled = newState;
                    break;
                case "SeatNo": // 同时切换座位号、无座、位置
                    newState = !IsSeatNoEnabled;
                    IsSeatNoEnabled = newState;
                    IsNoSeatEnabled = newState;
                    IsSeatPositionEnabled = newState;
                    OnPropertyChanged(nameof(IsSeatInputEnabled)); // 触发依赖属性更新
                    break;
                case "SeatType": IsSeatTypeEnabled = !IsSeatTypeEnabled; break;
                case "AdditionalInfo": IsAdditionalInfoEnabled = !IsAdditionalInfoEnabled; break;
                case "TicketPurpose": IsTicketPurposeEnabled = !IsTicketPurposeEnabled; break;
                case "Hint": // 同时切换提示和自定义提示
                    newState = !IsHintEnabled;
                    IsHintEnabled = newState;
                    IsCustomHintEnabled = newState;
                    break;
                case "TicketModificationType": IsTicketModificationTypeEnabled = !IsTicketModificationTypeEnabled; break;
                case "TicketType": IsTicketTypeEnabled = !IsTicketTypeEnabled; break;
                case "PaymentMethod": IsPaymentMethodEnabled = !IsPaymentMethodEnabled; break;
                // 可以根据需要添加更多字段
            }
        }

        

        // 辅助方法，用于简化属性设置和通知
        private bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 选择出发车站
        /// </summary>
        private void SelectDepartStation(StationInfo station)
        {
            System.Diagnostics.Debug.WriteLine("[PdfImportViewModel] SelectDepartStation执行，参数站点: " + 
                (station?.StationName ?? "null"));
            
            if (station != null)
            {
                // 设置忽略标志，防止更新文本后立刻触发搜索
                _ignoreSearchTextChange = true;
                
                // 保存选中站点
                SelectedDepartStation = station;
                
                // 移除站名中的"站"后缀
                string stationName = Utils.StationNameHelper.RemoveStationSuffix(station.StationName);
                
                // 更新出发车站文本框内容
                _suppressNotifications = true;
                DepartStationSearchText = stationName;
                DepartStation = stationName;
                DepartStationPinyin = station.StationPinyin;
                DepartStationCode = station.StationCode;
                _suppressNotifications = false;
                
                // 确保在UI线程上关闭下拉列表
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    // 必须先将DepartStationSuggestions清空，然后关闭下拉菜单
                    DepartStationSuggestions = new ObservableCollection<StationInfo>();
                    IsDepartStationDropdownOpen = false;
                });
                
                // 触发输入变更以更新UI
                OnPropertyChanged(nameof(DepartStationSearchText));
                OnPropertyChanged(nameof(DepartStation));
                OnPropertyChanged(nameof(DepartStationPinyin));
                OnPropertyChanged(nameof(DepartStationCode));
                
                System.Diagnostics.Debug.WriteLine("[PdfImportViewModel] 出发车站已更新为: " + DepartStationSearchText);
                
                // 重置忽略标志
                _ignoreSearchTextChange = false;
            }
        }

        /// <summary>
        /// 选择到达车站
        /// </summary>
        private void SelectArriveStation(StationInfo station)
        {
            System.Diagnostics.Debug.WriteLine("[PdfImportViewModel] SelectArriveStation执行，参数站点: " + 
                (station?.StationName ?? "null"));
            
            if (station != null)
            {
                // 设置忽略标志，防止更新文本后立刻触发搜索
                _ignoreSearchTextChange = true;
                
                // 保存选中站点
                SelectedArriveStation = station;
                
                // 移除站名中的"站"后缀
                string stationName = Utils.StationNameHelper.RemoveStationSuffix(station.StationName);
                
                // 更新到达车站文本框内容
                _suppressNotifications = true;
                ArriveStationSearchText = stationName;
                ArriveStation = stationName;
                ArriveStationPinyin = station.StationPinyin;
                ArriveStationCode = station.StationCode;
                _suppressNotifications = false;
                
                // 确保在UI线程上关闭下拉列表
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    // 必须先将ArriveStationSuggestions清空，然后关闭下拉菜单
                    ArriveStationSuggestions = new ObservableCollection<StationInfo>();
                    IsArriveStationDropdownOpen = false;
                });
                
                // 触发输入变更以更新UI
                OnPropertyChanged(nameof(ArriveStationSearchText));
                OnPropertyChanged(nameof(ArriveStation));
                OnPropertyChanged(nameof(ArriveStationPinyin));
                OnPropertyChanged(nameof(ArriveStationCode));
                
                System.Diagnostics.Debug.WriteLine("[PdfImportViewModel] 到达车站已更新为: " + ArriveStationSearchText);
                
                // 重置忽略标志
                _ignoreSearchTextChange = false;
            }
        }
        
        /// <summary>
        /// 处理出发车站选择事件
        /// </summary>
        public void HandleDepartStationSelected()
        {
            if (SelectedDepartStation != null)
            {
                SelectDepartStation(SelectedDepartStation);
            }
        }
        
        /// <summary>
        /// 处理到达车站选择事件
        /// </summary>
        public void HandleArriveStationSelected()
        {
            if (SelectedArriveStation != null)
            {
                SelectArriveStation(SelectedArriveStation);
            }
        }
    }
} 