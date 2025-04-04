using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 车票视图模型，负责管理车票数据
    /// </summary>
    public class TicketViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly PaginationViewModel _paginationViewModel;
        private List<TrainRideInfo> _allTickets = new List<TrainRideInfo>();
        private readonly MainViewModel _mainViewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="navigationService">导航服务</param>
        /// <param name="paginationViewModel">分页视图模型</param>
        /// <param name="mainViewModel">主视图模型</param>
        public TicketViewModel(DatabaseService databaseService, NavigationService navigationService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel) : base()
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // 初始化命令
            AddTicketCommand = new RelayCommand(AddTicket);
            QueryAllCommand = new RelayCommand(async () => await QueryAllAsync());
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());

            // 订阅分页事件
            _paginationViewModel.PageChanged += OnPageChanged;
            _paginationViewModel.PageSizeChanged += OnPageSizeChanged;
        }

        /// <summary>
        /// 添加车票命令
        /// </summary>
        public ICommand AddTicketCommand { get; }

        /// <summary>
        /// 查询所有车票命令
        /// </summary>
        public ICommand QueryAllCommand { get; }

        /// <summary>
        /// 加载数据命令
        /// </summary>
        public ICommand LoadDataCommand { get; }

        /// <summary>
        /// 添加车票
        /// </summary>
        private void AddTicket()
        {
            try
            {
                // 打开添加车票窗口
                bool result = _navigationService.OpenAddTicketWindow(_databaseService, _mainViewModel);

                // 如果用户保存了车票，在后台刷新数据，但不改变当前视图
                if (result)
                {
                    // 在后台刷新数据
                    _ = RefreshDataInBackgroundAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开添加车票窗口时出错: {ex.Message}");
                LogHelper.LogError($"打开添加车票窗口时出错", ex);
            }
        }

        /// <summary>
        /// 在后台刷新数据，不改变当前视图
        /// </summary>
        private async Task RefreshDataInBackgroundAsync()
        {
            try
            {
                // 获取总记录数
                _paginationViewModel.TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();

                // 清除缓存，确保下次加载时获取最新数据
                _paginationViewModel.ClearCache();

                // 如果当前正在显示数据表格，则刷新当前页数据
                if (_paginationViewModel.IsInitialized)
                {
                    // 设置加载状态为false，因为这是后台刷新，不需要显示加载动画
                    _paginationViewModel.IsLoading = false;

                    // 重新计算总页数
                    int totalPages = (_paginationViewModel.TotalItems + _paginationViewModel.PageSize - 1) / _paginationViewModel.PageSize;
                    _paginationViewModel.TotalPages = totalPages;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"后台刷新数据时出错: {ex.Message}");
                // 不显示错误消息，因为这是后台操作
            }
        }

        /// <summary>
        /// 查询所有车票
        /// </summary>
        private async Task QueryAllAsync()
        {
            try
            {
                _paginationViewModel.IsLoading = true;

                // 获取总记录数
                _paginationViewModel.TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();

                // 重置到第一页
                _paginationViewModel.CurrentPage = 1;

                // 加载第一页数据
                await LoadPageDataAsync();

                // 标记为已初始化
                _paginationViewModel.IsInitialized = true;

                // 显示数据表格
                _mainViewModel.ShowQueryAllTickets = true;
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
        /// 加载数据
        /// </summary>
        public async Task LoadDataAsync()
        {
            try
            {
                _paginationViewModel.IsLoading = true;

                // 清空当前数据
                _paginationViewModel.Items.Clear();

                // 获取总记录数
                _paginationViewModel.TotalItems = await _databaseService.GetTotalTrainRideInfoCountAsync();

                // 如果有数据，加载第一页
                if (_paginationViewModel.TotalItems > 0)
                {
                    // 重置到第一页
                    _paginationViewModel.CurrentPage = 1;

                    // 加载第一页数据
                    await LoadPageDataAsync();

                    // 显示数据表格
                    _mainViewModel.ShowQueryAllTickets = true;
                }

                // 标记为已初始化
                _paginationViewModel.IsInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载数据时出错: {ex.Message}");
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

                // 检测缓存中是否已有当前页数据，且页大小未变
                if (_paginationViewModel.PageCache.ContainsKey(_paginationViewModel.CurrentPage) &&
                    _paginationViewModel.CachePageSize == _paginationViewModel.PageSize)
                {
                    // 从缓存加载数据，使用临时集合避免直接清空Items导致的闪烁
                    var cachedItems = _paginationViewModel.PageCache[_paginationViewModel.CurrentPage];

                    // 使用批量更新方式更新UI，减少闪烁
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
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
                    }, DispatcherPriority.Render);

                    // 从缓存加载时，不需要延迟，立即关闭加载状态
                    _paginationViewModel.IsLoading = false;
                    return;
                }

                // 直接从数据库加载当前页的数据
                var pageData = await _databaseService.GetPagedTrainRideInfosAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize);

                // 更新缓存
                _paginationViewModel.PageCache[_paginationViewModel.CurrentPage] = new List<TrainRideInfo>(pageData);
                _paginationViewModel.CachePageSize = _paginationViewModel.PageSize;

                // 使用批量更新方式更新UI，减少闪烁
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
                }, DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载页面数据时出错: {ex.Message}");
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
        /// 页面大小变更事件处理
        /// </summary>
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            // 确保加载状态已设置，这里不需要条件判断，因为我们希望每次页面大小变更都显示加载动画
            _paginationViewModel.IsLoading = true;

            // 直接加载页面数据，不需要额外的Dispatcher调用
            _ = LoadPageDataAsync();
        }
    }
}