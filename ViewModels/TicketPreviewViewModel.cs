using System.Windows.Input;
using TA_WPF.Models;
using System.Windows.Media.Imaging;
using QRCoder;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using Microsoft.Win32;
// 使用别名避免命名冲突
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Bitmap = System.Drawing.Bitmap;

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
            TicketTypeFlags = (int)Models.TicketTypeFlags.ChildTicket // 设计时默认为儿童票
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

        public string IdentityInfo
        {
            get => _identityInfo;
            set
            {
                if (_identityInfo != value)
                {
                    _identityInfo = value;
                    OnPropertyChanged(nameof(IdentityInfo));
                }
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

        // 处理出发站名的字间距展示
        public string DepartStationWithSpacing
        {
            get
            {
                var name = DepartStationName;
                if (string.IsNullOrEmpty(name)) return string.Empty;
                return FormatStationNameWithSpacing(name);
            }
        }

        // 处理到达站名的字间距展示
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
            System.Diagnostics.Debug.WriteLine($"原始站名: '{stationName}', 长度: {length}");

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
            System.Diagnostics.Debug.WriteLine($"处理后的结果: '{result}'");
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

        // 出发站拼音的水平位置
        public double DepartStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.DepartStationPinyin))
                    return 83;

                // 根据出发站名称的长度计算拼音的居中位置
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

        // 到达站拼音的水平位置
        public double ArriveStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.ArriveStationPinyin))
                    return 540;

                // 根据到达站名称的长度计算拼音的居中位置
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
                // 获取出发站"站"字位置
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

                // 到达站位置固定
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

                // 构建默认文件名：出发站-车次号-到达站
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
                    // 检查用户是否输入了文件名
                    string fileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        // 使用MaterialDesign风格的对话框提示用户输入文件名
                        var result = Views.MessageDialog.Show(
                            "请输入文件名！",
                            "提示",
                            Views.MessageType.Warning,
                            Views.MessageButtons.Ok,
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

                    // 使用MaterialDesign风格的对话框显示成功消息
                    Views.MessageDialog.Show(
                        "图片导出成功！",
                        "提示",
                        Views.MessageType.Information,
                        Views.MessageButtons.Ok,
                        window);
                }
            }
            catch (Exception ex)
            {
                // 使用MaterialDesign风格的对话框显示错误消息
                var currentWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                Views.MessageDialog.Show(
                    $"导出图片时发生错误：{ex.Message}",
                    "错误",
                    Views.MessageType.Error,
                    Views.MessageButtons.Ok,
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
                // 出发站站名位置
                { "DepartStationMargin", new Thickness(60, 60, 0, 0) },
                // 出发站拼音位置基础值
                { "DepartStationPinyinBaseMargin", new Thickness(-10, 105, 0, 0) },
                // 出发站"站"字位置参数
                { "DepartStationWordParam", "285,63,513,362" },
                // 车次号位置
                { "TrainNumberMargin", new Thickness(356, 60, 0, 0) },
                // 车次号下箭头位置
                { "TrainArrowMargin", new Thickness(0, 100, 0, 0) },
                // 到达站站名位置
                { "ArriveStationMargin", new Thickness(540, 60, 0, 0) },
                // 到达站拼音位置基础值
                { "ArriveStationPinyinBaseMargin", new Thickness(0, 105, 0, 0) },
                // 到达站"站"字位置参数
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
                { "MoneyUnitParam", "185,280,0,0" },
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
                { "TicketTypeOnlineMargin", new Thickness(345, 185, 0, 0) },// 网票
                { "TicketTypeDiscountMargin", new Thickness(375, 185, 0, 0) },// 折扣票
                
                // 票种信息位置 - 带框
                { "TicketTypeStudentBoxedMargin", new Thickness(310, 185, 0, 0) },// 学生票
                { "TicketTypeChildBoxedMargin", new Thickness(310, 185, 0, 0) },// 儿童票 - 与学生票在同一位置，因为它们是互斥的
                { "TicketTypeOnlineBoxedMargin", new Thickness(345, 185, 0, 0) },// 网票
                { "TicketTypeDiscountBoxedMargin", new Thickness(375, 185, 0, 0) },// 折扣票
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
            _redTicketLayout["MoneyUnitParam"] = "350,330,0,0";
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
            _redTicketLayout["TicketTypeOnlineMargin"] = new Thickness(345, 185, 0, 0);
            _redTicketLayout["TicketTypeDiscountMargin"] = new Thickness(375, 185, 0, 0);
            
            _redTicketLayout["TicketTypeStudentBoxedMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeChildBoxedMargin"] = new Thickness(310, 185, 0, 0);
            _redTicketLayout["TicketTypeOnlineBoxedMargin"] = new Thickness(345, 185, 0, 0);
            _redTicketLayout["TicketTypeDiscountBoxedMargin"] = new Thickness(375, 185, 0, 0);
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

            // 票种信息位置
            OnPropertyChanged(nameof(TicketTypeStudentMargin));
            OnPropertyChanged(nameof(TicketTypeDiscountMargin));
            OnPropertyChanged(nameof(TicketTypeOnlineMargin));
            OnPropertyChanged(nameof(TicketTypeChildMargin));
            OnPropertyChanged(nameof(TicketTypeBoxStyle));

            // 然后触发使用转换器的属性的更新通知
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

        // 出发站站名位置
        public Thickness DepartStationMargin => (Thickness)_currentLayout["DepartStationMargin"];

        // 出发站拼音位置基础值
        public Thickness DepartStationPinyinBaseMargin => (Thickness)_currentLayout["DepartStationPinyinBaseMargin"];

        // 出发站"站"字位置参数
        public string DepartStationWordParam => (string)_currentLayout["DepartStationWordParam"];

        // 车次号位置
        public Thickness TrainNumberMargin => (Thickness)_currentLayout["TrainNumberMargin"];

        // 车次号下箭头位置
        public Thickness TrainArrowMargin => (Thickness)_currentLayout["TrainArrowMargin"];

        // 到达站站名位置
        public Thickness ArriveStationMargin => (Thickness)_currentLayout["ArriveStationMargin"];

        // 到达站拼音位置基础值
        public Thickness ArriveStationPinyinBaseMargin => (Thickness)_currentLayout["ArriveStationPinyinBaseMargin"];

        // 到达站"站"字位置参数
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

                var ticketTypeFlags = _selectedTicket.TicketTypeFlags;

                // 根据原始票种信息设置显示选项
                if ((ticketTypeFlags & (int)Models.TicketTypeFlags.StudentTicket) != 0)
                {
                    _showStudentTicket = true;
                }
                
                if ((ticketTypeFlags & (int)Models.TicketTypeFlags.DiscountTicket) != 0)
                {
                    _showDiscountTicket = true;
                }
                
                if ((ticketTypeFlags & (int)Models.TicketTypeFlags.OnlineTicket) != 0)
                {
                    _showOnlineTicket = true;
                }
                
                if ((ticketTypeFlags & (int)Models.TicketTypeFlags.ChildTicket) != 0)
                {
                    _showChildTicket = true;
                }

                // 通知UI更新
                OnPropertyChanged(nameof(ShowStudentTicket));
                OnPropertyChanged(nameof(ShowDiscountTicket));
                OnPropertyChanged(nameof(ShowOnlineTicket));
                OnPropertyChanged(nameof(ShowChildTicket));
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
    }
}