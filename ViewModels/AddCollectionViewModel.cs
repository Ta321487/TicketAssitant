using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.IO;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 添加收藏夹视图模型
    /// </summary>
    public class AddCollectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private string _collectionName;
        private string _description;
        private byte[] _coverImage;
        private string _coverImagePath;
        private int _importance;
        private bool _isLoading;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public AddCollectionViewModel(DatabaseService databaseService = null)
        {
            _databaseService = databaseService;
            _importance = 3; // 默认重要性为3星
            
            // 记录初始化的评分值
            Debug.WriteLine($"AddCollectionViewModel初始化时的评分值: {_importance}");
            
            // 初始化命令
            CreateCommand = new RelayCommand(CreateCollection, CanCreateCollection);
            CancelCommand = new RelayCommand(CancelOperation);
            BrowseImageCommand = new RelayCommand(BrowseImage);

            // 尝试加载默认图片
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pic", "blueTicket.png");
                if (File.Exists(imagePath))
                {
                    // 读取并处理默认图片
                    byte[] processedImage = LoadAndResizeImage(imagePath, 200, 100);
                    
                    if (processedImage != null && processedImage.Length > 0)
                    {
                    CoverImagePath = imagePath;
                        CoverImage = processedImage;
                        
                        // 通知UI更新按钮状态
                        OnPropertyChanged(nameof(IsValid));
                        CommandManager.InvalidateRequerySuggested();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载默认图片失败: {ex.Message}");
                Debug.WriteLine($"加载默认图片失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 收藏夹名称
        /// </summary>
        public string CollectionName
        {
            get => _collectionName;
            set
            {
                if (_collectionName != value)
                {
                    _collectionName = value;
                    OnPropertyChanged(nameof(CollectionName));
                    OnPropertyChanged(nameof(IsValid));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 收藏夹描述
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
            ? System.IO.Path.GetFileName(CoverImagePath) 
            : "暂未选择图片";

        /// <summary>
        /// 是否有封面图片
        /// </summary>
        public bool HasCoverImage => CoverImage != null && CoverImage.Length > 0;

        /// <summary>
        /// 重要性评分(1-5)
        /// </summary>
        public int Importance
        {
            get => _importance;
            set
            {
                if (_importance != value)
                {
                    Debug.WriteLine($"评分值从 {_importance} 改变为 {value}");
                    _importance = value;
                    OnPropertyChanged(nameof(Importance));
                    
                    // 确保UI能感知到评分变更
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 输入是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(CollectionName) && HasCoverImage;

        /// <summary>
        /// 创建收藏夹命令
        /// </summary>
        public ICommand CreateCommand { get; }

        /// <summary>
        /// 取消操作命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 浏览图片命令
        /// </summary>
        public ICommand BrowseImageCommand { get; }

        /// <summary>
        /// 创建收藏夹方法
        /// </summary>
        private async void CreateCollection()
        {
            try
            {
                // 验证用户输入
                if (string.IsNullOrWhiteSpace(CollectionName))
                {
                    MessageBoxHelper.ShowError("收藏夹名称不能为空");
                    return;
                }
                
                if (!HasCoverImage || CoverImage == null || CoverImage.Length == 0)
                {
                    MessageBoxHelper.ShowError("请选择封面图片");
                    return;
                }

                IsLoading = true;

                // 检查数据库服务是否初始化
                if (_databaseService == null)
                {
                    Debug.WriteLine("数据库服务未初始化，无法保存收藏夹");
                    MessageBoxHelper.ShowError("保存失败：数据库服务未初始化");
                    IsLoading = false;
                    return;
                }

                // 记录重要性评分值，确保评分值正确传递
                Debug.WriteLine($"创建收藏夹时的评分值: {Importance}");

                // 检查收藏夹名称是否已存在，并添加适当的后缀
                string originalName = CollectionName.Trim();
                string uniqueName = await GenerateUniqueCollectionNameAsync(originalName);

                // 创建收藏夹对象
                var collection = new TicketCollectionInfo
                {
                    CollectionName = uniqueName, // 使用可能已修改的唯一名称
                    Description = Description,
                    CoverImage = CoverImage,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    SortOrder = 0, // 默认排序顺序
                    Importance = Importance // 使用用户设置的评分
                };

                // 保存到数据库
                bool success = await _databaseService.AddCollectionAsync(collection);

                if (success)
                {
                    // 如果名称被修改了，提示用户
                    if (uniqueName != originalName)
                    {
                        MessageBoxHelper.ShowInfo($"收藏夹创建成功，名称已自动调整为 \"{uniqueName}\"");
                }
                else
                {
                        MessageBoxHelper.ShowInfo("收藏夹创建成功");
                }

                // 关闭窗口
                CloseWindow();
                }
                else
                {
                    MessageBoxHelper.ShowError("收藏夹创建失败，请重试");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建收藏夹异常: {ex.Message}");
                LogHelper.LogError($"创建收藏夹失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"创建收藏夹失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 生成唯一的收藏夹名称
        /// </summary>
        /// <param name="baseName">用户输入的基本名称</param>
        /// <returns>确保唯一的收藏夹名称</returns>
        private async Task<string> GenerateUniqueCollectionNameAsync(string baseName)
        {
            try
            {
                // 首先检查原始名称是否已存在
                var existingCollection = await _databaseService.GetCollectionByNameAsync(baseName);
                if (existingCollection == null)
                {
                    // 如果不存在相同名称的收藏夹，直接使用原始名称
                    return baseName;
                }

                // 获取所有以该名称为基础的收藏夹（如"11"、"11(1)"、"11(2)"等）
                var similarNames = await _databaseService.GetCollectionNamesByBaseNameAsync(baseName);
                
                // 找出最大后缀编号
                int maxSuffix = 0;
                foreach (var name in similarNames)
                {
                    // 使用正则表达式提取括号中的数字（如"11(3)"中的3）
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"\((\d+)\)$");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int suffix))
                    {
                        maxSuffix = Math.Max(maxSuffix, suffix);
                    }
                    else if (name == baseName)
                    {
                        // 原始名称已存在，至少需要从(1)开始
                        maxSuffix = Math.Max(maxSuffix, 0);
                    }
                }

                // 生成新名称，编号+1
                return $"{baseName}({maxSuffix + 1})";
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"生成唯一收藏夹名称失败: {ex.Message}", ex);
                Debug.WriteLine($"生成唯一收藏夹名称失败: {ex.Message}");
                
                // 出现异常时，为安全起见，添加一个时间戳后缀
                string timestamp = DateTime.Now.ToString("HHmmss");
                return $"{baseName}({timestamp})";
            }
        }

        /// <summary>
        /// 是否可以创建收藏夹
        /// </summary>
        private bool CanCreateCollection()
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
                
                // 尝试使用备用方法
                try 
                {
                    // 如果转换失败，尝试直接压缩原图
                    return CompressImageQuality(File.ReadAllBytes(imagePath), 50);
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"压缩图片失败: {innerEx.Message}");
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
    }
} 