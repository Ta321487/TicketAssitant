using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace TA_WPF.ViewModels
{
    public class RouteTicketViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly MainViewModel _mainViewModel;
        private PaginationViewModel _paginationViewModel;
        private RouteInfo _route;
        private ObservableCollection<RouteTicketMapping> _tickets;
        private ObservableCollection<RouteTicketMapping> _selectedTickets;
        private RouteTicketMapping _selectedTicket;
        private bool _isLoading;
        private int _totalCount;
        private bool _hasSelectedItems;
        private int _selectedItemsCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RouteTicketViewModel(RouteInfo route, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            // 初始化服务和数据
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _route = route ?? throw new ArgumentNullException(nameof(route));

            // 初始化分页控制器
            _paginationViewModel = new PaginationViewModel();
            _paginationViewModel.PageChanged += async (s, e) =>
            {
                // 页面变更时清空选择状态
                _selectedTickets.Clear();
                SelectedItemsCount = 0;
                await RefreshDataAsync();
            };

            _paginationViewModel.PageSizeChanged += async (s, e) =>
            {
                // 页面大小变更时清空选择状态
                _selectedTickets.Clear();
                SelectedItemsCount = 0;
                await RefreshDataAsync();
            };

            // 初始化集合
            _tickets = new ObservableCollection<RouteTicketMapping>();
            _selectedTickets = new ObservableCollection<RouteTicketMapping>();

            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            SelectAllCommand = new RelayCommand(SelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection);
            AddTicketsCommand = new RelayCommand(ShowAddTickets);
            RemoveTicketsCommand = new RelayCommand(RemoveSelectedTickets, CanRemoveTickets);

            // 初始加载数据
            _ = InitializeAsync();
        }

        /// <summary>
        /// 初始化并加载数据
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // 设置分页控制器为已初始化状态
                PaginationViewModel.IsInitialized = true;

                // 加载数据
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"初始化路线车票数据失败: {ex.Message}", ex);
            }
        }

        #region 属性

        /// <summary>
        /// 主ViewModel引用，用于绑定字体大小等全局设置
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 路线信息
        /// </summary>
        public RouteInfo Route
        {
            get => _route;
            set
            {
                if (_route != value)
                {
                    _route = value;
                    OnPropertyChanged(nameof(Route));
                }
            }
        }

        /// <summary>
        /// 车票列表
        /// </summary>
        public ObservableCollection<RouteTicketMapping> Tickets
        {
            get => _tickets;
            set
            {
                if (_tickets != value)
                {
                    _tickets = value;
                    OnPropertyChanged(nameof(Tickets));
                }
            }
        }

        /// <summary>
        /// 选中的车票列表
        /// </summary>
        public ObservableCollection<RouteTicketMapping> SelectedTickets
        {
            get => _selectedTickets;
            set
            {
                if (_selectedTickets != value)
                {
                    _selectedTickets = value;
                    OnPropertyChanged(nameof(SelectedTickets));
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        /// <summary>
        /// 选中的车票
        /// </summary>
        public RouteTicketMapping SelectedTicket
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

        /// <summary>
        /// 分页控制器
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

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
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged(nameof(TotalCount));
                    PaginationViewModel.TotalItems = value;
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems
        {
            get => _hasSelectedItems;
            set
            {
                if (_hasSelectedItems != value)
                {
                    _hasSelectedItems = value;
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount
        {
            get => _selectedItemsCount;
            set
            {
                if (_selectedItemsCount != value)
                {
                    _selectedItemsCount = value;
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    HasSelectedItems = value > 0;
                }
            }
        }

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => Tickets != null && Tickets.Count > 0;

        /// <summary>
        /// 是否没有数据
        /// </summary>
        public bool HasNoData => Tickets == null || Tickets.Count == 0;

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 全选命令
        /// </summary>
        public ICommand SelectAllCommand { get; }

        /// <summary>
        /// 取消选择命令
        /// </summary>
        public ICommand UnselectAllCommand { get; }

        /// <summary>
        /// 反选命令
        /// </summary>
        public ICommand InvertSelectionCommand { get; }

        /// <summary>
        /// 添加车票命令
        /// </summary>
        public ICommand AddTicketsCommand { get; }

        /// <summary>
        /// 移除车票命令
        /// </summary>
        public ICommand RemoveTicketsCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;

                // 获取总数
                TotalCount = await _databaseService.GetRouteTicketsCountAsync(_route.Id);

                System.Diagnostics.Debug.WriteLine($"刷新数据 - 总记录数: {TotalCount}，当前页: {PaginationViewModel.CurrentPage}，每页数量: {PaginationViewModel.PageSize}");

                // 设置分页控制器状态
                if (!PaginationViewModel.IsInitialized)
                {
                    PaginationViewModel.IsInitialized = true;
                }

                // 确保总记录数被正确设置
                PaginationViewModel.TotalItems = TotalCount;

                // 获取分页数据
                var tickets = await _databaseService.GetRouteTicketsAsync(
                    _route.Id,
                    PaginationViewModel.CurrentPage,
                    PaginationViewModel.PageSize);

                System.Diagnostics.Debug.WriteLine($"获取到分页数据 - 数量: {tickets.Count}");

                // 如果获取的数据超过页面大小，进行截断
                if (tickets.Count > PaginationViewModel.PageSize)
                {
                    System.Diagnostics.Debug.WriteLine($"警告：获取的数据量({tickets.Count})超过页面大小({PaginationViewModel.PageSize})，将进行截断");
                    tickets = tickets.Take(PaginationViewModel.PageSize).ToList();
                }

                // 清空选中状态
                _selectedTickets.Clear();
                SelectedItemsCount = 0;
                OnPropertyChanged(nameof(HasSelectedItems));

                // 清空并重新加载数据
                Tickets.Clear();

                // 使用HashSet跟踪已添加的项ID，防止重复
                var addedIds = new HashSet<int>();

                foreach (var ticket in tickets)
                {
                    // 确保IsSelected属性初始化为false
                    ticket.IsSelected = false;

                    // 防止重复添加相同ID的项
                    if (!addedIds.Contains(ticket.Id))
                    {
                        Tickets.Add(ticket);
                        addedIds.Add(ticket.Id);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"警告：检测到重复ID的数据项({ticket.Id})，已跳过");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"数据加载完成 - Tickets集合大小: {Tickets.Count}");

                // 更新UI状态
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(SelectedTickets));

                // 强制更新分页状态
                PaginationViewModel.NotifyCurrentPageChanged();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"刷新路线车票数据失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"刷新数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            // 获取当前页面的项数
            int expectedCount = Tickets.Count;
            System.Diagnostics.Debug.WriteLine($"开始全选操作 - 当前页数据项数量: {expectedCount}");

            try
            {
                // DataGrid的SelectAll方法会触发多次SelectionChanged事件
                // 这里不需要手动添加项，让SelectionChanged事件处理程序来处理选择状态
                var dataGrid = GetDataGrid();
                if (dataGrid != null)
                {
                    dataGrid.SelectAll();
                    return;
                }

                // 如果无法获取DataGrid，则使用备用方法手动设置选择状态
                // 清空选择集合，避免重复项
                _selectedTickets.Clear();

                // 防止重复添加的HashSet
                var addedItems = new HashSet<int>();

                // 为当前页所有项设置选中状态
                foreach (var ticket in Tickets)
                {
                    // 确保只将每个项添加一次（防止重复）
                    if (!addedItems.Contains(ticket.Id))
                    {
                        ticket.IsSelected = true;
                        _selectedTickets.Add(ticket);
                        addedItems.Add(ticket.Id);
                    }
                }

                // 更新UI和计数
                SelectedItemsCount = _selectedTickets.Count;

                // 验证选择数量是否与当前页项数一致
                if (SelectedItemsCount != expectedCount)
                {
                    System.Diagnostics.Debug.WriteLine($"警告：选择数量与当前页项数不一致！");
                }

                OnPropertyChanged(nameof(SelectedTickets));
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(Tickets)); // 强制刷新整个列表
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"全选操作异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试获取DataGrid控件
        /// </summary>
        private System.Windows.Controls.DataGrid GetDataGrid()
        {
            try
            {
                // 尝试从可视化树中查找DataGrid
                var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
                if (window != null)
                {
                    return FindDataGrid(window);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取DataGrid异常: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 递归查找DataGrid控件
        /// </summary>
        private System.Windows.Controls.DataGrid FindDataGrid(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.DataGrid dataGrid)
                {
                    // 确认这是我们的DataGrid
                    if (dataGrid.Name == "TicketsDataGrid")
                    {
                        return dataGrid;
                    }
                }

                var result = FindDataGrid(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        private void UnselectAll()
        {
            // 为所有项设置未选中状态
            foreach (var ticket in Tickets)
            {
                ticket.IsSelected = false;
            }

            // 清空选择集合
            _selectedTickets.Clear();
            SelectedItemsCount = 0;

            // 更新UI
            OnPropertyChanged(nameof(SelectedTickets));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(Tickets)); // 强制刷新整个列表
        }

        /// <summary>
        /// 反选
        /// </summary>
        private void InvertSelection()
        {
            System.Diagnostics.Debug.WriteLine($"开始反选操作 - 当前选中项数量: {_selectedTickets.Count}, 当前页项数: {Tickets.Count}");

            // 新建临时集合存储将要选中的项
            var newSelection = new List<RouteTicketMapping>();
            var addedIds = new HashSet<int>();

            // 反转每一项的选中状态（仅限当前页）
            foreach (var ticket in Tickets)
            {
                ticket.IsSelected = !ticket.IsSelected;
                if (ticket.IsSelected && !addedIds.Contains(ticket.Id))
                {
                    newSelection.Add(ticket);
                    addedIds.Add(ticket.Id);
                }
            }

            // 更新选中集合
            _selectedTickets.Clear();
            foreach (var ticket in newSelection)
            {
                _selectedTickets.Add(ticket);
            }

            // 更新UI和计数
            SelectedItemsCount = _selectedTickets.Count;
            System.Diagnostics.Debug.WriteLine($"反选完成 - 新选中项数量: {SelectedItemsCount}");

            OnPropertyChanged(nameof(SelectedTickets));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(Tickets)); // 强制刷新整个列表
        }

        /// <summary>
        /// 显示添加车票对话框
        /// </summary>
        private void ShowAddTickets()
        {
            // 暂不实现
            MessageBoxHelper.ShowInfo("功能尚未实现");
        }

        /// <summary>
        /// 是否可以移除车票
        /// </summary>
        private bool CanRemoveTickets()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 移除选中的车票
        /// </summary>
        private void RemoveSelectedTickets()
        {
            // 暂不实现
            MessageBoxHelper.ShowInfo("功能尚未实现");
        }

        /// <summary>
        /// 同步选择状态
        /// </summary>
        public void SynchronizeSelectionStates()
        {
            System.Diagnostics.Debug.WriteLine($"开始同步选择状态 - 当前SelectedTickets数量: {_selectedTickets.Count}");

            // 清空并重建选择集合
            _selectedTickets.Clear();

            // 记录当前页选中项
            int currentPageSelectedCount = 0;

            // 用于防止重复添加的HashSet
            var addedIds = new HashSet<int>();

            // 从当前页数据项中收集选中的项
            foreach (var ticket in Tickets)
            {
                if (ticket.IsSelected && !addedIds.Contains(ticket.Id))
                {
                    _selectedTickets.Add(ticket);
                    addedIds.Add(ticket.Id);
                    currentPageSelectedCount++;
                }
            }

            // 确保总数不超过当前页的项目数
            if (_selectedTickets.Count > Tickets.Count)
            {
                System.Diagnostics.Debug.WriteLine($"警告：选择数量({_selectedTickets.Count})超过当前页项目数({Tickets.Count})，将进行截断");

                // 清空集合并重新添加
                var tempList = _selectedTickets.Take(Tickets.Count).ToList();
                _selectedTickets.Clear();

                foreach (var ticket in tempList)
                {
                    _selectedTickets.Add(ticket);
                }
            }

            // 更新计数和UI
            SelectedItemsCount = _selectedTickets.Count;

            System.Diagnostics.Debug.WriteLine($"同步选择状态完成 - 当前页选中项: {currentPageSelectedCount}, SelectedTickets: {_selectedTickets.Count}, SelectedItemsCount: {SelectedItemsCount}");

            // 验证选择数量
            if (currentPageSelectedCount != SelectedItemsCount)
            {
                System.Diagnostics.Debug.WriteLine($"警告：选择数量不一致！当前页选中: {currentPageSelectedCount}, 选择集合: {SelectedItemsCount}");
            }

            OnPropertyChanged(nameof(SelectedTickets));
            OnPropertyChanged(nameof(HasSelectedItems));
        }

        /// <summary>
        /// 更新选中项数量
        /// </summary>
        public void UpdateSelectedItemsCount()
        {
            SelectedItemsCount = _selectedTickets.Count;
            HasSelectedItems = SelectedItemsCount > 0;
        }

        #endregion
    }
}