using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using TA_WPF.Models;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 分页视图模型，负责管理分页相关的数据和操作
    /// </summary>
    public class PaginationViewModel : INotifyPropertyChanged
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
                    
                    if (_isInitialized)
                    {
                        // 使用Dispatcher延迟触发页面变更事件，减少UI闪烁
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => PageChanged?.Invoke(this, EventArgs.Empty)));
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
                    // 设置加载状态
                    IsLoading = true;
                    
                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));
                    
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
        /// 是否正在加载数据
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
                    
                    // 不再触发导航按钮状态更新，允许在加载过程中点击分页按钮
                    // OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    // OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    // OnPropertyChanged(nameof(CanNavigateToNextPage));
                    // OnPropertyChanged(nameof(CanNavigateToLastPage));
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
        /// 页码变更事件
        /// </summary>
        public event EventHandler PageChanged;

        /// <summary>
        /// 页大小变更事件
        /// </summary>
        public event EventHandler PageSizeChanged;

        /// <summary>
        /// 导航到首页
        /// </summary>
        private void FirstPage()
        {
            if (CanNavigateToFirstPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;
                
                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    CurrentPage = 1;
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
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    CurrentPage--;
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
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    CurrentPage++;
                }));
            }
        }

        /// <summary>
        /// 导航到末页
        /// </summary>
        private void LastPage()
        {
            if (CanNavigateToLastPage)
            {
                // 先设置加载状态，确保UI立即响应
                IsLoading = true;
                
                // 使用Dispatcher确保加载状态能够立即更新到UI
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    CurrentPage = TotalPages;
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
            
            // 通知UI更新
            OnPropertyChanged(nameof(TotalPages));
            
            // 通知导航按钮状态可能已更改
            OnPropertyChanged(nameof(CanNavigateToNextPage));
            OnPropertyChanged(nameof(CanNavigateToLastPage));
            
            // 通知命令状态可能已更改
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _pageCache.Clear();
            _cachePageSize = 0;
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
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 