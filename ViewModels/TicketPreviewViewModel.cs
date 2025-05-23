using Microsoft.Win32;
using QRCoder;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
// 使用别名避免命名冲突
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TA_WPF.Models;
using TA_WPF.Views;
using Bitmap = System.Drawing.Bitmap;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using System.Text.RegularExpressions;
using System.Globalization;
using TA_WPF.Utils;
using System.Diagnostics;

namespace TA_WPF.ViewModels
{
    public class TicketPreviewViewModel : BaseViewModel
    {
        private TrainRideInfo _selectedTicket;
        private string _identityInfo;
        private string _encodingArea;
        private BitmapImage _qrCodeImage;
        private bool _isDarkMode;
        private double _fontSize;
        private bool _isRedTicket;
        // 红色车票布局参数
        private Dictionary<string, object> _redTicketLayout;
        // 蓝色车票布局参数
        private Dictionary<string, object> _blueTicketLayout;
        // 当前使用的布局参数
        private Dictionary<string, object> _currentLayout;
        private bool _showBoxedTicketType;
        // 支付渠道显示属性
        private bool _showAlipayPayment;
        private bool _showWeChatPayment;
        private bool _showABCPayment;
        private bool _showCCBPayment;
        private bool _showICBCPayment;
        private bool _showCMBPayment;
        private bool _showPSBCPayment;
        private bool _showBOCPayment;
        private bool _showCOMMPayment;
        private bool _isValidatingIdCard = false; // 添加验证状态标记
        // 添加获取当前窗口的属性
        private Window _ownerWindow;
        private bool _isIdCardValid = true; // 跟踪身份证验证状态
        private bool _isNewInputSession = true; // 标记是否是新的输入会话

        // 设计时构造函数
        public TicketPreviewViewModel() : this(new TrainRideInfo
        {
            TicketNumber = "A123456789",
            CheckInLocation = "检票位置5",
            DepartStation = "出发车站",
            DepartStationPinyin = "Chufachezhan",
            TrainNo = "K12",
            ArriveStation = "到达车车车站",
            ArriveStationPinyin = "Daodachezhan",
            DepartDate = new DateTime(9999, 12, 31),
            DepartTime = new TimeSpan(23, 59, 59),
            CoachNo = "99",
            SeatNo = "999",
            Money = 9999.99M,  // 确保非空
            SeatType = "上等座",
            AdditionalInfo = "限乘当日当次车",
            TicketPurpose = "仅供报销使用",
            Hint = "这是一条提示信息|这也是一条提示信息",
            TicketModificationType = "始发改签",
            TicketTypeFlags = (int)TicketTypeFlags.ChildTicket, // 设计时默认为儿童票
            PaymentChannelFlags = (int)PaymentChannelFlags.Alipay | (int)PaymentChannelFlags.WeChat | (int)PaymentChannelFlags.ABC | (int)PaymentChannelFlags.CCB | (int)PaymentChannelFlags.ICBC
        })
        {
            // 为设计视图添加默认的身份信息和编码区内容
            _identityInfo = "01234567890123456";
            _encodingArea = "01234567890123456789abc JM";
        }

        public TicketPreviewViewModel(TrainRideInfo selectedTicket)
        {
            _selectedTicket = selectedTicket;
            CloseCommand = new RelayCommand(Close);
            ExportImageCommand = new RelayCommand(ExportImage);
            ToggleTicketColorCommand = new RelayCommand(ToggleTicketColor);

            // 获取当前主题模式
            LoadThemeSettings();

            // 获取当前字体大小设置
            LoadFontSizeSettings();

            // 订阅主题服务的主题变更事件
            Services.ThemeService.Instance.ThemeChanged += (sender, isDark) =>
            {
                IsDarkMode = isDark;
            };

            // 初始化蓝色和红色车票布局
            InitializeTicketLayouts();

            // 默认使用蓝色车票和不带框模式
            _isRedTicket = false;
            _showBoxedTicketType = false;
            _currentLayout = _blueTicketLayout;

            // 根据票种信息初始化显示选项
            InitializeTicketTypeOptions();
        }

