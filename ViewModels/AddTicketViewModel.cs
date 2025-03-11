using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    public class AddTicketViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly List<string> _validationErrors = new List<string>();

        // 添加字体大小变化监听
        private readonly FontSizeChangeListener _fontSizeChangeListener;

        // 用于关闭窗口的事件
        public event EventHandler CloseWindow;

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

        public AddTicketViewModel(DatabaseService databaseService)
        {
            try
            {
                _databaseService = databaseService;
                
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
                    "自定义"
                };
                
                // 初始化车站搜索相关集合
                DepartStationSuggestions = new ObservableCollection<StationInfo>();
                ArriveStationSuggestions = new ObservableCollection<StationInfo>();
                
                // 初始化字符串属性为空字符串而不是null
                _ticketNumber = string.Empty;
                _checkInLocation = string.Empty;
                _departStation = string.Empty;
                _arriveStation = string.Empty;
                _departStationPinyin = string.Empty;
                _arriveStationPinyin = string.Empty;
                _departStationCode = string.Empty;
                _arriveStationCode = string.Empty;
                _selectedTrainType = TrainTypes.FirstOrDefault() ?? string.Empty;
                _trainNumber = string.Empty;
                _coachNo = string.Empty;
                _seatNo = string.Empty;
                _selectedSeatPosition = SeatPositions.FirstOrDefault() ?? string.Empty;
                _selectedSeatType = SeatTypes.FirstOrDefault() ?? string.Empty;
                _selectedAdditionalInfo = string.Empty;
                _selectedTicketPurpose = string.Empty;
                _selectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
                _customHint = string.Empty;
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
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"初始化添加车票视图模型时出错: {ex.Message}\n\n{ex.StackTrace}");
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
                    _ticketNumber = value;
                    OnPropertyChanged(nameof(TicketNumber));
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
                    ValidateStationName(value, true);
                    // UpdateStationInfo会在文本框失去焦点时调用
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
                    ValidateStationName(value, false);
                    // UpdateStationInfo会在文本框失去焦点时调用
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
                    
                    if (value == "自定义" && string.IsNullOrEmpty(_customHint))
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
                        SearchDepartStations(searchText);
                        
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
                        SearchArriveStations(searchText);
                        
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
                // 初始化Stations集合，避免null引用
                Stations = new ObservableCollection<StationInfo>();
                
                var stationList = await _databaseService.GetStationsAsync();
                
                if (stationList != null && stationList.Count > 0)
                {
                    Stations = new ObservableCollection<StationInfo>(stationList);
                }
                else
                {
                    MessageBoxHelper.ShowWarning("未能加载车站数据，返回的列表为空");
                }
            }
            catch (MySqlException sqlEx)
            {
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载车站数据失败: {ex.Message}");
            }
        }

        private void ValidateStationName(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return;

            // 检查是否包含"站"字
            if (stationName.Contains("站"))
            {
                MessageBoxHelper.ShowInformation("系统已自动添加站字，无需手动输入");
                
                // 移除"站"字
                string cleanName = stationName.Replace("站", "");
                if (isDepartStation)
                    DepartStation = cleanName;
                else
                    ArriveStation = cleanName;
                
                return;
            }

            // 检查长度是否在1-4个汉字之间
            if (stationName.Length > 4)
            {
                MessageBoxHelper.ShowInformation("车站名称不能超过4个汉字");
                
                // 截取前4个字符
                string truncatedName = stationName.Substring(0, 4);
                if (isDepartStation)
                    DepartStation = truncatedName;
                else
                    ArriveStation = truncatedName;
            }
        }

        private void UpdateStationInfo(string stationName, bool isDepartStation)
        {
            if (string.IsNullOrWhiteSpace(stationName) || Stations == null)
                return;

            // 查找匹配的车站
            var station = Stations.FirstOrDefault(s => s.StationName == stationName);
            if (station != null)
            {
                if (isDepartStation)
                {
                    DepartStationPinyin = station.StationPinyin ?? string.Empty;
                    DepartStationCode = station.StationCode ?? string.Empty;
                }
                else
                {
                    ArriveStationPinyin = station.StationPinyin ?? string.Empty;
                    ArriveStationCode = station.StationCode ?? string.Empty;
                }
            }
            else
            {
                // 如果找不到匹配的车站，清空相关字段
                if (isDepartStation)
                {
                    DepartStationPinyin = string.Empty;
                    DepartStationCode = string.Empty;
                }
                else
                {
                    ArriveStationPinyin = string.Empty;
                    ArriveStationCode = string.Empty;
                }
                
                // 不再显示警告，避免干扰用户输入
                // MessageBoxHelper.ShowWarning($"未找到车站\"{stationName}\"的信息，请检查车站名称是否正确。", "车站信息缺失");
            }
        }

        private void UpdateSeatPositions()
        {
            // 先清空集合
            SeatPositions.Clear();
            
            switch (SelectedSeatType)
            {
                case "新空调硬座":
                    // 对于硬座，不显示下拉框
                    break;
                case "新空调硬卧":
                    SeatPositions.Add("上");
                    SeatPositions.Add("中");
                    SeatPositions.Add("下");
                    break;
                case "新空调软卧":
                    SeatPositions.Add("上");
                    SeatPositions.Add("下");
                    break;
                case "商务座":
                case "一等座":
                    SeatPositions.Add("A");
                    SeatPositions.Add("C");
                    SeatPositions.Add("D");
                    SeatPositions.Add("F");
                    break;
                case "二等座":
                case "硬卧代硬座":
                    SeatPositions.Add("A");
                    SeatPositions.Add("B");
                    SeatPositions.Add("C");
                    SeatPositions.Add("D");
                    SeatPositions.Add("F");
                    break;
                default:
                    SeatPositions.Add("A");
                    SeatPositions.Add("B");
                    SeatPositions.Add("C");
                    SeatPositions.Add("D");
                    SeatPositions.Add("F");
                    break;
            }

            // 如果有选项，默认选择第一个
            if (SeatPositions.Count > 0)
                SelectedSeatPosition = SeatPositions[0];
            else
                SelectedSeatPosition = string.Empty;
                
            // 通知UI更新SeatPositions集合的可见性
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

        private bool ValidateForm()
        {
            _validationErrors.Clear();

            // 检查必填字段
            if (string.IsNullOrWhiteSpace(TicketNumber))
                _validationErrors.Add("未填写取票号");
                
            if (string.IsNullOrWhiteSpace(CheckInLocation))
                _validationErrors.Add("未填写检票位置");
                
            if (string.IsNullOrWhiteSpace(DepartStation))
                _validationErrors.Add("未填写出发站");
                
            if (string.IsNullOrWhiteSpace(ArriveStation))
                _validationErrors.Add("未填写到达站");
                
            if (string.IsNullOrWhiteSpace(DepartStationPinyin))
                _validationErrors.Add("未填写出发站拼音");
                
            if (string.IsNullOrWhiteSpace(ArriveStationPinyin))
                _validationErrors.Add("未填写到达站拼音");
                
            // 允许金额为0（免费票）
            if (Money < 0)
                _validationErrors.Add("金额不能为负数");
                
            if (string.IsNullOrWhiteSpace(DepartStationCode))
                _validationErrors.Add("未填写出发站代码");
                
            if (string.IsNullOrWhiteSpace(ArriveStationCode))
                _validationErrors.Add("未填写到达站代码");
                
            if (string.IsNullOrWhiteSpace(SelectedTrainType))
                _validationErrors.Add("未选择车次类型");
                
            if (string.IsNullOrWhiteSpace(TrainNumber))
                _validationErrors.Add("未填写车次号");
                
            if (string.IsNullOrWhiteSpace(CoachNo))
                _validationErrors.Add("未填写车厢号");
                
            if (!IsNoSeat && string.IsNullOrWhiteSpace(SeatNo))
                _validationErrors.Add("未填写座位号");
                
            if (string.IsNullOrWhiteSpace(SelectedSeatType))
                _validationErrors.Add("未选择座位类型");

            // 验证金额格式
            if (Money > 9999.99m)
                _validationErrors.Add("金额不能超过9999.99");

            // 验证车次号格式
            if (!string.IsNullOrWhiteSpace(TrainNumber) && !Regex.IsMatch(TrainNumber, @"^\d{1,4}$"))
                _validationErrors.Add("车次号必须为1-4位数字");

            // 验证车厢号格式
            if (!string.IsNullOrWhiteSpace(CoachNo))
            {
                if (!Regex.IsMatch(CoachNo, @"^\d+$"))
                    _validationErrors.Add("车厢号必须为数字");
                
                // 车厢号只能是00~99
                if (Regex.IsMatch(CoachNo, @"^\d+$"))
                {
                    int coachNumber;
                    if (int.TryParse(CoachNo, out coachNumber))
                    {
                        if (coachNumber < 0 || coachNumber > 99)
                            _validationErrors.Add("车厢号必须在00-99之间");
                    }
                }
            }

            // 验证座位号格式
            if (!IsNoSeat && !string.IsNullOrWhiteSpace(SeatNo))
            {
                if (!Regex.IsMatch(SeatNo, @"^\d+$"))
                    _validationErrors.Add("座位号必须为数字");
                
                // 座位号不能以0开头
                if (SeatNo.StartsWith("0"))
                    _validationErrors.Add("座位号不能以0开头");
            }

            return _validationErrors.Count == 0;
        }

        private bool CanSaveTicket()
        {
            // 简单验证，详细验证在保存时进行
            return !string.IsNullOrWhiteSpace(TicketNumber) && 
                   !string.IsNullOrWhiteSpace(DepartStation) && 
                   !string.IsNullOrWhiteSpace(ArriveStation);
        }

        private async void SaveTicket()
        {
            if (!ValidateForm())
            {
                // 显示验证错误
                string errorMessage = string.Join("\n", _validationErrors);
                MessageBoxHelper.ShowWarning(errorMessage, "表单验证失败");
                return;
            }

            try
            {
                // 创建车票对象
                var ticket = new TrainRideInfo
                {
                    TicketNumber = TicketNumber,
                    CheckInLocation = CheckInLocation,
                    DepartStation = DepartStation + "站",
                    ArriveStation = ArriveStation + "站",
                    DepartStationPinyin = DepartStationPinyin,
                    ArriveStationPinyin = ArriveStationPinyin,
                    Money = Money,
                    DepartStationCode = DepartStationCode,
                    ArriveStationCode = ArriveStationCode,
                    DepartDate = DepartDate,
                    DepartTime = new TimeSpan(DepartHour, DepartMinute, 0),
                    TrainNo = SelectedTrainType == "纯数字" ? TrainNumber : $"{SelectedTrainType}{TrainNumber}",
                    SeatType = SelectedSeatType
                };

                // 处理车厢号
                if (IsExtraCoach)
                    ticket.CoachNo = $"加{CoachNo}车";
                else
                    ticket.CoachNo = $"{CoachNo}车";

                // 处理座位号
                if (IsNoSeat)
                    ticket.SeatNo = "无座";
                else if (SelectedSeatType == "新空调硬座")
                    ticket.SeatNo = SeatNo;
                else
                    ticket.SeatNo = $"{SeatNo}{SelectedSeatPosition}";

                // 处理附加信息
                ticket.AdditionalInfo = SelectedAdditionalInfo;
                
                // 处理车票用途
                ticket.TicketPurpose = SelectedTicketPurpose;
                
                // 处理提示信息
                if (SelectedHint == "自定义")
                    ticket.Hint = CustomHint;
                else
                    ticket.Hint = SelectedHint;

                // 设置超时任务
                var saveTask = _databaseService.AddTicketAsync(ticket);
                
                // 添加5秒超时
                var timeoutTask = Task.Delay(5000);
                
                // 等待任务完成或超时
                if (await Task.WhenAny(saveTask, timeoutTask) == timeoutTask)
                {
                    // 操作超时
                    MessageBoxHelper.ShowError("保存车票操作超时，请检查数据库连接");
                    return;
                }
                
                // 确保任务完成且没有异常
                await saveTask;
                
                MessageBoxHelper.ShowInformation("车票添加成功！", "成功");
                
                // 安全地关闭窗口
                try
                {
                    // 关闭窗口，并设置DialogResult为true，表示保存成功
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    if (window != null)
                    {
                        window.DialogResult = true;
                    }
                    else
                    {
                        // 如果找不到窗口，使用事件关闭
                        CloseWindow?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError("关闭添加车票窗口时出错", ex);
                    // 尝试使用事件关闭
                    CloseWindow?.Invoke(this, EventArgs.Empty);
                }
                
                // 重置表单
                ResetForm();
            }
            catch (MySqlException sqlEx)
            {
                LogHelper.LogError($"数据库错误: {sqlEx.Message}", sqlEx);
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"保存车票失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"保存车票失败: {ex.Message}");
            }
        }

        private void ResetForm()
        {
            // 重置所有字段
            TicketNumber = string.Empty;
            CheckInLocation = string.Empty;
            DepartStation = string.Empty;
            ArriveStation = string.Empty;
            DepartStationPinyin = string.Empty;
            ArriveStationPinyin = string.Empty;
            Money = 0;
            DepartStationCode = string.Empty;
            ArriveStationCode = string.Empty;
            DepartDate = DateTime.Today;
            DepartHour = 0;
            DepartMinute = 0;
            SelectedTrainType = TrainTypes.FirstOrDefault() ?? string.Empty;
            TrainNumber = string.Empty;
            CoachNo = string.Empty;
            IsExtraCoach = false;
            SeatNo = string.Empty;
            IsNoSeat = false;
            SelectedSeatPosition = SeatPositions.FirstOrDefault() ?? string.Empty;
            SelectedSeatType = SeatTypes.FirstOrDefault() ?? string.Empty;
            SelectedAdditionalInfo = string.Empty;
            SelectedTicketPurpose = string.Empty;
            SelectedHint = HintOptions.FirstOrDefault() ?? string.Empty;
            CustomHint = string.Empty;
            
            // 清空搜索文本框内容
            DepartStationSearchText = string.Empty;
            ArriveStationSearchText = string.Empty;
            
            // 关闭下拉框
            IsDepartStationDropdownOpen = false;
            IsArriveStationDropdownOpen = false;
            
            // 清空搜索结果
            DepartStationSuggestions.Clear();
            ArriveStationSuggestions.Clear();
            
            // 清空验证错误
            _validationErrors.Clear();
        }

        // 车站搜索相关方法
        private async void SearchDepartStations(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                DepartStationSuggestions.Clear();
                IsDepartStationDropdownOpen = false;
                return;
            }

            try
            {
                // 设置超时任务
                var searchTask = _databaseService.SearchStationsByNameAsync(searchText);
                var timeoutTask = Task.Delay(3000); // 3秒超时
                
                // 等待任务完成或超时
                if (await Task.WhenAny(searchTask, timeoutTask) == timeoutTask)
                {
                    // 操作超时
                    MessageBoxHelper.ShowWarning("搜索车站操作超时，请检查数据库连接", "操作超时");
                    return;
                }
                
                // 确保任务完成且没有异常
                var stations = await searchTask;
                
                DepartStationSuggestions.Clear();
                foreach (var station in stations)
                {
                    DepartStationSuggestions.Add(station);
                }
                
                IsDepartStationDropdownOpen = DepartStationSuggestions.Count > 0;
                
                // 如果没有找到匹配的车站，提示用户
                if (DepartStationSuggestions.Count == 0 && !string.IsNullOrWhiteSpace(searchText))
                {
                    string warningMessage = $"未找到名称包含\"{searchText}\"的车站，请检查输入是否正确或考虑添加新车站。";
                    MessageBoxHelper.ShowWarning(warningMessage, "车站不存在");
                }
            }
            catch (MySqlException sqlEx)
            {
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"搜索车站时出错: {ex.Message}");
            }
        }

        private async void SearchArriveStations(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ArriveStationSuggestions.Clear();
                IsArriveStationDropdownOpen = false;
                return;
            }

            try
            {
                // 设置超时任务
                var searchTask = _databaseService.SearchStationsByNameAsync(searchText);
                var timeoutTask = Task.Delay(3000); // 3秒超时
                
                // 等待任务完成或超时
                if (await Task.WhenAny(searchTask, timeoutTask) == timeoutTask)
                {
                    // 操作超时
                    MessageBoxHelper.ShowWarning("搜索车站操作超时，请检查数据库连接", "操作超时");
                    return;
                }
                
                // 确保任务完成且没有异常
                var stations = await searchTask;
                
                ArriveStationSuggestions.Clear();
                foreach (var station in stations)
                {
                    ArriveStationSuggestions.Add(station);
                }
                
                IsArriveStationDropdownOpen = ArriveStationSuggestions.Count > 0;
                
                // 如果没有找到匹配的车站，提示用户
                if (ArriveStationSuggestions.Count == 0 && !string.IsNullOrWhiteSpace(searchText))
                {
                    string warningMessage = $"未找到名称包含\"{searchText}\"的车站，请检查输入是否正确或考虑添加新车站。";
                    MessageBoxHelper.ShowWarning(warningMessage, "车站不存在");
                }
            }
            catch (MySqlException sqlEx)
            {
                MessageBoxHelper.ShowError($"数据库错误: {sqlEx.Message}\n错误代码: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"搜索车站时出错: {ex.Message}");
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
            
            DepartStationPinyin = station.StationPinyin ?? string.Empty;
            DepartStationCode = station.StationCode ?? string.Empty;
            
            // 检查是否需要提示用户完善车站信息
            CheckStationInfoCompleteness(station, true);
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
            
            ArriveStationPinyin = station.StationPinyin ?? string.Empty;
            ArriveStationCode = station.StationCode ?? string.Empty;
            
            // 检查是否需要提示用户完善车站信息
            CheckStationInfoCompleteness(station, false);
        }

        // 检查车站信息是否完整
        private void CheckStationInfoCompleteness(StationInfo station, bool isDepartStation)
        {
            if (station == null)
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
                LogHelper.LogError("字体大小变化处理出错", ex);
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
                LogHelper.LogError("初始化字体大小设置出错", ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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