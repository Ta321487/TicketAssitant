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
    /// 修改收藏夹视图模型
    /// </summary>
    public class EditCollectionViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private readonly TicketCollectionInfo _originalCollection; // 原始收藏夹信息
        private string _collectionName;
        private string _description;
        private byte[] _coverImage;
        private string _coverImagePath;
        private int _importance;
        private bool _isLoading;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">要编辑的收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public EditCollectionViewModel(TicketCollectionInfo collection, DatabaseService databaseService = null, MainViewModel mainViewModel = null)
        {
            _databaseService = databaseService;
            _mainViewModel = mainViewModel;
            _originalCollection = collection;
            
            // 初始化属性
            CollectionName = collection.CollectionName;
            Description = collection.Description;
            CoverImage = collection.CoverImage;
            Importance = collection.Importance;
            
            // 初始化命令
            SaveCommand = new RelayCommand(SaveCollection, CanSaveCollection);
            CancelCommand = new RelayCommand(CancelOperation);
            BrowseImageCommand = new RelayCommand(BrowseImage);
            
            Debug.WriteLine($"初始化编辑收藏夹视图模型，评分值: {collection.Importance}");
        }

        /// <summary>
        /// 主视图模型，用于访问全局设置（如字号）
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

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
            : "暂未选择新图片";

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
        /// 保存收藏夹命令
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
        /// 保存收藏夹方法
        /// </summary>
        private async void SaveCollection()
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
                Debug.WriteLine($"修改收藏夹时的评分值: {Importance}");

                // 检查收藏夹名称是否发生变更，若变更则检查是否有重名
                string originalName = CollectionName.Trim();
                string uniqueName = originalName;
                
                // 只有当名称变更时才需要检查重名
                if (originalName != _originalCollection.CollectionName)
                {
                    uniqueName = await GenerateUniqueCollectionNameAsync(originalName);
                }

                // 更新收藏夹对象
                var updatedCollection = new TicketCollectionInfo
                {
                    Id = _originalCollection.Id, // 保持原有ID
                    CollectionName = uniqueName, // 使用可能已修改的唯一名称
                    Description = Description,
                    CoverImage = CoverImage,
                    CreateTime = _originalCollection.CreateTime, // 保持原有创建时间
                    UpdateTime = DateTime.Now, // 更新修改时间
                    SortOrder = _originalCollection.SortOrder, // 保持原有排序顺序
                    Importance = Importance, // 使用用户设置的评分
                    TicketCount = _originalCollection.TicketCount // 保持原有票数
                };

                // 保存到数据库
                bool success = await _databaseService.UpdateCollectionAsync(updatedCollection);

                if (success)
                {
                    // 如果名称被修改了，提示用户
                    if (uniqueName != originalName)
                    {
                        MessageBoxHelper.ShowInfo($"收藏夹修改成功，名称已自动调整为 \"{uniqueName}\"");
                    }
                    else
                    {
                        MessageBoxHelper.ShowInfo("收藏夹修改成功");
                    }

                    // 关闭窗口
                    CloseWindow();
                }
                else
                {
                    MessageBoxHelper.ShowError("收藏夹修改失败，请重试");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"修改收藏夹异常: {ex.Message}");
                LogHelper.LogError($"修改收藏夹失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"修改收藏夹失败: {ex.Message}");
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
                // 首先检查原名称是否存在（排除当前正在编辑的收藏夹）
                var existingCollection = await _databaseService.GetCollectionByNameAsync(baseName);
                if (existingCollection == null || existingCollection.Id == _originalCollection.Id)
                {
                    // 名称不存在或者就是自己，可以直接使用
                    return baseName;
                }

                // 名称已存在，需要添加后缀
                var existingNames = await _databaseService.GetCollectionNamesByBaseNameAsync(baseName);
                int suffix = 1;

                // 寻找可用的后缀
                string newName;
                do
                {
                    newName = $"{baseName}({suffix})";
                    suffix++;
                } while (existingNames.Contains(newName));

                return newName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"生成唯一收藏夹名称失败: {ex.Message}");
                LogHelper.LogError($"生成唯一收藏夹名称失败: {ex.Message}", ex);
                
                // 发生错误时直接返回原名称
                return baseName;
            }
        }

        /// <summary>
        /// 是否可以保存收藏夹
        /// </summary>
        private bool CanSaveCollection()
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
                    Debug.WriteLine($"图片处理异常: {ex.Message}");
                    LogHelper.LogError($"图片处理异常: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"图片处理失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 加载并调整图片尺寸
        /// </summary>
        /// <param name="filePath">图片文件路径</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="quality">压缩质量</param>
        /// <returns>处理后的图片数据</returns>
        private byte[] LoadAndResizeImage(string filePath, int maxWidth, int quality)
        {
            try
            {
                // 读取原始图片
                BitmapImage originalImage = new BitmapImage();
                originalImage.BeginInit();
                originalImage.UriSource = new Uri(filePath);
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.EndInit();

                // 计算新尺寸，保持宽高比
                double scale = (double)maxWidth / originalImage.PixelWidth;
                int newWidth = maxWidth;
                int newHeight = (int)(originalImage.PixelHeight * scale);

                // 创建调整大小后的图片
                var resizedImage = new TransformedBitmap(
                    originalImage,
                    new ScaleTransform(scale, scale)
                );

                // 将图片编码为JPEG并返回字节数组
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(BitmapFrame.Create(resizedImage));

                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理图片失败: {ex.Message}");
                LogHelper.LogError($"处理图片失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            // 获取当前窗口并设置对话框结果
            if (Application.Current?.Windows != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.DialogResult = true;
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
} 