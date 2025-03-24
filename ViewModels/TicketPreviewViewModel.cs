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
            
            // 获取当前主题模式
            LoadThemeSettings();
            
            // 获取当前字体大小设置
            LoadFontSizeSettings();
            
            // 订阅主题服务的主题变更事件
            Services.ThemeService.Instance.ThemeChanged += (sender, isDark) => 
            {
                IsDarkMode = isDark;
            };
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

        public ICommand CloseCommand { get; }
        public ICommand ExportImageCommand { get; }

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

        public event EventHandler RequestClose;

        protected virtual void OnRequestClose()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}