using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    public class QueryAllTicketsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;

        public QueryAllTicketsViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // 订阅分页事件
            _paginationViewModel.PageChanged += OnPageChanged;
            _paginationViewModel.PageSizeChanged += OnPageSizeChanged;
            
            // 订阅MainViewModel的字体大小变更事件
            _mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.DataGridRowHeight))
                {
                    OnPropertyChanged(nameof(DataGridRowHeight));
                }
            };
        }

        /// <summary>
        /// 查询所有车票
        /// </summary>
        public async Task QueryAllAsync()
        {
            try
            {
                _paginationViewModel.IsLoading = true;
                
                // 获取总记录数
                int totalCount = await _databaseService.GetTotalTrainRideInfoCountAsync();
                
                // 设置总记录数，这会触发TotalPages的重新计算
                _paginationViewModel.TotalItems = totalCount;
                
                // 重置到第一页
                _paginationViewModel.CurrentPage = 1;
                
                // 加载第一页数据
                await LoadPageDataAsync();
                
                // 标记为已初始化
                _paginationViewModel.IsInitialized = true;
                
                // 显示数据表格
                _mainViewModel.ShowQueryAllTickets = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(TrainRideInfos));
                
                // 手动触发导航按钮状态更新
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 手动刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"查询数据时出错: {ex.Message}");
            }
            finally
            {
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 加载页面数据
        /// </summary>
        private async Task LoadPageDataAsync()
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 检查缓存中是否已有当前页数据，且页大小未变
                if (_paginationViewModel.PageCache.ContainsKey(_paginationViewModel.CurrentPage) && 
                    _paginationViewModel.CachePageSize == _paginationViewModel.PageSize)
                {
                    // 从缓存加载数据
                    var cachedItems = _paginationViewModel.PageCache[_paginationViewModel.CurrentPage];
                    
                    // 使用批量更新方式更新UI，减少闪烁
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        // 如果数量相同，尝试更新现有项而不是清空重建
                        if (_paginationViewModel.Items.Count == cachedItems.Count)
                        {
                            for (int i = 0; i < cachedItems.Count; i++)
                            {
                                if (!object.Equals(_paginationViewModel.Items[i], cachedItems[i]))
                                {
                                    _paginationViewModel.Items[i] = cachedItems[i];
                                }
                            }
                        }
                        else
                        {
                            // 数量不同时才清空并重新添加
                            _paginationViewModel.Items.Clear();
                            foreach (var item in cachedItems)
                            {
                                _paginationViewModel.Items.Add(item);
                            }
                        }
                    });
                    
                    // 从缓存加载时，不需要延迟，立即关闭加载状态
                    _paginationViewModel.IsLoading = false;
                    
                    // 手动触发属性变更通知，确保UI更新
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(CanNavigateToFirstPage));
                    OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                    OnPropertyChanged(nameof(CanNavigateToNextPage));
                    OnPropertyChanged(nameof(CanNavigateToLastPage));
                    
                    // 刷新命令状态
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                    
                    return;
                }
                
                // 直接从数据库加载当前页的数据
                var pageData = await _databaseService.GetPagedTrainRideInfosAsync(
                    _paginationViewModel.CurrentPage, 
                    _paginationViewModel.PageSize);
                
                // 更新缓存
                _paginationViewModel.PageCache[_paginationViewModel.CurrentPage] = pageData;
                _paginationViewModel.CachePageSize = _paginationViewModel.PageSize;
                
                // 使用批量更新方式更新UI，减少闪烁
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    // 如果数量相同，尝试更新现有项而不是清空重建
                    if (_paginationViewModel.Items.Count == pageData.Count)
                    {
                        for (int i = 0; i < pageData.Count; i++)
                        {
                            if (!object.Equals(_paginationViewModel.Items[i], pageData[i]))
                            {
                                _paginationViewModel.Items[i] = pageData[i];
                            }
                        }
                    }
                    else
                    {
                        // 数量不同时才清空并重新添加
                        _paginationViewModel.Items.Clear();
                        foreach (var item in pageData)
                        {
                            _paginationViewModel.Items.Add(item);
                        }
                    }
                });
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载页面数据时出错: {ex.Message}");
                MessageBoxHelper.ShowError($"加载页面数据时出错: {ex.Message}");
            }
            finally
            {
                // 结束加载状态
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// 页码变更事件处理
        /// </summary>
        private void OnPageChanged(object sender, EventArgs e)
        {
            // 确保加载状态已设置，这里不需要条件判断，因为我们希望每次页面变更都显示加载动画
            _paginationViewModel.IsLoading = true;
            
            // 直接加载页面数据，不需要额外的Dispatcher调用
            _ = LoadPageDataAsync();
        }

        /// <summary>
        /// 页大小变更事件处理
        /// </summary>
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageSize));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // 加载新的页面数据
                _ = LoadPageDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"页大小变更处理出错: {ex.Message}");
                // 确保加载状态被重置
                _paginationViewModel.IsLoading = false;
            }
        }

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _paginationViewModel.IsLoading;
            set => _paginationViewModel.IsLoading = value;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _paginationViewModel.CurrentPage;
            set => _paginationViewModel.CurrentPage = value;
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _paginationViewModel.TotalPages;

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalItems => _paginationViewModel.TotalItems;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize
        {
            get => _paginationViewModel.PageSize;
            set => _paginationViewModel.PageSize = value;
        }

        /// <summary>
        /// 页大小选项
        /// </summary>
        public int[] PageSizeOptions => _paginationViewModel.PageSizeOptions;

        /// <summary>
        /// 是否可以导航到首页
        /// </summary>
        public bool CanNavigateToFirstPage => _paginationViewModel.CanNavigateToFirstPage;

        /// <summary>
        /// 是否可以导航到上一页
        /// </summary>
        public bool CanNavigateToPreviousPage => _paginationViewModel.CanNavigateToPreviousPage;

        /// <summary>
        /// 是否可以导航到下一页
        /// </summary>
        public bool CanNavigateToNextPage => _paginationViewModel.CanNavigateToNextPage;

        /// <summary>
        /// 是否可以导航到末页
        /// </summary>
        public bool CanNavigateToLastPage => _paginationViewModel.CanNavigateToLastPage;

        /// <summary>
        /// 车票数据
        /// </summary>
        public ObservableCollection<TrainRideInfo> TrainRideInfos => _paginationViewModel.Items;

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;

        #endregion

        #region 命令

        /// <summary>
        /// 首页命令
        /// </summary>
        public System.Windows.Input.ICommand FirstPageCommand => _paginationViewModel.FirstPageCommand;

        /// <summary>
        /// 上一页命令
        /// </summary>
        public System.Windows.Input.ICommand PreviousPageCommand => _paginationViewModel.PreviousPageCommand;

        /// <summary>
        /// 下一页命令
        /// </summary>
        public System.Windows.Input.ICommand NextPageCommand => _paginationViewModel.NextPageCommand;

        /// <summary>
        /// 末页命令
        /// </summary>
        public System.Windows.Input.ICommand LastPageCommand => _paginationViewModel.LastPageCommand;

        #endregion
    }
} 