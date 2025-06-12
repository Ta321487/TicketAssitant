using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 编辑路线视图模型
    /// </summary>
    public class EditRouteViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private RouteInfo _originalRoute;
        private int _routeId;
        private string _routeName;
        private string _description;
        private byte[] _coverImage;
        private string _coverImagePath;
        private string _totalDistance;
        private bool _isFavorite;
        private bool _isLoading;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="route">要编辑的路线</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public EditRouteViewModel(RouteInfo route, DatabaseService databaseService = null, MainViewModel mainViewModel = null)
        {
            if (route == null)
            {
                MessageBoxHelper.ShowError("路线对象为空，无法编辑");
                return;
            }

            _databaseService = databaseService;
            _mainViewModel = mainViewModel;
            _originalRoute = route;

            // 初始化路线数据
            _routeId = route.Id;
            _routeName = route.RouteName;
            _description = route.Description;

            // 添加调试输出跟踪图片数据
            Debug.WriteLine($"EditRouteViewModel初始化: 路线ID={_routeId}, 名称={_routeName}");
            Debug.WriteLine($"原始路线中的图片数据: {(route.CoverImage != null ? $"{route.CoverImage.Length}字节" : "空")}");

            // 确保正确初始化图片数据
            _coverImage = route.CoverImage;
            _isFavorite = route.IsFavorite;
            _totalDistance = route.TotalDistance.ToString();

            // 添加调试输出确认图片数据已被赋值
            Debug.WriteLine($"ViewModel中的图片数据: {(_coverImage != null ? $"{_coverImage.Length}字节" : "空")}");
            Debug.WriteLine($"HasCoverImage值: {HasCoverImage}");

            // 初始化命令
            SaveCommand = new RelayCommand(SaveRoute, CanSaveRoute);
            CancelCommand = new RelayCommand(CancelOperation);
            BrowseImageCommand = new RelayCommand(BrowseImage);

            // 通知UI更新所有相关属性
            OnPropertyChanged(nameof(CoverImage));
            OnPropertyChanged(nameof(HasCoverImage));
            OnPropertyChanged(nameof(CoverImageFileName));
            OnPropertyChanged(nameof(IsValid));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 主视图模型，用于访问全局设置（如字号）
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 路线ID
        /// </summary>
        public int RouteId => _routeId;

        /// <summary>
        /// 路线名称
        /// </summary>
        public string RouteName
        {
            get => _routeName;
            set
            {
                if (_routeName != value)
                {
                    _routeName = value;
                    OnPropertyChanged(nameof(RouteName));
                    OnPropertyChanged(nameof(IsValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 路线描述
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// 总里程
        /// </summary>
        public string TotalDistance
        {
            get => _totalDistance;
            set
            {
                if (_totalDistance != value)
                {
                    _totalDistance = value;
                    OnPropertyChanged(nameof(TotalDistance));
                    OnPropertyChanged(nameof(IsValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
        }

        /// <summary>
        /// 封面图片
        /// </summary>
        public byte[] CoverImage
        {
            get => _coverImage;
            set
            {
                if (_coverImage != value)
                {
                    _coverImage = value;
                    OnPropertyChanged(nameof(CoverImage));
                    OnPropertyChanged(nameof(HasCoverImage));
                    OnPropertyChanged(nameof(IsValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 封面图片路径
        /// </summary>
        public string CoverImagePath
        {
            get => _coverImagePath;
            set
            {
                if (_coverImagePath != value)
                {
                    _coverImagePath = value;
                    OnPropertyChanged(nameof(CoverImagePath));
                    OnPropertyChanged(nameof(HasCoverImage));
                    OnPropertyChanged(nameof(CoverImageFileName));
                }
            }
        }

        /// <summary>
        /// 封面图片文件名
        /// </summary>
        public string CoverImageFileName => !string.IsNullOrEmpty(CoverImagePath)
            ? Path.GetFileName(CoverImagePath)
            : (HasCoverImage ? "原始封面图片" : "暂未选择图片");

        /// <summary>
        /// 是否有封面图片
        /// </summary>
        public bool HasCoverImage => CoverImage != null && CoverImage.Length > 0;

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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 输入是否有效
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteName) &&
            HasCoverImage &&
            !string.IsNullOrWhiteSpace(TotalDistance) &&
            decimal.TryParse(TotalDistance, out _);

        /// <summary>
        /// 保存路线命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 取消操作命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 浏览图片命令
        /// </summary>
        public ICommand BrowseImageCommand { get; }

        /// <summary>
        /// 保存路线方法
        /// </summary>
        private async void SaveRoute()
        {
            try
            {
                // 验证用户输入
                if (string.IsNullOrWhiteSpace(RouteName))
                {
                    MessageBoxHelper.ShowError("路线名称不能为空");
                    return;
                }

                if (!HasCoverImage || CoverImage == null || CoverImage.Length == 0)
                {
                    MessageBoxHelper.ShowError("请选择封面图片");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TotalDistance))
                {
                    MessageBoxHelper.ShowError("请输入总里程");
                    return;
                }

                if (!decimal.TryParse(TotalDistance, out decimal totalDistanceValue))
                {
                    MessageBoxHelper.ShowError("总里程必须是有效的数值");
                    return;
                }

                IsLoading = true;

                // 检查数据库服务是否初始化
                if (_databaseService == null)
                {
                    Debug.WriteLine("数据库服务未初始化，无法保存路线");
                    MessageBoxHelper.ShowError("保存失败：数据库服务未初始化");
                    LogHelper.LogSystemError("EditRouteViewModel", "数据库服务未初始化，无法保存路线");
                    IsLoading = false;
                    return;
                }

                // 更新原始路线对象
                _originalRoute.RouteName = RouteName.Trim();
                _originalRoute.Description = Description;
                _originalRoute.CoverImage = CoverImage;
                _originalRoute.UpdateTime = DateTime.Now;
                _originalRoute.TotalDistance = totalDistanceValue;
                _originalRoute.IsFavorite = IsFavorite;

                // 保存到数据库
                bool success = await _databaseService.UpdateRouteAsync(_originalRoute);

                if (success)
                {
                    MessageBoxHelper.ShowInfo("路线保存成功");

                    // 关闭窗口
                    CloseWindow();
                }
                else
                {
                    MessageBoxHelper.ShowError("路线保存失败，请重试");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存路线异常: {ex.Message}");
                LogHelper.LogError($"保存路线失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"保存路线失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 是否可以保存路线
        /// </summary>
        private bool CanSaveRoute()
        {
            return IsValid && !IsLoading;
        }

        /// <summary>
        /// 取消操作方法
        /// </summary>
        private void CancelOperation()
        {
            // 关闭窗口
            CloseWindow();
        }

        /// <summary>
        /// 浏览图片方法
        /// </summary>
        private void BrowseImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
                Title = "选择封面图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 读取文件路径
                    string filePath = openFileDialog.FileName;

                    // 处理图片：调整尺寸和压缩
                    byte[] processedImage = LoadAndResizeImage(filePath, 200, 100);

                    if (processedImage != null && processedImage.Length > 0)
                    {
                        // 保存路径和处理后的图片数据
                        CoverImagePath = filePath;
                        CoverImage = processedImage;

                        // 通知UI更新按钮状态
                        OnPropertyChanged(nameof(IsValid));
                        CommandManager.InvalidateRequerySuggested();
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("图片处理失败，请选择其他图片");
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError($"读取或处理图片失败: {ex.Message}");
                    Debug.WriteLine($"图片处理异常: {ex.Message}");
                    LogHelper.LogError($"图片处理异常: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 加载并调整图片尺寸
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <returns>处理后的图片字节数组</returns>
        private byte[] LoadAndResizeImage(string imagePath, int maxWidth, int maxHeight)
        {
            try
            {
                // 创建位图
                BitmapImage originalImage = new BitmapImage();
                originalImage.BeginInit();
                originalImage.UriSource = new Uri(imagePath);
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.EndInit();

                // 确定缩放比例
                double scaleX = (double)maxWidth / originalImage.PixelWidth;
                double scaleY = (double)maxHeight / originalImage.PixelHeight;
                double scale = Math.Min(scaleX, scaleY); // 等比缩放，取小的缩放比例

                // 如果图片比目标尺寸小，则不需要缩放
                if (scale >= 1.0 && originalImage.PixelWidth <= maxWidth && originalImage.PixelHeight <= maxHeight)
                {
                    // 直接使用原图，只压缩质量
                    return CompressImageQuality(File.ReadAllBytes(imagePath), 75);
                }

                // 计算缩放后的尺寸
                int newWidth = (int)(originalImage.PixelWidth * scale);
                int newHeight = (int)(originalImage.PixelHeight * scale);

                // 创建缩放后的位图
                TransformedBitmap transformedBitmap = new TransformedBitmap(
                    originalImage,
                    new ScaleTransform(scale, scale)
                );

                // 编码为JPEG
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 75; // 较好的质量
                encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));

                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"调整图片尺寸失败: {ex.Message}");
                LogHelper.LogError($"调整图片尺寸失败: {ex.Message}", ex);
                // 尝试使用备用方法
                try
                {
                    // 如果转换失败，尝试直接压缩原图
                    return CompressImageQuality(File.ReadAllBytes(imagePath), 50);
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"压缩图片失败: {innerEx.Message}");
                    LogHelper.LogError($"压缩图片失败: {innerEx.Message}", innerEx);
                    return null;
                }
            }
        }

        /// <summary>
        /// 压缩图片质量
        /// </summary>
        /// <param name="imageBytes">原始图片字节数组</param>
        /// <param name="quality">压缩质量(1-100)</param>
        /// <returns>压缩后的图片字节数组</returns>
        private byte[] CompressImageQuality(byte[] imageBytes, int quality)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return imageBytes;

            try
            {
                // 创建图片源
                BitmapImage bitmapImage = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // 重要：使位图可在线程间共享
                }

                // 转换为可写入的位图格式
                var jpegEncoder = new JpegBitmapEncoder();
                jpegEncoder.QualityLevel = quality;
                jpegEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                // 保存压缩后的图像
                using (var outputStream = new MemoryStream())
                {
                    jpegEncoder.Save(outputStream);
                    return outputStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"压缩图片异常: {ex.Message}");

                // 如果压缩失败但文件不大，返回原图
                if (imageBytes.Length <= 100 * 1024)
                {
                    return imageBytes;
                }

                return null; // 如果压缩失败且文件过大，返回null
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            // 获取当前窗口实例并关闭
            if (Application.Current.Windows.Count > 0)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.DialogResult = true;
                        window.Close();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // 释放较大的资源
                _coverImage = null;
                _coverImagePath = null;
                _originalRoute = null;
                
                Debug.WriteLine("EditRouteViewModel - 资源已清理");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EditRouteViewModel.Cleanup 异常: {ex.Message}");
            }
        }
    }
}