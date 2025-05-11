using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;
using TA_WPF.Views;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 车票收藏夹视图模型，负责管理所有收藏夹数据
    /// </summary>
    public class QueryAllCollectionsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PaginationViewModel _paginationViewModel;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<TicketCollectionInfo> _collections;
        private ObservableCollection<TicketCollectionInfo> _selectedCollections;
        private TicketCollectionInfo _selectedCollection;
        private int _totalCount;
        private bool _isLoading;
        private bool _isGridView = true; // 默认为网格视图
        private bool _isQueryPanelVisible = false;

        // 用于跟踪上次查询条件的变量 (未来实现使用)
        // private CollectionQueryFilterEventArgs _lastQueryFilter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="paginationViewModel">分页视图模型</param>
        /// <param name="mainViewModel">主视图模型</param>
        public QueryAllCollectionsViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            _collections = new ObservableCollection<TicketCollectionInfo>();
            _selectedCollections = new ObservableCollection<TicketCollectionInfo>();
            _collections.CollectionChanged += Collections_CollectionChanged; // 订阅集合自身的变化
            
            // 订阅分页事件
            _paginationViewModel.PageChanged += async (s, e) => await LoadCollectionsAsync();
            _paginationViewModel.PageSizeChanged += async (s, e) => await LoadCollectionsAsync();

            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await LoadCollectionsAsync());
            AddCollectionCommand = new RelayCommand(AddCollection);
            EditCollectionCommand = new RelayCommand(EditCollection, CanEditCollection);
            DeleteCollectionCommand = new RelayCommand(DeleteCollection, CanDeleteCollection);
            CopyCollectionCommand = new RelayCommand(CopyCollection, CanCopyCollection);
            MoveCollectionCommand = new RelayCommand(MoveCollection, CanMoveCollection);
            SortCollectionsCommand = new RelayCommand<string>(SortCollections);
            
            // 视图切换命令
            ToggleViewCommand = new RelayCommand(ToggleView);
            
            // 选择相关命令
            SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll, CanUnselectAll);
            InvertSelectionCommand = new RelayCommand(InvertSelection, CanInvertSelection);
        }

        /// <summary>
        /// 主视图模型
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 收藏夹列表
        /// </summary>
        public ObservableCollection<TicketCollectionInfo> Collections
        {
            get => _collections;
            set
            {
                if (_collections != value)
                {
                    UnsubscribeFromCollectionItemChanges(_collections); // 取消订阅旧集合
                    _collections = value;
                    SubscribeToCollectionItemChanges(_collections);   // 订阅新集合
                    OnPropertyChanged(nameof(Collections));
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(HasNoData));
                    RefreshSelectedCollectionsFromSource(); // 根据新集合刷新选中项
                    NotifySelectionChanged(); // 通知选择相关的属性变更
                }
            }
        }

        /// <summary>
        /// 选中的收藏夹列表
        /// </summary>
        public ObservableCollection<TicketCollectionInfo> SelectedCollections
        {
            get => _selectedCollections;
            set
            {
                if (_selectedCollections != value)
                {
                    _selectedCollections = value;
                    OnPropertyChanged(nameof(SelectedCollections));
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(CanEditSelectedCollection));
                }
            }
        }

        /// <summary>
        /// 选中的收藏夹
        /// </summary>
        public TicketCollectionInfo SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                if (_selectedCollection != value)
                {
                    _selectedCollection = value;
                    OnPropertyChanged(nameof(SelectedCollection));
                    // IsSelected状态和SelectedCollections列表的管理
                    // 现在由控件绑定和CollectionItem_PropertyChanged处理
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
                }
            }
        }

        /// <summary>
        /// 是否为网格视图
        /// </summary>
        public bool IsGridView
        {
            get => _isGridView;
            set
            {
                if (_isGridView != value)
                {
                    _isGridView = value;
                    OnPropertyChanged(nameof(IsGridView));
                    OnPropertyChanged(nameof(IsListView));
                }
            }
        }

        /// <summary>
        /// 是否为列表视图
        /// </summary>
        public bool IsListView => !IsGridView;

        /// <summary>
        /// 分页视图模型
        /// </summary>
        public PaginationViewModel PaginationViewModel => _paginationViewModel;

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => _collections != null && _collections.Count > 0;

        /// <summary>
        /// 是否没有数据
        /// </summary>
        public bool HasNoData => _collections == null || _collections.Count == 0;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => SelectedItemsCount > 0;

        /// <summary>
        /// 是否选中了所有项
        /// </summary>
        public bool IsAllSelected => _collections != null && _selectedCollections != null && 
                               _collections.Count > 0 && _collections.Count == _selectedCollections.Count;

        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount => _selectedCollections?.Count ?? 0;

        /// <summary>
        /// 是否可以编辑选中的收藏夹
        /// </summary>
        public bool CanEditSelectedCollection => SelectedItemsCount == 1;

        /// <summary>
        /// 查询面板是否可见
        /// </summary>
        public bool IsQueryPanelVisible
        {
            get => _isQueryPanelVisible;
            set
            {
                if (_isQueryPanelVisible != value)
                {
                    _isQueryPanelVisible = value;
                    OnPropertyChanged(nameof(IsQueryPanelVisible));
                }
            }
        }

        /// <summary>
        /// 数据表格行高
        /// </summary>
        public double DataGridRowHeight => _mainViewModel.DataGridRowHeight;

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 添加收藏夹命令
        /// </summary>
        public ICommand AddCollectionCommand { get; }

        /// <summary>
        /// 编辑收藏夹命令
        /// </summary>
        public ICommand EditCollectionCommand { get; }

        /// <summary>
        /// 删除收藏夹命令
        /// </summary>
        public ICommand DeleteCollectionCommand { get; }

        /// <summary>
        /// 复制收藏夹命令
        /// </summary>
        public ICommand CopyCollectionCommand { get; }

        /// <summary>
        /// 移动收藏夹命令
        /// </summary>
        public ICommand MoveCollectionCommand { get; }

        /// <summary>
        /// 排序收藏夹命令
        /// </summary>
        public ICommand SortCollectionsCommand { get; }

        /// <summary>
        /// 切换视图命令
        /// </summary>
        public ICommand ToggleViewCommand { get; }

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
        /// 添加收藏夹
        /// </summary>
        private async void AddCollection()
        {
            // 创建并显示添加收藏夹窗口
            var addCollectionWindow = new AddCollectionWindow(_databaseService, _mainViewModel);
            addCollectionWindow.Owner = Application.Current.MainWindow;
            bool? result = addCollectionWindow.ShowDialog();

            // 在成功添加后刷新数据
            if (result == true)
            {
                await LoadCollectionsAsync();
            }
        }

        /// <summary>
        /// 编辑收藏夹
        /// </summary>
        private async void EditCollection()
        {
            if (SelectedCollection == null) return;

            // 创建并显示修改收藏夹窗口
            var editCollectionWindow = new EditCollectionWindow(SelectedCollection, _databaseService, _mainViewModel);
            editCollectionWindow.Owner = Application.Current.MainWindow;
            bool? result = editCollectionWindow.ShowDialog();

            // 在成功修改后刷新数据
            if (result == true)
            {
                await LoadCollectionsAsync();
            }
        }

        /// <summary>
        /// 是否可以编辑收藏夹
        /// </summary>
        private bool CanEditCollection() => SelectedItemsCount == 1;

        /// <summary>
        /// 删除收藏夹
        /// </summary>
        private async void DeleteCollection()
        {
            if (SelectedCollections.Count == 0) return;

            // 确认删除
            string message = SelectedCollections.Count == 1
                ? $"确定要删除收藏夹 \"{SelectedCollections[0].CollectionName}\" 吗？"
                : $"确定要删除选中的 {SelectedCollections.Count} 个收藏夹吗？";

            if (MessageBoxHelper.ShowConfirmation(message) == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    // 获取要删除的收藏夹ID列表
                    var idsToDelete = SelectedCollections.Select(c => c.Id).ToList();
                    
                    // 调用数据库服务删除收藏夹
                    bool success = await _databaseService.DeleteCollectionsByIdsAsync(idsToDelete);
                    
                    if (success)
                    {
                        MessageBoxHelper.ShowInfo(SelectedCollections.Count == 1 
                            ? "收藏夹删除成功"
                            : $"{SelectedCollections.Count} 个收藏夹删除成功");
                            
                        // 刷新收藏夹列表
                        await LoadCollectionsAsync();
                    }
                    else
                    {
                        MessageBoxHelper.ShowError("删除收藏夹失败，请稍后重试");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"删除收藏夹时出错: {ex.Message}", ex);
                    MessageBoxHelper.ShowError($"删除收藏夹时出错: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 是否可以删除收藏夹
        /// </summary>
        private bool CanDeleteCollection() => SelectedItemsCount > 0;

        /// <summary>
        /// 复制收藏夹
        /// </summary>
        private void CopyCollection()
        {
            if (SelectedCollection == null) return;

            // 复制收藏夹功能实现（未来实现）
            MessageBoxHelper.ShowInfo("复制收藏夹功能尚未实现");
        }

        /// <summary>
        /// 是否可以复制收藏夹
        /// </summary>
        private bool CanCopyCollection() => SelectedItemsCount == 1;

        /// <summary>
        /// 移动收藏夹
        /// </summary>
        private void MoveCollection()
        {
            if (SelectedCollections.Count == 0) return;

            // 移动收藏夹功能实现（未来实现）
            MessageBoxHelper.ShowInfo("移动收藏夹功能尚未实现");
        }

        /// <summary>
        /// 是否可以移动收藏夹
        /// </summary>
        private bool CanMoveCollection() => SelectedItemsCount > 0;

        /// <summary>
        /// 排序收藏夹
        /// </summary>
        /// <param name="sortBy">排序字段：TicketCount_Asc/Desc, UpdateTime_Asc/Desc, CreateTime_Asc/Desc, Importance_Asc/Desc</param>
        private async void SortCollections(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy) || Collections == null || Collections.Count == 0)
            {
                return;
            }

            IsLoading = true;
            try
            {
                // 根据排序字段进行排序
                IOrderedEnumerable<TicketCollectionInfo> sortedCollections = null;
                string sortField = sortBy.Split('_')[0];
                bool isAscending = sortBy.EndsWith("_Asc");

                switch (sortField)
                {
                    case "TicketCount":
                        sortedCollections = isAscending 
                            ? Collections.OrderBy(c => c.TicketCount) 
                            : Collections.OrderByDescending(c => c.TicketCount);
                        break;
                    case "UpdateTime":
                        sortedCollections = isAscending 
                            ? Collections.OrderBy(c => c.UpdateTime) 
                            : Collections.OrderByDescending(c => c.UpdateTime);
                        break;
                    case "CreateTime":
                        sortedCollections = isAscending 
                            ? Collections.OrderBy(c => c.CreateTime) 
                            : Collections.OrderByDescending(c => c.CreateTime);
                        break;
                    case "Importance":
                        sortedCollections = isAscending 
                            ? Collections.OrderBy(c => c.Importance) 
                            : Collections.OrderByDescending(c => c.Importance);
                        break;
                    default:
                        sortedCollections = Collections.OrderBy(c => c.Id);
                        break;
                }

                // 保存选定状态和临时索引映射
                Dictionary<int, bool> selectedStates = new Dictionary<int, bool>();
                
                // 用于批量更新数据库中的SortOrder
                Dictionary<int, int> newSortOrders = new Dictionary<int, int>();
                
                // 转换为列表以便处理
                var sortedList = sortedCollections.ToList();

                // 1. 保存选中状态
                foreach (var collection in Collections)
                {
                    selectedStates[collection.Id] = collection.IsSelected;
                }

                // 2. 分配新的排序顺序值
                for (int i = 0; i < sortedList.Count; i++)
                {
                    var collection = sortedList[i];
                    
                    // 更新内存中的SortOrder值
                    collection.SortOrder = (i + 1) * 10; // 以10为步长，便于将来在中间插入新元素
                    
                    // 记录新的排序顺序值，稍后更新数据库
                    newSortOrders[collection.Id] = collection.SortOrder;
                }

                // 3. 创建包含更新后SortOrder的新集合
                var newCollections = new ObservableCollection<TicketCollectionInfo>(sortedList);
                
                // 4. 恢复选定状态
                foreach (var collection in newCollections)
                {
                    if (selectedStates.TryGetValue(collection.Id, out bool isSelected))
                    {
                        collection.IsSelected = isSelected;
                    }
                }

                // 5. 更新UI显示的集合
                Collections = newCollections;

                // 6. 异步更新数据库中的排序顺序
                bool updateSuccess = await _databaseService.UpdateCollectionSortOrdersAsync(newSortOrders);
                
                if (!updateSuccess)
                {
                    // 如果数据库更新失败，记录错误但不影响UI显示
                    LogHelper.LogError("更新收藏夹排序顺序到数据库失败");
                }

                // 提示排序完成
                string sortName = "";
                string sortDirection = isAscending ? "升序" : "降序";
                
                switch (sortField)
                {
                    case "TicketCount":
                        sortName = "车票数量";
                        break;
                    case "UpdateTime":
                        sortName = "修改日期";
                        break;
                    case "CreateTime":
                        sortName = "创建时间";
                        break;
                    case "Importance":
                        sortName = "评分";
                        break;
                }
                
                MessageBoxHelper.ShowInfo($"已按{sortName}({sortDirection})排序完成");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"排序收藏夹时出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"排序收藏夹时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 切换视图（网格/列表）
        /// </summary>
        private void ToggleView()
        {
            IsGridView = !IsGridView;
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll()
        {
            SelectedCollections.Clear();
            foreach (var collection in Collections)
            {
                collection.IsSelected = true;
            }
            NotifySelectionChanged();
        }

        /// <summary>
        /// 是否可以全选
        /// </summary>
        private bool CanSelectAll() => Collections.Count > 0 && SelectedCollections.Count < Collections.Count;

        /// <summary>
        /// 取消全选
        /// </summary>
        private void UnselectAll()
        {
            foreach (var collection in Collections)
            {
                collection.IsSelected = false;
            }
            SelectedCollections.Clear();
            NotifySelectionChanged();
        }

        /// <summary>
        /// 是否可以取消全选
        /// </summary>
        private bool CanUnselectAll() => SelectedCollections.Count > 0;

        /// <summary>
        /// 反选
        /// </summary>
        private void InvertSelection()
        {
            var newSelection = new ObservableCollection<TicketCollectionInfo>();
            
            foreach (var collection in Collections)
            {
                collection.IsSelected = !collection.IsSelected;
                if (collection.IsSelected)
                {
                    newSelection.Add(collection);
                }
            }
            
            SelectedCollections.Clear();
            foreach (var collection in newSelection)
            {
                SelectedCollections.Add(collection);
            }
            
            NotifySelectionChanged();
        }

        /// <summary>
        /// 是否可以反选
        /// </summary>
        private bool CanInvertSelection() => Collections.Count > 0;

        #endregion

        #region 方法

        /// <summary>
        /// 查询所有收藏夹
        /// </summary>
        public async Task QueryAllAsync()
        {
            _paginationViewModel.CurrentPage = 1; // 重置到第一页
            await LoadCollectionsAsync();
        }

        /// <summary>
        /// 加载收藏夹数据
        /// </summary>
        public async Task LoadCollectionsAsync()
        {
            IsLoading = true;
            try
            {
                // 从数据库加载数据
                TotalCount = await _databaseService.GetCollectionCountAsync();
                var collectionsData = await _databaseService.GetCollectionsAsync(
                    _paginationViewModel.CurrentPage,
                    _paginationViewModel.PageSize);
                
                // 将列表转换为ObservableCollection并更新UI
                Collections = new ObservableCollection<TicketCollectionInfo>(collectionsData);
                
                // 如果没有数据，且TotalCount显示应该有数据，则可能是最后一页没有数据了
                // 返回到第一页重新加载
                if (Collections.Count == 0 && TotalCount > 0 && _paginationViewModel.CurrentPage > 1)
                {
                    _paginationViewModel.CurrentPage = 1;
                    await LoadCollectionsAsync();
                    return;
                }
                
                // 清除选择
                SelectedCollections.Clear();
                
                // 通知UI更新
                NotifySelectionChanged();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"加载收藏夹列表失败: {ex.Message}");
                Collections.Clear();
                SelectedCollections.Clear();
                TotalCount = 0;
                
                // 通知UI更新
                NotifySelectionChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 通知选择状态变更
        /// </summary>
        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectedItemsCount));
            OnPropertyChanged(nameof(CanEditSelectedCollection));
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Private Helper Methods for Selection Synchronization

        private void SubscribeToCollectionItemChanges(ObservableCollection<TicketCollectionInfo> collection)
        {
            if (collection == null) return;
            foreach (var item in collection)
            {
                if (item is INotifyPropertyChanged notifyItem) // 确保项实现了INotifyPropertyChanged
                {
                    notifyItem.PropertyChanged += CollectionItem_PropertyChanged;
                }
            }
        }

        private void UnsubscribeFromCollectionItemChanges(ObservableCollection<TicketCollectionInfo> collection)
        {
            if (collection == null) return;
            foreach (var item in collection)
            {
                if (item is INotifyPropertyChanged notifyItem)
                {
                    notifyItem.PropertyChanged -= CollectionItem_PropertyChanged;
                }
            }
        }

        private void Collections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is INotifyPropertyChanged notifyItem)
                    {
                        notifyItem.PropertyChanged -= CollectionItem_PropertyChanged;
                    }
                    if (item is TicketCollectionInfo ticketItem && _selectedCollections.Contains(ticketItem))
                    {
                        _selectedCollections.Remove(ticketItem);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is INotifyPropertyChanged notifyItem)
                    {
                        notifyItem.PropertyChanged += CollectionItem_PropertyChanged;
                    }
                    // 如果项在添加时已被选中 (例如，状态被持久化或在添加前设置)
                    if (item is TicketCollectionInfo ticketItem && ticketItem.IsSelected && !_selectedCollections.Contains(ticketItem))
                    {
                        _selectedCollections.Add(ticketItem);
                    }
                }
            }
            NotifySelectionChanged(); // 集合更改后，依赖于选择的属性可能需要更新
        }

        private void CollectionItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TicketCollectionInfo.IsSelected))
            {
                if (sender is TicketCollectionInfo item)
                {
                    if (item.IsSelected)
                    {
                        if (!_selectedCollections.Contains(item))
                        {
                            _selectedCollections.Add(item);
                        }
                    }
                    else
                    {
                        if (_selectedCollections.Contains(item))
                        {
                            _selectedCollections.Remove(item);
                        }
                    }
                    NotifySelectionChanged(); // IsSelected更改后，依赖于选择的属性需要更新
                }
            }
        }
        
        private void RefreshSelectedCollectionsFromSource()
        {
            if (_collections == null)
            {
                _selectedCollections?.Clear();
                return;
            }

            var currentlySelected = _collections.Where(item => item.IsSelected).ToList();
            
            // 从_selectedCollections中移除不再被选中的项
            var toRemove = _selectedCollections.Where(sc => !currentlySelected.Contains(sc)).ToList();
            foreach(var itemToRemove in toRemove) 
            {
                _selectedCollections.Remove(itemToRemove);
            }
            
            // 向_selectedCollections中添加新选中的项
            var toAdd = currentlySelected.Where(cs => !_selectedCollections.Contains(cs)).ToList();
            foreach(var itemToAdd in toAdd) 
            {
                _selectedCollections.Add(itemToAdd);
            }
        }

        #endregion
    }
} 