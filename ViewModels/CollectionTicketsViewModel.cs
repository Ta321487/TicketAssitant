using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Input; // Added for CommandManager
using System.Collections.Generic;

namespace TA_WPF.ViewModels
{
    public class CollectionTicketsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;
        private readonly TicketCollectionInfo _collection;
        private ObservableCollection<TrainRideInfo> _tickets;
        private ObservableCollection<TrainRideInfo> _selectedTickets;
        private TrainRideInfo _selectedTicket;
        private bool _isLoading;
        private int _totalCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public CollectionTicketsViewModel(TicketCollectionInfo collection, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            
            // 初始化集合
            _tickets = new ObservableCollection<TrainRideInfo>();
            _selectedTickets = new ObservableCollection<TrainRideInfo>();
            _selectedTickets.CollectionChanged += SelectedTickets_CollectionChanged;
            
            // 初始化分页视图模型
            _paginationViewModel = new PaginationViewModel();
            _paginationViewModel.PageChanged += PageChanged;
            _paginationViewModel.PageSizeChanged += PageSizeChanged;
            
            // 初始化命令
            AddTicketsCommand = new RelayCommand(AddTickets);
            RemoveTicketsCommand = new RelayCommand(RemoveTickets, CanRemoveTickets);
            RefreshCommand = new RelayCommand(() => _ = LoadTicketsAsync());
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection, CanInvertSelection);
            
            // 标记分页组件为已初始化
            _paginationViewModel.IsInitialized = true;
            