        // 加载主题设置
        private void LoadThemeSettings()
        {
            try
            {
                // 从应用程序资源中获取当前主题设置
                if (Application.Current?.Resources != null &&
                    Application.Current.Resources.Contains("Theme.Dark"))
                {
                    _isDarkMode = (bool)Application.Current.Resources["Theme.Dark"];
                }
                else
                {
                    // 如果资源中没有，则从ThemeService获取
                    _isDarkMode = Services.ThemeService.Instance.IsDarkThemeActive();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载主题设置时出错: {ex.Message}");
                _isDarkMode = false; // 默认浅色主题
            }
        }

        // 加载字体大小设置
        private void LoadFontSizeSettings()
        {
            try
            {
                // 从应用程序资源中获取当前字体大小设置
                if (Application.Current?.Resources != null &&
                    Application.Current.Resources.Contains("MaterialDesignFontSize"))
                {
                    _fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];
                }
                else
                {
                    // 如果资源中没有，则从配置文件获取
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["FontSize"] != null)
                    {
                        if (double.TryParse(config.AppSettings.Settings["FontSize"].Value, out double fontSize))
                        {
                            _fontSize = fontSize;
                        }
                        else
                        {
                            _fontSize = 13; // 默认字体大小
                        }
                    }
                    else
                    {
                        _fontSize = 13; // 默认字体大小
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载字体大小设置时出错: {ex.Message}");
                _fontSize = 13; // 默认字体大小
            }
        }

        // 是否为深色模式
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    OnPropertyChanged(nameof(FormBackgroundBrush));
                    OnPropertyChanged(nameof(FormForegroundBrush));
                    OnPropertyChanged(nameof(FormBorderBrush));
                    OnPropertyChanged(nameof(FormInputBackgroundBrush));
                    OnPropertyChanged(nameof(TicketBackgroundSource));
                }
            }
        }

        // 当前字体大小
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (Math.Abs(_fontSize - value) > 0.01)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                    OnPropertyChanged(nameof(SmallFontSize));
                    OnPropertyChanged(nameof(MediumFontSize));
                    OnPropertyChanged(nameof(LargeFontSize));
                    OnPropertyChanged(nameof(HeaderFontSize));
                }
            }
        }

        // 小字体大小（用于标签等）
        public double SmallFontSize => _fontSize - 1;

        // 中等字体大小（用于普通文本）
        public double MediumFontSize => _fontSize;

        // 大字体大小（用于标题等）
        public double LargeFontSize => _fontSize + 2;

        // 表单标题字体大小
        public double HeaderFontSize => _fontSize + 4;

        // 表单背景色刷
        public SolidColorBrush FormBackgroundBrush
        {
            get
            {
                return _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
                    : new SolidColorBrush(Colors.White);
            }
        }

        // 表单前景色刷
        public SolidColorBrush FormForegroundBrush
        {
            get
            {
                return _isDarkMode
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        // 表单边框色刷
        public SolidColorBrush FormBorderBrush
        {
            get
            {
                return _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
                    : new SolidColorBrush(Color.FromRgb(220, 220, 220));
            }
        }

        // 表单输入框背景色刷
        public SolidColorBrush FormInputBackgroundBrush
        {
            get
            {
                return _isDarkMode
                    ? new SolidColorBrush(Color.FromRgb(60, 60, 60))
                    : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
        }

        public TrainRideInfo SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                if (_selectedTicket != value)
                {
                    _selectedTicket = value;
                    OnPropertyChanged(nameof(SelectedTicket));

                    // 根据车票的票种信息初始化票种显示选项
                    InitializeTicketTypeOptions();
                }
            }
        }

        // 添加获取当前窗口的属性
        public Window OwnerWindow
        {
            get => _ownerWindow;
            set => _ownerWindow = value;
        }

        // 身份证验证状态属性
        public bool IsIdCardValid
        {
            get => _isIdCardValid;
            private set
            {
                if (_isIdCardValid != value)
                {
                    _isIdCardValid = value;
                    OnPropertyChanged(nameof(IsIdCardValid));
                }
            }
        }

        public string IdentityInfo
        {
            get => _identityInfo;
            set
            {
                // 如果新值与旧值相同，则不处理
                if (_identityInfo == value)
                    return;
                
                // 如果是空值，表示开始新的输入会话
                if (string.IsNullOrEmpty(value))
                {
                    _isNewInputSession = true;
                    IsIdCardValid = true;
                }
                
                // 处理验证失败后的重新输入
                if (!_isIdCardValid && !_isNewInputSession)
                {
                    // 当验证状态为失败且不是新的输入会话时，阻止输入
                    return;
                }
                
                _identityInfo = value;
                _isNewInputSession = false; // 标记不再是新的输入会话
                
                // 验证身份证格式，但避免无限循环
                if (!_isValidatingIdCard)
                {
                    ValidateIdCard(value);
                }
                
                OnPropertyChanged(nameof(IdentityInfo));
            }
        }

        // 验证身份证号码
        private void ValidateIdCard(string id)
        {
            // 防止重入验证
            if (_isValidatingIdCard)
            {
                return;
            }

            try
            {
                _isValidatingIdCard = true;
                
                // 跳过空字符串和长度不足的身份证号（用户可能正在输入）
                if (string.IsNullOrWhiteSpace(id) || id.Length != 18)
                {
                    return;
                }
                
                // 验证基本格式：18位，前17位为数字，最后一位为数字或X/x
                string pattern = @"^\d{17}[\dXx]$";
                if (!Regex.IsMatch(id, pattern))
                {
                    // 使用当前窗口作为对话框的Owner
                    MessageBoxHelper.ShowWarning("身份证号格式错误，应为18位！", "格式错误", _ownerWindow);
                    
                    // 标记身份证验证失败
                    IsIdCardValid = false;
                    
                    // 验证失败时设置一个空值
                    _identityInfo = string.Empty;
                    _isNewInputSession = true; // 重置为新的输入会话
                    
                    // 确保属性通知生效
                    Application.Current.Dispatcher.InvokeAsync(() => {
                        OnPropertyChanged(nameof(IdentityInfo));
                        // 给界面一点时间处理属性变更，然后重置验证状态
                        Task.Delay(50).ContinueWith(_ => {
                            Application.Current.Dispatcher.InvokeAsync(() => {
                                IsIdCardValid = true;
                            });
                        });
                    });
                    
                    return;
                }

                // 验证出生日期部分
                string birthPart = id.Substring(6, 8);
                string birthDate = birthPart.Insert(6, "-").Insert(4, "-");
                if (!DateTime.TryParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    MessageBoxHelper.ShowWarning("身份证中的出生日期无效！", "格式错误", _ownerWindow);
                    
                    // 标记身份证验证失败
                    IsIdCardValid = false;
                    
                    // 验证失败时设置一个空值
                    _identityInfo = string.Empty;
                    _isNewInputSession = true; // 重置为新的输入会话
                    
                    // 确保属性通知生效
                    Application.Current.Dispatcher.InvokeAsync(() => {
                        OnPropertyChanged(nameof(IdentityInfo));
                        // 给界面一点时间处理属性变更，然后重置验证状态
                        Task.Delay(50).ContinueWith(_ => {
                            Application.Current.Dispatcher.InvokeAsync(() => {
                                IsIdCardValid = true;
                            });
                        });
                    });
                    
                    return;
                }

                // 验证校验码
                int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
                string[] checkCodes = { "1", "0", "X", "9", "8", "7", "6", "5", "4", "3", "2" };
                int sum = 0;

                for (int i = 0; i < 17; i++)
                    sum += weights[i] * (id[i] - '0');

                char actualCode = char.ToUpper(id[17]); // 处理小写x
                string expectedCode = checkCodes[sum % 11];

                if (actualCode.ToString() != expectedCode)
                {
                    MessageBoxHelper.ShowWarning("身份证号最后一位校验码错误！", "格式错误", _ownerWindow);
                    
                    // 标记身份证验证失败
                    IsIdCardValid = false;
                    
                    // 验证失败时设置一个空值
                    _identityInfo = string.Empty;
                    _isNewInputSession = true; // 重置为新的输入会话
                    
                    // 确保属性通知生效
                    Application.Current.Dispatcher.InvokeAsync(() => {
                        OnPropertyChanged(nameof(IdentityInfo));
                        // 给界面一点时间处理属性变更，然后重置验证状态
                        Task.Delay(50).ContinueWith(_ => {
                            Application.Current.Dispatcher.InvokeAsync(() => {
                                IsIdCardValid = true;
                            });
                        });
                    });
                    
                    return;
                }
                
                // 所有验证都通过
                IsIdCardValid = true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"身份证验证过程中出现错误：{ex.Message}", "验证错误", _ownerWindow);
                
                // 标记身份证验证失败
                IsIdCardValid = false;
                
                // 发生异常时设置一个空值
                _identityInfo = string.Empty;
                _isNewInputSession = true; // 重置为新的输入会话
                
                // 确保属性通知生效
                Application.Current.Dispatcher.InvokeAsync(() => {
                    OnPropertyChanged(nameof(IdentityInfo));
                    // 给界面一点时间处理属性变更，然后重置验证状态
                    Task.Delay(50).ContinueWith(_ => {
                        Application.Current.Dispatcher.InvokeAsync(() => {
                            IsIdCardValid = true;
                        });
                    });
                });
            }
            finally
            {
                // 确保验证状态标记被重置
                _isValidatingIdCard = false;
            }
        }

        public string EncodingArea
        {
            get => _encodingArea;
            set
            {
                if (_encodingArea != value)
                {
                    _encodingArea = value;
                    OnPropertyChanged(nameof(EncodingArea));
                    // 当编码区内容变化时，更新二维码
                    GenerateQRCode();
                }
            }
        }

        // 二维码图像
        public BitmapImage QrCodeImage
        {
            get => _qrCodeImage;
            private set
            {
                if (_qrCodeImage != value)
                {
                    _qrCodeImage = value;
                    OnPropertyChanged(nameof(QrCodeImage));
                }
            }
        }

        // 格式化车站名（去掉"站"字）
        public string DepartStationName => _selectedTicket?.DepartStation?.Replace("站", "") ?? string.Empty;

        public string ArriveStationName => _selectedTicket?.ArriveStation?.Replace("站", "") ?? string.Empty;

        // 处理出发车站名的字间距展示
        public string DepartStationWithSpacing
        {
            get
            {
                var name = DepartStationName;
                if (string.IsNullOrEmpty(name)) return string.Empty;
                return FormatStationNameWithSpacing(name);
            }
        }

        // 处理到达车站名的字间距展示
        public string ArriveStationWithSpacing
        {
            get
            {
                var name = ArriveStationName;
                if (string.IsNullOrEmpty(name)) return string.Empty;
                return FormatStationNameWithSpacing(name);
            }
        }

        // 提取年份
        public string DepartYear
        {
            get
            {
                if (_selectedTicket?.DepartDate == null)
                    return string.Empty;

                return _selectedTicket.DepartDate.Value.Year.ToString();
            }
        }

        // 提取月份
        public string DepartMonth
        {
            get
            {
                if (_selectedTicket?.DepartDate == null)
                    return string.Empty;

                return _selectedTicket.DepartDate.Value.Month.ToString();
            }
        }

        // 提取日期
        public string DepartDay
        {
            get
            {
                if (_selectedTicket?.DepartDate == null)
                    return string.Empty;

                return _selectedTicket.DepartDate.Value.Day.ToString();
            }
        }

        // 获取车厢号（不含"车"字和"加"字）
        public string CoachNumber
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.CoachNo))
                    return string.Empty;

                // 移除"车"字和"加"字
                return _selectedTicket.CoachNo.Replace("车", "").Replace("加", "");
            }
        }

        // 获取金额的数值部分（不含¥符号）
        public string MoneyValue
        {
            get
            {
                // 如果是空值，返回空字符串
                if (_selectedTicket?.Money == null)
                    return string.Empty;

                try
                {
                    // 使用不变文化进行格式化，确保在设计时和运行时都能正确格式化
                    // 改为1位小数，采用四舍五入
                    return _selectedTicket.Money.Value.ToString("N1", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    // 发生异常时返回设计时的默认值
                    return "9999.9";
                }
            }
        }

        // 根据字数添加适当的字间距
        private string FormatStationNameWithSpacing(string stationName)
        {
            int length = stationName.Length;
            if (length <= 1) return stationName;

            // 调试输出，查看处理前的站名和长度
            Debug.WriteLine($"原始站名: '{stationName}', 长度: {length}");

            string result;
            if (length == 2)
            {
                // 直接返回带有适当间距的双字站名
                result = stationName[0] + "\u2002\u2002" + stationName[1]; // 两个半角空格
            }
            else if (length == 3)
            {
                // 三字站名，较小间距
                result = stationName[0] + "\u2002" + stationName[1] + "\u2002" + stationName[2]; // 一个半角空格
            }
            else if (length == 4)
            {
                // 四字站名，小间距
                result = stationName[0] + "\u2004" + stationName[1] + "\u2004" + stationName[2] + "\u2004" + stationName[3]; // 三分之一em空格
            }
            else if (length == 5)
            {
                // 五字站名，最小间距 - 使用更小的间距
                result = stationName[0] + "\u200A" + stationName[1] + "\u200A" + stationName[2] + "\u200A" + stationName[3] + "\u200A" + stationName[4]; // 极细空格
            }
            else
            {
                // 其他情况原样返回
                result = stationName;
            }

            // 调试输出，查看处理后的结果
            Debug.WriteLine($"处理后的结果: '{result}'");
            return result;
        }

        // 格式化时间（HH:mm格式）
        public string DepartTimeFormatted
        {
            get
            {
                if (_selectedTicket?.DepartTime == null)
                    return string.Empty;

                try
                {
                    // 确保TimeSpan正确格式化为HH:mm
                    var time = _selectedTicket.DepartTime.Value;
                    return $"{time.Hours:D2}:{time.Minutes:D2}";
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        // 出发车站拼音的水平位置
        public double DepartStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.DepartStationPinyin))
                    return 83;

                // 根据出发车站名称的长度计算拼音的居中位置
                var name = DepartStationName;
                if (string.IsNullOrEmpty(name)) return 83;

                switch (name.Length)
                {
                    case 1: return 66;   // 单字站名，使拼音居中
                    case 2: return 83;   // 双字站名
                    case 3: return 83;   // 三字站名
                    case 4: return 83;   // 四字站名
                    case 5: return 83;   // 五字站名
                    default: return 83;  // 其他情况
                }
            }
        }

        // 到达车站拼音的水平位置
        public double ArriveStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.ArriveStationPinyin))
                    return 540;

                // 根据到达车站名称的长度计算拼音的居中位置
                var name = ArriveStationName;
                if (string.IsNullOrEmpty(name)) return 540;

                switch (name.Length)
                {
                    case 1: return 556;  // 单字站名，使拼音居中
                    case 2: return 540;  // 双字站名
                    case 3: return 540;  // 三字站名
                    case 4: return 540;  // 四字站名
                    case 5: return 540;  // 五字站名
                    default: return 540; // 其他情况
                }
            }
        }

        // 判断座位号是否为"无座"
        public bool HasNoSeat => _selectedTicket?.SeatNo == "无座";

        // 判断车厢号是否包含"加"字
        public bool HasAddedCoach => _selectedTicket?.CoachNo?.Contains("加") ?? false;

        // 座位类型的位置信息
        public double SeatTypePosition
        {
            get
            {
                // 无座情况下座位类型位置更靠左
                if (HasNoSeat)
                {
                    return 580;  // 无座时靠左显示
                }
                else
                {
                    return 640;  // 有座时原位置显示
                }
            }
        }

        // 计算车次号居中位置，用于箭头定位
        public double TrainNumberCenterPosition
        {
            get
            {
                // 获取出发车站"站"字位置
                double departStationEndPosition = 0;
                switch (DepartStationName?.Length ?? 0)
                {
                    case 1:
                        departStationEndPosition = 87;
                        break;
                    case 2:
                        departStationEndPosition = 175;
                        break;
                    case 3:
                        departStationEndPosition = 210;
                        break;
                    case 4:
                        departStationEndPosition = 245;
                        break;
                    case 5:
                        departStationEndPosition = 260;
                        break;
                    default:
                        departStationEndPosition = 40 + (35 * (DepartStationName?.Length ?? 0));
                        break;
                }

                // 到达车站位置固定
                double arriveStationStartPosition = 530;

                // 计算中心点位置
                return (departStationEndPosition + arriveStationStartPosition) / 2;
            }
        }

        // 获取提示信息并以"|"为分隔符分割
        public IEnumerable<string> HintLines
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.Hint))
                    return new List<string>();

                return _selectedTicket.Hint.Split('|');
            }
        }

        // 车票颜色状态
        public bool IsRedTicket
        {
            get => _isRedTicket;
            set
            {
                if (_isRedTicket != value)
                {
                    _isRedTicket = value;
                    _currentLayout = _isRedTicket ? _redTicketLayout : _blueTicketLayout;
                    OnPropertyChanged(nameof(IsRedTicket));
                    OnPropertyChanged(nameof(TicketBackgroundSource));
                    OnPropertyChanged(nameof(ToggleTicketColorButtonText));
                    // 更新所有布局相关属性
                    UpdateAllLayoutProperties();
                }
            }
        }

        // 票种信息显示属性
        private bool _showStudentTicket;
        private bool _isTicketTypeOptionsLocked; // 是否锁定票种选项

        public bool IsTicketTypeOptionsLocked => _isTicketTypeOptionsLocked;

        public bool ShowStudentTicket
        {
            get => _showStudentTicket;
            set
            {
                // 如果选项被锁定，则忽略设置请求
                if (_isTicketTypeOptionsLocked)
                    return;

                if (_showStudentTicket != value)
                {
                    _showStudentTicket = value;
                    if (value && _showChildTicket) ShowChildTicket = false;
                    OnPropertyChanged(nameof(ShowStudentTicket));
                }
            }
        }

        private bool _showDiscountTicket;
        public bool ShowDiscountTicket
        {
            get => _showDiscountTicket;
            set
            {
                // 如果选项被锁定，则忽略设置请求
                if (_isTicketTypeOptionsLocked)
                    return;

                if (_showDiscountTicket != value)
                {
                    _showDiscountTicket = value;
                    OnPropertyChanged(nameof(ShowDiscountTicket));
                }
            }
        }

        private bool _showOnlineTicket;
        public bool ShowOnlineTicket
        {
            get => _showOnlineTicket;
            set
            {
                // 如果选项被锁定，则忽略设置请求
                if (_isTicketTypeOptionsLocked)
                    return;

                if (_showOnlineTicket != value)
                {
                    _showOnlineTicket = value;
                    OnPropertyChanged(nameof(ShowOnlineTicket));
                }
            }
        }

        private bool _showChildTicket;
        public bool ShowChildTicket
        {
            get => _showChildTicket;
            set
            {
                // 如果选项被锁定，则忽略设置请求
                if (_isTicketTypeOptionsLocked)
                    return;

                if (_showChildTicket != value)
                {
                    _showChildTicket = value;
                    if (value && _showStudentTicket) ShowStudentTicket = false;
                    OnPropertyChanged(nameof(ShowChildTicket));
                }
            }
        }

        private bool _useDiscountAsDiscount; // 是否使用"折"代替"惠"
        public bool UseDiscountAsDiscount
        {
            get => _useDiscountAsDiscount;
            set
            {
                if (_useDiscountAsDiscount != value)
                {
                    _useDiscountAsDiscount = value;
                    OnPropertyChanged(nameof(UseDiscountAsDiscount));
                    OnPropertyChanged(nameof(DiscountText));
                }
            }
        }

        public bool ShowBoxedTicketType
        {
            get => _showBoxedTicketType;
            set
            {
                if (_showBoxedTicketType != value)
                {
                    _showBoxedTicketType = value;
                    // 通知所有相关绑定更新
                    OnPropertyChanged(nameof(ShowBoxedTicketType));
                    // 直接通知所有票种边距属性更新
                    OnPropertyChanged(nameof(TicketTypeStudentMargin));
                    OnPropertyChanged(nameof(TicketTypeDiscountMargin));
                    OnPropertyChanged(nameof(TicketTypeOnlineMargin));
                    OnPropertyChanged(nameof(TicketTypeChildMargin));
                    OnPropertyChanged(nameof(TicketTypeBoxStyle));
                    // 通知支付渠道边距属性更新
                    OnPropertyChanged(nameof(PaymentAlipayMargin));
                    OnPropertyChanged(nameof(PaymentWeChatMargin));
                    OnPropertyChanged(nameof(PaymentABCMargin));
                    OnPropertyChanged(nameof(PaymentCCBMargin));
                    OnPropertyChanged(nameof(PaymentICBCMargin));
                    OnPropertyChanged(nameof(PaymentCMBMargin));
                    OnPropertyChanged(nameof(PaymentPSBCMargin));
                    OnPropertyChanged(nameof(PaymentBOCMargin));
                    OnPropertyChanged(nameof(PaymentCOMMMargin));

                    // 强制刷新UI，确保边框样式正确应用
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                        if (window != null)
                        {
                            var grid = window.FindName("PreviewGrid") as Grid;
                            if (grid != null)
                            {
                                // 强制重新绘制整个Grid
                                grid.InvalidateVisual();
                            }
                        }
                    });
                }
            }
        }

        // 票种文本
        public string StudentText => "学";
        public string DiscountText => UseDiscountAsDiscount ? "折" : "惠";
        public string OnlineText => "网";
        public string ChildText => "孩";

        // 支付渠道文本
        public string AlipayText => "支";
        public string WeChatText => "微";
        public string ABCText => "农";
        public string CCBText => "建";
        public string ICBCText => "工";
        public string CMBText => "招";
        public string PSBCText => "邮";
        public string BOCText => "中";
        public string COMMText => "交";

        // 支付渠道显示属性
        public bool ShowAlipayPayment
        {
            get => _showAlipayPayment;
            set
            {
                if (_showAlipayPayment != value)
                {
                    _showAlipayPayment = value;
                    OnPropertyChanged(nameof(ShowAlipayPayment));
                }
            }
        }

        public bool ShowWeChatPayment
        {
            get => _showWeChatPayment;
            set
            {
                if (_showWeChatPayment != value)
                {
                    _showWeChatPayment = value;
                    OnPropertyChanged(nameof(ShowWeChatPayment));
                }
            }
        }

        public bool ShowABCPayment
        {
            get => _showABCPayment;
            set
            {
                if (_showABCPayment != value)
                {
                    _showABCPayment = value;
                    OnPropertyChanged(nameof(ShowABCPayment));
                }
            }
        }

        public bool ShowCCBPayment
        {
            get => _showCCBPayment;
            set
            {
                if (_showCCBPayment != value)
                {
                    _showCCBPayment = value;
                    OnPropertyChanged(nameof(ShowCCBPayment));
                }
            }
        }

        public bool ShowICBCPayment
        {
            get => _showICBCPayment;
            set
            {
                if (_showICBCPayment != value)
                {
                    _showICBCPayment = value;
                    OnPropertyChanged(nameof(ShowICBCPayment));
                }
            }
        }

        public bool ShowCMBPayment
        {
            get => _showCMBPayment;
            set
            {
                if (_showCMBPayment != value)
                {
                    _showCMBPayment = value;
                    OnPropertyChanged(nameof(ShowCMBPayment));
                }
            }
        }

        public bool ShowPSBCPayment
        {
            get => _showPSBCPayment;
            set
            {
                if (_showPSBCPayment != value)
                {
                    _showPSBCPayment = value;
                    OnPropertyChanged(nameof(ShowPSBCPayment));
                }
            }
        }

        public bool ShowBOCPayment
        {
            get => _showBOCPayment;
            set
            {
                if (_showBOCPayment != value)
                {
                    _showBOCPayment = value;
                    OnPropertyChanged(nameof(ShowBOCPayment));
                }
            }
        }

        public bool ShowCOMMPayment
        {
            get => _showCOMMPayment;
            set
            {
                if (_showCOMMPayment != value)
                {
                    _showCOMMPayment = value;
                    OnPropertyChanged(nameof(ShowCOMMPayment));
                }
            }
        }

        // 票种框样式
        public Style TicketTypeBoxStyle
        {
            get
            {
                Style style = null;
                if (IsRedTicket)
                {
                    // 使用方形边框样式
                    style = Application.Current.Resources["TicketTypeSquareBoxStyle"] as Style;
                }
                else
                {
                    // 使用圆形边框样式
                    style = Application.Current.Resources["TicketTypeCircleBoxStyle"] as Style;
                }

                // 确保返回有效的样式
                if (style == null)
                {
                    // 创建一个基本的边框样式作为后备
                    style = new Style(typeof(Border));
                    style.Setters.Add(new Setter(Border.WidthProperty, 30.0));
                    style.Setters.Add(new Setter(Border.HeightProperty, 30.0));
                    style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1.5)));
                    style.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.Black));
                    style.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Transparent));
                    style.Setters.Add(new Setter(Border.HorizontalAlignmentProperty, HorizontalAlignment.Left));
                    style.Setters.Add(new Setter(Border.VerticalAlignmentProperty, VerticalAlignment.Top));

                    if (!IsRedTicket)
                    {
                        // 为蓝色票添加圆角
                        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(15)));
                    }
                    else
                    {
                        // 为红色票添加小圆角
                        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(3)));
                    }
                }

                return style;
            }
        }

        // 票种位置信息
        public Thickness TicketTypeStudentMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["TicketTypeStudentBoxedMargin"]
            : (Thickness)_currentLayout["TicketTypeStudentMargin"];

        public Thickness TicketTypeDiscountMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["TicketTypeDiscountBoxedMargin"]
            : (Thickness)_currentLayout["TicketTypeDiscountMargin"];

        public Thickness TicketTypeOnlineMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["TicketTypeOnlineBoxedMargin"]
            : (Thickness)_currentLayout["TicketTypeOnlineMargin"];

        public Thickness TicketTypeChildMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["TicketTypeChildBoxedMargin"]
            : (Thickness)_currentLayout["TicketTypeChildMargin"];

        // 车票背景图片源
        public string TicketBackgroundSource
        {
            get => _isRedTicket
                ? "pack://application:,,,/Assets/pic/redTicket.png"
                : "pack://application:,,,/Assets/pic/blueTicket.png";
        }

        // 切换车票颜色按钮文本
        public string ToggleTicketColorButtonText
        {
            get => _isRedTicket ? "蓝色车票" : "红色车票";
        }

        public ICommand CloseCommand { get; }
        public ICommand ExportImageCommand { get; }
        public ICommand ToggleTicketColorCommand { get; }

        private void Close()
        {
            OnRequestClose();
        }

        private void ExportImage()
        {
            try
            {
                // 获取预览区域的Grid
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (window == null) return;

                var grid = window.FindName("PreviewGrid") as Grid;
                if (grid == null) return;

                // 构建默认文件名：出发车站-车次号-到达车站
                string defaultFileName = $"{_selectedTicket.DepartStation?.Replace("站", "")}-{_selectedTicket.TrainNo}-{_selectedTicket.ArriveStation?.Replace("站", "")}";

                // 创建保存文件对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG图片|*.png",
                    Title = "保存车票图片",
                    FileName = defaultFileName
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 检测用户是否输入了文件名
                    string fileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        // 使用MaterialDesign风格的对话框提示用户输入文件名
                        var result = MessageDialog.Show(
                            "请输入文件名！",
                            "提示",
                            MessageType.Warning,
                            MessageButtons.Ok,
                            window);
                        return;
                    }

                    // 创建RenderTargetBitmap
                    var bounds = VisualTreeHelper.GetDescendantBounds(grid);
                    var renderTargetBitmap = new RenderTargetBitmap(
                        811, // 使用BlueTicket图片的原始宽度
                        509, // 使用BlueTicket图片的原始高度
                        96,
                        96,
                        PixelFormats.Pbgra32);

                    var drawingVisual = new DrawingVisual();
                    using (var drawingContext = drawingVisual.RenderOpen())
                    {
                        // 首先绘制白色背景
                        drawingContext.DrawRectangle(Brushes.White, null, new Rect(new Point(), new Size(811, 509)));
                        // 然后绘制车票内容
                        var visualBrush = new VisualBrush(grid);
                        drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(), new Size(811, 509)));
                    }

                    renderTargetBitmap.Render(drawingVisual);

                    // 保存为PNG文件
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                    using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBoxHelper.ShowInformation(
                        "图片导出成功！",
                        "提示",
                        window);
                }
            }
            catch (Exception ex)
            {
                var currentWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                MessageBoxHelper.ShowError(
                    $"导出图片时发生错误：{ex.Message}",
                    "错误",
                    currentWindow);
            }
        }

        // 生成二维码
        private void GenerateQRCode()
        {
            try
            {
                if (string.IsNullOrEmpty(_encodingArea)) return;

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(_encodingArea, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        // 生成二维码，不要白色背景，使用透明背景
                        using (Bitmap qrCodeImage = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.Transparent, false))
                        {
                            QrCodeImage = BitmapToImageSource(qrCodeImage);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 生成二维码异常时不处理
            }
        }

        // 将Bitmap转换为BitmapImage
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // 一定要冻结，否则可能出现访问异常
                return bitmapImage;
            }
        }

        // 切换车票颜色
        private void ToggleTicketColor()
        {
            IsRedTicket = !IsRedTicket;

            // 强制刷新票种信息边框样式
            OnPropertyChanged(nameof(TicketTypeBoxStyle));
            OnPropertyChanged(nameof(TicketTypeStudentMargin));
            OnPropertyChanged(nameof(TicketTypeDiscountMargin));
            OnPropertyChanged(nameof(TicketTypeOnlineMargin));
            OnPropertyChanged(nameof(TicketTypeChildMargin));
        }

        // 初始化车票布局参数
        private void InitializeTicketLayouts()
        {
            // 蓝色车票布局参数
            _blueTicketLayout = new Dictionary<string, object>
            {
                // 车票号码位置 
                { "TicketNumberMargin", new Thickness(55, 20, 0, 0) },
                // 检票口位置
                { "CheckInLocationMargin", new Thickness(510, 20, 0, 0) },
                // 出发车站站名位置
                { "DepartStationMargin", new Thickness(60, 60, 0, 0) },
                // 出发车站拼音位置基础值
                { "DepartStationPinyinBaseMargin", new Thickness(-10, 105, 0, 0) },
                // 出发车站"站"字位置参数
                { "DepartStationWordParam", "285,63,513,362" },
                // 车次号位置
                { "TrainNumberMargin", new Thickness(356, 60, 0, 0) },
                // 车次号下箭头位置
                { "TrainArrowMargin", new Thickness(0, 100, 0, 0) },
                // 到达车站站名位置
                { "ArriveStationMargin", new Thickness(540, 60, 0, 0) },
                // 到达车站拼音位置基础值
                { "ArriveStationPinyinBaseMargin", new Thickness(0, 105, 0, 0) },
                // 到达车站"站"字位置参数
                { "ArriveStationWordParam", "763,63,25,351" },
                // 年月日显示位置
                { "DateDisplayMargin", new Thickness(60, 145, 0, 0) },
                // 出发时间位置
                { "DepartTimeMargin", new Thickness(280, 145, 0, 0) },
                // "开"字位置
                { "DepartTimeWordMargin", new Thickness(358, 155, 0, 0) },
                // 车厢号位置
                { "CoachNumberMargin", new Thickness(534, 145, 0, 0) },
                // "车"字位置
                { "CoachWordMargin", new Thickness(571, 155, 0, 0) },
                // "加"字位置
                { "AddedCoachWordMargin", new Thickness(510, 155, 0, 0) },
                // 座位号位置
                { "SeatNumberMargin", new Thickness(597, 145, 0, 0) },
                // "无座"显示位置
                { "NoSeatDisplayMargin", new Thickness(600, 150, 0, 0) },
                // "号"字位置
                { "SeatNumberWordMargin", new Thickness(654, 155, 0, 0) },
                // "¥"符号位置
                { "MoneySymbolMargin", new Thickness(55, 185, 0, 0) },
                // 金额数值位置
                { "MoneyValueMargin", new Thickness(74, 185, 0, 0) },
                // "元"字位置参数
                { "MoneyUnitParam", "74,185,0,0" },
                // 座位类型位置 - 普通
                { "SeatTypeMargin", new Thickness(600, 195, 0, 0) },
                // 座位类型位置 - 无座
                { "SeatTypeMarginNoSeat", new Thickness(530, 190, 0, 0) },
                // 附加信息位置
                { "AdditionalInfoMargin", new Thickness(55, 260, 0, 0) },
                // 车票用途位置
                { "TicketPurposeMargin", new Thickness(54, 320, 0, 0) },
                // 车票用途位置 - 与改签类型共存
                { "TicketPurposeWithModMargin", new Thickness(180, 320, 0, 0) },
                // 车票改签类型位置
                { "TicketModificationTypeMargin", new Thickness(54, 320, 0, 0) },
                // 身份信息位置
                { "IdentityInfoMargin", new Thickness(60, 354, 0, 0) },
                // 编码区位置
                { "EncodingAreaMargin", new Thickness(44, 467, 0, 0) },
                // 提示信息框位置
                { "HintBoxMargin", new Thickness(-170, 390, 0, 0) },
                // 二维码位置
                { "QRCodeMargin", new Thickness(0, 0, 50, 80) },
                // 票种信息位置 - 不带框
                { "TicketTypeStudentMargin", new Thickness(310, 185, 0, 0) },// 学生票
                { "TicketTypeChildMargin", new Thickness(310, 185, 0, 0) },// 儿童票 - 与学生票在同一位置，因为它们是互斥的
                { "TicketTypeOnlineMargin", new Thickness(335, 185, 0, 0) },// 网票
                { "TicketTypeDiscountMargin", new Thickness(360, 185, 0, 0) },// 折扣票
                // 支付渠道位置 - 不带框
                { "PaymentAlipayMargin", new Thickness(335, 185, 0, 0) }, // 支付宝，与网票在同一位置
                { "PaymentWeChatMargin", new Thickness(335, 185, 0, 0) }, // 微信，与网票在同一位置
                { "PaymentABCMargin", new Thickness(385, 185, 0, 0) },    // 农业银行，在惠字旁边
                { "PaymentCCBMargin", new Thickness(385, 185, 0, 0) },    // 建设银行，在惠字旁边
                { "PaymentICBCMargin", new Thickness(385, 185, 0, 0) },   // 工商银行，在惠字旁边
                { "PaymentCMBMargin", new Thickness(385, 185, 0, 0) },    // 招商银行，在惠字旁边
                { "PaymentPSBCMargin", new Thickness(385, 185, 0, 0) },   // 邮储银行，在惠字旁边
                { "PaymentBOCMargin", new Thickness(385, 185, 0, 0) },    // 中国银行，在惠字旁边
                { "PaymentCOMMMargin", new Thickness(385, 185, 0, 0) },   // 交通银行，在惠字旁边
                
                // 票种信息位置 - 带框
                { "TicketTypeStudentBoxedMargin", new Thickness(310, 185, 0, 0) },// 学生票
                { "TicketTypeChildBoxedMargin", new Thickness(310, 185, 0, 0) },// 儿童票 - 与学生票在同一位置，因为它们是互斥的
                { "TicketTypeOnlineBoxedMargin", new Thickness(345, 185, 0, 0) },// 网票
                { "TicketTypeDiscountBoxedMargin", new Thickness(380, 185, 0, 0) },// 折扣票
                // 支付渠道位置 - 带框
                { "PaymentAlipayBoxedMargin", new Thickness(345, 185, 0, 0) }, // 支付宝，与网票在同一位置
                { "PaymentWeChatBoxedMargin", new Thickness(345, 185, 0, 0) }, // 微信，与网票在同一位置
                { "PaymentABCBoxedMargin", new Thickness(415, 185, 0, 0) },    // 农业银行，在惠字旁边
                { "PaymentCCBBoxedMargin", new Thickness(415, 185, 0, 0) },    // 建设银行，在惠字旁边
                { "PaymentICBCBoxedMargin", new Thickness(415, 185, 0, 0) },   // 工商银行，在惠字旁边
                { "PaymentCMBBoxedMargin", new Thickness(415, 185, 0, 0) },    // 招商银行，在惠字旁边
                { "PaymentPSBCBoxedMargin", new Thickness(415, 185, 0, 0) },   // 邮储银行，在惠字旁边
                { "PaymentBOCBoxedMargin", new Thickness(415, 185, 0, 0) },    // 中国银行，在惠字旁边
                { "PaymentCOMMBoxedMargin", new Thickness(415, 185, 0, 0) },   // 交通银行，在惠字旁边
            };

            // 红色车票布局参数 - 部分参数与蓝色不同
            _redTicketLayout = new Dictionary<string, object>(_blueTicketLayout); // 复制蓝色车票布局基础

            // 修改红色车票特定布局参数
            _redTicketLayout["TicketNumberMargin"] = new Thickness(55, 25, 0, 0);
            _redTicketLayout["CheckInLocationMargin"] = new Thickness(510, 25, 0, 0);
            _redTicketLayout["DepartStationMargin"] = new Thickness(60, 65, 0, 0);
            _redTicketLayout["DepartStationPinyinBaseMargin"] = new Thickness(-10, 110, 0, 0);
            _redTicketLayout["DepartStationWordParam"] = "285,68,513,362";
            _redTicketLayout["TrainNumberMargin"] = new Thickness(356, 65, 0, 0);
            _redTicketLayout["TrainArrowMargin"] = new Thickness(0, 105, 0, 0);
            _redTicketLayout["ArriveStationMargin"] = new Thickness(540, 65, 0, 0);
            _redTicketLayout["ArriveStationPinyinBaseMargin"] = new Thickness(0, 110, 0, 0);
            _redTicketLayout["ArriveStationWordParam"] = "758,68,25,351";
            _redTicketLayout["DateDisplayMargin"] = new Thickness(60, 150, 0, 0);
            _redTicketLayout["DepartTimeMargin"] = new Thickness(280, 150, 0, 0);
            _redTicketLayout["DepartTimeWordMargin"] = new Thickness(358, 160, 0, 0);
            _redTicketLayout["CoachNumberMargin"] = new Thickness(534, 150, 0, 0);
            _redTicketLayout["CoachWordMargin"] = new Thickness(571, 160, 0, 0);
            _redTicketLayout["AddedCoachWordMargin"] = new Thickness(510, 160, 0, 0);
            _redTicketLayout["SeatNumberMargin"] = new Thickness(597, 150, 0, 0);
            _redTicketLayout["NoSeatDisplayMargin"] = new Thickness(600, 155, 0, 0);
            _redTicketLayout["SeatNumberWordMargin"] = new Thickness(654, 160, 0, 0);
            _redTicketLayout["MoneySymbolMargin"] = new Thickness(55, 190, 0, 0);
            _redTicketLayout["MoneyValueMargin"] = new Thickness(74, 190, 0, 0);
            _redTicketLayout["MoneyUnitParam"] = "74,190,0,0";  // 修改为与MoneyValueMargin相同的坐标
            _redTicketLayout["SeatTypeMargin"] = new Thickness(600, 200, 0, 0);
            _redTicketLayout["SeatTypeMarginNoSeat"] = new Thickness(530, 195, 0, 0);
            _redTicketLayout["AdditionalInfoMargin"] = new Thickness(55, 265, 0, 0);
            _redTicketLayout["TicketPurposeMargin"] = new Thickness(54, 325, 0, 0);
            _redTicketLayout["TicketPurposeWithModMargin"] = new Thickness(180, 325, 0, 0);
            _redTicketLayout["TicketModificationTypeMargin"] = new Thickness(54, 325, 0, 0);
            _redTicketLayout["IdentityInfoMargin"] = new Thickness(60, 359, 0, 0);
            _redTicketLayout["EncodingAreaMargin"] = new Thickness(44, 450, 0, 0);
            _redTicketLayout["HintBoxMargin"] = new Thickness(-170, 395, 0, 0);
            _redTicketLayout["QRCodeMargin"] = new Thickness(0, 0, 75, 45);

            // 更新红色车票的票种信息位置
            _redTicketLayout["TicketTypeStudentMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeChildMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeOnlineMargin"] = new Thickness(335, 185, 0, 0);
            _redTicketLayout["TicketTypeDiscountMargin"] = new Thickness(360, 185, 0, 0);

            _redTicketLayout["TicketTypeStudentBoxedMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeChildBoxedMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeOnlineBoxedMargin"] = new Thickness(345, 185, 0, 0);
            _redTicketLayout["TicketTypeDiscountBoxedMargin"] = new Thickness(380, 185, 0, 0);

            // 更新红色车票的支付渠道位置
            _redTicketLayout["PaymentAlipayMargin"] = new Thickness(335, 185, 0, 0);
            _redTicketLayout["PaymentWeChatMargin"] = new Thickness(335, 185, 0, 0);
            _redTicketLayout["PaymentABCMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentCCBMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentICBCMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentCMBMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentPSBCMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentBOCMargin"] = new Thickness(385, 185, 0, 0);
            _redTicketLayout["PaymentCOMMMargin"] = new Thickness(385, 185, 0, 0);

            _redTicketLayout["PaymentAlipayBoxedMargin"] = new Thickness(345, 185, 0, 0);
            _redTicketLayout["PaymentWeChatBoxedMargin"] = new Thickness(345, 185, 0, 0);
            _redTicketLayout["PaymentABCBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentCCBBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentICBCBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentCMBBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentPSBCBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentBOCBoxedMargin"] = new Thickness(415, 185, 0, 0);
            _redTicketLayout["PaymentCOMMBoxedMargin"] = new Thickness(415, 185, 0, 0);
        }

        // 更新所有布局相关属性
        private void UpdateAllLayoutProperties()
        {
            // 首先触发布局参数本身的更新通知
            OnPropertyChanged(nameof(TicketNumberMargin));
            OnPropertyChanged(nameof(CheckInLocationMargin));
            OnPropertyChanged(nameof(DepartStationMargin));
            OnPropertyChanged(nameof(DepartStationPinyinBaseMargin));
            OnPropertyChanged(nameof(DepartStationWordParam));
            OnPropertyChanged(nameof(TrainNumberMargin));
            OnPropertyChanged(nameof(TrainArrowMargin));
            OnPropertyChanged(nameof(ArriveStationMargin));
            OnPropertyChanged(nameof(ArriveStationPinyinBaseMargin));
            OnPropertyChanged(nameof(ArriveStationWordParam));
            OnPropertyChanged(nameof(DateDisplayMargin));
            OnPropertyChanged(nameof(DepartTimeMargin));
            OnPropertyChanged(nameof(DepartTimeWordMargin));
            OnPropertyChanged(nameof(CoachNumberMargin));
            OnPropertyChanged(nameof(CoachWordMargin));
            OnPropertyChanged(nameof(AddedCoachWordMargin));
            OnPropertyChanged(nameof(SeatNumberMargin));
            OnPropertyChanged(nameof(NoSeatDisplayMargin));
            OnPropertyChanged(nameof(SeatNumberWordMargin));
            OnPropertyChanged(nameof(MoneySymbolMargin));
            OnPropertyChanged(nameof(MoneyValueMargin));
            OnPropertyChanged(nameof(MoneyUnitParam));
            OnPropertyChanged(nameof(SeatTypeMargin));
            OnPropertyChanged(nameof(SeatTypeMarginNoSeat));
            OnPropertyChanged(nameof(AdditionalInfoMargin));
            OnPropertyChanged(nameof(TicketPurposeMargin));
            OnPropertyChanged(nameof(TicketPurposeWithModMargin));
            OnPropertyChanged(nameof(TicketModificationTypeMargin));
            OnPropertyChanged(nameof(IdentityInfoMargin));
            OnPropertyChanged(nameof(EncodingAreaMargin));
            OnPropertyChanged(nameof(HintBoxMargin));
            OnPropertyChanged(nameof(QRCodeMargin));

            // 更新票种位置
            OnPropertyChanged(nameof(TicketTypeStudentMargin));
            OnPropertyChanged(nameof(TicketTypeDiscountMargin));
            OnPropertyChanged(nameof(TicketTypeOnlineMargin));
            OnPropertyChanged(nameof(TicketTypeChildMargin));
            OnPropertyChanged(nameof(TicketTypeBoxStyle));

            // 更新支付渠道位置
            OnPropertyChanged(nameof(PaymentAlipayMargin));
            OnPropertyChanged(nameof(PaymentWeChatMargin));
            OnPropertyChanged(nameof(PaymentABCMargin));
            OnPropertyChanged(nameof(PaymentCCBMargin));
            OnPropertyChanged(nameof(PaymentICBCMargin));
            OnPropertyChanged(nameof(PaymentCMBMargin));
            OnPropertyChanged(nameof(PaymentPSBCMargin));
            OnPropertyChanged(nameof(PaymentBOCMargin));
            OnPropertyChanged(nameof(PaymentCOMMMargin));

            // 这些属性在XAML中绑定了MultiBinding，需要特别触发
            OnPropertyChanged(nameof(DepartStationWithSpacing));
            OnPropertyChanged(nameof(ArriveStationWithSpacing));
            OnPropertyChanged(nameof(DepartStationName));
            OnPropertyChanged(nameof(ArriveStationName));

            // 为了强制使用MultiBinding的转换器重新计算，我们需要触发转换器所依赖的所有属性
            OnPropertyChanged(nameof(DepartStationPinyinPosition));
            OnPropertyChanged(nameof(ArriveStationPinyinPosition));
            OnPropertyChanged(nameof(TrainNumberCenterPosition));

            // 刷新整个视图，确保所有转换器都能重新计算
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (window != null)
                {
                    var grid = window.FindName("PreviewGrid") as Grid;
                    if (grid != null)
                    {
                        // 强制重新绘制整个Grid
                        grid.InvalidateVisual();
                    }
                }
            });
        }

        // 添加一个公共方法，允许外部代码更新布局
        public void UpdateLayout(Dictionary<string, object> layoutValues, bool isRedTicket)
        {
            if (layoutValues == null) return;

            // 根据车票颜色选择要更新的布局参数
            var targetLayout = isRedTicket ? _redTicketLayout : _blueTicketLayout;

            // 更新所有提供的布局参数
            foreach (var kvp in layoutValues)
            {
                if (targetLayout.ContainsKey(kvp.Key))
                {
                    targetLayout[kvp.Key] = kvp.Value;
                }
            }

            // 如果当前正在显示的是被更新的那种车票类型，则立即更新UI
            if (_isRedTicket == isRedTicket)
            {
                _currentLayout = targetLayout;
                UpdateAllLayoutProperties();
            }
        }

        // 以下属性用于获取当前布局参数

        // 车票号码位置
        public Thickness TicketNumberMargin => (Thickness)_currentLayout["TicketNumberMargin"];

        // 检票口位置
        public Thickness CheckInLocationMargin => (Thickness)_currentLayout["CheckInLocationMargin"];

        // 出发车站站名位置
        public Thickness DepartStationMargin => (Thickness)_currentLayout["DepartStationMargin"];

        // 出发车站拼音位置基础值
        public Thickness DepartStationPinyinBaseMargin => (Thickness)_currentLayout["DepartStationPinyinBaseMargin"];

        // 出发车站"站"字位置参数
        public string DepartStationWordParam => (string)_currentLayout["DepartStationWordParam"];

        // 车次号位置
        public Thickness TrainNumberMargin => (Thickness)_currentLayout["TrainNumberMargin"];

        // 车次号下箭头位置
        public Thickness TrainArrowMargin => (Thickness)_currentLayout["TrainArrowMargin"];

        // 到达车站站名位置
        public Thickness ArriveStationMargin => (Thickness)_currentLayout["ArriveStationMargin"];

        // 到达车站拼音位置基础值
        public Thickness ArriveStationPinyinBaseMargin => (Thickness)_currentLayout["ArriveStationPinyinBaseMargin"];

        // 到达车站"站"字位置参数
        public string ArriveStationWordParam => (string)_currentLayout["ArriveStationWordParam"];

        // 年月日显示位置
        public Thickness DateDisplayMargin => (Thickness)_currentLayout["DateDisplayMargin"];

        // 出发时间位置
        public Thickness DepartTimeMargin => (Thickness)_currentLayout["DepartTimeMargin"];

        // "开"字位置
        public Thickness DepartTimeWordMargin => (Thickness)_currentLayout["DepartTimeWordMargin"];

        // 车厢号位置
        public Thickness CoachNumberMargin => (Thickness)_currentLayout["CoachNumberMargin"];

        // "车"字位置
        public Thickness CoachWordMargin => (Thickness)_currentLayout["CoachWordMargin"];

        // "加"字位置
        public Thickness AddedCoachWordMargin => (Thickness)_currentLayout["AddedCoachWordMargin"];

        // 座位号位置
        public Thickness SeatNumberMargin => (Thickness)_currentLayout["SeatNumberMargin"];

        // "无座"显示位置
        public Thickness NoSeatDisplayMargin => (Thickness)_currentLayout["NoSeatDisplayMargin"];

        // "号"字位置
        public Thickness SeatNumberWordMargin => (Thickness)_currentLayout["SeatNumberWordMargin"];

        // "¥"符号位置
        public Thickness MoneySymbolMargin => (Thickness)_currentLayout["MoneySymbolMargin"];

        // 金额数值位置
        public Thickness MoneyValueMargin => (Thickness)_currentLayout["MoneyValueMargin"];

        // "元"字位置参数
        public string MoneyUnitParam => (string)_currentLayout["MoneyUnitParam"];

        // 座位类型位置 - 普通
        public Thickness SeatTypeMargin => (Thickness)_currentLayout["SeatTypeMargin"];

        // 座位类型位置 - 无座
        public Thickness SeatTypeMarginNoSeat => (Thickness)_currentLayout["SeatTypeMarginNoSeat"];

        // 附加信息位置
        public Thickness AdditionalInfoMargin => (Thickness)_currentLayout["AdditionalInfoMargin"];

        // 车票用途位置
        public Thickness TicketPurposeMargin => (Thickness)_currentLayout["TicketPurposeMargin"];

        // 车票用途位置 - 与改签类型共存
        public Thickness TicketPurposeWithModMargin => (Thickness)_currentLayout["TicketPurposeWithModMargin"];

        // 车票改签类型位置
        public Thickness TicketModificationTypeMargin => (Thickness)_currentLayout["TicketModificationTypeMargin"];

        // 身份信息位置
        public Thickness IdentityInfoMargin => (Thickness)_currentLayout["IdentityInfoMargin"];

        // 编码区位置
        public Thickness EncodingAreaMargin => (Thickness)_currentLayout["EncodingAreaMargin"];

        // 提示信息框位置
        public Thickness HintBoxMargin => (Thickness)_currentLayout["HintBoxMargin"];

        // 二维码位置
        public Thickness QRCodeMargin => (Thickness)_currentLayout["QRCodeMargin"];

        // 支付渠道位置信息
        public Thickness PaymentAlipayMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentAlipayBoxedMargin"]
            : (Thickness)_currentLayout["PaymentAlipayMargin"];

        public Thickness PaymentWeChatMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentWeChatBoxedMargin"]
            : (Thickness)_currentLayout["PaymentWeChatMargin"];

        public Thickness PaymentABCMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentABCBoxedMargin"]
            : (Thickness)_currentLayout["PaymentABCMargin"];

        public Thickness PaymentCCBMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentCCBBoxedMargin"]
            : (Thickness)_currentLayout["PaymentCCBMargin"];

        public Thickness PaymentICBCMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentICBCBoxedMargin"]
            : (Thickness)_currentLayout["PaymentICBCMargin"];

        public Thickness PaymentCMBMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentCMBBoxedMargin"]
            : (Thickness)_currentLayout["PaymentCMBMargin"];

        public Thickness PaymentPSBCMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentPSBCBoxedMargin"]
            : (Thickness)_currentLayout["PaymentPSBCMargin"];

        public Thickness PaymentBOCMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentBOCBoxedMargin"]
            : (Thickness)_currentLayout["PaymentBOCMargin"];

        public Thickness PaymentCOMMMargin => ShowBoxedTicketType
            ? (Thickness)_currentLayout["PaymentCOMMBoxedMargin"]
            : (Thickness)_currentLayout["PaymentCOMMMargin"];

        // 根据原始票种信息初始化票种显示选项并锁定
        private void InitializeTicketTypeOptions()
        {
            if (_selectedTicket != null)
            {
                // 清除所有票种显示选项
                _isTicketTypeOptionsLocked = true;
                _showStudentTicket = false;
                _showDiscountTicket = false;
                _showOnlineTicket = false;
                _showChildTicket = false;

                // 清除所有支付渠道显示选项
                _showAlipayPayment = false;
                _showWeChatPayment = false;
                _showABCPayment = false;
                _showCCBPayment = false;
                _showICBCPayment = false;
                _showCMBPayment = false;
                _showPSBCPayment = false;
                _showBOCPayment = false;
                _showCOMMPayment = false;

                var ticketTypeFlags = _selectedTicket.TicketTypeFlags;
                var paymentChannelFlags = _selectedTicket.PaymentChannelFlags;

                // 根据原始票种信息设置显示选项
                if ((ticketTypeFlags & (int)TicketTypeFlags.StudentTicket) != 0)
                {
                    _showStudentTicket = true;
                }

                if ((ticketTypeFlags & (int)TicketTypeFlags.DiscountTicket) != 0)
                {
                    _showDiscountTicket = true;
                }

                // 先检测是否有支付宝或微信支付
                bool hasOnlinePayment = false;

                if ((paymentChannelFlags & (int)PaymentChannelFlags.Alipay) != 0)
                {
                    _showAlipayPayment = true;
                    hasOnlinePayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.WeChat) != 0)
                {
                    _showWeChatPayment = true;
                    hasOnlinePayment = true;
                }

                // 只有当没有支付宝和微信支付渠道时，才显示网络售票标识
                if (!hasOnlinePayment && (ticketTypeFlags & (int)TicketTypeFlags.OnlineTicket) != 0)
                {
                    _showOnlineTicket = true;
                }

                if ((ticketTypeFlags & (int)TicketTypeFlags.ChildTicket) != 0)
                {
                    _showChildTicket = true;
                }

                // 设置银行类支付渠道
                if ((paymentChannelFlags & (int)PaymentChannelFlags.ABC) != 0)
                {
                    _showABCPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.CCB) != 0)
                {
                    _showCCBPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.ICBC) != 0)
                {
                    _showICBCPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.CMB) != 0)
                {
                    _showCMBPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.PSBC) != 0)
                {
                    _showPSBCPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.BOC) != 0)
                {
                    _showBOCPayment = true;
                }

                if ((paymentChannelFlags & (int)PaymentChannelFlags.COMM) != 0)
                {
                    _showCOMMPayment = true;
                }

                // 通知UI更新
                OnPropertyChanged(nameof(ShowStudentTicket));
                OnPropertyChanged(nameof(ShowDiscountTicket));
                OnPropertyChanged(nameof(ShowOnlineTicket));
                OnPropertyChanged(nameof(ShowChildTicket));
                OnPropertyChanged(nameof(ShowAlipayPayment));
                OnPropertyChanged(nameof(ShowWeChatPayment));
                OnPropertyChanged(nameof(ShowABCPayment));
                OnPropertyChanged(nameof(ShowCCBPayment));
                OnPropertyChanged(nameof(ShowICBCPayment));
                OnPropertyChanged(nameof(ShowCMBPayment));
                OnPropertyChanged(nameof(ShowPSBCPayment));
                OnPropertyChanged(nameof(ShowBOCPayment));
                OnPropertyChanged(nameof(ShowCOMMPayment));
                OnPropertyChanged(nameof(IsTicketTypeOptionsLocked));
            }
            else
            {
                // 如果没有票种信息，则解锁选项
                _isTicketTypeOptionsLocked = false;
                OnPropertyChanged(nameof(IsTicketTypeOptionsLocked));
            }
        }

        public event EventHandler RequestClose;

        protected virtual void OnRequestClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // 判断是否为卧铺座位
        public bool IsBerthSeat => (_selectedTicket?.SeatType?.Contains("新空调硬卧") == true) || 
                                  (_selectedTicket?.SeatType?.Contains("新空调软卧") == true);

        // 判断座位号是否包含上中下
        public bool HasBerthPosition => IsBerthSeat && 
                                      ((_selectedTicket?.SeatNo?.Contains("上") == true) || 
                                       (_selectedTicket?.SeatNo?.Contains("中") == true) || 
                                       (_selectedTicket?.SeatNo?.Contains("下") == true));

        // 获取修改后的座位号显示文本（只返回数字部分）
        public string BerthSeatDisplay
        {
            get
            {
                if (!HasBerthPosition || _selectedTicket?.SeatNo == null)
                    return _selectedTicket?.SeatNo ?? string.Empty;
                    
                string seatNo = _selectedTicket.SeatNo;
                // 提取数字部分
                if (seatNo.Contains("上"))
                    return seatNo.Replace("上", "").Replace("号", "");
                else if (seatNo.Contains("中"))
                    return seatNo.Replace("中", "").Replace("号", "");
                else if (seatNo.Contains("下"))
                    return seatNo.Replace("下", "").Replace("号", "");
                    
                return seatNo;
            }
        }

        // 铺位类型显示（上铺/中铺/下铺）
        public string BerthPositionDisplay
        {
            get
            {
                if (!HasBerthPosition || _selectedTicket?.SeatNo == null)
                    return string.Empty;
                    
                string seatNo = _selectedTicket.SeatNo;
                if (seatNo.Contains("上"))
                    return "上铺";
                else if (seatNo.Contains("中"))
                    return "中铺";
                else if (seatNo.Contains("下"))
                    return "下铺";
                    
                return string.Empty;
            }
        }

        // 卧铺"铺"字位置
        public Thickness BerthWordMargin => (Thickness)_currentLayout["SeatNumberWordMargin"];

        // 铺位类型文本位置
        public Thickness BerthTypeMargin
        {
            get
            {
                // 获取基础座位号位置
                Thickness baseMargin = (Thickness)_currentLayout["SeatNumberMargin"];
                
                // 计算数字部分宽度 (每个数字约14像素宽)
                double digitWidth = BerthSeatDisplay.Length * 14;
                
                // 红蓝车票使用不同的间距和高度调整(正值向下移动，负值向上移动）
                double extraSpacing; // 额外添加的间距值
                double verticalAdjustment; // 垂直位置调整
                
                if (IsRedTicket)
                {
                    // 红色车票参数
                    extraSpacing = 2;
                    verticalAdjustment = 10;
                }
                else
                {
                    // 蓝色车票参数
                    extraSpacing = 2;
                    verticalAdjustment = 4;
                }
                
                // 调整左边距和上边距，让铺位类型显示在适当位置
                return new Thickness(
                    baseMargin.Left + digitWidth + extraSpacing,
                    baseMargin.Top + verticalAdjustment,
                    baseMargin.Right,
                    baseMargin.Bottom
                );
            }
        }
    }
}