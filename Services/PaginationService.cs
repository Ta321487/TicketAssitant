using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.ViewModels;

namespace TA_WPF.Services
{
    /// <summary>
    /// 分页服务，处理分页相关的逻辑
    /// </summary>
    public class PaginationService : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly Action _updateDataCallback;
        
        private int _currentPage = 1;
        private int _totalPages;
        private int _totalItems;
        private int _pageSize = 25;
        private bool _isLoading;
        private Dictionary<int, List<TrainRideInfo>> _pageCache = new Dictionary<int, List<TrainRideInfo>>();
        private int _cachePageSize = 0;

        /// <summary>
        /// 初始化分页服务
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="updateDataCallback">数据更新回调</param>
        public PaginationService(DatabaseService databaseService, Action updateDataCallback)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _updateDataCallback = updateDataCallback ?? throw new ArgumentNullException(nameof(updateDataCallback));
            
            // 初始化命令
            FirstPageCommand = new RelayCommand(FirstPage, () => CanNavigateToFirstPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CanNavigateToPreviousPage);
            NextPageCommand = new RelayCommand(NextPage, () => CanNavigateToNextPage);
            LastPageCommand = new RelayCommand(LastPage, () => CanNavigateToLastPage);
            
            // 初始化页大小选项
            PageSizeOptions = new[] { 25, 50, 75, 100 };
        }

        #region 属性

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value && value > 0)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    
                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                    
                    // 更新页面数据
                    _updateDataCallback?.Invoke();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged();
                    
                    // 通知命令状态可能已更改
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnPropertyChanged();
                    
                    // 重新计算总页数
                    CalculateTotalPages();
                }
            }
        }

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged();
                    
                    // 清除缓存，因为页大小变了
                    _pageCache.Clear();
                    _cachePageSize = value;
                    
                    // 重新计算总页数
                    CalculateTotalPages();
                    
                    // 确保当前页在有效范围内
                    if (CurrentPage > TotalPages)
                    {
                        CurrentPage = TotalPages;
                    }
                    
                    // 更新页面数据
                    _updateDataCallback?.Invoke();
                }
            }
        }

        /// <summary>
        /// 页大小选项
        /// </summary>
        public int[] PageSizeOptions { get; }

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
                    OnPropertyChanged();
                    
                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                }
            }
        }

        /// <summary>
        /// 是否可以导航到第一页
        /// </summary>
        public bool CanNavigateToFirstPage => CurrentPage > 1 && !IsLoading;

        /// <summary>
        /// 是否可以导航到上一页
        /// </summary>
        public bool CanNavigateToPreviousPage => CurrentPage > 1 && !IsLoading;

        /// <summary>
        /// 是否可以导航到下一页
        /// </summary>
        public bool CanNavigateToNextPage => CurrentPage < TotalPages && !IsLoading;

        /// <summary>
        /// 是否可以导航到最后一页
        /// </summary>
        public bool CanNavigateToLastPage => CurrentPage < TotalPages && !IsLoading;

        #endregion

        #region 命令

        /// <summary>
        /// 首页命令
        /// </summary>
        public ICommand FirstPageCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand { get; }

        /// <summary>
        /// 最后一页命令
        /// </summary>
        public ICommand LastPageCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 计算总页数
        /// </summary>
        private void CalculateTotalPages()
        {
            if (PageSize <= 0)
            {
                TotalPages = 1;
                return;
            }

            int pages = TotalItems / PageSize;
            if (TotalItems % PageSize > 0)
            {
                pages++;
            }

            TotalPages = Math.Max(1, pages);
            
            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        /// <summary>
        /// 导航到第一页
        /// </summary>
        private void FirstPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// 导航到上一页
        /// </summary>
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 导航到下一页
        /// </summary>
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 导航到最后一页
        /// </summary>
        private void LastPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        /// <summary>
        /// 重置分页
        /// </summary>
        public void Reset()
        {
            CurrentPage = 1;
            _pageCache.Clear();
        }

        /// <summary>
        /// 异步加载页面数据
        /// </summary>
        /// <returns>当前页的数据</returns>
        public async Task<List<TrainRideInfo>> LoadPageDataAsync()
        {
            try
            {
                // 设置加载状态
                IsLoading = true;
                
                // 检测缓存中是否已有当前页数据，且页大小未变
                if (_pageCache.ContainsKey(CurrentPage) && _cachePageSize == PageSize)
                {
                    // 从缓存返回数据
                    return _pageCache[CurrentPage];
                }
                
                // 直接从数据库加载当前页的数据
                var pageData = await _databaseService.GetPagedTrainRideInfosAsync(CurrentPage, PageSize);
                
                // 更新缓存
                _pageCache[CurrentPage] = new List<TrainRideInfo>(pageData);
                _cachePageSize = PageSize;
                
                return pageData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载页面数据时出错: {ex.Message}");
                throw;
            }
            finally
            {
                // 结束加载状态
                IsLoading = false;
            }
        }

        /// <summary>
        /// 异步获取总记录数
        /// </summary>
        public async Task UpdateTotalItemsAsync()
        {
            try
            {
                TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取总记录数时出错: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 