            // 加载数据
            _ = LoadTicketsAsync();
        }

        #region 属性

        /// <summary>
        /// 主视图模型
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 收藏夹信息
        /// </summary>
        public TicketCollectionInfo Collection => _collection;

        /// <summary>
        /// 车票列表
        /// </summary>
        public ObservableCollection<TrainRideInfo> Tickets
        {
            get => _tickets;
            set
            {
                if (_tickets != value)
                {
                    if (_tickets != null)
                    {
                        _tickets.CollectionChanged -= Tickets_CollectionChanged;
                        UnsubscribeFromTicketItemChanges(_tickets);
                    }

                    _tickets = value;

                    if (_tickets != null)
                    {
                        _tickets.CollectionChanged += Tickets_CollectionChanged;
                        SubscribeToTicketItemChanges(_tickets);
                        RefreshSelectedTicketsFromSource();
                    }
                    else
                    {
                        _selectedTickets?.Clear();
                    }
                    
                    OnPropertyChanged(nameof(Tickets));
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(HasNoData));
                    NotifySelectionChanged();
                }
            }
        }

        /// <summary>
        /// 选中的车票列表
        /// </summary>
        public ObservableCollection<TrainRideInfo> SelectedTickets
        {
            get => _selectedTickets;
            set
            {
                if (_selectedTickets != value)
                {
                    if (_selectedTickets != null)
                    {
                        _selectedTickets.CollectionChanged -= SelectedTickets_CollectionChanged;
                    }
                    _selectedTickets = value;
                    if (_selectedTickets != null)
                    {
                        _selectedTickets.CollectionChanged += SelectedTickets_CollectionChanged;
                    }
                    OnPropertyChanged(nameof(SelectedTickets));
                    NotifySelectionChanged();
                }
            }
        }

        /// <summary>
        /// 选中的车票
        /// </summary>
        public TrainRideInfo SelectedTicket
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
                    _paginationViewModel.TotalItems = value;
                }
            }
        }

        /// <summary>
        /// 分页视图模型
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => _tickets != null && _tickets.Count > 0;

        /// <summary>
        /// 是否没有数据
        /// </summary>
        public bool HasNoData => _tickets == null || _tickets.Count == 0;

        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount => _selectedTickets?.Count ?? 0;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems => SelectedItemsCount > 0;

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle => $"收藏夹 - {Collection.CollectionName}";

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 添加车票命令
        /// </summary>
        public ICommand AddTicketsCommand { get; }

        /// <summary>
        /// 移除车票命令
        /// </summary>
        public ICommand RemoveTicketsCommand { get; }

        /// <summary>
        /// 全选命令
        /// </summary>
        public ICommand SelectAllCommand { get; }

        /// <summary>
        /// 取消全选命令
        /// </summary>
        public ICommand UnselectAllCommand { get; }

        /// <summary>
        /// 反选命令
        /// </summary>
        public ICommand InvertSelectionCommand { get; }

        #endregion

        #region 命令方法

        /// <summary>
        /// 打开添加车票窗口
        /// </summary>
        private void AddTickets()
        {
            // 创建并打开添加车票窗口
            var addTicketsWindow = new Views.AddTicketsToCollectionWindow(_collection, _databaseService, _mainViewModel);
            addTicketsWindow.Owner = Application.Current.MainWindow;
            addTicketsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            // 显示窗口
            bool? result = addTicketsWindow.ShowDialog();
            
            // 如果窗口返回true，表示有数据变更，刷新列表
            if (result == true)
            {
                RefreshTickets();
            }
        }

        /// <summary>
        /// 移除选中的车票
        /// </summary>
        private async void RemoveTickets()
        {
            // 确保有选中的车票
            if (!HasSelectedItems)
                return;
                
            // 获取选中的车票ID列表
            var selectedTicketIds = SelectedTickets.Select(t => t.Id).ToList();
            
            // 询问用户是否确认移除
            var result = MessageBoxHelper.ShowConfirmation(
                $"确定要从收藏夹中移除选中的 {selectedTicketIds.Count} 张车票吗？",
                "确认操作");
                
            if (result != MessageBoxResult.Yes)
                return;
                
            // 显示加载状态
            IsLoading = true;
            
            try
            {
                // 调用数据库服务移除车票
                bool success = await _databaseService.RemoveTicketsFromCollectionAsync(_collection.Id, selectedTicketIds);
                
                if (success)
                {
                    // 更新收藏夹中的车票数量
                    _collection.TicketCount = await _databaseService.GetCollectionTicketCountAsync(_collection.Id);
                    
                    // 显示成功消息
                    MessageBoxHelper.ShowInformation(
                        $"已成功从收藏夹中移除 {selectedTicketIds.Count} 张车票");
                    
                    // 刷新列表
                    await LoadTicketsAsync();
                }
                else
                {
                    MessageBoxHelper.ShowError("移除车票失败，请重试");
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"移除车票时发生错误: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 是否可以移除车票
        /// </summary>
        private bool CanRemoveTickets()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            // 确保当前页面所有票被选中
            int currentPageCount = Tickets?.Count ?? 0;
            
            // 仅当有票可选时才执行选择操作
            if (currentPageCount > 0)
            {
                try
                {
                    // 使用批量更新，确保所有项都被选中
                    foreach (var ticket in Tickets)
                    {
                        ticket.IsSelected = true;
                    }
                    
                    // 确保选中数量与UI保持同步
                    if (SelectedTickets.Count != currentPageCount)
                    {
                        // 刷新选择状态
                        SynchronizeSelectionStates();
                    }
                    
                    // 通知DataGrid更新选中状态
                    SelectionChanged?.Invoke(this, new TicketSelectionChangedEventArgs(
                        new List<TrainRideInfo>(), // 没有移除的项
                        Tickets.ToList() // 所有当前页面的票都被添加到选择中
                    ));
                }
                finally
                {
                    // 通知UI状态更新
                    NotifySelectionChanged();
                }
            }
        }

        /// <summary>
        /// 同步选择状态，确保UI和模型数据一致
        /// </summary>
        private void SynchronizeSelectionStates()
        {
            if (Tickets == null)
                return;
                
            // 暂时取消事件订阅，避免事件循环
            SelectedTickets.CollectionChanged -= SelectedTickets_CollectionChanged;
            
            try
            {
                // 清空现有选择
                SelectedTickets.Clear();
                
                // 添加所有选中的项
                foreach (var ticket in Tickets)
                {
                    if (ticket.IsSelected && !SelectedTickets.Contains(ticket))
                    {
                        SelectedTickets.Add(ticket);
                    }
                }
            }
            finally
            {
                // 恢复事件订阅
                SelectedTickets.CollectionChanged += SelectedTickets_CollectionChanged;
                
                // 通知UI更新
                NotifySelectionChanged();
            }
        }

        /// <summary>
        /// 是否可以全选
        /// </summary>
        private bool CanSelectAll() => Tickets.Count > 0 && SelectedItemsCount < Tickets.Count;

        /// <summary>
        /// 取消全选
        /// </summary>
        private void UnselectAll()
        {
            // 没有选中项时不执行操作
            if (Tickets == null || Tickets.Count == 0 || SelectedTickets.Count == 0)
                return;
                
            // 备份当前选中项以便触发事件
            var previousSelected = new List<TrainRideInfo>(_selectedTickets);
            
            foreach (var ticket in Tickets)
            {
                ticket.IsSelected = false;
            }
            
            SelectedTickets.Clear();
            NotifySelectionChanged();
            
            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new TicketSelectionChangedEventArgs(
                previousSelected, // 之前选中的项都被移除
                new List<TrainRideInfo>() // 没有新增的选中项
            ));
        }

        /// <summary>
        /// 是否可以取消全选
        /// </summary>
        private bool CanUnselectAll() => SelectedItemsCount > 0;

        /// <summary>
        /// 反选
        /// </summary>
        private void InvertSelection()
        {
            if (Tickets == null || Tickets.Count == 0)
                return;
                
            var currentSelection = new HashSet<TrainRideInfo>(_selectedTickets);
            var toAdd = new List<TrainRideInfo>();
            var toRemove = new List<TrainRideInfo>(_selectedTickets);
            
            foreach (var ticket in Tickets)
            {
                bool previousState = ticket.IsSelected;
                ticket.IsSelected = !previousState;
                
                if (!previousState)
                {
                    toAdd.Add(ticket);
                }
            }
            
            SynchronizeSelectionStates();
            
            // 通知DataGrid更新选中状态
            SelectionChanged?.Invoke(this, new TicketSelectionChangedEventArgs(
                toRemove,  // 之前选中现在取消选中的项
                toAdd      // 之前未选中现在选中的项
            ));
        }

        /// <summary>
        /// 是否可以反选
        /// </summary>
        private bool CanInvertSelection() => Tickets.Count > 0;

        /// <summary>
        /// 刷新车票列表
        /// </summary>
        private async void RefreshTickets()
        {
            // 记录刷新操作
            Debug.WriteLine("正在刷新收藏夹车票列表...");
            
            // 重置缓存
            _paginationViewModel.ClearCache();
            
            // 重置到第一页
            _paginationViewModel.CurrentPage = 1;
            
            // 重新加载数据
            await LoadTicketsAsync();
            
            // 更新收藏夹的车票数量
            _collection.TicketCount = await _databaseService.GetCollectionTicketCountAsync(_collection.Id);
            
            Debug.WriteLine($"刷新完成，收藏夹中有 {_collection.TicketCount} 张车票");
        }

        #endregion

        #region 方法

        /// <summary>
        /// 加载收藏夹内的车票
        /// </summary>
        public async Task LoadTicketsAsync()
        {
            IsLoading = true;
            try
            {
                // 从数据库加载车票数据
                var ticketsData = await LoadTicketsFromDatabaseAsync();
                
                // 添加调试信息
                Debug.WriteLine($"从数据库加载了 {ticketsData.Count} 张车票");
                
                Tickets = new ObservableCollection<TrainRideInfo>(ticketsData);
                
                // 清除选择
                SelectedTickets.Clear();
                
                // 更新总记录数
                TotalCount = await GetTotalTicketCountAsync();
                Debug.WriteLine($"收藏夹车票总数: {TotalCount}");
                
                // 确保分页计算正确
                int maxPage = (TotalCount + _paginationViewModel.PageSize - 1) / _paginationViewModel.PageSize;
                maxPage = Math.Max(1, maxPage); // 确保至少有1页
                _paginationViewModel.TotalPages = maxPage;
                
                Debug.WriteLine($"当前页: {_paginationViewModel.CurrentPage}, 总页数: {_paginationViewModel.TotalPages}");
                
                // 如果当前页超出范围，则重置为第一页
                if (_paginationViewModel.CurrentPage > maxPage)
                {
                    _paginationViewModel.CurrentPage = 1;
                    Debug.WriteLine("当前页超出范围，重置为第一页");
                }

                // 尝试强制刷新Tickets绑定
                OnPropertyChanged(nameof(Tickets));
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载收藏夹车票列表失败: {ex.Message}");
                Debug.WriteLine($"加载收藏夹车票列表错误: {ex.Message}");
                Tickets?.Clear();
                SelectedTickets?.Clear();
                TotalCount = 0;
                NotifySelectionChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从数据库加载车票数据
        /// </summary>
        private async Task<List<TrainRideInfo>> LoadTicketsFromDatabaseAsync()
        {
            // 使用数据库服务获取收藏夹内的车票（使用最新的车票信息）
            return await _databaseService.GetCollectionTicketsWithLatestInfoAsync(
                _collection.Id,
                _paginationViewModel.CurrentPage,
                _paginationViewModel.PageSize);
        }

        /// <summary>
        /// 获取收藏夹内车票总数
        /// </summary>
        private async Task<int> GetTotalTicketCountAsync()
        {
            // 使用数据库服务获取收藏夹内的车票总数
            return await _databaseService.GetCollectionTicketCountAsync(_collection.Id);
        }

        /// <summary>
        /// 处理页面变更事件
        /// </summary>
        private async void PageChanged(object sender, EventArgs e)
        {
            await LoadTicketsAsync();
        }
        
        /// <summary>
        /// 处理页大小变更事件
        /// </summary>
        private async void PageSizeChanged(object sender, EventArgs e)
        {
            await LoadTicketsAsync();
        }

        #endregion

        #region Selection Synchronization Methods

        private void SubscribeToTicketItemChanges(ObservableCollection<TrainRideInfo> collection)
        {
            if (collection == null) return;
            foreach (var item in collection)
            {
                item.PropertyChanged += TicketItem_PropertyChanged;
            }
        }

        private void UnsubscribeFromTicketItemChanges(ObservableCollection<TrainRideInfo> collection)
        {
            if (collection == null) return;
            foreach (var item in collection)
            {
                item.PropertyChanged -= TicketItem_PropertyChanged;
            }
        }

        private void Tickets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (TrainRideInfo item in e.OldItems)
                {
                    item.PropertyChanged -= TicketItem_PropertyChanged;
                    if (_selectedTickets.Contains(item))
                    {
                        _selectedTickets.Remove(item);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (TrainRideInfo item in e.NewItems)
                {
                    item.PropertyChanged += TicketItem_PropertyChanged;
                    if (item.IsSelected && !_selectedTickets.Contains(item))
                    {
                        _selectedTickets.Add(item);
                    }
                }
            }
            NotifySelectionChanged();
        }

        private void TicketItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrainRideInfo.IsSelected))
            {
                if (sender is TrainRideInfo item)
                {
                    if (item.IsSelected)
                    {
                        if (!_selectedTickets.Contains(item))
                        {
                            _selectedTickets.Add(item);
                        }
                    }
                    else
                    {
                        if (_selectedTickets.Contains(item))
                        {
                            _selectedTickets.Remove(item);
                        }
                    }
                }
            }
        }
        
        private void SelectedTickets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifySelectionChanged();
        }

        private void RefreshSelectedTicketsFromSource()
        {
            if (_selectedTickets == null) return;

            _selectedTickets.CollectionChanged -= SelectedTickets_CollectionChanged;

            _selectedTickets.Clear();
            if (_tickets != null)
            {
                foreach (var item in _tickets.Where(t => t.IsSelected))
                {
                    _selectedTickets.Add(item);
                }
            }

            _selectedTickets.CollectionChanged += SelectedTickets_CollectionChanged;
            
            NotifySelectionChanged();
        }

        /// <summary>
        /// 通知选择状态变化
        /// </summary>
        public void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(HasSelectedItems));

            // Use CommandManager to re-evaluate command states
            CommandManager.InvalidateRequerySuggested(); 
        }

        #endregion

        #region Selection Event

        // 事件用于通知View更新DataGrid的选中状态
        public event EventHandler<TicketSelectionChangedEventArgs> SelectionChanged;

        // 事件参数类
        public class TicketSelectionChangedEventArgs : EventArgs
        {
            public List<TrainRideInfo> RemovedItems { get; }
            public List<TrainRideInfo> AddedItems { get; }
            
            public TicketSelectionChangedEventArgs(List<TrainRideInfo> removedItems, List<TrainRideInfo> addedItems)
            {
                RemovedItems = removedItems;
                AddedItems = addedItems;
            }
        }
        
        #endregion
    }
} 