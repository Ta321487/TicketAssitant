using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Linq;
using TA_WPF.Models;
using System.Windows.Media.Imaging;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Controls;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Color = System.Windows.Media.Color;
using System.Configuration;

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
            TicketModificationType = "始发改签"
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
            
            // 默认使用蓝色车票
            _isRedTicket = false;
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

        // 车票颜色状态
        public bool IsRedTicket
        {
            get => _isRedTicket;
            set
            {
                if (_isRedTicket != value)
                {
                    _isRedTicket = value;
                    OnPropertyChanged(nameof(IsRedTicket));
                    OnPropertyChanged(nameof(TicketBackgroundSource));
                    OnPropertyChanged(nameof(ToggleTicketColorButtonText));
                    
                    // 更新所有与布局相关的属性
                    OnPropertyChanged(nameof(TicketNumberMargin));
                    OnPropertyChanged(nameof(CheckInLabelMargin));
                    OnPropertyChanged(nameof(DepartStationMargin));
                    OnPropertyChanged(nameof(ArriveStationMargin));
                    OnPropertyChanged(nameof(DepartStationPinyinMargin));
                    OnPropertyChanged(nameof(ArriveStationPinyinMargin));
                    OnPropertyChanged(nameof(DepartStationPinyinPosition));
                    OnPropertyChanged(nameof(ArriveStationPinyinPosition));
                    OnPropertyChanged(nameof(DepartStationWordPositionParams));
                    OnPropertyChanged(nameof(ArriveStationWordPositionParams));
                    OnPropertyChanged(nameof(DateDisplayMargin));
                    OnPropertyChanged(nameof(DepartTimeMargin));
                    OnPropertyChanged(nameof(DepartTimeKaiMargin));
                    OnPropertyChanged(nameof(CoachNumberMargin));
                    OnPropertyChanged(nameof(CoachCarMargin));
                    OnPropertyChanged(nameof(CoachAddedMargin));
                    OnPropertyChanged(nameof(SeatNumberMargin));
                    OnPropertyChanged(nameof(NoSeatMargin));
                    OnPropertyChanged(nameof(SeatNumberHaoMargin));
                    OnPropertyChanged(nameof(MoneyYuanMargin));
                    OnPropertyChanged(nameof(MoneyValueMargin));
                    OnPropertyChanged(nameof(MoneyUnitPositionParams));
                    OnPropertyChanged(nameof(SeatTypeMargin));
                    OnPropertyChanged(nameof(AdditionalInfoMargin));
                    OnPropertyChanged(nameof(TicketPurposeMargin));
                    OnPropertyChanged(nameof(TicketModificationTypeMargin));
                    OnPropertyChanged(nameof(IdentityInfoMargin));
                    OnPropertyChanged(nameof(EncodingAreaMargin));
                    OnPropertyChanged(nameof(HintBorderMargin));
                    OnPropertyChanged(nameof(QRCodeMargin));
                    OnPropertyChanged(nameof(TrainNumberMargin));
                    OnPropertyChanged(nameof(TrainNumberCenterPosition));
                }
            }
        }
        
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

        // 车票号码位置
        public Thickness TicketNumberMargin
        {
            get => IsRedTicket 
                ? new Thickness(65, 25, 0, 0)   // 红色车票
                : new Thickness(55, 20, 0, 0);  // 蓝色车票
        }
        
        // 检票口位置
        public Thickness CheckInLabelMargin
        {
            get => IsRedTicket 
                ? new Thickness(520, 28, 0, 0)  // 红色车票
                : new Thickness(510, 20, 0, 0); // 蓝色车票
        }
        
        // 出发站站名位置
        public Thickness DepartStationMargin
        {
            get => IsRedTicket 
                ? new Thickness(70, 65, 0, 0)   // 红色车票
                : new Thickness(60, 60, 0, 0);  // 蓝色车票
        }
        
        // 到达站站名位置
        public Thickness ArriveStationMargin
        {
            get => IsRedTicket 
                ? new Thickness(550, 65, 0, 0)  // 红色车票
                : new Thickness(540, 60, 0, 0); // 蓝色车票
        }
        
        // 出发站拼音边距
        public Thickness DepartStationPinyinMargin
        {
            get => IsRedTicket 
                ? new Thickness(-10, 110, 0, 0) // 红色车票
                : new Thickness(-10, 105, 0, 0); // 蓝色车票
        }
        
        // 到达站拼音边距
        public Thickness ArriveStationPinyinMargin
        {
            get => IsRedTicket 
                ? new Thickness(0, 110, 0, 0)  // 红色车票
                : new Thickness(0, 105, 0, 0); // 蓝色车票
        }
        
        // 出发站"站"字位置参数 - 用于转换器
        public string DepartStationWordPositionParams
        {
            get => IsRedTicket 
                ? "80,63,513,362"   // 红色车票 - 基准位置
                : "70,60,513,362";  // 蓝色车票 - 基准位置
        }
        
        // 到达站"站"字位置参数 - 用于转换器
        public string ArriveStationWordPositionParams
        {
            get => IsRedTicket 
                ? "550,63,25,351"   // 红色车票 - 基准位置
                : "540,60,25,351";  // 蓝色车票 - 基准位置
        }
        
        // 日期显示位置
        public Thickness DateDisplayMargin
        {
            get => IsRedTicket 
                ? new Thickness(70, 150, 0, 0)  // 红色车票
                : new Thickness(60, 145, 0, 0); // 蓝色车票
        }
        
        // 出发时间位置
        public Thickness DepartTimeMargin
        {
            get => IsRedTicket 
                ? new Thickness(290, 150, 0, 0) // 红色车票
                : new Thickness(280, 145, 0, 0); // 蓝色车票
        }
        
        // "开"字位置
        public Thickness DepartTimeKaiMargin
        {
            get => IsRedTicket 
                ? new Thickness(368, 160, 0, 0) // 红色车票
                : new Thickness(358, 155, 0, 0); // 蓝色车票
        }
        
        // 车厢号位置
        public Thickness CoachNumberMargin
        {
            get => IsRedTicket 
                ? new Thickness(544, 150, 0, 0) // 红色车票
                : new Thickness(534, 145, 0, 0); // 蓝色车票
        }
        
        // "车"字位置
        public Thickness CoachCarMargin
        {
            get => IsRedTicket 
                ? new Thickness(581, 160, 0, 0) // 红色车票
                : new Thickness(571, 155, 0, 0); // 蓝色车票
        }
        
        // "加"字位置
        public Thickness CoachAddedMargin
        {
            get => IsRedTicket 
                ? new Thickness(520, 160, 0, 0) // 红色车票
                : new Thickness(510, 155, 0, 0); // 蓝色车票
        }
        
        // 座位号位置
        public Thickness SeatNumberMargin
        {
            get => IsRedTicket 
                ? new Thickness(607, 150, 0, 0) // 红色车票
                : new Thickness(597, 145, 0, 0); // 蓝色车票
        }
        
        // "无座"位置
        public Thickness NoSeatMargin
        {
            get => IsRedTicket 
                ? new Thickness(610, 155, 0, 0) // 红色车票
                : new Thickness(600, 150, 0, 0); // 蓝色车票
        }
        
        // "号"字位置
        public Thickness SeatNumberHaoMargin
        {
            get => IsRedTicket 
                ? new Thickness(664, 160, 0, 0) // 红色车票
                : new Thickness(654, 155, 0, 0); // 蓝色车票
        }
        
        // "¥"符号位置
        public Thickness MoneyYuanMargin
        {
            get => IsRedTicket 
                ? new Thickness(65, 190, 0, 0)  // 红色车票
                : new Thickness(55, 185, 0, 0); // 蓝色车票
        }
        
        // 金额数值位置
        public Thickness MoneyValueMargin
        {
            get => IsRedTicket 
                ? new Thickness(84, 190, 0, 0)  // 红色车票
                : new Thickness(74, 185, 0, 0); // 蓝色车票
        }
        
        // 金额"元"字位置参数 - 用于转换器
        public string MoneyUnitPositionParams
        {
            get => IsRedTicket 
                ? "160,190,0,0"       // 红色车票
                : "150,185,0,0";      // 蓝色车票
        }
        
        // 座位类型位置 - 无座时用固定值
        public Thickness SeatTypeMargin
        {
            get
            {
                if (HasNoSeat)
                {
                    return IsRedTicket 
                        ? new Thickness(540, 195, 0, 0) // 红色车票无座
                        : new Thickness(530, 190, 0, 0); // 蓝色车票无座
                }
                else
                {
                    return IsRedTicket 
                        ? new Thickness(610, 200, 0, 0) // 红色车票有座
                        : new Thickness(600, 195, 0, 0); // 蓝色车票有座
                }
            }
        }
        
        // 附加信息位置
        public Thickness AdditionalInfoMargin
        {
            get => IsRedTicket 
                ? new Thickness(65, 265, 0, 0)  // 红色车票
                : new Thickness(55, 260, 0, 0); // 蓝色车票
        }
        
        // 车票用途位置 - 根据改签类型调整
        public Thickness TicketPurposeMargin
        {
            get
            {
                // 如果车票改签类型有值，车票用途需要向右移动
                bool hasModificationType = !string.IsNullOrEmpty(_selectedTicket?.TicketModificationType);
                
                return IsRedTicket
                    ? (hasModificationType ? new Thickness(190, 325, 0, 0) : new Thickness(64, 325, 0, 0)) // 红色车票
                    : (hasModificationType ? new Thickness(180, 320, 0, 0) : new Thickness(54, 320, 0, 0)); // 蓝色车票
            }
        }
        
        // 车票改签类型位置
        public Thickness TicketModificationTypeMargin
        {
            get => IsRedTicket 
                ? new Thickness(64, 325, 0, 0)  // 红色车票
                : new Thickness(54, 320, 0, 0); // 蓝色车票
        }
        
        // 身份信息位置
        public Thickness IdentityInfoMargin
        {
            get => IsRedTicket 
                ? new Thickness(65, 359, 0, 0)  // 红色车票
                : new Thickness(60, 354, 0, 0); // 蓝色车票
        }
        
        // 编码区位置
        public Thickness EncodingAreaMargin
        {
            get => IsRedTicket 
                ? new Thickness(49, 472, 0, 0)  // 红色车票
                : new Thickness(44, 467, 0, 0); // 蓝色车票
        }
        
        // 提示信息框位置
        public Thickness HintBorderMargin
        {
            get => IsRedTicket 
                ? new Thickness(-170, 395, 0, 0) // 红色车票
                : new Thickness(-170, 390, 0, 0); // 蓝色车票
        }
        
        // 二维码位置
        public Thickness QRCodeMargin
        {
            get => IsRedTicket 
                ? new Thickness(0, 0, 60, 85)   // 红色车票
                : new Thickness(0, 0, 50, 80);  // 蓝色车票
        }
        
        // 车次号位置
        public Thickness TrainNumberMargin
        {
            get => IsRedTicket 
                ? new Thickness(366, 65, 0, 0)  // 红色车票
                : new Thickness(356, 60, 0, 0); // 蓝色车票
        }

        // 出发站拼音的水平位置
        public double DepartStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.DepartStationPinyin))
                    return IsRedTicket ? 93 : 83;

                // 根据出发站名称的长度计算拼音的居中位置
                var name = DepartStationName;
                if (string.IsNullOrEmpty(name)) 
                    return IsRedTicket ? 93 : 83;

                if (IsRedTicket)
                {
                    // 红色车票的位置计算
                    switch (name.Length)
                    {
                        case 1: return 76;   // 单字站名，使拼音居中
                        case 2: return 93;   // 双字站名
                        case 3: return 93;   // 三字站名
                        case 4: return 93;   // 四字站名
                        case 5: return 93;   // 五字站名
                        default: return 93;  // 其他情况
                    }
                }
                else
                {
                    // 蓝色车票的位置计算
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
        }

        // 到达站拼音的水平位置
        public double ArriveStationPinyinPosition
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedTicket?.ArriveStationPinyin))
                    return IsRedTicket ? 550 : 540;

                // 根据到达站名称的长度计算拼音的居中位置
                var name = ArriveStationName;
                if (string.IsNullOrEmpty(name)) 
                    return IsRedTicket ? 550 : 540;

                if (IsRedTicket)
                {
                    // 红色车票的位置计算
                    switch (name.Length)
                    {
                        case 1: return 566;  // 单字站名，使拼音居中
                        case 2: return 550;  // 双字站名
                        case 3: return 550;  // 三字站名
                        case 4: return 550;  // 四字站名
                        case 5: return 550;  // 五字站名
                        default: return 550; // 其他情况
                    }
                }
                else
                {
                    // 蓝色车票的位置计算
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
        }

        // 计算车次号居中位置，用于箭头定位
        public double TrainNumberCenterPosition
        {
            get
            {
                // 获取出发站"站"字位置
                double departStationEndPosition = 0;
                
                // 根据出发站名长度计算终点位置
                switch (DepartStationName?.Length ?? 0)
                {
                    case 1:
                        departStationEndPosition = IsRedTicket ? 97 : 87;
                        break;
                    case 2:
                        departStationEndPosition = IsRedTicket ? 185 : 175;
                        break;
                    case 3:
                        departStationEndPosition = IsRedTicket ? 220 : 210;
                        break;
                    case 4:
                        departStationEndPosition = IsRedTicket ? 255 : 245;
                        break;
                    case 5:
                        departStationEndPosition = IsRedTicket ? 270 : 260;
                        break;
                    default:
                        departStationEndPosition = IsRedTicket 
                            ? 50 + (35 * (DepartStationName?.Length ?? 0))
                            : 40 + (35 * (DepartStationName?.Length ?? 0));
                        break;
                }

                // 到达站位置固定但根据票颜色调整
                double arriveStationStartPosition = IsRedTicket ? 540 : 530;

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
        }

        public event EventHandler RequestClose;

        protected virtual void OnRequestClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}