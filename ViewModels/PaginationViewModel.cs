using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TA_WPF.Models;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 分页视图模型，负责管理分页相关的数据和操作
    /// </summary>
    public class PaginationViewModel : BaseViewModel
    {
        private int _currentPage = 1;
        private int _totalPages;
        private int _totalItems;
        private int _pageSize = 25;
        private bool _isLoading;
        private Dictionary<int, List<TrainRideInfo>> _pageCache = new Dictionary<int, List<TrainRideInfo>>();
        private int _cachePageSize = 0;
        private ObservableCollection<TrainRideInfo> _items = new ObservableCollection<TrainRideInfo>();
        private bool _isInitialized = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PaginationViewModel()
        {
            // 初始化命令
            FirstPageCommand = new RelayCommand(FirstPage, () => CanNavigateToFirstPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, () => CanNavigateToPreviousPage);
            NextPageCommand = new RelayCommand(NextPage, () => CanNavigateToNextPage);
            LastPageCommand = new RelayCommand(LastPage, () => CanNavigateToLastPage);

            // 初始化页大小选项
            PageSizeOptions = new[] { 25, 50, 75, 100 };
        }

        /// <summary>
        /// 尝试获取缓存的页面数据
        /// </summary>
        /// <param name="cachedItems">输出缓存的数据项</param>
        /// <returns>如果成功获取到缓存数据返回true，否则返回false</returns>
        public bool TryGetCachedPage(out List<TrainRideInfo> cachedItems)
        {
            // 检测缓存中是否已有当前页数据，且页大小未变
            if (_pageCache.ContainsKey(_currentPage) && _cachePageSize == _pageSize)
            {
                // 从缓存加载数据
                cachedItems = _pageCache[_currentPage];
                return true;
            }

            cachedItems = null;
            return false;
        }

        /// <summary>
        /// 更新页面缓存
        /// </summary>
        /// <param name="items">要缓存的数据项</param>
        public void UpdateCache(List<TrainRideInfo> items)
        {
            // 更新缓存
            _pageCache[_currentPage] = items;
            _cachePageSize = _pageSize;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _pageCache.Clear();

            // 在清除缓存后，确保UI状态与缓存状态一致
            // 异步执行以避免阻塞主线程
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                // 刷新导航按钮状态
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));

                // 刷新命令状态
                CommandManager.InvalidateRequerySuggested();
            }));
        }

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
                    OnPropertyChanged(nameof(CurrentPage));

                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));

                    // 强制刷新命令状态
                    CommandManager.InvalidateRequerySuggested();

                    if (_isInitialized)
                    {
                        // 使用Dispatcher以较高优先级触发页面变更事件
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                // 再次刷新按钮状态
                                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                                OnPropertyChanged(nameof(CanNavigateToNextPage));
                                OnPropertyChanged(nameof(CanNavigateToLastPage));

                                // 再次刷新命令状态
                                CommandManager.InvalidateRequerySuggested();

                                // 触发页面变更事件
                                PageChanged?.Invoke(this, EventArgs.Empty);
                            }));
                    }
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged(nameof(TotalPages));

                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));

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
                    OnPropertyChanged(nameof(TotalItems));

                    // 重新计算总页数
                    CalculateTotalPages();

                    // 通知UI更新相关属性
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));

                    // 通知命令状态可能已更改
                    CommandManager.InvalidateRequerySuggested();
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
                    // 记录旧的页码和总页数
                    int oldCurrentPage = _currentPage;
                    int oldTotalPages = _totalPages;

                    // 设置加载状态
                    IsLoading = true;

                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));

                    // 清除缓存，因为页大小变了
                    _pageCache.Clear();
                    _cachePageSize = value;

                    // 重新计算总页数
                    CalculateTotalPages();

                    // 计算新的当前页，尽量保持在相同的数据区域
                    // 例如：如果在第2页，每页25条，现在改为每页50条，应该保持在第1页
                    if (oldTotalPages > 0 && _totalPages > 0)
                    {
                        // 计算当前页在总数据中的位置比例
                        double positionRatio = (double)(oldCurrentPage - 1) / oldTotalPages;
                        // 根据比例计算新的页码
                        int newPage = Math.Max(1, (int)Math.Ceiling(positionRatio * _totalPages) + 1);
                        // 确保不超过总页数
                        newPage = Math.Min(newPage, _totalPages);
                        // 设置新的当前页
                        _currentPage = newPage;
                    }
                    else
                    {
                        // 如果没有足够信息计算，则设为第一页
                        _currentPage = 1;
                    }

                    // 通知UI更新相关属性
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(CurrentPage));

                    // 通知导航按钮状态可能已更改
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));

                    // 通知命令状态可能已更改
                    CommandManager.InvalidateRequerySuggested();

                    // 通知页大小已更改
                    PageSizeChanged?.Invoke(this, EventArgs.Empty);
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
                    OnPropertyChanged(nameof(IsLoading));

                    // 注意：我们不再在这里更新导航按钮状态
                    // 因为我们修改了导航按钮的启用条件

                    // 刷新命令状态
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 当前页的数据项
        /// </summary>
        public ObservableCollection<TrainRideInfo> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized
        {
            get => _isInitialized;
            set
            {
                if (_isInitialized != value)
                {
                    _isInitialized = value;
                    OnPropertyChanged(nameof(IsInitialized));
                }
            }
        }

        /// <summary>
        /// 页面缓存
        /// </summary>
        public Dictionary<int, List<TrainRideInfo>> PageCache
        {
            get => _pageCache;
            set
            {
                _pageCache = value;
                OnPropertyChanged(nameof(PageCache));
            }
        }

        /// <summary>
        /// 缓存的页大小
        /// </summary>
        public int CachePageSize
        {
            get => _cachePageSize;
            set
            {
                _cachePageSize = value;
                OnPropertyChanged(nameof(CachePageSize));
            }
        }

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
        /// 末页命令
        /// </summary>
        public ICommand LastPageCommand { get; }

        /// <summary>
        /// 是否可以导航到首页
        /// </summary>
        public bool CanNavigateToFirstPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以导航到上一页
        /// </summary>
        public bool CanNavigateToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以导航到下一页
        /// </summary>
        public bool CanNavigateToNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 是否可以导航到末页
        /// </summary>
        public bool CanNavigateToLastPage => CurrentPage < TotalPages;

        /// <summary>
        /// 页面变化事件
        /// </summary>
        public event EventHandler PageChanged;

        /// <summary>
        /// 页大小变更事件
        /// </summary>
        public event EventHandler PageSizeChanged;

        /// <summary>
        /// 导航到第一页
        /// </summary>
        private void FirstPage()
        {
            if (CanNavigateToFirstPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;

                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    CurrentPage = 1;

                    // 强制刷新命令状态
                    CommandManager.InvalidateRequerySuggested();

                    // 触发页面变更事件，确保数据被刷新
                    PageChanged?.Invoke(this, EventArgs.Empty);
                }));
            }
        }

        /// <summary>
        /// 导航到上一页
        /// </summary>
        private void PreviousPage()
        {
            if (CanNavigateToPreviousPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;

                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    CurrentPage--;

                    // 强制刷新命令状态
                    CommandManager.InvalidateRequerySuggested();

                    // 触发页面变更事件，确保数据被刷新
                    PageChanged?.Invoke(this, EventArgs.Empty);
                }));
            }
        }

        /// <summary>
        /// 导航到下一页
        /// </summary>
        private void NextPage()
        {
            if (CanNavigateToNextPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;

                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    CurrentPage++;

                    // 强制刷新命令状态
                    CommandManager.InvalidateRequerySuggested();

                    // 触发页面变更事件，确保数据被刷新
                    PageChanged?.Invoke(this, EventArgs.Empty);
                }));
            }
        }

        /// <summary>
        /// 导航到最后一页
        /// </summary>
        private void LastPage()
        {
            if (CanNavigateToLastPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;

                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    CurrentPage = TotalPages;

                    // 强制刷新命令状态
                    CommandManager.InvalidateRequerySuggested();

                    // 触发页面变更事件，确保数据被刷新
                    PageChanged?.Invoke(this, EventArgs.Empty);
                }));
            }
        }

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

            // 确保TotalItems不为负数
            int itemCount = Math.Max(0, TotalItems);

            // 计算总页数
            int pages = itemCount / PageSize;
            if (itemCount % PageSize > 0)
            {
                pages++;
            }

            // 确保至少有一页
            TotalPages = Math.Max(1, pages);

            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // 通知UI更新
            OnPropertyChanged(nameof(TotalPages));

            // 通知导航按钮状态可能已更改
            OnPropertyChanged(nameof(CanNavigateToFirstPage));
            OnPropertyChanged(nameof(CanNavigateToPreviousPage));
            OnPropertyChanged(nameof(CanNavigateToNextPage));
            OnPropertyChanged(nameof(CanNavigateToLastPage));

            // 通知命令状态可能已更改
            CommandManager.InvalidateRequerySuggested();

            // 使用Dispatcher确保UI状态更新
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                // 再次刷新状态，确保UI完全更新
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));

                // 再次刷新命令状态
                CommandManager.InvalidateRequerySuggested();
            }));
        }

        /// <summary>
        /// 重置分页
        /// </summary>
        public void Reset()
        {
            CurrentPage = 1;
            TotalItems = 0;
            TotalPages = 1;
            IsInitialized = false;
            ClearCache();
            Items.Clear();
        }

        /// <summary>
        /// 刷新总记录数
        /// </summary>
        /// <returns>异步任务</returns>
        public virtual async Task RefreshTotalItemsAsync()
        {
            // 子类需要重写此方法以实现具体的刷新逻辑
            await Task.CompletedTask;
        }
    }
}