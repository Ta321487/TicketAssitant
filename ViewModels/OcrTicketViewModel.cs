using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// OCR识别车票视图模型
    /// </summary>
    public class OcrTicketViewModel : BaseViewModel
    {
        private readonly PythonService _pythonService;
        private readonly OcrEnvironmentService _ocrEnvironmentService;
        private readonly MainViewModel _mainViewModel;
        private readonly DatabaseService _databaseService;
        private readonly StationSearchService _stationSearchService;

        private string _selectedImagePath;
        private BitmapImage _selectedImage;
        private bool _isLoading;
        private string _jsonResult;
        private ObservableCollection<OcrResult> _ocrResults;
        private double _averageConfidence;
        private bool _isTicketFormExpanded;
        private int _progress;
        private bool _isDownloadingModel;

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
        private bool _isPSBCPayment;
        private bool _isBOCPayment;
        private bool _isCMBPayment;
        private bool _isCOMMPayment;
        private bool _isAlipayPaymentEnabled = true;
        private bool _isWeChatPaymentEnabled = true;

        // 车站搜索相关属性
        private ObservableCollection<StationInfo> _departStationSuggestions;
        private ObservableCollection<StationInfo> _arriveStationSuggestions;
        private bool _isDepartStationDropdownOpen;
        private bool _isArriveStationDropdownOpen;
        private string _departStationSearchText;
        private string _arriveStationSearchText;

        // 标记字段，用于避免循环更新
        private bool _isUpdatingDepartStation = false;
        private bool _isUpdatingArriveStation = false;

        // 添加表单项启用状态属性
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
        private bool _isPaymentMethodEnabled;

        /// <summary>
        /// 请求关闭窗口的事件
        /// </summary>
        public event Action RequestCloseAction;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        public OcrTicketViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _pythonService = new PythonService();
            _ocrEnvironmentService = new OcrEnvironmentService(_pythonService);
            _ocrResults = new ObservableCollection<OcrResult>();
            _isTicketFormExpanded = false;

            // 初始化数据库服务和站点搜索服务
            // 使用MainViewModel中的完整连接字符串
            var connectionString = mainViewModel.ConnectionString;
            _databaseService = new DatabaseService(connectionString);
            _stationSearchService = new StationSearchService(_databaseService);

            // 订阅环境服务的属性更改事件以保持UI同步
            _ocrEnvironmentService.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(OcrEnvironmentService.IsLoading))
                {
                    IsLoading = _ocrEnvironmentService.IsLoading;
                }
                // 触发属性更改通知以更新绑定
                OnPropertyChanged(e.PropertyName);
            };
            
            // 订阅环境检测完成事件，以便在环境检测完成后自动刷新命令状态
            _ocrEnvironmentService.EnvironmentCheckCompleted += OnEnvironmentCheckCompleted;

            // 使用项目中现有的RelayCommand实现
            SelectImageCommand = new RelayCommand(async () => await SelectImage(), CanImportTicket);
            RunOcrCommand = new RelayCommand(async () => await RunOcr(), CanRunOcr);
            CheckEnvironmentCommand = new RelayCommand(async () => await CheckEnvironment());
            OpenCnocrInstallGuideCommand = new RelayCommand(() => _ocrEnvironmentService.OpenCnocrInstallGuide());
            DownloadPythonCommand = new RelayCommand(async () => await DownloadPython(), CanDownloadPython);
            DownloadCnocrCommand = new RelayCommand(async () => await DownloadCnocr(), CanDownloadCnocr);

            // 初始化表单相关命令
            SelectDepartStationCommand = new RelayCommand<StationInfo>(station => SelectStation(station, true)); // 更新命令以调用新方法
            SelectArriveStationCommand = new RelayCommand<StationInfo>(station => SelectStation(station, false)); // 更新命令以调用新方法
            ToggleFieldCommand = new RelayCommand<string>(ToggleField);
            SaveTicketCommand = new RelayCommand(SaveTicket);

            // 初始化表单相关集合
            TrainTypes = new ObservableCollection<string> { "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字" };
            HourOptions = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            MinuteOptions = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();

            SeatTypes = new ObservableCollection<string>
            {
                "新空调硬座", "软座", "新空调硬卧", "新空调软卧",
                "商务座", "一等座", "二等座", "硬卧代硬座"
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
            SeatPositions = new ObservableCollection<string>();

            // 初始化车站搜索相关集合
            DepartStationSuggestions = new ObservableCollection<StationInfo>();
            ArriveStationSuggestions = new ObservableCollection<StationInfo>();

            // 初始化并加载车站数据
            Task.Run(async () => await LoadStationsAsync());

            // 重置表单状态
            ResetFormState();
            
            // 初始化进度
            Progress = 0;
            
            // 检查环境
            Task.Run(async () => await _ocrEnvironmentService.CheckEnvironment());
        }

        // 命令属性
        public ICommand SelectImageCommand { get; }
        public ICommand RunOcrCommand { get; }
        public ICommand CheckEnvironmentCommand { get; }
        public ICommand OpenCnocrInstallGuideCommand { get; }
        public ICommand DownloadPythonCommand { get; }
        public ICommand DownloadCnocrCommand { get; }

        /// <summary>
        /// 选中的图片路径
        /// </summary>
        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set
            {
                if (_selectedImagePath != value)
                {
                    _selectedImagePath = value;
                    OnPropertyChanged(nameof(SelectedImagePath));
                }
            }
        }

        /// <summary>
        /// 选中的图片
        /// </summary>
        public BitmapImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (_selectedImage != value)
                {
                    _selectedImage = value;
                    OnPropertyChanged(nameof(SelectedImage));
                }
            }
        }

        /// <summary>
        /// Python是否已安装
        /// </summary>
        public bool? IsPythonInstalled => _ocrEnvironmentService.IsPythonInstalled;

        /// <summary>
        /// CNOCR是否已安装
        /// </summary>
        public bool? IsCnocrInstalled => _ocrEnvironmentService.IsCnocrInstalled;

        /// <summary>
        /// OCR模型是否已安装
        /// </summary>
        public bool? IsOcrModelInstalled => _ocrEnvironmentService.IsOcrModelInstalled;

        /// <summary>
        /// 是否正在加载中
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
        /// JSON结果
        /// </summary>
        public string JsonResult
        {
            get => _jsonResult;
            set
            {
                if (_jsonResult != value)
                {
                    _jsonResult = value;
                    OnPropertyChanged(nameof(JsonResult));
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage => _ocrEnvironmentService.StatusMessage;

        /// <summary>
        /// 加载消息
        /// </summary>
        public string LoadingMessage
        {
            get => _ocrEnvironmentService.LoadingMessage;
            set => _ocrEnvironmentService.SetLoadingMessage(value);
        }

        /// <summary>
        /// OCR结果集合
        /// </summary>
        public ObservableCollection<OcrResult> OcrResults
        {
            get => _ocrResults;
            set
            {
                if (_ocrResults != value)
                {
                    _ocrResults = value;
                    OnPropertyChanged(nameof(OcrResults));
                }
            }
        }

        /// <summary>
        /// 平均置信度
        /// </summary>
        public double AverageConfidence
        {
            get => _averageConfidence;
            set
            {
                if (_averageConfidence != value)
                {
                    _averageConfidence = value;
                    OnPropertyChanged(nameof(AverageConfidence));
                }
            }
        }

        /// <summary>
        /// 主视图模型
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 是否展开车票表单
        /// </summary>
        public bool IsTicketFormExpanded
        {
            get => _isTicketFormExpanded;
            set
            {
                if (_isTicketFormExpanded != value)
                {
                    _isTicketFormExpanded = value;
                    OnPropertyChanged(nameof(IsTicketFormExpanded));
                }
            }
        }

        /// <summary>
        /// OCR处理进度，0-100
        /// </summary>
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        /// <summary>
        /// 是否正在下载OCR模型
        /// </summary>
        public bool IsDownloadingModel
        {
            get => _isDownloadingModel;
            set
            {
                if (_isDownloadingModel != value)
                {
                    _isDownloadingModel = value;
                    OnPropertyChanged(nameof(IsDownloadingModel));
                }
            }
        }

        /// <summary>
        /// 环境是否准备就绪
        /// </summary>
        public bool IsEnvironmentReady => _ocrEnvironmentService.IsEnvironmentReady;

        /// <summary>
        /// 是否正在下载CNOCR
        /// </summary>
        public bool IsDownloadingCnocr => _ocrEnvironmentService.IsDownloadingCnocr;

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
                    _ticketNumber = FormValidationHelper.EnsureFirstLetterUpperCase(value);
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
                    _checkInLocation = FormValidationHelper.EnsureFirstLetterUpperCase(value);
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
        /// 无座
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
                    OnPropertyChanged(nameof(IsSeatInputEnabled));
                    
                    if (value) // 勾选了无座
                    {
                        // 无座时强制锁定座位号和座位位置
                        _isSeatNoEnabled = false;
                        _isSeatPositionEnabled = false;
                        OnPropertyChanged(nameof(IsSeatNoEnabled));
                        OnPropertyChanged(nameof(IsSeatPositionEnabled));
                        LogHelper.LogInfo("勾选无座，锁定座位号和座位位置输入框");
                    }
                    else if (IsNoSeatEnabled) // 取消勾选无座且无座复选框已解锁
                    {
                        // 当取消勾选无座，如果无座复选框已解锁（通过问号按钮），则恢复座位号和座位位置为可编辑状态
                        _isSeatNoEnabled = true;
                        _isSeatPositionEnabled = true;
                        OnPropertyChanged(nameof(IsSeatNoEnabled));
                        OnPropertyChanged(nameof(IsSeatPositionEnabled));
                        LogHelper.LogInfo("取消勾选无座且已解锁，恢复座位号和座位位置输入框为可编辑状态");
                    }
                }
            }
        }

        /// <summary>
        /// 座位号输入框是否可用
        /// </summary>
        public bool IsSeatInputEnabled => IsSeatNoEnabled;

        /// <summary>
        /// 是否显示座位位置
        /// </summary>
        public bool IsSeatPositionVisible => SeatPositions != null && SeatPositions.Count > 0 && _selectedSeatType != "新空调硬座";

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

                    // 根据座位类型更新座位位置选项
                    UpdateSeatPositions();

                    OnPropertyChanged(nameof(IsSeatPositionVisible));
                }
            }
        }

        /// <summary>
        /// 座位位置选项
        /// </summary>
        public ObservableCollection<string> SeatPositions
        {
            get => _seatPositions;
            set
            {
                if (_seatPositions != value)
                {
                    _seatPositions = value;
                    OnPropertyChanged(nameof(SeatPositions));
                    OnPropertyChanged(nameof(IsSeatPositionVisible));
                }
            }
        }

        /// <summary>
        /// 出发车站搜索关键词
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

                    // 执行站点搜索
                    if (!_isUpdatingDepartStation && !string.IsNullOrEmpty(value))
                    {
                        SearchStations(value, true);
                    }
                }
            }
        }

        /// <summary>
        /// 到达车站搜索关键词
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

                    // 执行站点搜索
                    if (!_isUpdatingArriveStation && !string.IsNullOrEmpty(value))
                    {
                        SearchStations(value, false);
                    }
                }
            }
        }

        /// <summary>
        /// 出发车站下拉列表是否打开
        /// </summary>
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

        /// <summary>
        /// 到达车站下拉列表是否打开
        /// </summary>
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

        /// <summary>
        /// 出发车站推荐列表
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
        /// 到达车站推荐列表
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
        /// 车型选项
        /// </summary>
        public ObservableCollection<string> TrainTypes { get; }

        /// <summary>
        /// 小时选项
        /// </summary>
        public List<string> HourOptions { get; }

        /// <summary>
        /// 分钟选项
        /// </summary>
        public List<string> MinuteOptions { get; }

        /// <summary>
        /// 座位类型选项
        /// </summary>
        public ObservableCollection<string> SeatTypes { get; }

        /// <summary>
        /// 选择出发车站命令
        /// </summary>
        public ICommand SelectDepartStationCommand { get; }

        /// <summary>
        /// 选择到达车站命令
        /// </summary>
        public ICommand SelectArriveStationCommand { get; }

        /// <summary>
        /// 附加信息选项
        /// </summary>
        public ObservableCollection<string> AdditionalInfoOptions { get; }

        /// <summary>
        /// 票种类型选项
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

                    // 学生票和儿童票互斥
                    if (value && IsChildTicket)
                    {
                        IsChildTicket = false;
                    }
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

                    // 儿童票和学生票互斥
                    if (value && IsStudentTicket)
                    {
                        IsStudentTicket = false;
                    }
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
                    OnPropertyChanged(nameof(IsAlipayPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("Alipay");
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
                    OnPropertyChanged(nameof(IsWeChatPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("WeChat");
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
                    OnPropertyChanged(nameof(IsABCPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("ABC");
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
                    OnPropertyChanged(nameof(IsCCBPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("CCB");
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
                    OnPropertyChanged(nameof(IsICBCPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("ICBC");
                }
            }
        }

        public bool IsPSBCPayment
        {
            get => _isPSBCPayment;
            set {
                if (_isPSBCPayment != value)
                {
                    _isPSBCPayment = value;
                    OnPropertyChanged(nameof(IsPSBCPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("PSBC");
                }
            }
        }

        public bool IsCMBPayment {
            get=>_isCMBPayment;
            set
            {
                // 修复私有字段名
                if (_isCMBPayment != value)
                {
                    _isCMBPayment = value;
                    OnPropertyChanged(nameof(IsCMBPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("CMB");
                }
            }
        }
        public bool IsBOCPayment {
            get => _isBOCPayment;
            set
            {
                if (_isBOCPayment != value)
                {
                    _isBOCPayment = value;
                    OnPropertyChanged(nameof(IsBOCPayment));
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("BOC");
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
                    // 应用互斥逻辑
                    HandlePaymentChannelMutualExclusion("COMM");
                }
            }
        }

        /// <summary>
        /// 支付宝支付是否启用
        /// </summary>
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

        /// <summary>
        /// 微信支付是否启用
        /// </summary>
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

        /// <summary>
        /// 问号按钮是否启用
        /// </summary>
        public bool IsQuestionButtonEnabled
        {
            get => _isQuestionButtonEnabled;
            set
            {
                if (_isQuestionButtonEnabled != value)
                {
                    _isQuestionButtonEnabled = value;
                    OnPropertyChanged(nameof(IsQuestionButtonEnabled));
                }
            }
        }

        /// <summary>
        /// 取票号是否启用
        /// </summary>
        public bool IsTicketNumberEnabled
        {
            get => _isTicketNumberEnabled;
            set
            {
                if (_isTicketNumberEnabled != value)
                {
                    _isTicketNumberEnabled = value;
                    OnPropertyChanged(nameof(IsTicketNumberEnabled));
                }
            }
        }

        /// <summary>
        /// 检票位置是否启用
        /// </summary>
        public bool IsCheckInLocationEnabled
        {
            get => _isCheckInLocationEnabled;
            set
            {
                if (_isCheckInLocationEnabled != value)
                {
                    _isCheckInLocationEnabled = value;
                    OnPropertyChanged(nameof(IsCheckInLocationEnabled));
                }
            }
        }

        /// <summary>
        /// 出发车站是否启用
        /// </summary>
        public bool IsDepartStationEnabled
        {
            get => _isDepartStationEnabled;
            set
            {
                if (_isDepartStationEnabled != value)
                {
                    _isDepartStationEnabled = value;
                    OnPropertyChanged(nameof(IsDepartStationEnabled));
                }
            }
        }

        /// <summary>
        /// 到达车站是否启用
        /// </summary>
        public bool IsArriveStationEnabled
        {
            get => _isArriveStationEnabled;
            set
            {
                if (_isArriveStationEnabled != value)
                {
                    _isArriveStationEnabled = value;
                    OnPropertyChanged(nameof(IsArriveStationEnabled));
                }
            }
        }

        /// <summary>
        /// 出发车站拼音是否启用
        /// </summary>
        public bool IsDepartStationPinyinEnabled
        {
            get => _isDepartStationPinyinEnabled;
            set
            {
                if (_isDepartStationPinyinEnabled != value)
                {
                    _isDepartStationPinyinEnabled = value;
                    OnPropertyChanged(nameof(IsDepartStationPinyinEnabled));
                }
            }
        }

        /// <summary>
        /// 到达车站拼音是否启用
        /// </summary>
        public bool IsArriveStationPinyinEnabled
        {
            get => _isArriveStationPinyinEnabled;
            set
            {
                if (_isArriveStationPinyinEnabled != value)
                {
                    _isArriveStationPinyinEnabled = value;
                    OnPropertyChanged(nameof(IsArriveStationPinyinEnabled));
                }
            }
        }

        /// <summary>
        /// 金额是否启用
        /// </summary>
        public bool IsMoneyEnabled
        {
            get => _isMoneyEnabled;
            set
            {
                if (_isMoneyEnabled != value)
                {
                    _isMoneyEnabled = value;
                    OnPropertyChanged(nameof(IsMoneyEnabled));
                }
            }
        }

        /// <summary>
        /// 出发车站代码是否启用
        /// </summary>
        public bool IsDepartStationCodeEnabled
        {
            get => _isDepartStationCodeEnabled;
            set
            {
                if (_isDepartStationCodeEnabled != value)
                {
                    _isDepartStationCodeEnabled = value;
                    OnPropertyChanged(nameof(IsDepartStationCodeEnabled));
                }
            }
        }

        /// <summary>
        /// 到达车站代码是否启用
        /// </summary>
        public bool IsArriveStationCodeEnabled
        {
            get => _isArriveStationCodeEnabled;
            set
            {
                if (_isArriveStationCodeEnabled != value)
                {
                    _isArriveStationCodeEnabled = value;
                    OnPropertyChanged(nameof(IsArriveStationCodeEnabled));
                }
            }
        }

        /// <summary>
        /// 出发日期是否启用
        /// </summary>
        public bool IsDepartDateEnabled
        {
            get => _isDepartDateEnabled;
            set
            {
                if (_isDepartDateEnabled != value)
                {
                    _isDepartDateEnabled = value;
                    OnPropertyChanged(nameof(IsDepartDateEnabled));
                }
            }
        }

        /// <summary>
        /// 车型是否启用
        /// </summary>
        public bool IsTrainTypeEnabled
        {
            get => _isTrainTypeEnabled;
            set
            {
                if (_isTrainTypeEnabled != value)
                {
                    _isTrainTypeEnabled = value;
                    OnPropertyChanged(nameof(IsTrainTypeEnabled));
                }
            }
        }

        /// <summary>
        /// 车次号是否启用
        /// </summary>
        public bool IsTrainNumberEnabled
        {
            get => _isTrainNumberEnabled;
            set
            {
                if (_isTrainNumberEnabled != value)
                {
                    _isTrainNumberEnabled = value;
                    OnPropertyChanged(nameof(IsTrainNumberEnabled));
                }
            }
        }

        /// <summary>
        /// 出发时间是否启用
        /// </summary>
        public bool IsDepartTimeEnabled
        {
            get => _isDepartTimeEnabled;
            set
            {
                if (_isDepartTimeEnabled != value)
                {
                    _isDepartTimeEnabled = value;
                    OnPropertyChanged(nameof(IsDepartTimeEnabled));
                }
            }
        }

        /// <summary>
        /// 车厢号是否启用
        /// </summary>
        public bool IsCoachNoEnabled
        {
            get => _isCoachNoEnabled;
            set
            {
                if (_isCoachNoEnabled != value)
                {
                    _isCoachNoEnabled = value;
                    OnPropertyChanged(nameof(IsCoachNoEnabled));
                }
            }
        }

        /// <summary>
        /// 加车选项是否启用
        /// </summary>
        public bool IsExtraCoachEnabled
        {
            get => _isExtraCoachEnabled;
            set
            {
                if (_isExtraCoachEnabled != value)
                {
                    _isExtraCoachEnabled = value;
                    OnPropertyChanged(nameof(IsExtraCoachEnabled));
                }
            }
        }

        /// <summary>
        /// 座位号是否启用
        /// </summary>
        public bool IsSeatNoEnabled
        {
            get => _isSeatNoEnabled;
            set
            {
                if (_isSeatNoEnabled != value)
                {
                    _isSeatNoEnabled = value;
                    OnPropertyChanged(nameof(IsSeatNoEnabled));
                    // 同时触发IsSeatInputEnabled的通知，确保UI能正确更新
                    OnPropertyChanged(nameof(IsSeatInputEnabled));
                }
            }
        }

        /// <summary>
        /// 无座选项是否启用
        /// </summary>
        public bool IsNoSeatEnabled
        {
            get => _isNoSeatEnabled;
            set
            {
                if (_isNoSeatEnabled != value)
                {
                    _isNoSeatEnabled = value;
                    OnPropertyChanged(nameof(IsNoSeatEnabled));
                }
            }
        }

        /// <summary>
        /// 座位位置是否启用
        /// </summary>
        public bool IsSeatPositionEnabled
        {
            get => _isSeatPositionEnabled;
            set
            {
                if (_isSeatPositionEnabled != value)
                {
                    _isSeatPositionEnabled = value;
                    OnPropertyChanged(nameof(IsSeatPositionEnabled));
                }
            }
        }

        /// <summary>
        /// 座位类型是否启用
        /// </summary>
        public bool IsSeatTypeEnabled
        {
            get => _isSeatTypeEnabled;
            set
            {
                if (_isSeatTypeEnabled != value)
                {
                    _isSeatTypeEnabled = value;
                    OnPropertyChanged(nameof(IsSeatTypeEnabled));
                }
            }
        }

        /// <summary>
        /// 附加信息是否启用
        /// </summary>
        public bool IsAdditionalInfoEnabled
        {
            get => _isAdditionalInfoEnabled;
            set
            {
                if (_isAdditionalInfoEnabled != value)
                {
                    _isAdditionalInfoEnabled = value;
                    OnPropertyChanged(nameof(IsAdditionalInfoEnabled));
                }
            }
        }

        /// <summary>
        /// 票种用途是否启用
        /// </summary>
        public bool IsTicketPurposeEnabled
        {
            get => _isTicketPurposeEnabled;
            set
            {
                if (_isTicketPurposeEnabled != value)
                {
                    _isTicketPurposeEnabled = value;
                    OnPropertyChanged(nameof(IsTicketPurposeEnabled));
                }
            }
        }

        /// <summary>
        /// 提示信息是否启用
        /// </summary>
        public bool IsHintEnabled
        {
            get => _isHintEnabled;
            set
            {
                if (_isHintEnabled != value)
                {
                    _isHintEnabled = value;
                    OnPropertyChanged(nameof(IsHintEnabled));
                }
            }
        }

        /// <summary>
        /// 自定义提示是否启用
        /// </summary>
        public bool IsCustomHintEnabled
        {
            get => _isCustomHintEnabled && SelectedHint == "自定义";
            set
            {
                if (_isCustomHintEnabled != value)
                {
                    _isCustomHintEnabled = value;
                    OnPropertyChanged(nameof(IsCustomHintEnabled));
                }
            }
        }

        /// <summary>
        /// 改签类型是否启用
        /// </summary>
        public bool IsTicketModificationTypeEnabled
        {
            get => _isTicketModificationTypeEnabled;
            set
            {
                if (_isTicketModificationTypeEnabled != value)
                {
                    _isTicketModificationTypeEnabled = value;
                    OnPropertyChanged(nameof(IsTicketModificationTypeEnabled));
                }
            }
        }

        /// <summary>
        /// 票种类型是否启用
        /// </summary>
        public bool IsTicketTypeEnabled
        {
            get => _isTicketTypeEnabled;
            set
            {
                if (_isTicketTypeEnabled != value)
                {
                    _isTicketTypeEnabled = value;
                    OnPropertyChanged(nameof(IsTicketTypeEnabled));
                }
            }
        }

        /// <summary>
        /// 支付方式是否启用
        /// </summary>
        public bool IsPaymentMethodEnabled
        {
            get => _isPaymentMethodEnabled;
            set
            {
                if (_isPaymentMethodEnabled != value)
                {
                    _isPaymentMethodEnabled = value;
                    OnPropertyChanged(nameof(IsPaymentMethodEnabled));
                }
            }
        }

        /// <summary>
        /// 切换字段启用状态的命令
        /// </summary>
        public ICommand ToggleFieldCommand { get; }

        /// <summary>
        /// 保存车票命令
        /// </summary>
        public ICommand SaveTicketCommand { get; }

        #endregion

        /// <summary>
        /// 选择图片
        /// </summary>
        private async Task SelectImage()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "选择车票图片",
                    Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == true)
                {
                    // 验证选择的是图片文件
                    string fileExtension = Path.GetExtension(dialog.FileName).ToLower();
                    if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png" && fileExtension != ".bmp")
                    {
                        MessageBoxHelper.ShowWarning("请选择有效的图片文件(jpg, jpeg, png, bmp)");
                        return;
                    }

                    // 重置之前的表单状态和OCR结果
                    ResetFormState();
                    
                    // 清除之前的OCR结果
                    OcrResults.Clear();
                    AverageConfidence = 0;
                    JsonResult = string.Empty;
                    
                    // 设置新选择的图片路径
                    SelectedImagePath = dialog.FileName;
                    LogHelper.LogInfo($"已选择新图片: {SelectedImagePath}，已重置表单状态");

                    // 如果环境未检测，先检测环境
                    if (!_ocrEnvironmentService.IsEnvironmentReady)
                    {
                        await _ocrEnvironmentService.CheckEnvironment();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("选择图片时出错", ex);
                MessageBoxHelper.ShowError($"选择图片时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行OCR识别
        /// </summary>
        private async Task RunOcr()
        {
            try
            {
                // 检查环境和图片
                if (!_ocrEnvironmentService.IsEnvironmentReady)
                {
                    MessageBoxHelper.ShowWarning("OCR环境未准备就绪，请先检测环境。");
                    return;
                }

                if (string.IsNullOrEmpty(SelectedImagePath))
                {
                    MessageBoxHelper.ShowWarning("请先选择图片。");
                    return;
                }

                // 设置加载中状态
                IsLoading = true;
                Progress = 0;
                LoadingMessage = "正在执行OCR识别...";
                JsonResult = string.Empty;
                OcrResults = new ObservableCollection<OcrResult>();

                // 执行OCR识别
                var jsonResult = await _pythonService.RunOcrWithExternalProcess(SelectedImagePath);

                // 检查JSON格式并处理可能的错误
                if (string.IsNullOrEmpty(jsonResult))
                {
                    IsLoading = false;
                    MessageBoxHelper.ShowError("OCR识别失败，未返回结果");
                    return;
                }

                // 判断返回的是错误对象还是正常结果数组
                if (jsonResult.TrimStart().StartsWith("{") && jsonResult.Contains("\"error\""))
                {
                    try
                    {
                        // 如果是JSON对象且包含error字段，尝试解析为错误对象
                        var ocrError = JsonConvert.DeserializeObject<OcrError>(jsonResult);
                        if (ocrError?.Error != null)
                        {
                            IsLoading = false;
                            MessageBoxHelper.ShowError($"OCR识别出错: {ocrError.Error}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        IsLoading = false;
                        MessageBoxHelper.ShowError($"解析OCR结果时出错: {ex.Message}");
                        return;
                    }
                }

                // 格式化JSON结果
                JsonResult = GetFormattedJson(jsonResult);

                try
                {
                    // 解析OCR结果
                    var ocrResults = JsonConvert.DeserializeObject<List<OcrResult>>(jsonResult);
                    if (ocrResults != null && ocrResults.Count > 0)
                    {
                        // 计算平均置信度
                        AverageConfidence = ocrResults.Average(r => r.Score);

                        // 添加到结果集合
                        OcrResults = new ObservableCollection<OcrResult>(ocrResults);

                        // 尝试填充表单数据
                        ProcessOcrResultsAndFillForm(ocrResults);

                        // 展开表单
                        IsTicketFormExpanded = true;
                    }
                    else
                    {
                        MessageBoxHelper.ShowWarning("OCR识别未返回有效结果，请尝试使用其他图片。");
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError($"解析OCR结果时出错: {ex.Message}");
                }

                // 更新OCR模型状态（可能在首次运行时已下载模型）
                // 使用logMessages=true参数，以便在模型状态更改时提供反馈
                await _ocrEnvironmentService.UpdateOcrModelStatus(true);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"执行OCR识别时出错: {ex.Message}");
            }
            finally
            {
                // 重置加载状态
                IsLoading = false;
                Progress = 100;
                LoadingMessage = string.Empty;
            }
        }

        /// <summary>
        /// 判断是否可以运行OCR
        /// </summary>
        private bool CanRunOcr()
        {
            return !IsLoading && IsPythonInstalled == true && IsCnocrInstalled == true && !string.IsNullOrEmpty(SelectedImagePath);
        }

        /// <summary>
        /// 获取格式化的JSON字符串
        /// </summary>
        private string GetFormattedJson(string inputJson)
        {
            if (string.IsNullOrWhiteSpace(inputJson))
                return string.Empty;

            try
            {
                // 尝试解析JSON并重新格式化
                var parsedJson = JsonConvert.DeserializeObject(inputJson);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch
            {
                // 如果解析失败，返回原始JSON
                return inputJson;
            }
        }

        /// <summary>
        /// 是否可以导入车票图片
        /// </summary>
        public bool CanImportTicket()
        {
            // 检查环境是否准备就绪，以及是否正在加载中
            return !IsLoading && _ocrEnvironmentService.IsEnvironmentReady;
        }

        #region 表单相关方法

        /// <summary>
        /// 加载车站数据
        /// </summary>
        private async Task LoadStationsAsync()
        {
            try
            {
                if (_stationSearchService != null)
                {
                    await _stationSearchService.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("加载车站数据失败", ex);
            }
        }

        /// <summary>
        /// 更新座位位置选项
        /// </summary>
        private void UpdateSeatPositions()
        {
            try
            {
                // 保存当前选择的位置
                string currentPosition = SelectedSeatPosition;

                SeatPositions.Clear();

                // 根据不同的座位类型设置不同的位置选项
                switch (SelectedSeatType)
                {
                    case "商务座":
                    case "一等座":
                    case "二等座":
                        SeatPositions.Add("A");
                        SeatPositions.Add("B");
                        SeatPositions.Add("C");
                        SeatPositions.Add("D");
                        SeatPositions.Add("F");
                        break;
                    case "新空调硬座":
                        // 硬座通常不显示位置
                        break;
                    case "软座":
                        SeatPositions.Add("A");
                        SeatPositions.Add("B");
                        SeatPositions.Add("C");
                        SeatPositions.Add("D");
                        break;
                    case "新空调硬卧":
                    case "新空调软卧":
                        SeatPositions.Add("上");
                        SeatPositions.Add("中");
                        SeatPositions.Add("下");
                        break;
                    default:
                        SeatPositions.Add("A");
                        SeatPositions.Add("B");
                        SeatPositions.Add("C");
                        SeatPositions.Add("D");
                        SeatPositions.Add("F");
                        break;
                }

                // 尝试保留原来的位置选择（如果该位置在新的选项中存在）
                if (!string.IsNullOrEmpty(currentPosition) && SeatPositions.Contains(currentPosition))
                {
                    SelectedSeatPosition = currentPosition;
                    LogHelper.LogInfo($"保留原有座位位置: {currentPosition}");
                }
                // 如果没有有效的位置选择且有位置选项，则设置默认选择第一个
                else if (string.IsNullOrEmpty(currentPosition) && SeatPositions.Count > 0)
                {
                    SelectedSeatPosition = SeatPositions[0];
                    LogHelper.LogInfo($"设置默认座位位置: {SelectedSeatPosition}");
                }
                else if (SeatPositions.Count == 0)
                {
                    SelectedSeatPosition = string.Empty;
                }

                // 更新UI
                OnPropertyChanged(nameof(SelectedSeatPosition));
                OnPropertyChanged(nameof(IsSeatPositionVisible));
            }
            catch (Exception ex)
            {
                LogHelper.LogError("更新座位位置选项时出错", ex);
            }
        }

        /// <summary>
        /// 站点输入框失去焦点时的处理
        /// </summary>
        public void OnStationLostFocus(bool isDepartStation)
        {
            try
            {
                // 获取当前站名
                string stationName = isDepartStation ? DepartStationSearchText : ArriveStationSearchText;

                // 检测是否在下拉框上操作
                if ((isDepartStation && IsDepartStationDropdownOpen) || 
                    (!isDepartStation && IsArriveStationDropdownOpen))
                {
                    return;
                }

                // 关闭下拉列表
                if (isDepartStation)
                {
                    IsDepartStationDropdownOpen = false;
                }
                else
                {
                    IsArriveStationDropdownOpen = false;
                }

                // 如果输入为空，则不进行验证
                if (string.IsNullOrWhiteSpace(stationName))
                {
                    return;
                }

                // 验证站名并更新站点信息 (调用重构后的方法)
                ValidateStationName(stationName, isDepartStation);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"车站名失去焦点处理时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 搜索车站
        /// </summary>
        private async void SearchStations(string searchText, bool isDepartStation)
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

                if (_stationSearchService == null)
                    return;

                // 使用StationSearchService搜索站点
                var suggestions = await _stationSearchService.SearchStationsAsync(searchText);

                if (isDepartStation)
                {
                    DepartStationSuggestions.Clear();
                    foreach (var station in suggestions)
                    {
                        DepartStationSuggestions.Add(station);
                    }
                    IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                }
                else
                {
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

        /// <summary>
        /// 切换字段启用状态
        /// </summary>
        private void ToggleField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return;

            switch (fieldName)
            {
                case "TicketNumber":
                    IsTicketNumberEnabled = !IsTicketNumberEnabled;
                    break;
                case "CheckInLocation":
                    IsCheckInLocationEnabled = !IsCheckInLocationEnabled;
                    break;
                case "DepartStation":
                    IsDepartStationEnabled = !IsDepartStationEnabled;
                    break;
                case "ArriveStation":
                    IsArriveStationEnabled = !IsArriveStationEnabled;
                    break;
                case "DepartStationPinyin":
                    IsDepartStationPinyinEnabled = !IsDepartStationPinyinEnabled;
                    break;
                case "ArriveStationPinyin":
                    IsArriveStationPinyinEnabled = !IsArriveStationPinyinEnabled;
                    break;
                case "Money":
                    IsMoneyEnabled = !IsMoneyEnabled;
                    break;
                case "DepartStationCode":
                    IsDepartStationCodeEnabled = !IsDepartStationCodeEnabled;
                    break;
                case "ArriveStationCode":
                    IsArriveStationCodeEnabled = !IsArriveStationCodeEnabled;
                    break;
                case "DepartDate":
                    IsDepartDateEnabled = !IsDepartDateEnabled;
                    break;
                case "TrainNumber":
                    IsTrainTypeEnabled = !IsTrainTypeEnabled;
                    IsTrainNumberEnabled = !IsTrainNumberEnabled;
                    break;
                case "DepartTime":
                    IsDepartTimeEnabled = !IsDepartTimeEnabled;
                    break;
                case "CoachNo":
                    IsCoachNoEnabled = !IsCoachNoEnabled;
                    IsExtraCoachEnabled = !IsExtraCoachEnabled;
                    break;
                case "SeatNo":
                    // 记录点击前状态
                    LogHelper.LogInfo($"点击前状态：无座复选框={IsNoSeatEnabled}，座位号={IsSeatNoEnabled}，座位位置={IsSeatPositionEnabled}，无座状态={IsNoSeat}");
                    
                    // 切换无座复选框的可编辑状态
                    IsNoSeatEnabled = !IsNoSeatEnabled;
                    
                    // 座位号和座位位置根据无座状态决定
                    if (!IsNoSeat) // 非无座状态
                    {
                        // 非无座状态下，座位号和座位位置跟随无座复选框一起切换
                        IsSeatNoEnabled = IsNoSeatEnabled; 
                        IsSeatPositionEnabled = IsNoSeatEnabled;
                    }
                    else // 无座状态
                    {
                        // 无座状态下座位号和座位位置始终锁定
                        IsSeatNoEnabled = false;
                        IsSeatPositionEnabled = false;
                    }
                    
                    // 额外触发IsSeatInputEnabled的通知，确保UI更新
                    OnPropertyChanged(nameof(IsSeatInputEnabled));
                    
                    LogHelper.LogInfo($"点击后状态：无座复选框={IsNoSeatEnabled}，座位号={IsSeatNoEnabled}，座位位置={IsSeatPositionEnabled}，无座状态={IsNoSeat}");
                    break;
                case "SeatType":
                    IsSeatTypeEnabled = !IsSeatTypeEnabled;
                    break;
                case "AdditionalInfo":
                    IsAdditionalInfoEnabled = !IsAdditionalInfoEnabled;
                    break;
                case "TicketPurpose":
                    IsTicketPurposeEnabled = !IsTicketPurposeEnabled;
                    break;
                case "Hint":
                    IsHintEnabled = !IsHintEnabled;
                    IsCustomHintEnabled = !IsCustomHintEnabled;
                    break;
                case "TicketModificationType":
                    IsTicketModificationTypeEnabled = !IsTicketModificationTypeEnabled;
                    break;
                case "TicketType":
                    IsTicketTypeEnabled = !IsTicketTypeEnabled;
                    break;
                case "PaymentMethod":
                    IsPaymentMethodEnabled = !IsPaymentMethodEnabled;
                    break;
            }
        }

        /// <summary>
        /// 保存车票信息
        /// </summary>
        private void SaveTicket()
        {
            try
            {
                // 创建一个错误消息列表，用于保存所有验证错误
                var validationErrors = new List<string>();
                StringBuilder errorMessages = new StringBuilder();

                // 创建TrainRideInfo对象用于验证
                var ticket = new TrainRideInfo
                {
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation.EndsWith("站") ? DepartStation : DepartStation + "站",
                    ArriveStation = ArriveStation.EndsWith("站") ? ArriveStation : ArriveStation + "站",
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

                // 1. 收集所有必填项错误，但不立即返回
                bool isBasicValidationPassed = FormValidationHelper.ValidateTicketForm(ticket, validationErrors);

                // 2. 验证车站外键关系前，确保StationSearchService已初始化
                if (!_stationSearchService.IsInitialized)
                {
                    var initTask = _stationSearchService.InitializeAsync();
                    initTask.Wait(); // 在UI线程中谨慎使用Wait()
                }

                bool departHasError = false;
                bool arriveHasError = false;

                // 3. 检测出发车站和到达车站信息
                // 只有在用户填写了站名的情况下才检测代码和拼音是否匹配
                if (!string.IsNullOrWhiteSpace(DepartStation))
                {
                    // 通过站名查找出发车站信息
                    var departByName = _stationSearchService.Stations
                        .FirstOrDefault(s => s.StationName == DepartStation ||
                                            s.StationName == DepartStation + "站" ||
                                            s.StationName?.Replace("站", "") == DepartStation);

                    // 通过代码查找出发车站信息
                    var departByCode = !string.IsNullOrWhiteSpace(DepartStationCode) ?
                        _stationSearchService.Stations.FirstOrDefault(s => s.StationCode == DepartStationCode) : null;

                    // 如果站名能找到，但代码不匹配或为空
                    if (departByName != null)
                    {
                        // 检测代码是否匹配或为空
                        if (string.IsNullOrWhiteSpace(DepartStationCode))
                        {
                            // 将代码为空的情况添加到验证错误
                            if (!validationErrors.Any(e => e.Contains("未填写出发车站代码")))
                            {
                                validationErrors.Add("未填写出发车站代码");
                            }
                        }
                        else if (departByCode == null || departByName.Id != departByCode.Id)
                        {
                            // 代码不匹配
                            departHasError = true;
                            errorMessages.AppendLine($"出发车站【{DepartStation}】的代码错误：");
                            errorMessages.AppendLine($"- 当前填写的代码【{DepartStationCode}】与车站不匹配");
                            errorMessages.AppendLine($"- 正确的代码应为：【{departByName.StationCode}】");
                            errorMessages.AppendLine();
                        }

                        // 检测拼音是否匹配或为空
                        if (string.IsNullOrWhiteSpace(DepartStationPinyin))
                        {
                            // 将拼音为空的情况添加到验证错误
                            if (!validationErrors.Any(e => e.Contains("未填写出发车站拼音")))
                            {
                                validationErrors.Add("未填写出发车站拼音");
                            }
                        }
                        else if (DepartStationPinyin != departByName.StationPinyin)
                        {
                            // 拼音不匹配
                            departHasError = true;
                            errorMessages.AppendLine($"出发车站【{DepartStation}】的拼音错误：");
                            errorMessages.AppendLine($"- 当前填写的拼音【{DepartStationPinyin}】与车站记录不匹配");
                            errorMessages.AppendLine($"- 正确的拼音应为：【{departByName.StationPinyin}】");
                            errorMessages.AppendLine();
                        }
                    }
                    // 如果无法通过站名找到匹配的车站
                    else if (departByName == null && departByCode == null)
                    {
                        departHasError = true;
                        errorMessages.AppendLine($"出发车站【{DepartStation}】在车站中心不存在，请先添加该车站信息。");
                        errorMessages.AppendLine();
                    }
                }

                // 检测到达车站，逻辑与出发车站类似
                if (!string.IsNullOrWhiteSpace(ArriveStation))
                {
                    // 通过站名查找到达车站信息
                    var arriveByName = _stationSearchService.Stations
                        .FirstOrDefault(s => s.StationName == ArriveStation ||
                                            s.StationName == ArriveStation + "站" ||
                                            s.StationName?.Replace("站", "") == ArriveStation);

                    // 通过代码查找到达车站信息
                    var arriveByCode = !string.IsNullOrWhiteSpace(ArriveStationCode) ?
                        _stationSearchService.Stations.FirstOrDefault(s => s.StationCode == ArriveStationCode) : null;

                    // 如果站名能找到，但代码不匹配或为空
                    if (arriveByName != null)
                    {
                        // 检测代码是否匹配或为空
                        if (string.IsNullOrWhiteSpace(ArriveStationCode))
                        {
                            // 将代码为空的情况添加到验证错误
                            if (!validationErrors.Any(e => e.Contains("未填写到达车站代码")))
                            {
                                validationErrors.Add("未填写到达车站代码");
                            }
                        }
                        else if (arriveByCode == null || arriveByName.Id != arriveByCode.Id)
                        {
                            // 代码不匹配
                            arriveHasError = true;
                            errorMessages.AppendLine($"到达车站【{ArriveStation}】的代码错误：");
                            errorMessages.AppendLine($"- 当前填写的代码【{ArriveStationCode}】与车站不匹配");
                            errorMessages.AppendLine($"- 正确的代码应为：【{arriveByName.StationCode}】");
                            errorMessages.AppendLine();
                        }

                        // 检测拼音是否匹配或为空
                        if (string.IsNullOrWhiteSpace(ArriveStationPinyin))
                        {
                            // 将拼音为空的情况添加到验证错误
                            if (!validationErrors.Any(e => e.Contains("未填写到达车站拼音")))
                            {
                                validationErrors.Add("未填写到达车站拼音");
                            }
                        }
                        else if (ArriveStationPinyin != arriveByName.StationPinyin)
                        {
                            // 拼音不匹配
                            arriveHasError = true;
                            errorMessages.AppendLine($"到达车站【{ArriveStation}】的拼音错误：");
                            errorMessages.AppendLine($"- 当前填写的拼音【{ArriveStationPinyin}】与车站记录不匹配");
                            errorMessages.AppendLine($"- 正确的拼音应为：【{arriveByName.StationPinyin}】");
                            errorMessages.AppendLine();
                        }
                    }
                    // 如果无法通过站名找到匹配的车站
                    else if (arriveByName == null && arriveByCode == null)
                    {
                        arriveHasError = true;
                        errorMessages.AppendLine($"到达车站【{ArriveStation}】在车站中心不存在，请先添加该车站信息。");
                        errorMessages.AppendLine();
                    }
                }

                // 4. 组合所有错误信息
                if (validationErrors.Count > 0 || departHasError || arriveHasError)
                {
                    // 构建完整的错误信息
                    StringBuilder fullErrorMessage = new StringBuilder("请修正以下错误：\n");

                    // 先添加必填项错误
                    foreach (var error in validationErrors)
                    {
                        fullErrorMessage.AppendLine($"- {error}");
                    }

                    // 如果有必填项错误和其他错误，添加一个分隔行
                    if (validationErrors.Count > 0 && (departHasError || arriveHasError))
                    {
                        fullErrorMessage.AppendLine();
                    }

                    // 添加车站匹配错误
                    if (departHasError || arriveHasError)
                    {
                        fullErrorMessage.Append(errorMessages);
                    }

                    MessageBoxHelper.ShowWarning(fullErrorMessage.ToString(), "保存验证");
                    return;
                }

                // 5. 验证都通过，创建最终要保存的TrainRideInfo对象
                var finalTicket = new TrainRideInfo
                {
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation.EndsWith("站") ? DepartStation : DepartStation + "站",
                    ArriveStation = ArriveStation.EndsWith("站") ? ArriveStation : ArriveStation + "站",
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

                // 保存车票到数据库
                _databaseService.AddTicketAsync(finalTicket).ContinueWith(task =>
                {
                    // 在UI线程上执行操作
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (task.IsCompleted && !task.IsFaulted)
                        {
                            MessageBoxHelper.ShowInformation("车票已成功保存！", "成功");

                            // 重置表单状态
                            ResetFormState();

                            // 通知主窗口刷新数据
                            _mainViewModel.TicketListCommand.Execute(null);

                            // 触发关闭窗口事件
                            RequestCloseAction?.Invoke(); 
                        }
                        else
                        {
                            MessageBoxHelper.ShowError($"保存车票时出错: {task.Exception?.InnerException?.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"保存车票时出错: {ex.Message}");
                LogHelper.LogError($"OCR导入 - 保存车票时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取票种类型标志位
        /// </summary>
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
        /// 获取支付渠道标志位
        /// </summary>
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

        /// <summary>
        /// 处理OCR结果并填充表单
        /// </summary>
        /// <param name="ocrResults">OCR识别结果列表</param>
        private async void ProcessOcrResultsAndFillForm(List<OcrResult> ocrResults)
        {
            if (ocrResults == null || ocrResults.Count == 0)
            {
                return;
            }

            try
            {
                // 确保车站数据已经加载
                if (_stationSearchService != null && !_stationSearchService.IsInitialized)
                {
                    await _stationSearchService.InitializeAsync();
                }

                // 将所有OCR结果文本合并到一个列表中，方便处理
                List<string> allTexts = ocrResults.Select(r => r.Text).ToList();
                string combinedText = string.Join(" ", allTexts);

                // 记录OCR结果，帮助调试
                if (ocrResults.Count > 0) LogHelper.LogInfo($"OCR结果第1个文本: {ocrResults[0].Text}");
                if (ocrResults.Count > 1) LogHelper.LogInfo($"OCR结果第2个文本: {ocrResults[1].Text}");
                if (ocrResults.Count > 2) LogHelper.LogInfo($"OCR结果第3个文本: {ocrResults[2].Text}");
                if (ocrResults.Count > 3) LogHelper.LogInfo($"OCR结果第4个文本: {ocrResults[3].Text}");

                // 启用所有要自动填充的表单项的问号按钮
                EnableFormFields(); // 现在这个方法只启用问号按钮

                // 1. 检票号：首字母大写、一般为6位数字
                foreach (var text in allTexts)
                {
                    if (text.Length >= 6 && char.IsUpper(text[0]) && text.Substring(1).All(c => char.IsDigit(c)))
                    {
                        TicketNumber = text;
                        break;
                    }
                }

                // 2. 检票地点：包含"检票："、"候车："或"xx候车室"
                foreach (var text in allTexts)
                {
                    if (text.Contains("检票：") || text.Contains("检票:"))
                    {
                        // 处理中文冒号或英文冒号
                        string pattern = text.Contains("检票：") ? "检票：" : "检票:";
                        int index = text.IndexOf(pattern) + pattern.Length;
                        CheckInLocation = text.Substring(index).Trim();
                        break;
                    }
                    else if (text.Contains("候车：") || text.Contains("候车:"))
                    {
                        // 处理中文冒号或英文冒号
                        string pattern = text.Contains("候车：") ? "候车：" : "候车:";
                        int index = text.IndexOf(pattern) + pattern.Length;
                        CheckInLocation = text.Substring(index).Trim();
                        break;
                    }
                    else if (text.StartsWith("检票"))
                    {
                        // 去掉开头的"检票"前缀
                        CheckInLocation = text.Substring("检票".Length).Trim();
                        break;
                    }
                    else if (text.Contains("检票") && text.Contains("候车室"))
                    {
                        // 处理"检票候车室X"的格式
                        int index = text.IndexOf("检票") + "检票".Length;
                        CheckInLocation = text.Substring(index).Trim();
                        break;
                    }
                    else if (text.Contains("候车室"))
                    {
                        CheckInLocation = text;
                        break;
                    }
                }

                // 提取车站信息
                List<string> stations = new List<string>();
                HashSet<string> addedStations = new HashSet<string>(); // 用于去重

                // 首先查找带有"站"字的高置信度文本作为出发车站和到达车站
                var highConfidenceStations = new List<(string stationName, double score, int index)>();

                // 遍历所有OCR结果，找出高置信度的站名（无论是否带"站"字）
                for (int i = 0; i < ocrResults.Count; i++)
                {
                    var result = ocrResults[i];
                    
                    // 考虑不带"站"字的情况，且识别度高于0.7的结果
                    if ((result.Text.EndsWith("站") || 
                         _stationSearchService.IsValidStation(result.Text)) 
                        && !result.Text.Contains("检票") 
                        && !result.Text.Contains("候车") 
                        // 排除车次号被误识别为站名的情况
                        && !(result.Text.StartsWith("K") || result.Text.StartsWith("G") || 
                             result.Text.StartsWith("D") || result.Text.StartsWith("C") || 
                             result.Text.StartsWith("Z") || result.Text.StartsWith("T") || 
                             result.Text.StartsWith("Y") || result.Text.StartsWith("L"))
                        && result.Score > 0.7)
                    {
                        highConfidenceStations.Add((result.Text, result.Score, i));
                        LogHelper.LogInfo($"找到高置信度的站名: {result.Text}, 置信度: {result.Score:F2}, 索引: {i}");
                    }
                }

                // 优先考虑带"站"字的文本，进一步过滤车次号等非站名文本
                var stationWithSuffix = highConfidenceStations.Where(s => s.stationName.EndsWith("站")).ToList();
                
                // 如果找到带"站"字的文本，优先使用它们
                if (stationWithSuffix.Count >= 2)
                {
                    LogHelper.LogInfo("优先使用带'站'字的文本作为站名");
                    highConfidenceStations = stationWithSuffix.OrderBy(s => s.index).ToList();
                }

                // 如果找到至少两个高置信度站名，根据顺序识别出发车站和到达车站
                if (highConfidenceStations.Count >= 2)
                {
                    string departStationName = null;
                    string arriveStationName = null;
                    
                    // 优先使用带"站"字的文本作为站名
                    var stationsWithSuffix = highConfidenceStations.Where(s => s.stationName.EndsWith("站")).OrderBy(s => s.index).ToList();
                    if (stationsWithSuffix.Count >= 2)
                    {
                        departStationName = stationsWithSuffix[0].stationName;
                        arriveStationName = stationsWithSuffix[1].stationName;
                        LogHelper.LogInfo($"使用带'站'字的文本作为站名：出发车站={departStationName}, 到达车站={arriveStationName}");
                    }
                    // 如果没有足够的带"站"字的文本，则回退到之前的逻辑
                    else if (highConfidenceStations.Any(s => s.index == 2) && 
                             highConfidenceStations.Any(s => s.index == 3))
                    {
                        // 索引2作为出发车站，索引3作为到达车站
                        departStationName = highConfidenceStations.First(s => s.index == 2).stationName;
                        arriveStationName = highConfidenceStations.First(s => s.index == 3).stationName;
                        LogHelper.LogInfo($"使用索引2的项作为出发车站: {departStationName}, 索引3的项作为到达车站: {arriveStationName}");
                    }
                    // 如果没有这些索引但有至少两个站名，使用前两个
                    else
                    {
                        departStationName = highConfidenceStations[0].stationName;
                        arriveStationName = highConfidenceStations[1].stationName;
                        LogHelper.LogInfo($"使用第一个高置信度项作为出发车站: {departStationName}, 第二个作为到达车站: {arriveStationName}");
                    }
                    
                    // 检查是否有误识别车次号为站名的情况
                    if (arriveStationName != null && 
                        (arriveStationName.StartsWith("K") || arriveStationName.StartsWith("G") || 
                         arriveStationName.StartsWith("D") || arriveStationName.StartsWith("C") || 
                         arriveStationName.StartsWith("Z") || arriveStationName.StartsWith("T") || 
                         arriveStationName.StartsWith("Y") || arriveStationName.StartsWith("L")))
                    {
                        LogHelper.LogInfo($"检测到车次号 {arriveStationName} 被错误填充到到达车站，清空到达车站字段以便正确识别");
                        arriveStationName = null;
                        
                        // 尝试从所有OCR结果中查找带"站"字的文本作为到达车站
                        foreach (var text in allTexts)
                        {
                            if (text != departStationName && text.EndsWith("站") && 
                                !addedStations.Contains(text) && !text.Contains("退票"))
                            {
                                arriveStationName = text;
                                LogHelper.LogInfo($"找到合适的到达车站: {arriveStationName}");
                                break;
                            }
                        }
                    }
                    
                    // 处理出发车站
                    if (departStationName != null)
                    {
                        string departStationNameWithoutStation = departStationName.EndsWith("站")
                            ? departStationName.Substring(0, departStationName.Length - 1)
                            : departStationName;
                        
                        // 设置去掉站字的名称
                        DepartStation = departStationNameWithoutStation;
                        
                        // 设置搜索文本，确保UI显示
                        _isUpdatingDepartStation = true;
                        DepartStationSearchText = departStationNameWithoutStation;
                        _isUpdatingDepartStation = false;
                        
                        // 确保UI更新
                        OnPropertyChanged(nameof(DepartStation));
                        OnPropertyChanged(nameof(DepartStationSearchText));
                        
                        // 尝试填充拼音和代码
                        var departStationInfo = _stationSearchService.GetStationInfo(departStationName);
                        if (departStationInfo != null)
                        {
                            LogHelper.LogInfo($"出发车站在数据库中找到: {departStationInfo.StationName}");
                            
                            // 车站在数据库中存在
                            if (!string.IsNullOrEmpty(departStationInfo.StationPinyin))
                            {
                                DepartStationPinyin = departStationInfo.StationPinyin;
                            }
                            
                            if (!string.IsNullOrEmpty(departStationInfo.StationCode))
                            {
                                DepartStationCode = departStationInfo.StationCode;
                            }
                        }
                        else
                        {
                            // 尝试搜索不带站字的名称
                            var altStationInfo = _stationSearchService.GetStationInfo(departStationNameWithoutStation);
                            if (altStationInfo != null)
                            {
                                LogHelper.LogInfo($"出发车站(不带站字)在数据库中找到: {altStationInfo.StationName}");
                                
                                if (!string.IsNullOrEmpty(altStationInfo.StationPinyin))
                                {
                                    DepartStationPinyin = altStationInfo.StationPinyin;
                                }
                                
                                if (!string.IsNullOrEmpty(altStationInfo.StationCode))
                                {
                                    DepartStationCode = altStationInfo.StationCode;
                                }
                            }
                        }
                        
                        // 添加到已处理站点列表
                        addedStations.Add(departStationName);
                        stations.Add(departStationName);
                    }
                    
                    // 处理到达车站
                    if (arriveStationName != null)
                    {
                        string arriveStationNameWithoutStation = arriveStationName.EndsWith("站")
                            ? arriveStationName.Substring(0, arriveStationName.Length - 1)
                            : arriveStationName;
                        
                        // 设置去掉站字的名称
                        ArriveStation = arriveStationNameWithoutStation;
                        
                        // 设置搜索文本，确保UI显示
                        _isUpdatingArriveStation = true;
                        ArriveStationSearchText = arriveStationNameWithoutStation;
                        _isUpdatingArriveStation = false;
                        
                        // 确保UI更新
                        OnPropertyChanged(nameof(ArriveStation));
                        OnPropertyChanged(nameof(ArriveStationSearchText));
                        
                        // 尝试填充拼音和代码
                        var arriveStationInfo = _stationSearchService.GetStationInfo(arriveStationName);
                        if (arriveStationInfo != null)
                        {
                            LogHelper.LogInfo($"到达车站在数据库中找到: {arriveStationInfo.StationName}");
                            
                            // 车站在数据库中存在
                            if (!string.IsNullOrEmpty(arriveStationInfo.StationPinyin))
                            {
                                ArriveStationPinyin = arriveStationInfo.StationPinyin;
                            }
                            
                            if (!string.IsNullOrEmpty(arriveStationInfo.StationCode))
                            {
                                ArriveStationCode = arriveStationInfo.StationCode;
                            }
                        }
                        else
                        {
                            // 尝试搜索不带站字的名称
                            var altStationInfo = _stationSearchService.GetStationInfo(arriveStationNameWithoutStation);
                            if (altStationInfo != null)
                            {
                                LogHelper.LogInfo($"到达车站(不带站字)在数据库中找到: {altStationInfo.StationName}");
                                
                                if (!string.IsNullOrEmpty(altStationInfo.StationPinyin))
                                {
                                    ArriveStationPinyin = altStationInfo.StationPinyin;
                                }
                                
                                if (!string.IsNullOrEmpty(altStationInfo.StationCode))
                                {
                                    ArriveStationCode = altStationInfo.StationCode;
                                }
                            }
                        }
                        
                        // 添加到已处理站点列表
                        addedStations.Add(arriveStationName);
                        stations.Add(arriveStationName);
                    }
                }
                else
                {
                    // 如果没有找到两个高置信度站名，回退到原来的逻辑
                    LogHelper.LogInfo("未找到两个高置信度的站名，回退到原来的逻辑");
                    // ... existing code ...
                }

                // 再处理包含"站"字的文本，这些文本更可能是车站名（现在仅用于补充信息，不再覆盖上面设置的站点）
                foreach (var text in allTexts)
                {
                    // 判断是否包含"站"字并且不是已添加过的站点
                    if (text.EndsWith("站") && !addedStations.Contains(text))
                    {
                        stations.Add(text);
                        addedStations.Add(text);
                        LogHelper.LogInfo($"找到带站字的车站名: {text}");
                    }
                }

                // 然后处理不含"站"字但在车站数据库中的文本
                foreach (var text in allTexts)
                {
                    // 判断是否是有效车站但不含"站"字且不是已添加过的
                    if (!text.EndsWith("站") && _stationSearchService.IsValidStation(text) && !addedStations.Contains(text))
                    {
                        stations.Add(text);
                        addedStations.Add(text);
                        LogHelper.LogInfo($"找到不带站字但有效的车站名: {text}");
                    }
                }

                // 3. 出发车站：第一个车站
                if (stations.Count > 0 && string.IsNullOrEmpty(DepartStation))
                {
                    string departStationName = stations[0];
                    // 去掉站字
                    string departStationNameWithoutStation = departStationName.EndsWith("站")
                        ? departStationName.Substring(0, departStationName.Length - 1)
                        : departStationName;

                    LogHelper.LogInfo($"提取出出发车站: {departStationNameWithoutStation}");

                    // 设置去掉站字的名称
                    DepartStation = departStationNameWithoutStation;

                    // 设置搜索文本，确保UI显示
                    _isUpdatingDepartStation = true;
                    DepartStationSearchText = departStationNameWithoutStation;
                    _isUpdatingDepartStation = false;

                    // 确保UI更新
                    OnPropertyChanged(nameof(DepartStation));
                    OnPropertyChanged(nameof(DepartStationSearchText));

                    // 尝试填充拼音和代码
                    var stationInfo = _stationSearchService.GetStationInfo(departStationName);
                    if (stationInfo != null)
                    {
                        LogHelper.LogInfo($"出发车站在数据库中找到: {stationInfo.StationName}");

                        // 车站在数据库中存在
                        if (!string.IsNullOrEmpty(stationInfo.StationPinyin))
                        {
                            DepartStationPinyin = stationInfo.StationPinyin;
                        }

                        if (!string.IsNullOrEmpty(stationInfo.StationCode))
                        {
                            DepartStationCode = stationInfo.StationCode;
                        }
                    }
                    else
                    {
                        LogHelper.LogInfo($"出发车站在数据库中未找到: {departStationName}");
                    }
                    // 如果车站不在数据库内或者代码/拼音为空，这些字段会保持为空
                }

                // 5. 到达车站：第二个车站
                if (stations.Count > 1 && string.IsNullOrEmpty(ArriveStation))
                {
                    string arriveStationName = stations[1];
                    // 去掉站字
                    string arriveStationNameWithoutStation = arriveStationName.EndsWith("站")
                        ? arriveStationName.Substring(0, arriveStationName.Length - 1)
                        : arriveStationName;

                    LogHelper.LogInfo($"提取出到达车站: {arriveStationNameWithoutStation}");

                    // 设置去掉站字的名称
                    ArriveStation = arriveStationNameWithoutStation;

                    // 设置搜索文本，确保UI显示
                    _isUpdatingArriveStation = true;
                    ArriveStationSearchText = arriveStationNameWithoutStation;
                    _isUpdatingArriveStation = false;

                    // 确保UI更新
                    OnPropertyChanged(nameof(ArriveStation));
                    OnPropertyChanged(nameof(ArriveStationSearchText));

                    // 尝试填充拼音和代码
                    var stationInfo = _stationSearchService.GetStationInfo(arriveStationName);
                    if (stationInfo != null)
                    {
                        LogHelper.LogInfo($"到达车站在数据库中找到: {stationInfo.StationName}");

                        // 车站在数据库中存在
                        if (!string.IsNullOrEmpty(stationInfo.StationPinyin))
                        {
                            ArriveStationPinyin = stationInfo.StationPinyin;
                        }

                        if (!string.IsNullOrEmpty(stationInfo.StationCode))
                        {
                            ArriveStationCode = stationInfo.StationCode;
                        }
                    }
                    else
                    {
                        LogHelper.LogInfo($"到达车站在数据库中未找到: {arriveStationName}");
                    }
                    // 如果车站不在数据库内或者代码/拼音为空，这些字段会保持为空
                }

                // 如果前面的方式都没找到到达车站，继续尝试从其他OCR结果查找
                else if (string.IsNullOrEmpty(ArriveStation) && stations.Count < 2 && ocrResults.Count >= 3)
                {
                    // 从第三个OCR文本开始查找
                    for (int i = 2; i < ocrResults.Count; i++)
                    {
                        // 跳过已经用于出发车站或到达车站的元素
                        if (i == 1 || i == 3) continue;

                        string possibleArriveStation = ocrResults[i].Text;

                        // 尝试匹配包含"站"或K、L、G、D等字母后面跟数字的文本，可能是车次号+站名
                        if (possibleArriveStation.EndsWith("站") ||
                            _stationSearchService.IsValidStation(possibleArriveStation) ||
                            Regex.IsMatch(possibleArriveStation, @"([KLGDTZC]\d+).*站$"))
                        {
                            // 如果匹配到车次号+站名的格式，需要提取站名部分
                            string stationNamePart = possibleArriveStation;
                            var match = Regex.Match(possibleArriveStation, @"([KLGDTZC]\d+)(.+站)$");

                            if (match.Success && match.Groups.Count >= 3)
                            {
                                stationNamePart = match.Groups[2].Value;
                            }

                            // 去掉站字
                            string arriveStationNameWithoutStation = stationNamePart.EndsWith("站")
                                ? stationNamePart.Substring(0, stationNamePart.Length - 1)
                                : stationNamePart;

                            LogHelper.LogInfo($"备选方式从OCR结果中提取到达车站: {arriveStationNameWithoutStation}");

                            // 检测是否是有效站点
                            if (_stationSearchService.IsValidStation(arriveStationNameWithoutStation))
                            {
                                // 设置去掉站字的名称
                                ArriveStation = arriveStationNameWithoutStation;

                                // 设置搜索文本，确保UI显示
                                _isUpdatingArriveStation = true;
                                ArriveStationSearchText = arriveStationNameWithoutStation;
                                _isUpdatingArriveStation = false;

                                // 确保UI更新
                                OnPropertyChanged(nameof(ArriveStation));
                                OnPropertyChanged(nameof(ArriveStationSearchText));

                                // 尝试填充拼音和代码
                                var stationInfo = _stationSearchService.GetStationInfo(arriveStationNameWithoutStation);
                                if (stationInfo != null)
                                {
                                    LogHelper.LogInfo($"到达车站在数据库中找到: {stationInfo.StationName}");

                                    // 车站在数据库中存在
                                    if (!string.IsNullOrEmpty(stationInfo.StationPinyin))
                                    {
                                        ArriveStationPinyin = stationInfo.StationPinyin;
                                    }

                                    if (!string.IsNullOrEmpty(stationInfo.StationCode))
                                    {
                                        ArriveStationCode = stationInfo.StationCode;
                                    }
                                }

                                break;  // 找到有效站点后退出循环
                            }
                        }
                    }
                }

                // 4. 车次号：首字母为G、C、D、Z、T、K、L、S，后接1~4位数字
                foreach (var text in allTexts)
                {
                    // 匹配车次号格式
                    string trainTypeRegex = @"^([GCDZTKLSY])(\d{1,4})$";
                    var match = Regex.Match(text, trainTypeRegex);
                    if (match.Success)
                    {
                        SelectedTrainType = match.Groups[1].Value; // 字母部分
                        TrainNumber = match.Groups[2].Value; // 数字部分
                        
                        // 特殊处理：如果到达车站字段包含了车次号，说明是错误填充，需要修正
                        if (ArriveStation == text)
                        {
                            LogHelper.LogInfo($"检测到车次号 {text} 被错误填充到到达车站，清空到达车站字段以便正确识别");
                            ArriveStation = string.Empty;
                            ArriveStationSearchText = string.Empty;
                            // 确保UI更新
                            OnPropertyChanged(nameof(ArriveStation));
                            OnPropertyChanged(nameof(ArriveStationSearchText));
                            
                            // 重新检查其他OCR结果中是否包含正确的到达车站
                            foreach (var result in ocrResults)
                            {
                                if (result.Text != text && 
                                    (result.Text.EndsWith("站") || _stationSearchService.IsValidStation(result.Text)) &&
                                    !addedStations.Contains(result.Text) && 
                                    result.Text != DepartStation && 
                                    result.Text != DepartStation + "站")
                                {
                                    string arriveStationName = result.Text;
                                    // 去掉站字
                                    string arriveStationNameWithoutStation = arriveStationName.EndsWith("站")
                                        ? arriveStationName.Substring(0, arriveStationName.Length - 1)
                                        : arriveStationName;
                                    
                                    LogHelper.LogInfo($"找到可能的正确到达车站: {arriveStationNameWithoutStation}");
                                    
                                    // 设置去掉站字的名称
                                    ArriveStation = arriveStationNameWithoutStation;
                                    
                                    // 设置搜索文本，确保UI显示
                                    _isUpdatingArriveStation = true;
                                    ArriveStationSearchText = arriveStationNameWithoutStation;
                                    _isUpdatingArriveStation = false;
                                    
                                    // 确保UI更新
                                    OnPropertyChanged(nameof(ArriveStation));
                                    OnPropertyChanged(nameof(ArriveStationSearchText));
                                    
                                    // 尝试填充拼音和代码
                                    var stationInfo = _stationSearchService.GetStationInfo(arriveStationName);
                                    if (stationInfo != null)
                                    {
                                        if (!string.IsNullOrEmpty(stationInfo.StationPinyin))
                                        {
                                            ArriveStationPinyin = stationInfo.StationPinyin;
                                        }
                                        
                                        if (!string.IsNullOrEmpty(stationInfo.StationCode))
                                        {
                                            ArriveStationCode = stationInfo.StationCode;
                                        }
                                    }
                                    
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    // 纯数字车次
                    else if (Regex.IsMatch(text, @"^\d{1,4}$"))
                    {
                        SelectedTrainType = "纯数字";
                        TrainNumber = text;
                        break;
                    }
                }

                // 6. 处理出发日期、时间
                foreach (var text in allTexts)
                {
                    // 匹配日期格式 yyyy年mm月dd日
                    var dateMatch = Regex.Match(text, @"(\d{4})年(\d{1,2})月(\d{1,2})日");
                    if (dateMatch.Success)
                    {
                        int year = int.Parse(dateMatch.Groups[1].Value);
                        int month = int.Parse(dateMatch.Groups[2].Value);
                        int day = int.Parse(dateMatch.Groups[3].Value);

                        try
                        {
                            DepartDate = new DateTime(year, month, day);
                        }
                        catch
                        {
                            // 日期格式错误，使用当前日期
                        }
                    }

                    // 匹配时间格式 HH:mm
                    var timeMatch = Regex.Match(text, @"(\d{1,2}):(\d{2})");
                    if (timeMatch.Success)
                    {
                        int hour = int.Parse(timeMatch.Groups[1].Value);
                        int minute = int.Parse(timeMatch.Groups[2].Value);

                        if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60)
                        {
                            DepartHour = hour;
                            DepartMinute = minute;
                        }
                    }
                }

                // 7. 车厢号：两位数字+车
                foreach (var text in allTexts)
                {
                    var coachMatch = Regex.Match(text, @"(\d{1,2})车");
                    if (coachMatch.Success)
                    {
                        CoachNo = coachMatch.Groups[1].Value;

                        // 检测是否有"加"字
                        IsExtraCoach = text.Contains("加");
                        break;
                    }
                }

                // 8和10. 座位号和座位类型
                // 先直接从文本中查找座位类型
                bool seatTypeFound = false;
                foreach (var text in allTexts)
                {
                    // 直接查找座位类型标识
                    if (text.Equals("商务座") || text.Equals("一等座") || text.Equals("二等座") ||
                        text.Equals("新空调硬座") || text.Equals("新空调硬卧") || text.Equals("新空调软卧") ||
                        text.Equals("软座") || text.Contains("无座"))
                    {
                        LogHelper.LogInfo($"直接识别到座位类型: {text}");

                        if (text.Contains("无座"))
                        {
                            IsNoSeat = true;
                            SelectedSeatType = "新空调硬座"; // 无座通常对应硬座
                            
                            // 无座时，确保座位号和座位位置都是锁定的
                            IsSeatNoEnabled = false;
                            IsSeatPositionEnabled = false;
                            
                            // 确保UI更新
                            OnPropertyChanged(nameof(IsSeatNoEnabled));
                            OnPropertyChanged(nameof(IsSeatPositionEnabled));
                            
                            LogHelper.LogInfo("识别到无座，锁定座位号和座位位置输入框");
                        }
                        else
                        {
                            SelectedSeatType = text;

                            // 除了新空调硬座外，其余座位类型都解除位置下拉框锁定
                            if (SelectedSeatType != "新空调硬座")
                            {
                                IsSeatPositionEnabled = true;
                                LogHelper.LogInfo($"解除座位位置下拉框锁定，座位类型: {SelectedSeatType}");
                            }
                        }

                        // 更新座位位置选项
                        UpdateSeatPositions();
                        seatTypeFound = true;
                        break;
                    }
                }

                // 如果已经找到了座位类型，标记为找到
                if (seatTypeFound)
                {
                    LogHelper.LogInfo($"忠实于JSON数据，使用原始座位类型: {SelectedSeatType}");
                }

                // 先查找直接包含座位号和位置的文本，如"11车104号下"
                foreach (var text in allTexts)
                {
                    // 匹配包含"号上"、"号中"、"号下"的文本
                    var seatWithPositionMatch = Regex.Match(text, @"(\d{1,3})号([上中下])");
                    if (seatWithPositionMatch.Success)
                    {
                        LogHelper.LogInfo($"找到座位号和位置信息：{text}");
                        SeatNo = seatWithPositionMatch.Groups[1].Value;
                        string position = seatWithPositionMatch.Groups[2].Value;

                        // 只有在没有找到座位类型时才设置卧铺座位类型
                        if (!seatTypeFound)
                        {
                            // 设置卧铺座位类型
                            if (text.Contains("硬卧"))
                            {
                                SelectedSeatType = "新空调硬卧";
                            }
                            else if (text.Contains("软卧"))
                            {
                                SelectedSeatType = "新空调软卧";
                            }
                            else
                            {
                                // 默认硬卧
                                SelectedSeatType = "新空调硬卧";
                            }
                        }

                        // 设置座位位置
                        SelectedSeatPosition = position;

                        // 解除座位位置下拉框锁定
                        IsSeatPositionEnabled = true;

                        // 确保UI更新
                        OnPropertyChanged(nameof(SelectedSeatPosition));
                        OnPropertyChanged(nameof(IsSeatPositionEnabled));

                        // 更新座位位置选项
                        UpdateSeatPositions();

                        LogHelper.LogInfo($"识别到座位号：{SeatNo}，位置：{position}，类型：{SelectedSeatType}，解除位置下拉框锁定");
                        break;
                    }

                    // 匹配包含"号A"至"号F"的文本
                    var seatWithLetterMatch = Regex.Match(text, @"(\d{1,3})号([A-F])");
                    if (seatWithLetterMatch.Success)
                    {
                        LogHelper.LogInfo($"找到座位号和字母位置信息：{text}");
                        SeatNo = seatWithLetterMatch.Groups[1].Value;
                        string position = seatWithLetterMatch.Groups[2].Value;

                        // 只有在没有找到座位类型时才设置座位类型
                        if (!seatTypeFound)
                        {
                            // 设置座位类型
                            if (text.Contains("商务座"))
                            {
                                SelectedSeatType = "商务座";
                            }
                            else if (text.Contains("一等座"))
                            {
                                SelectedSeatType = "一等座";
                            }
                            else
                            {
                                SelectedSeatType = "二等座";
                            }
                        }

                        // 设置座位位置
                        SelectedSeatPosition = position;

                        // 解除座位位置下拉框锁定
                        IsSeatPositionEnabled = true;

                        // 确保UI更新
                        OnPropertyChanged(nameof(SelectedSeatPosition));
                        OnPropertyChanged(nameof(IsSeatPositionEnabled));

                        // 更新座位位置选项
                        UpdateSeatPositions();

                        LogHelper.LogInfo($"识别到座位号：{SeatNo}，位置：{position}，类型：{SelectedSeatType}，解除位置下拉框锁定");
                        break;
                    }
                }

                // 如果上面没有找到同时包含座位号和位置的文本，使用之前的逻辑继续查找
                if (string.IsNullOrEmpty(SeatNo))
                {
                    // 座位号：一到三位数字+号
                    foreach (var text in allTexts)
                    {
                        var seatMatch = Regex.Match(text, @"(\d{1,3})号");
                        if (seatMatch.Success)
                        {
                            SeatNo = seatMatch.Groups[1].Value;

                            // 只有在没有找到座位类型时才尝试设置座位类型
                            if (!seatTypeFound)
                            {
                                // 检测是否有座位类型指示
                                if (text.Contains("A") || text.Contains("B") || text.Contains("C") ||
                                    text.Contains("D") || text.Contains("F"))
                                {
                                    // 设置座位类型
                                    if (text.Contains("商务座"))
                                    {
                                        SelectedSeatType = "商务座";
                                    }
                                    else if (text.Contains("特等座"))
                                    {
                                        SelectedSeatType = "特等座";
                                    }
                                    else if (text.Contains("一等座"))
                                    {
                                        SelectedSeatType = "一等座";
                                    }
                                    else
                                    {
                                        SelectedSeatType = "二等座";
                                    }

                                    // 设置座位位置
                                    if (text.Contains("A")) SelectedSeatPosition = "A";
                                    else if (text.Contains("B")) SelectedSeatPosition = "B";
                                    else if (text.Contains("C")) SelectedSeatPosition = "C";
                                    else if (text.Contains("D")) SelectedSeatPosition = "D";
                                    else if (text.Contains("F")) SelectedSeatPosition = "F";
                                }
                                else if (text.Contains("上") || text.Contains("中") || text.Contains("下"))
                                {
                                    // 设置卧铺座位类型
                                    if (text.Contains("硬卧"))
                                    {
                                        SelectedSeatType = "新空调硬卧";
                                    }
                                    else if (text.Contains("软卧"))
                                    {
                                        SelectedSeatType = "新空调软卧";
                                    }
                                    else
                                    {
                                        // 默认硬卧
                                        SelectedSeatType = "新空调硬卧";
                                    }

                                    // 设置卧铺位置
                                    if (text.Contains("上")) SelectedSeatPosition = "上";
                                    else if (text.Contains("中")) SelectedSeatPosition = "中";
                                    else if (text.Contains("下")) SelectedSeatPosition = "下";
                                }
                            }
                            // 即使已经找到了座位类型，也可以设置座位位置
                            else
                            {
                                // 根据座位类型设置对应的位置选项
                                if (SelectedSeatType == "商务座" || SelectedSeatType == "一等座" || SelectedSeatType == "二等座")
                                {
                                    if (text.Contains("A")) SelectedSeatPosition = "A";
                                    else if (text.Contains("B")) SelectedSeatPosition = "B";
                                    else if (text.Contains("C")) SelectedSeatPosition = "C";
                                    else if (text.Contains("D")) SelectedSeatPosition = "D";
                                    else if (text.Contains("F")) SelectedSeatPosition = "F";
                                }
                                else if (SelectedSeatType == "新空调硬卧" || SelectedSeatType == "新空调软卧")
                                {
                                    if (text.Contains("上")) SelectedSeatPosition = "上";
                                    else if (text.Contains("中")) SelectedSeatPosition = "中";
                                    else if (text.Contains("下")) SelectedSeatPosition = "下";
                                }
                            }

                            break;
                        }
                    }
                }

                // 如果还没找到座位号，尝试匹配其他格式
                if (string.IsNullOrEmpty(SeatNo))
                {
                    // 尝试匹配：数字+字母 格式 (例如: 15A)
                    foreach (var text in allTexts)
                    {
                        var seatLetterMatch = Regex.Match(text, @"(\d{1,3})([A-F])");
                        if (seatLetterMatch.Success)
                        {
                            SeatNo = seatLetterMatch.Groups[1].Value;
                            string position = seatLetterMatch.Groups[2].Value;

                            // 设置座位类型为二等座（除非已经设置过）
                            if (string.IsNullOrEmpty(SelectedSeatType) && !seatTypeFound)
                            {
                                SelectedSeatType = "二等座";
                            }

                            // 设置座位位置
                            SelectedSeatPosition = position;

                            // 解除座位位置下拉框锁定
                            IsSeatPositionEnabled = true;

                            // 确保UI更新
                            OnPropertyChanged(nameof(SelectedSeatPosition));
                            OnPropertyChanged(nameof(IsSeatPositionEnabled));

                            LogHelper.LogInfo($"识别到数字+字母格式座位号: {SeatNo}{SelectedSeatPosition}，解除位置下拉框锁定");
                            break;
                        }

                        // 尝试匹配：数字+上/中/下 格式 (例如: 15上)
                        var seatPositionMatch = Regex.Match(text, @"(\d{1,3})([上中下])");
                        if (seatPositionMatch.Success)
                        {
                            SeatNo = seatPositionMatch.Groups[1].Value;
                            string position = seatPositionMatch.Groups[2].Value;

                            // 设置座位类型为硬卧（除非已经设置过）
                            if (string.IsNullOrEmpty(SelectedSeatType) && !seatTypeFound)
                            {
                                SelectedSeatType = "新空调硬卧";
                            }

                            // 设置座位位置
                            SelectedSeatPosition = position;

                            // 解除座位位置下拉框锁定
                            IsSeatPositionEnabled = true;

                            // 确保UI更新
                            OnPropertyChanged(nameof(SelectedSeatPosition));
                            OnPropertyChanged(nameof(IsSeatPositionEnabled));

                            // 更新座位位置选项，但保留当前位置
                            UpdateSeatPositions();

                            LogHelper.LogInfo($"识别到数字+上/中/下格式座位号: {SeatNo}{position}，解除位置下拉框锁定");
                            break;
                        }

                        // 尝试匹配：纯数字座位号 (例如: 15)
                        var pureNumberMatch = Regex.Match(text, @"^(\d{1,3})$");
                        if (pureNumberMatch.Success && text.Length <= 3)
                        {
                            // 检测是否可能是车号或其他编号而非座位号
                            bool isLikelySeatNumber = true;

                            // 排除可能是车厢号的情况（车厢号通常会有"车"字）
                            if (text.Contains("车") ||
                                // 排除可能是车次号的情况
                                (allTexts.Any(t => t.Contains(text + "次")) ||
                                 allTexts.Any(t => Regex.IsMatch(t, @"[GCDZTKLSY]" + text))))
                            {
                                isLikelySeatNumber = false;
                            }

                            if (isLikelySeatNumber)
                            {
                                SeatNo = pureNumberMatch.Groups[1].Value;

                                // 如果是硬座，通常没有位置标识，且未设置过座位类型
                                if (string.IsNullOrEmpty(SelectedSeatType) && !seatTypeFound)
                                {
                                    SelectedSeatType = "新空调硬座";
                                }

                                LogHelper.LogInfo($"识别到纯数字座位号: {SeatNo}，座位类型: {SelectedSeatType}");
                                break;
                            }
                        }
                    }
                }

                // 更新座位位置选项（根据已选座位类型）
                if (!string.IsNullOrEmpty(SelectedSeatType))
                {
                    UpdateSeatPositions();
                }

                // 9. 金额：去掉￥和元字的数字部分
                foreach (var text in allTexts)
                {
                    // 优先匹配带￥和元的完整金额格式
                    var fullMoneyMatch = Regex.Match(text, @"￥(\d+(\.\d{1,2})?)元");
                    if (fullMoneyMatch.Success)
                    {
                        if (decimal.TryParse(fullMoneyMatch.Groups[1].Value, out decimal moneyValue))
                        {
                            Money = moneyValue;
                            break;
                        }
                    }

                    // 其次匹配带￥的金额格式
                    var yenMoneyMatch = Regex.Match(text, @"￥(\d+(\.\d{1,2})?)");
                    if (yenMoneyMatch.Success && !text.Contains("票"))  // 排除票号
                    {
                        if (decimal.TryParse(yenMoneyMatch.Groups[1].Value, out decimal moneyValue))
                        {
                            Money = moneyValue;
                            break;
                        }
                    }

                    // 最后匹配带元的金额格式
                    var yuanMoneyMatch = Regex.Match(text, @"(\d+(\.\d{1,2})?)元");
                    if (yuanMoneyMatch.Success && !text.Contains("票"))  // 排除票号
                    {
                        if (decimal.TryParse(yuanMoneyMatch.Groups[1].Value, out decimal moneyValue))
                        {
                            Money = moneyValue;
                            break;
                        }
                    }
                }

                // 11. 附加信息
                if (combinedText.Contains("限乘当日当次车"))
                {
                    SelectedAdditionalInfo = "限乘当日当次车";
                }
                else if (combinedText.Contains("退票费"))
                {
                    SelectedAdditionalInfo = "退票费";
                }

                // 12. 车票用途
                if (combinedText.Contains("仅供报销使用"))
                {
                    SelectedTicketPurpose = "仅供报销使用";
                }

                // 13. 改签类型
                if (combinedText.Contains("始发改签"))
                {
                    SelectedTicketModificationType = "始发改签";
                }
                else if (combinedText.Contains("变更到站"))
                {
                    SelectedTicketModificationType = "变更到站";
                }

                // 14. 提示信息
                // 将所有可能的提示信息片段合并
                StringBuilder hintTextBuilder = new StringBuilder();
                List<string> hintFragments = new List<string>();

                // 首先收集所有可能是提示信息的文本片段
                foreach (var text in allTexts)
                {
                    // 排除"仅供报销使用"，它是车票用途而非提示信息
                    if (text.Contains("仅供报销使用"))
                    {
                        continue;
                    }

                    // 排除"退票费"，它是附加信息而非提示信息
                    if (text == "退票费" || text.Trim() == "退票费")
                    {
                        continue;
                    }

                    if (text.Contains("铁路") || text.Contains("旅途") ||
                        text.Contains("报销凭证") || text.Contains("凭证") ||
                        (text.Contains("退票") && text.Contains("改签")) || // 要求同时包含"退票"和"改签"
                        text.Contains("交回车站") ||
                        text.Contains("12306") || text.Contains("祝您") ||
                        text.Contains("国庆") || text.Contains("祖国") ||
                        text.Contains("遗失") || text.Contains("不补") ||
                        text.Contains("发货") || text.Contains("95306") ||
                        text.Contains("锦州银行") || text.Contains("沈阳局") ||
                        text.Contains("团体") || text.Contains("订票电话"))
                    {
                        hintFragments.Add(text);
                    }
                }

                // 尝试合并相关的提示信息片段
                string combinedHintText = string.Join("|", hintFragments);
                LogHelper.LogInfo($"合并的提示信息: {combinedHintText}");

                // 标记是否找到匹配的预设提示信息
                bool foundPredefinedHint = false;

                // 检测是否匹配预设的提示信息选项
                foreach (var hint in HintOptions)
                {
                    // 跳过自定义选项
                    if (hint == "自定义")
                        continue;

                    LogHelper.LogInfo($"检测预设提示选项: {hint}");

                    // 创建不含空格的版本用于比较
                    string hintNoSpace = hint.Replace(" ", "");
                    string combinedHintTextNoSpace = combinedHintText.Replace(" ", "");

                    // 检测组合后的文本是否包含所有预设提示的部分
                    string[] hintParts = hint.Split('|');
                    string[] hintPartsNoSpace = hintNoSpace.Split('|');
                    bool allPartsFound = true;
                    int matchedParts = 0;

                    // 如果组合文本直接包含完整的预设选项，直接匹配成功
                    if (combinedHintText.Contains(hint) || combinedHintTextNoSpace.Contains(hintNoSpace))
                    {
                        SelectedHint = hint;
                        foundPredefinedHint = true;
                        LogHelper.LogInfo($"完整匹配到预设提示信息: {hint}");
                        break;
                    }

                    // 检测"报销凭证"和"退票改签"特殊情况 - 这些关键词高度指示是预设选项
                    if (hint.Contains("报销凭证") && combinedHintText.Contains("报销") && combinedHintText.Contains("凭证")
                        && hint.Contains("退票") && hint.Contains("改签") && combinedHintText.Contains("退票") && combinedHintText.Contains("改签"))
                    {
                        SelectedHint = hint;
                        foundPredefinedHint = true;
                        LogHelper.LogInfo($"关键词匹配到预设提示信息: {hint}");
                        break;
                    }

                    for (int i = 0; i < hintParts.Length; i++)
                    {
                        string part = hintParts[i];
                        string partNoSpace = hintPartsNoSpace[i];

                        // 检测组合文本是否包含这部分（带空格和不带空格两种情况）
                        if (combinedHintText.Contains(part) || combinedHintTextNoSpace.Contains(partNoSpace))
                        {
                            matchedParts++;
                        }
                        else
                        {
                            // 检测原始OCR结果是否有任何一个文本包含这部分
                            bool partFoundInOriginal = false;
                            foreach (var text in allTexts)
                            {
                                string textNoSpace = text.Replace(" ", "");
                                if (text.Contains(part) || textNoSpace.Contains(partNoSpace))
                                {
                                    partFoundInOriginal = true;
                                    matchedParts++;
                                    break;
                                }
                            }

                            if (!partFoundInOriginal)
                            {
                                allPartsFound = false;
                                break;
                            }
                        }
                    }

                    // 如果匹配度超过70%认为是匹配的
                    if (allPartsFound || (hintParts.Length > 0 && matchedParts >= (hintParts.Length * 0.7)))
                    {
                        SelectedHint = hint;
                        foundPredefinedHint = true;
                        LogHelper.LogInfo($"部分匹配到预设提示信息: {hint}, 匹配度: {matchedParts}/{hintParts.Length}");
                        break;
                    }
                }

                // 如果没有匹配到预设的提示信息，设置为自定义并填充内容
                if (!foundPredefinedHint && hintFragments.Count > 0)
                {
                    SelectedHint = "自定义";

                    // 设置CustomHint为OCR识别出的提示信息
                    CustomHint = combinedHintText;
                    LogHelper.LogInfo($"设置为自定义提示信息: {CustomHint}");

                    // 显示自定义提示对话框，并传入已识别的提示内容
                    OpenCustomHintDialog();
                }

                // 展开表单
                IsTicketFormExpanded = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理OCR结果并填充表单时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"处理OCR结果并填充表单时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开自定义提示对话框
        /// </summary>
        private void OpenCustomHintDialog()
        {
            try
            {
                LogHelper.LogInfo("打开自定义提示对话框");

                // 创建自定义提示对话框
                var dialog = new Views.InputDialog("请输入你想展示在车票上的提示信息（车票上的虚线框区域）");

                // 如果已有自定义内容，则预填充
                if (!string.IsNullOrEmpty(CustomHint))
                {
                    dialog.ResponseText = CustomHint;
                }

                if (dialog.ShowDialog() == true)
                {
                    CustomHint = dialog.ResponseText;

                    // 确保自定义选项已选择
                    SelectedHint = "自定义";

                    LogHelper.LogInfo($"自定义提示信息已设置: {CustomHint}");
                }
                else
                {
                    // 用户取消，如果没有自定义内容，恢复选择
                    if (string.IsNullOrEmpty(CustomHint) && SelectedHint == "自定义")
                    {
                        SelectedHint = HintOptions.FirstOrDefault(h => h != "自定义") ?? string.Empty;
                        LogHelper.LogInfo($"用户取消自定义，恢复为: {SelectedHint}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"显示自定义提示对话框时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"显示自定义提示对话框时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用表单字段
        /// </summary>
        private void EnableFormFields()
        {
            // 修正逻辑：仅启用问号按钮，让用户点击问号手动启用字段
            IsQuestionButtonEnabled = true; // 只启用这个

            // 清除之前的状态（如果需要的话，或者依赖ResetFormState）
            // 以下字段保持禁用状态，直到用户点击问号
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
            IsNoSeatEnabled = false; // 无座复选框本身的状态
            IsSeatPositionEnabled = false;
            IsSeatTypeEnabled = false;
            IsAdditionalInfoEnabled = false;
            IsTicketPurposeEnabled = false;
            IsHintEnabled = false;
            IsCustomHintEnabled = false;
            IsTicketModificationTypeEnabled = false;
            IsTicketTypeEnabled = false;
            IsPaymentMethodEnabled = false;

            // 触发所有相关属性的通知，确保UI正确反映禁用状态和问号按钮的启用状态
            OnPropertyChanged(nameof(IsQuestionButtonEnabled));
            OnPropertyChanged(nameof(IsTicketNumberEnabled));
            OnPropertyChanged(nameof(IsCheckInLocationEnabled));
            OnPropertyChanged(nameof(IsDepartStationEnabled));
            OnPropertyChanged(nameof(IsArriveStationEnabled));
            OnPropertyChanged(nameof(IsDepartStationPinyinEnabled));
            OnPropertyChanged(nameof(IsArriveStationPinyinEnabled));
            OnPropertyChanged(nameof(IsMoneyEnabled));
            OnPropertyChanged(nameof(IsDepartStationCodeEnabled));
            OnPropertyChanged(nameof(IsArriveStationCodeEnabled));
            OnPropertyChanged(nameof(IsDepartDateEnabled));
            OnPropertyChanged(nameof(IsTrainTypeEnabled));
            OnPropertyChanged(nameof(IsTrainNumberEnabled));
            OnPropertyChanged(nameof(IsDepartTimeEnabled));
            OnPropertyChanged(nameof(IsCoachNoEnabled));
            OnPropertyChanged(nameof(IsExtraCoachEnabled));
            OnPropertyChanged(nameof(IsSeatNoEnabled));
            OnPropertyChanged(nameof(IsNoSeatEnabled));
            OnPropertyChanged(nameof(IsSeatPositionEnabled));
            OnPropertyChanged(nameof(IsSeatTypeEnabled));
            OnPropertyChanged(nameof(IsAdditionalInfoEnabled));
            OnPropertyChanged(nameof(IsTicketPurposeEnabled));
            OnPropertyChanged(nameof(IsHintEnabled));
            OnPropertyChanged(nameof(IsCustomHintEnabled));
            OnPropertyChanged(nameof(IsTicketModificationTypeEnabled));
            OnPropertyChanged(nameof(IsTicketTypeEnabled));
            OnPropertyChanged(nameof(IsPaymentMethodEnabled));
            // 确保与座位相关的UI状态正确
            OnPropertyChanged(nameof(IsSeatInputEnabled));
        }

        #endregion

        /// <summary>
        /// 重置表单状态
        /// </summary>
        private void ResetFormState()
        {
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
            _selectedSeatPosition = string.Empty;
            _selectedSeatType = SeatTypes.FirstOrDefault() ?? "新空调硬座";
            _departStationSearchText = string.Empty;
            _arriveStationSearchText = string.Empty;

            // 初始化附加信息字符串属性
            _selectedAdditionalInfo = string.Empty;
            _selectedTicketPurpose = string.Empty;
            _selectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
            _customHint = string.Empty;
            _selectedTicketModificationType = null;

            // 重置数值属性
            _averageConfidence = 0;
            _departHour = 0;
            _departMinute = 0;
            _money = 0;
            _departDate = DateTime.Today;

            // 重置布尔属性
            _isNoSeat = false;
            _isExtraCoach = false;
            _isStudentTicket = false;
            _isDiscountTicket = false;
            _isOnlineTicket = false;
            _isChildTicket = false;
            _isAlipayPayment = false;
            _isWeChatPayment = false;
            _isABCPayment = false;
            _isCCBPayment = false;
            _isICBCPayment = false;
            _isPSBCPayment = false; // 添加新增的银行
            _isBOCPayment = false; // 添加新增的银行
            _isCMBPayment = false; // 添加新增的银行
            _isCOMMPayment = false; // 添加新增的银行

            // 重置表单项启用状态 (直接在这里设置，不再调用 ResetFormFieldsState)
            _isQuestionButtonEnabled = false; // OCR前问号按钮禁用
            _isTicketNumberEnabled = false;
            _isCheckInLocationEnabled = false;
            _isDepartStationEnabled = false;
            _isArriveStationEnabled = false;
            _isDepartStationPinyinEnabled = false;
            _isArriveStationPinyinEnabled = false;
            _isMoneyEnabled = false;
            _isDepartStationCodeEnabled = false;
            _isArriveStationCodeEnabled = false;
            _isDepartDateEnabled = false;
            _isTrainTypeEnabled = false;
            _isTrainNumberEnabled = false;
            _isDepartTimeEnabled = false;
            _isCoachNoEnabled = false;
            _isExtraCoachEnabled = false;
            _isSeatNoEnabled = false;
            _isNoSeatEnabled = false; // 无座复选框本身
            _isSeatPositionEnabled = false;
            _isSeatTypeEnabled = false;
            _isAdditionalInfoEnabled = false;
            _isTicketPurposeEnabled = false;
            _isHintEnabled = false;
            _isCustomHintEnabled = false;
            _isTicketModificationTypeEnabled = false;
            _isTicketTypeEnabled = false;
            _isPaymentMethodEnabled = false;

            // 触发所有相关属性的通知
            OnPropertyChanged(nameof(IsQuestionButtonEnabled));
            // ... (为所有 Is...Enabled 属性调用 OnPropertyChanged) ...
            OnPropertyChanged(nameof(IsPaymentMethodEnabled));
            OnPropertyChanged(nameof(IsSeatInputEnabled)); // 确保相关UI更新

            // 根据默认座位类型更新座位位置选项
            UpdateSeatPositions();

             // 通知所有值属性更改
            OnPropertyChanged(nameof(TicketNumber));
            OnPropertyChanged(nameof(CheckInLocation));
            OnPropertyChanged(nameof(DepartStation));
            OnPropertyChanged(nameof(ArriveStation));
            OnPropertyChanged(nameof(DepartStationPinyin));
            OnPropertyChanged(nameof(ArriveStationPinyin));
            OnPropertyChanged(nameof(Money));
            OnPropertyChanged(nameof(DepartStationCode));
            OnPropertyChanged(nameof(ArriveStationCode));
            OnPropertyChanged(nameof(DepartDate));
            OnPropertyChanged(nameof(SelectedTrainType));
            OnPropertyChanged(nameof(TrainNumber));
            OnPropertyChanged(nameof(DepartHour));
            OnPropertyChanged(nameof(DepartMinute));
            OnPropertyChanged(nameof(CoachNo));
            OnPropertyChanged(nameof(IsExtraCoach));
            OnPropertyChanged(nameof(SeatNo));
            OnPropertyChanged(nameof(IsNoSeat));
            OnPropertyChanged(nameof(SelectedSeatPosition));
            OnPropertyChanged(nameof(SelectedSeatType));
            OnPropertyChanged(nameof(SelectedAdditionalInfo));
            OnPropertyChanged(nameof(SelectedTicketPurpose));
            OnPropertyChanged(nameof(SelectedHint));
            OnPropertyChanged(nameof(CustomHint));
            OnPropertyChanged(nameof(SelectedTicketModificationType));
            OnPropertyChanged(nameof(IsStudentTicket));
            OnPropertyChanged(nameof(IsDiscountTicket));
            OnPropertyChanged(nameof(IsOnlineTicket));
            OnPropertyChanged(nameof(IsChildTicket));
            OnPropertyChanged(nameof(IsAlipayPayment));
            OnPropertyChanged(nameof(IsWeChatPayment));
            OnPropertyChanged(nameof(IsABCPayment));
            OnPropertyChanged(nameof(IsCCBPayment));
            OnPropertyChanged(nameof(IsICBCPayment));
            OnPropertyChanged(nameof(IsPSBCPayment));
            OnPropertyChanged(nameof(IsBOCPayment));
            OnPropertyChanged(nameof(IsCMBPayment));
            OnPropertyChanged(nameof(IsCOMMPayment));
            OnPropertyChanged(nameof(DepartStationSearchText));
            OnPropertyChanged(nameof(ArriveStationSearchText));

        }

        /// <summary>
        /// 设置状态消息
        /// </summary>
        /// <param name="message">状态消息</param>
        private void SetStatusMessage(string message)
        {
            _ocrEnvironmentService.SetStatusMessage(message);
        }

        /// <summary>
        /// 设置加载消息
        /// </summary>
        /// <param name="message">加载消息</param>
        private void SetLoadingMessage(string message)
        {
            _ocrEnvironmentService.SetLoadingMessage(message);
        }

        /// <summary>
        /// 析构函数，确保取消事件订阅
        /// </summary>
        ~OcrTicketViewModel()
        {
            // 取消订阅事件
            if (_ocrEnvironmentService != null)
            {
                _ocrEnvironmentService.EnvironmentCheckCompleted -= OnEnvironmentCheckCompleted;
            }
        }
        
        // 事件处理方法，避免在lambda表达式中使用事件订阅，以便取消订阅
        private void OnEnvironmentCheckCompleted(object sender, bool isReady)
        {
            // 在UI线程上执行，确保界面及时更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 当环境准备就绪时，刷新命令状态
                if (isReady)
                {
                    LogHelper.LogInfo("OCR环境检测完成，已启用导入按钮");
                }
                
                // 无论环境是否准备就绪，都刷新命令状态，以便在UI上正确反映
                CommandManager.InvalidateRequerySuggested();
            });
        }

        /// <summary>
        /// 设置窗口已关闭状态
        /// </summary>
        public void SetWindowClosed()
        {
            _ocrEnvironmentService.SetWindowClosed();
        }

        // 添加互斥逻辑方法
        private void HandlePaymentChannelMutualExclusion(string selectedChannel)
        {
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

        // --- 新增或修改的辅助方法 ---

        /// <summary>
        /// 选择车站（出发或到达）
        /// </summary>
        private void SelectStation(StationInfo station, bool isDepartStation)
        {
            try
            {
                if (station != null)
                {
                    SetStationProperties(station, isDepartStation); // 使用新的设置方法

                    // 关闭下拉列表
                    if (isDepartStation)
                    {
                        IsDepartStationDropdownOpen = false;
                    }
                    else
                    {
                        IsArriveStationDropdownOpen = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"选择{(isDepartStation ? "出发" : "到达")}站时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置站点相关属性（重载用于 StationInfo 对象）
        /// </summary>
        private void SetStationProperties(StationInfo station, bool isDepartStation)
        {
            if (station == null) return;

            string stationName = station.StationName;
            string stationPinyin = station.StationPinyin;
            string stationCode = station.StationCode;

            SetStationPropertiesInternal(stationName, stationPinyin, stationCode, isDepartStation);
        }

        /// <summary>
        /// 设置站点相关属性（重载用于从 OCR 识别的站名字符串）
        /// </summary>
        private void SetStationProperties(string ocrStationName, bool isDepartStation)
        {
             if (string.IsNullOrWhiteSpace(ocrStationName)) return;

            // 尝试从数据库获取完整信息
            var stationInfo = _stationSearchService.GetStationInfo(ocrStationName);
            string stationName = ocrStationName; // 默认使用OCR的原始名称
            string stationPinyin = string.Empty;
            string stationCode = string.Empty;

            if (stationInfo != null)
            {
                LogHelper.LogInfo($"{(isDepartStation ? "出发" : "到达")}站在数据库中找到: {stationInfo.StationName}");
                stationName = stationInfo.StationName; // 使用数据库中的规范名称
                stationPinyin = stationInfo.StationPinyin;
                stationCode = stationInfo.StationCode;
            }
            else
            {
                 // 如果带"站"字没找到，尝试去掉"站"字再找一次
                 if (ocrStationName.EndsWith("站"))
                 {
                     string nameWithoutSuffix = ocrStationName.Substring(0, ocrStationName.Length - 1);
                     stationInfo = _stationSearchService.GetStationInfo(nameWithoutSuffix);
                     if (stationInfo != null)
                     {
                         LogHelper.LogInfo($"{(isDepartStation ? "出发" : "到达")}站(去后缀)在数据库中找到: {stationInfo.StationName}");
                         stationName = stationInfo.StationName; // 使用数据库中的规范名称
                         stationPinyin = stationInfo.StationPinyin;
                         stationCode = stationInfo.StationCode;
                     }
                     else
                     {
                         LogHelper.LogInfo($"{(isDepartStation ? "出发" : "到达")}站 '{ocrStationName}' 在数据库中未找到");
                         stationName = ocrStationName; // 保持原始名称
                     }
                 } else {
                      LogHelper.LogInfo($"{(isDepartStation ? "出发" : "到达")}站 '{ocrStationName}' 在数据库中未找到");
                      stationName = ocrStationName; // 保持原始名称
                 }
            }

            SetStationPropertiesInternal(stationName, stationPinyin, stationCode, isDepartStation);
        }


        /// <summary>
        /// 内部方法，实际设置站点属性并触发通知
        /// </summary>
        private void SetStationPropertiesInternal(string stationName, string stationPinyin, string stationCode, bool isDepartStation)
        {
             // 去掉站字用于显示和搜索框
            string displayName = stationName.EndsWith("站")
                ? stationName.Substring(0, stationName.Length - 1)
                : stationName;

             // 站名本身可以保留 "站" 字，也可以不保留，取决于 TrainRideInfo 的期望格式
             // 当前保存逻辑会强制加"站"，所以这里可以直接用 displayName
             // string stationValueToSet = stationName; // 或者用 displayName

            if (isDepartStation)
            {
                _isUpdatingDepartStation = true;
                DepartStation = displayName; // 设置不带站字的
                DepartStationPinyin = stationPinyin;
                DepartStationCode = stationCode;
                DepartStationSearchText = displayName;
                _isUpdatingDepartStation = false;

                // 触发通知
                OnPropertyChanged(nameof(DepartStation));
                OnPropertyChanged(nameof(DepartStationPinyin));
                OnPropertyChanged(nameof(DepartStationCode));
                OnPropertyChanged(nameof(DepartStationSearchText));
            }
            else
            {
                _isUpdatingArriveStation = true;
                ArriveStation = displayName; // 设置不带站字的
                ArriveStationPinyin = stationPinyin;
                ArriveStationCode = stationCode;
                ArriveStationSearchText = displayName;
                _isUpdatingArriveStation = false;

                // 触发通知
                OnPropertyChanged(nameof(ArriveStation));
                OnPropertyChanged(nameof(ArriveStationPinyin));
                OnPropertyChanged(nameof(ArriveStationCode));
                OnPropertyChanged(nameof(ArriveStationSearchText));
            }
        }


        /// <summary>
        /// 验证站名并更新相关信息 (使用辅助方法)
        /// </summary>
        private void ValidateStationName(string stationName, bool isDepartStation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stationName) || _stationSearchService == null)
                {
                    return;
                }

                // 当前搜索文本，这是用户实际看到和可能编辑的
                string currentSearchText = isDepartStation ? DepartStationSearchText : ArriveStationSearchText;

                // 检测是否是用户从下拉列表中选择的有效值，若是则跳过验证
                string currentValue = isDepartStation ? DepartStation : ArriveStation;
                if (stationName == currentValue && !string.IsNullOrEmpty(currentValue))
                {
                     // 如果当前值已经是有效的，且与检查的站名一致，则不需要再次验证
                     return;
                }

                // 使用StationSearchService检测是否是有效站点
                var stationInfo = _stationSearchService.GetStationInfo(stationName);

                if (stationInfo != null)
                {
                    // 更新站点信息
                    SetStationProperties(stationInfo, isDepartStation);
                }
                else
                {
                    // 展示提示时使用当前搜索框中的文本（用户看到的），而不是原始OCR结果
                    string displayText = currentSearchText;
                    
                    // 不清空站点信息，保留无效站名并显示提示
                    LogHelper.LogInfo($"站点 \"{displayText}\" 不存在于数据库中，建议在车站表中完善该站信息");
                    MessageBoxHelper.ShowWarning($"{(isDepartStation ? "出发车站" : "到达车站")}【{displayText}】在车站中心不存在，请先添加该车站信息。");

                    // 仍然设置站名，但不设置拼音和代码
                    // 使用内部设置方法，传入空代码和拼音
                    SetStationPropertiesInternal(displayText, string.Empty, string.Empty, isDepartStation);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"验证站名时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查环境状态
        /// </summary>
        private async Task CheckEnvironment()
        {
            try
            {
                // 检查环境
                await _ocrEnvironmentService.CheckEnvironment();
                
                // 添加提示：如果环境就绪但模型未安装，提示用户模型会在识别时自动下载
                if (_ocrEnvironmentService.IsEnvironmentReady && 
                    (_ocrEnvironmentService.IsOcrModelInstalled == null || 
                     _ocrEnvironmentService.IsOcrModelInstalled == false))
                {
                    // 提示用户模型会在识别时自动下载
                    MessageBoxHelper.ShowInfo("Python和CNOCR已安装，OCR模型将在首次识别时自动下载。\n\n请导入图片并点击\"开始识别\"来触发下载。", "环境检测");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("检查OCR环境时出错", ex);
                MessageBoxHelper.ShowError($"检查OCR环境时出错: {ex.Message}");
            }
        }

        private async Task DownloadPython()
        {
            try
            {
                // 显示正在下载的状态
                IsLoading = true;
                LoadingMessage = "正在准备下载Python...";
                Progress = 0;
                
                // 开始下载Python
                await _ocrEnvironmentService.DownloadPython();
            }
            catch (Exception ex)
            {
                SetStatusMessage($"下载Python时出错: {ex.Message}");
                LogHelper.LogError($"下载Python时出错：{ex.Message}");
                MessageBoxHelper.ShowError($"下载Python时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
            }
        }
        
        private bool CanDownloadPython()
        {
            // 如果已经在进行其他操作，则不允许下载
            if (IsLoading || IsDownloadingModel)
                return false;
                
            // 如果Python已经安装，则不需要下载
            if (IsPythonInstalled == true)
                return false;
                
            return true;
        }

        /// <summary>
        /// 下载安装CNOCR包
        /// </summary>
        private async Task DownloadCnocr()
        {
            try
            {
                // 检查Python是否已安装
                if (!IsPythonInstalled.HasValue || !IsPythonInstalled.Value)
                {
                    MessageBoxHelper.ShowWarning("请先安装Python再安装CNOCR");
                    return;
                }
                
                // 调用环境服务下载并安装CNOCR
                await _ocrEnvironmentService.DownloadInstallCnocr();
            }
            catch (Exception ex)
            {
                // 错误处理
                MessageBoxHelper.ShowError($"下载CNOCR时出错: {ex.Message}");
                LogHelper.LogError("下载CNOCR时出错", ex);
                SetStatusMessage($"下载CNOCR时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否可以下载CNOCR
        /// </summary>
        private bool CanDownloadCnocr()
        {
            // 如果Python未安装，不能安装CNOCR
            if (!IsPythonInstalled.HasValue || !IsPythonInstalled.Value)
                return false;
                
            // 如果已经安装了CNOCR，不需要再次安装
            if (IsCnocrInstalled.HasValue && IsCnocrInstalled.Value)
                return false;
                
            // 如果正在加载中或下载中，不能开始新的下载
            if (IsLoading || _ocrEnvironmentService.IsDownloadingPython || _ocrEnvironmentService.IsDownloadingCnocr)
                return false;
                
            return true;
        }

    }
}
