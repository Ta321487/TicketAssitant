using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.IO;

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

            // 尝试加载默认图片
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pic", "blueTicket.png");
                if (File.Exists(imagePath))
                {
                    CoverImage = File.ReadAllBytes(imagePath);
                    CoverImagePath = imagePath;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"加载默认图片失败: {ex.Message}");
            }

            // 初始化命令
            CreateCommand = new RelayCommand(CreateCollection, CanCreateCollection);
            CancelCommand = new RelayCommand(CancelOperation);
            BrowseImageCommand = new RelayCommand(BrowseImage);
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
                    _importance = value;
                    OnPropertyChanged(nameof(Importance));
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
        public bool IsValid => !string.IsNullOrWhiteSpace(CollectionName);

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
        private void CreateCollection()
        {
            try
            {
                IsLoading = true;

                // 创建收藏夹对象
                var collection = new TicketCollectionInfo
                {
                    CollectionName = CollectionName,
                    Description = Description,
                    CoverImage = CoverImage,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    SortOrder = 0 // 默认排序顺序
                };

                // 这里只是UI展示，不实际保存到数据库
                // 后续实现时可添加如下代码：
                // await _databaseService.AddCollectionAsync(collection);

                MessageBoxHelper.ShowInfo("收藏夹创建成功");

                // 关闭窗口
                CloseWindow();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"创建收藏夹失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
                    // 读取文件
                    CoverImagePath = openFileDialog.FileName;
                    CoverImage = File.ReadAllBytes(CoverImagePath);
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError($"读取图片失败: {ex.Message}");
                }
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