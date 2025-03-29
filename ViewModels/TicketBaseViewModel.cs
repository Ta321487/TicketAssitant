using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using System.Windows.Threading;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// 支付渠道视图模型基类，处理支付渠道的选择逻辑
    /// </summary>
    /// <remarks>
    /// 该类实现了支付渠道选择的业务逻辑，包括：
    /// 1. 支付宝售票和微信售票互斥（只能选择其中一个）
    /// 2. 农业银行、建设银行、工商银行三选一
    /// 
    /// 如需添加新的支付渠道，请按照以下步骤操作：
    /// 1. 在 Models/PaymentChannelFlags.cs 中的枚举中添加新的支付渠道标志，例如：
    ///    PSBC = 32       // 邮政银行
    ///    
    /// 2. 在本类中添加新的属性，例如：
    ///    private bool _isPSBCPayment;
    ///    public bool IsPSBCPayment
    ///    {
    ///        get => _isPSBCPayment;
    ///        set
    ///        {
    ///            if (_isPSBCPayment != value)
    ///            {
    ///                _isPSBCPayment = value;
    ///                OnPropertyChanged(nameof(IsPSBCPayment));
    ///                
    ///                // 如果需要与其他银行互斥
    ///                if (!_isInitializing && value)
    ///                {
    ///                    IsABCPayment = false;
    ///                    IsCCBPayment = false;
    ///                    IsICBCPayment = false;
    ///                }
    ///                
    ///                if (!_isInitializing) OnPaymentChannelChanged();
    ///            }
    ///        }
    ///    }
    ///    
    /// 3. 修改其他互斥的支付渠道属性，确保它们也与新添加的支付渠道互斥
    /// 
    /// 4. 在 GetPaymentChannelFlags() 和 SetPaymentChannelFlags() 方法中添加对新支付渠道的处理
    /// 
    /// 5. 在XAML文件(Views/AddTicketWindow.xaml和Views/EditTicketWindow.xaml)中添加新的复选框
    ///    <CheckBox Content="邮政银行" 
    ///              IsChecked="{Binding IsPSBCPayment}" 
    ///              Margin="0,0,24,0"
    ///              VerticalAlignment="Center"/>
    /// </remarks>
    public class PaymentChannelViewModel : INotifyPropertyChanged
    {
        protected bool _isInitializing = true; // 初始化标志
        
        // 票种类型启用/禁用状态
        private bool _isStudentTicketEnabled;
        private bool _isChildTicketEnabled;
        
        private bool _isAlipayPayment;
        private bool _isWeChatPayment;
        private bool _isABCPayment;
        private bool _isCCBPayment;
        private bool _isICBCPayment;

        public PaymentChannelViewModel()
        {
            // 初始化票种和支付渠道启用状态
            _isStudentTicketEnabled = true;
            _isChildTicketEnabled = true;
            _isAlipayPaymentEnabled = true;
            _isWeChatPaymentEnabled = true;
            
            _isInitializing = false;
        }

        // 支付渠道属性
        public bool IsAlipayPayment
        {
            get => _isAlipayPayment;
            set
            {
                if (_isAlipayPayment != value)
                {
                    _isAlipayPayment = value;
                    OnPropertyChanged(nameof(IsAlipayPayment));
                    
                    // 如果勾选了支付宝售票，那么微信售票不可选择
                    if (!_isInitializing && value)
                    {
                        IsWeChatPaymentEnabled = false;
                    }
                    else if (!_isInitializing && !value)
                    {
                        IsWeChatPaymentEnabled = true;
                    }
                    
                    if (!_isInitializing) OnPaymentChannelChanged();
                }
            }
        }

        public bool IsWeChatPayment
        {
            get => _isWeChatPayment;
            set
            {
                if (_isWeChatPayment != value)
                {
                    _isWeChatPayment = value;
                    OnPropertyChanged(nameof(IsWeChatPayment));
                    
                    // 如果勾选了微信售票，那么支付宝售票不可选择
                    if (!_isInitializing && value)
                    {
                        IsAlipayPaymentEnabled = false;
                    }
                    else if (!_isInitializing && !value)
                    {
                        IsAlipayPaymentEnabled = true;
                    }
                    
                    if (!_isInitializing) OnPaymentChannelChanged();
                }
            }
        }

        public bool IsABCPayment
        {
            get => _isABCPayment;
            set
            {
                if (_isABCPayment != value)
                {
                    _isABCPayment = value;
                    OnPropertyChanged(nameof(IsABCPayment));
                    
                    // 农业银行、建设银行、工商银行只能选择一个
                    if (!_isInitializing && value)
                    {
                        IsCCBPayment = false;
                        IsICBCPayment = false;
                    }
                    
                    if (!_isInitializing) OnPaymentChannelChanged();
                }
            }
        }

        public bool IsCCBPayment
        {
            get => _isCCBPayment;
            set
            {
                if (_isCCBPayment != value)
                {
                    _isCCBPayment = value;
                    OnPropertyChanged(nameof(IsCCBPayment));
                    
                    // 农业银行、建设银行、工商银行只能选择一个
                    if (!_isInitializing && value)
                    {
                        IsABCPayment = false;
                        IsICBCPayment = false;
                    }
                    
                    if (!_isInitializing) OnPaymentChannelChanged();
                }
            }
        }

        public bool IsICBCPayment
        {
            get => _isICBCPayment;
            set
            {
                if (_isICBCPayment != value)
                {
                    _isICBCPayment = value;
                    OnPropertyChanged(nameof(IsICBCPayment));
                    
                    // 农业银行、建设银行、工商银行只能选择一个
                    if (!_isInitializing && value)
                    {
                        IsABCPayment = false;
                        IsCCBPayment = false;
                    }
                    
                    if (!_isInitializing) OnPaymentChannelChanged();
                }
            }
        }

        // 控制支付宝售票是否可用
        private bool _isAlipayPaymentEnabled = true;
        public bool IsAlipayPaymentEnabled
        {
            get => _isAlipayPaymentEnabled;
            set
            {
                if (_isAlipayPaymentEnabled != value)
                {
                    _isAlipayPaymentEnabled = value;
                    OnPropertyChanged(nameof(IsAlipayPaymentEnabled));
                }
            }
        }

        // 控制微信售票是否可用
        private bool _isWeChatPaymentEnabled = true;
        public bool IsWeChatPaymentEnabled
        {
            get => _isWeChatPaymentEnabled;
            set
            {
                if (_isWeChatPaymentEnabled != value)
                {
                    _isWeChatPaymentEnabled = value;
                    OnPropertyChanged(nameof(IsWeChatPaymentEnabled));
                }
            }
        }

        /// <summary>
        /// 获取支付渠道标志位
        /// </summary>
        /// <remarks>
        /// 当添加新的支付渠道时，需要在此方法中添加对应的标志位处理。
        /// 例如添加邮政银行后，需要添加如下代码：
        /// if (IsPSBCPayment) flags |= (int)PaymentChannelFlags.PSBC;
        /// </remarks>
        /// <returns>支付渠道标志位的整数表示</returns>
        public int GetPaymentChannelFlags()
        {
            int flags = 0;
            if (IsAlipayPayment) flags |= (int)PaymentChannelFlags.Alipay;
            if (IsWeChatPayment) flags |= (int)PaymentChannelFlags.WeChat;
            if (IsABCPayment) flags |= (int)PaymentChannelFlags.ABC;
            if (IsCCBPayment) flags |= (int)PaymentChannelFlags.CCB;
            if (IsICBCPayment) flags |= (int)PaymentChannelFlags.ICBC;
            return flags;
        }

        /// <summary>
        /// 设置支付渠道标志位
        /// </summary>
        /// <remarks>
        /// 当添加新的支付渠道时，需要在此方法中添加对应的标志位处理。
        /// 例如添加邮政银行后，需要添加如下代码：
        /// IsPSBCPayment = (flags & (int)PaymentChannelFlags.PSBC) != 0;
        /// 
        /// 如果新添加的支付渠道与现有支付渠道有互斥关系，还需要在应用互斥逻辑部分添加相应处理。
        /// </remarks>
        /// <param name="flags">支付渠道标志位的整数表示</param>
        public void SetPaymentChannelFlags(int flags)
        {
            _isInitializing = true;
            
            IsAlipayPayment = (flags & (int)PaymentChannelFlags.Alipay) != 0;
            IsWeChatPayment = (flags & (int)PaymentChannelFlags.WeChat) != 0;
            IsABCPayment = (flags & (int)PaymentChannelFlags.ABC) != 0;
            IsCCBPayment = (flags & (int)PaymentChannelFlags.CCB) != 0;
            IsICBCPayment = (flags & (int)PaymentChannelFlags.ICBC) != 0;
            
            _isInitializing = false;
            
            // 应用互斥逻辑
            if (IsAlipayPayment)
            {
                IsWeChatPaymentEnabled = false;
            }
            else
            {
                IsWeChatPaymentEnabled = true;
            }
            
            if (IsWeChatPayment)
            {
                IsAlipayPaymentEnabled = false;
            }
            else
            {
                IsAlipayPaymentEnabled = true;
            }
        }

        /// <summary>
        /// 支付渠道变更事件
        /// </summary>
        public event EventHandler PaymentChannelChanged;

        /// <summary>
        /// 触发支付渠道变更事件
        /// </summary>
        protected virtual void OnPaymentChannelChanged()
        {
            PaymentChannelChanged?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 控制学生票是否可用
        public bool IsStudentTicketEnabled
        {
            get => _isStudentTicketEnabled;
            set
            {
                if (_isStudentTicketEnabled != value)
                {
                    _isStudentTicketEnabled = value;
                    OnPropertyChanged(nameof(IsStudentTicketEnabled));
                }
            }
        }

        // 控制儿童票是否可用
        public bool IsChildTicketEnabled
        {
            get => _isChildTicketEnabled;
            set
            {
                if (_isChildTicketEnabled != value)
                {
                    _isChildTicketEnabled = value;
                    OnPropertyChanged(nameof(IsChildTicketEnabled));
                }
            }
        }
    }

    /// <summary>
    /// 车票视图模型基类，提供所有车票相关视图模型共用的功能
    /// </summary>
    public abstract class TicketBaseViewModel : BaseViewModel
    {
        protected readonly DatabaseService _databaseService;
        protected readonly PaginationViewModel _paginationViewModel;
        protected readonly MainViewModel _mainViewModel;
        protected readonly NavigationService _navigationService;
        
        // 票种类型启用/禁用状态
        private bool _isStudentTicketEnabled;
        private bool _isChildTicketEnabled;
        
        // 是否正在初始化
        protected bool _isTicketTypeInitializing = false;
        
        // 选中项数量
        private int _selectedItemsCount = 0;
        // 标记是否正在更新IsAllSelected属性，避免循环调用
        private bool _isUpdatingAllSelected = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="paginationViewModel">分页视图模型</param>
        /// <param name="mainViewModel">主视图模型</param>
        protected TicketBaseViewModel(DatabaseService databaseService, PaginationViewModel paginationViewModel, MainViewModel mainViewModel)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _paginationViewModel = paginationViewModel ?? throw new ArgumentNullException(nameof(paginationViewModel));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _navigationService = new NavigationService();

            // 初始化票种类型启用状态
            _isStudentTicketEnabled = true;
            _isChildTicketEnabled = true;

            // 设置为车票查询视图
            IsTicketQuery = true;

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
            
            // 初始化添加车票命令
            AddTicketCommand = new RelayCommand(AddTicket);
            
            // 初始化选择相关命令
            SelectAllCommand = new RelayCommand(SelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll);
            ToggleSelectionCommand = new RelayCommand(ToggleSelection);
            InvertSelectionCommand = new RelayCommand(InvertSelection);
            ClearSelectionCommand = new RelayCommand(ClearSelection);
            
            // 初始化删除车票命令
            DeleteTicketsCommand = new RelayCommand(DeleteTickets, () => SelectedItemsCount > 0);
            
            // 初始化修改车票命令
            EditTicketCommand = new RelayCommand(EditTicket, () => SelectedItemsCount == 1);
            DoubleClickEditCommand = new RelayCommand<TrainRideInfo>(EditTicketByDoubleClick);
            
            // 订阅TrainRideInfo的属性变更事件
            _paginationViewModel.Items.CollectionChanged += (s, e) =>
            {
                // 当集合变更时，为新添加的项订阅属性变更事件
                if (e.NewItems != null)
                {
                    foreach (TrainRideInfo item in e.NewItems)
                    {
                        item.PropertyChanged += OnTrainRideInfoPropertyChanged;
                    }
                }
                
                // 当项被移除时，取消订阅属性变更事件
                if (e.OldItems != null)
                {
                    foreach (TrainRideInfo item in e.OldItems)
                    {
                        item.PropertyChanged -= OnTrainRideInfoPropertyChanged;
                    }
                }
                
                // 更新选中项计数
                UpdateSelectedItemsCount();
            };
            
            // 为现有项订阅属性变更事件
            foreach (var item in _paginationViewModel.Items)
            {
                item.PropertyChanged += OnTrainRideInfoPropertyChanged;
            }
        }

        /// <summary>
        /// 添加车票命令
        /// </summary>
        public ICommand AddTicketCommand { get; }
        
        /// <summary>
        /// 修改车票命令
        /// </summary>
        public ICommand EditTicketCommand { get; }
        
        /// <summary>
        /// 双击修改车票命令
        /// </summary>
        public ICommand DoubleClickEditCommand { get; }
        
        /// <summary>
        /// 添加车票
        /// </summary>
        protected virtual void AddTicket()
        {
            try
            {
                // 打开添加车票窗口
                bool result = _navigationService.OpenAddTicketWindow(_databaseService, _mainViewModel);
                
                // 如果用户保存了车票，刷新数据
                if (result)
                {
                    // 刷新数据
                    _ = RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开添加车票窗口时出错: {ex.Message}");
                LogHelper.LogTicketError("添加", "打开添加车票窗口时失败", ex);
            }
        }
        
        /// <summary>
        /// 修改车票
        /// </summary>
        protected virtual void EditTicket()
        {
            try
            {
                var selectedTicket = _paginationViewModel.Items.FirstOrDefault(t => t.IsSelected);
                if (selectedTicket != null)
                {
                    EditTicketByDoubleClick(selectedTicket);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"修改车票时出错: {ex.Message}");
                LogHelper.LogTicketError("修改", "执行车票修改操作时失败", ex);
            }
        }
        
        /// <summary>
        /// 通过双击修改车票
        /// </summary>
        /// <param name="ticket">要修改的车票</param>
        protected virtual void EditTicketByDoubleClick(TrainRideInfo ticket)
        {
            try
            {
                if (ticket != null)
                {
                    // 打开修改车票窗口
                    bool result = _navigationService.OpenEditTicketWindow(_databaseService, _mainViewModel, ticket);
                    
                    // 如果用户保存了修改，刷新数据
                    if (result)
                    {
                        // 刷新数据
                        _ = RefreshDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"修改车票时出错: {ex.Message}");
                LogHelper.LogTicketError("修改", $"双击修改车票(ID:{ticket?.Id})时失败", ex);
            }
        }
        
        /// <summary>
        /// 全选
        /// </summary>
        protected virtual void SelectAll()
        {
            // 设置全选状态
            IsAllSelected = true;
            
            // 确保所有项都被选中
            ApplySelectionToAll(true);
            
            // 通知UI更新
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectionToggleText));
            OnPropertyChanged(nameof(SelectionToggleIcon));
            OnPropertyChanged(nameof(SelectionToggleTooltip));
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        protected virtual void UnselectAll()
        {
            // 设置取消全选状态
            IsAllSelected = false;
            
            // 确保所有项都被取消选中
            ApplySelectionToAll(false);
            
            // 通知UI更新
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectionToggleText));
            OnPropertyChanged(nameof(SelectionToggleIcon));
            OnPropertyChanged(nameof(SelectionToggleTooltip));
        }

        /// <summary>
        /// 切换全选/取消全选
        /// </summary>
        protected virtual void ToggleSelection()
        {
            // 切换全选状态
            IsAllSelected = !IsAllSelected;
            
            // 应用选择状态到所有项
            ApplySelectionToAll(IsAllSelected);
            
            // 通知UI更新
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectionToggleText));
            OnPropertyChanged(nameof(SelectionToggleIcon));
            OnPropertyChanged(nameof(SelectionToggleTooltip));
        }

        /// <summary>
        /// 反选
        /// </summary>
        protected virtual void InvertSelection()
        {
            if (TrainRideInfos == null || TrainRideInfos.Count == 0)
                return;

            // 反转每一项的选择状态
            foreach (var item in TrainRideInfos)
            {
                item.IsSelected = !item.IsSelected;
            }

            // 更新选中项计数
            UpdateSelectedItemsCount();
            
            // 检查是否所有项都被选中或取消选中，以更新IsAllSelected属性
            bool allSelected = TrainRideInfos.Count > 0 && TrainRideInfos.All(item => item.IsSelected);
            
            // 避免触发ApplySelectionToAll
            _isUpdatingAllSelected = true;
            IsAllSelected = allSelected;
            _isUpdatingAllSelected = false;
            
            // 通知UI更新
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(SelectionToggleText));
            OnPropertyChanged(nameof(SelectionToggleIcon));
            OnPropertyChanged(nameof(SelectionToggleTooltip));
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        protected virtual void ClearSelection()
        {
            if (TrainRideInfos == null || TrainRideInfos.Count == 0)
                return;

            // 清除所有选择
            foreach (var item in TrainRideInfos)
            {
                item.IsSelected = false;
            }

            // 更新选中项计数
            UpdateSelectedItemsCount();
        }

        /// <summary>
        /// 应用选择状态到所有项
        /// </summary>
        /// <param name="isSelected">是否选中</param>
        protected virtual void ApplySelectionToAll(bool isSelected)
        {
            if (TrainRideInfos == null || TrainRideInfos.Count == 0)
                return;

            // 批量更新所有项的选择状态
            foreach (var item in TrainRideInfos)
            {
                // 仅当需要更改状态时才更新，避免不必要的UI更新
                if (item.IsSelected != isSelected)
                {
                    item.IsSelected = isSelected;
                }
            }

            // 更新UI状态（改为同步更新，避免使用Dispatcher触发更新问题）
            UpdateSelectedItemsCount();
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(SelectionToggleText));
            OnPropertyChanged(nameof(SelectionToggleIcon));
            OnPropertyChanged(nameof(SelectionToggleTooltip));
            
            // 刷新命令状态
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 允许外部组件更新选中项计数的公共方法
        /// </summary>
        /// <param name="count">选中项数量</param>
        public void UpdateSelectedItemsCountExternal(int count)
        {
            SelectedItemsCount = count;
            
            // 刷新命令状态，确保EditTicketCommand可用性正确更新
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 更新选中项计数
        /// </summary>
        protected virtual void UpdateSelectedItemsCount()
        {
            if (TrainRideInfos == null)
            {
                SelectedItemsCount = 0;
                return;
            }

            SelectedItemsCount = TrainRideInfos.Count(item => item.IsSelected);
            
            // 通知UI更新HasSelectedItems属性
            OnPropertyChanged(nameof(HasSelectedItems));
            
            // 刷新命令状态
            CommandManager.InvalidateRequerySuggested();
        }
        
        /// <summary>
        /// 删除选中的车票
        /// </summary>
        protected virtual async void DeleteTickets()
        {
            if (TrainRideInfos == null || SelectedItemsCount == 0)
                return;

            // 获取选中的车票
            var selectedTickets = TrainRideInfos.Where(item => item.IsSelected).ToList();
            
            // 构建确认消息
            string confirmMessage = SelectedItemsCount == 1
                ? "确定要删除选中的车票吗？此操作不可撤销。"
                : $"确定要删除选中的 {SelectedItemsCount} 张车票吗？此操作不可撤销。";
            
            // 显示确认对话框
            var result = TA_WPF.Views.MessageDialog.Show(
                confirmMessage,
                "删除确认",
                TA_WPF.Views.MessageType.Warning,
                TA_WPF.Views.MessageButtons.YesNo);
            
            if (result != true)
                return;
            
            try
            {
                // 设置加载状态
                _paginationViewModel.IsLoading = true;
                
                // 删除车票
                int deletedCount = 0;
                foreach (var ticket in selectedTickets)
                {
                    // 调用数据库服务删除车票
                    bool success = await _databaseService.DeleteTicketAsync(ticket.Id);
                    if (success)
                        deletedCount++;
                }
                
                // 显示结果
                if (deletedCount > 0)
                {
                    string resultMessage = deletedCount == 1
                        ? "已成功删除1张车票。"
                        : $"已成功删除{deletedCount}张车票。";
                    
                    TA_WPF.Views.MessageDialog.Show(
                        resultMessage,
                        "删除成功",
                        TA_WPF.Views.MessageType.Information,
                        TA_WPF.Views.MessageButtons.Ok);
                    
                    // 清除所有缓存数据
                    _paginationViewModel.ClearCache();
                    
                    // 重新获取总记录数
                    await RefreshTotalItemsAsync();
                    
                    // 计算当前页是否超出总页数
                    if (_paginationViewModel.CurrentPage > _paginationViewModel.TotalPages && _paginationViewModel.TotalPages > 0)
                    {
                        // 如果当前页超出总页数，则跳转到最后一页
                        _paginationViewModel.CurrentPage = _paginationViewModel.TotalPages;
                    }
                    else if (_paginationViewModel.TotalPages == 0)
                    {
                        // 如果没有数据，则跳转到第一页
                        _paginationViewModel.CurrentPage = 1;
                    }
                    
                    // 刷新数据
                    await RefreshDataAsync();
                    
                    // 删除后，重置全选状态
                    if (IsAllSelected)
                    {
                        _isUpdatingAllSelected = true;
                        IsAllSelected = false;
                        _isUpdatingAllSelected = false;
                    }
                }
                else
                {
                    TA_WPF.Views.MessageDialog.Show(
                        "未能删除任何车票，请稍后重试。",
                        "删除失败",
                        TA_WPF.Views.MessageType.Error,
                        TA_WPF.Views.MessageButtons.Ok);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"删除车票时出错: {ex.Message}");
                LogHelper.LogTicketError("删除", $"删除选中的车票(共{selectedTickets.Count}张)时失败", ex);
            }
            finally
            {
                // 结束加载状态
                _paginationViewModel.IsLoading = false;
            }
        }
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        protected virtual async Task RefreshDataAsync()
        {
            try
            {
                // 设置加载状态
                _paginationViewModel.IsLoading = true;
                
                // 清除缓存
                _paginationViewModel.ClearCache();
                
                // 保存当前的全选状态
                bool wasAllSelected = IsAllSelected;
                
                // 重新加载当前页数据
                await LoadPageDataAsync();
                
                // 如果之前是全选状态，则在数据刷新后重新应用全选
                if (wasAllSelected)
                {
                    ApplySelectionToAll(true);
                }
                
                // 手动触发属性变更通知，确保UI更新
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
                MessageBoxHelper.ShowError($"刷新数据时出错: {ex.Message}");
                LogHelper.LogTicketError("刷新", "刷新车票数据时失败", ex);
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
        protected virtual void OnPageChanged(object sender, EventArgs e)
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // 翻页时，确保重置全选状态
                _isUpdatingAllSelected = true;
                IsAllSelected = false;
                _isUpdatingAllSelected = false;
                
                // 使用Dispatcher以更高优先级执行数据加载
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(async () =>
                {
                    try 
                    {
                        // 加载新的页面数据
                        await LoadPageDataAsync();
                        
                        // 更新选中项计数
                        UpdateSelectedItemsCount();
                        
                        // 再次触发UI更新通知
                        OnPropertyChanged(nameof(TrainRideInfos));
                        OnPropertyChanged(nameof(IsAllSelected));
                        OnPropertyChanged(nameof(SelectionToggleText));
                        OnPropertyChanged(nameof(SelectionToggleIcon));
                        OnPropertyChanged(nameof(SelectionToggleTooltip));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载页面数据出错: {ex.Message}");
                    }
                    finally
                    {
                        // 确保加载状态被重置
                        _paginationViewModel.IsLoading = false;
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"页面变更处理出错: {ex.Message}");
                // 确保加载状态被重置
                _paginationViewModel.IsLoading = false;
            }
        }
        
        /// <summary>
        /// 页大小变更事件处理
        /// </summary>
        protected virtual void OnPageSizeChanged(object sender, EventArgs e)
        {
            try
            {
                // 确保加载状态已设置
                _paginationViewModel.IsLoading = true;
                
                // 手动触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(CanNavigateToFirstPage));
                OnPropertyChanged(nameof(CanNavigateToPreviousPage));
                OnPropertyChanged(nameof(CanNavigateToNextPage));
                OnPropertyChanged(nameof(CanNavigateToLastPage));
                
                // 刷新命令状态
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // 页大小改变时，重置全选状态
                _isUpdatingAllSelected = true;
                IsAllSelected = false;
                _isUpdatingAllSelected = false;
                
                // 使用Dispatcher以更高优先级执行数据加载
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(async () =>
                {
                    try 
                    {
                        // 先刷新总记录数，确保总页数计算正确
                        await RefreshTotalItemsAsync();
                        
                        // 加载新的页面数据
                        await LoadPageDataAsync();
                        
                        // 更新选中项计数
                        UpdateSelectedItemsCount();
                        
                        // 再次触发UI更新通知
                        OnPropertyChanged(nameof(TotalPages));
                        OnPropertyChanged(nameof(TrainRideInfos));
                        OnPropertyChanged(nameof(IsAllSelected));
                        OnPropertyChanged(nameof(SelectionToggleText));
                        OnPropertyChanged(nameof(SelectionToggleIcon));
                        OnPropertyChanged(nameof(SelectionToggleTooltip));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载页面数据出错: {ex.Message}");
                    }
                    finally
                    {
                        // 确保加载状态被重置
                        _paginationViewModel.IsLoading = false;
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"页大小变更处理出错: {ex.Message}");
                // 确保加载状态被重置
                _paginationViewModel.IsLoading = false;
            }
        }

        /// <summary>
        /// TrainRideInfo属性变更事件处理
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        protected virtual void OnTrainRideInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrainRideInfo.IsSelected))
            {
                UpdateSelectedItemsCount();
                
                // 检查是否所有项都被选中或取消选中，以更新IsAllSelected属性
                if (TrainRideInfos != null && TrainRideInfos.Count > 0)
                {
                    bool allSelected = TrainRideInfos.All(item => item.IsSelected);
                    bool noneSelected = TrainRideInfos.All(item => !item.IsSelected);
                    
                    if (allSelected && !IsAllSelected)
                    {
                        // 避免触发ApplySelectionToAll
                        _isUpdatingAllSelected = true;
                        IsAllSelected = true;
                        _isUpdatingAllSelected = false;
                    }
                    else if (noneSelected && IsAllSelected)
                    {
                        // 避免触发ApplySelectionToAll
                        _isUpdatingAllSelected = true;
                        IsAllSelected = false;
                        _isUpdatingAllSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// 加载页面数据
        /// </summary>
        protected abstract Task LoadPageDataAsync();

        /// <summary>
        /// 刷新总记录数
        /// </summary>
        /// <returns>异步任务</returns>
        protected virtual async Task RefreshTotalItemsAsync()
        {
            await _paginationViewModel.RefreshTotalItemsAsync();
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
        
        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount
        {
            get => _selectedItemsCount;
            protected set
            {
                if (_selectedItemsCount != value)
                {
                    _selectedItemsCount = value;
                    OnPropertyChanged(nameof(SelectedItemsCount));
                    OnPropertyChanged(nameof(SelectionSummary));
                    OnPropertyChanged(nameof(HasSelectedItems));
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems => SelectedItemsCount > 0;

        /// <summary>
        /// 选择摘要信息
        /// </summary>
        public string SelectionSummary => SelectedItemsCount > 0 
            ? $"已选择 {SelectedItemsCount} 项" 
            : string.Empty;

        /// <summary>
        /// 切换按钮文本
        /// </summary>
        public string SelectionToggleText => IsAllSelected ? "取消全选" : "全选";

        /// <summary>
        /// 切换按钮图标
        /// </summary>
        public string SelectionToggleIcon => IsAllSelected ? "CheckboxMultipleBlankOutline" : "CheckboxMultipleMarkedOutline";

        /// <summary>
        /// 切换按钮提示
        /// </summary>
        public string SelectionToggleTooltip => IsAllSelected ? "取消全选" : "全选";

        /// <summary>
        /// 是否全选
        /// </summary>
        public override bool IsAllSelected
        {
            get => base.IsAllSelected;
            set
            {
                if (base.IsAllSelected != value)
                {
                    base.IsAllSelected = value;
                    
                    // 避免循环调用
                    if (!_isUpdatingAllSelected)
                    {
                        // 应用选择状态到所有项
                        ApplySelectionToAll(value);
                    }
                    
                    // 通知UI更新切换按钮的文本和图标
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(SelectionToggleText));
                    OnPropertyChanged(nameof(SelectionToggleIcon));
                    OnPropertyChanged(nameof(SelectionToggleTooltip));
                    
                    // 刷新命令状态
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 数据库服务
        /// </summary>
        public DatabaseService DatabaseService => _databaseService;

        /// <summary>
        /// 主视图模型
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        // 票种类型互斥逻辑属性
        
        // 控制学生票是否可用
        public bool IsStudentTicketEnabled
        {
            get => _isStudentTicketEnabled;
            set
            {
                if (_isStudentTicketEnabled != value)
                {
                    _isStudentTicketEnabled = value;
                    OnPropertyChanged(nameof(IsStudentTicketEnabled));
                }
            }
        }

        // 控制儿童票是否可用
        public bool IsChildTicketEnabled
        {
            get => _isChildTicketEnabled;
            set
            {
                if (_isChildTicketEnabled != value)
                {
                    _isChildTicketEnabled = value;
                    OnPropertyChanged(nameof(IsChildTicketEnabled));
                }
            }
        }

        // 学生票选择状态变更方法
        protected virtual void OnStudentTicketChanged(bool value)
        {
            // 如果选择了学生票，则儿童票不可选择
            if (!_isTicketTypeInitializing && value)
            {
                IsChildTicketEnabled = false;
            }
            else if (!_isTicketTypeInitializing && !value)
            {
                IsChildTicketEnabled = true;
            }
        }

        // 儿童票选择状态变更方法
        protected virtual void OnChildTicketChanged(bool value)
        {
            // 如果选择了儿童票，则学生票不可选择
            if (!_isTicketTypeInitializing && value)
            {
                IsStudentTicketEnabled = false;
            }
            else if (!_isTicketTypeInitializing && !value)
            {
                IsStudentTicketEnabled = true;
            }
        }

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
        
        /// <summary>
        /// 全选命令
        /// </summary>
        public System.Windows.Input.ICommand SelectAllCommand { get; }

        /// <summary>
        /// 取消全选命令
        /// </summary>
        public System.Windows.Input.ICommand UnselectAllCommand { get; }

        /// <summary>
        /// 切换全选/取消全选命令
        /// </summary>
        public System.Windows.Input.ICommand ToggleSelectionCommand { get; }

        /// <summary>
        /// 反选命令
        /// </summary>
        public System.Windows.Input.ICommand InvertSelectionCommand { get; }

        /// <summary>
        /// 清除选择命令
        /// </summary>
        public System.Windows.Input.ICommand ClearSelectionCommand { get; }

        /// <summary>
        /// 删除车票命令
        /// </summary>
        public ICommand DeleteTicketsCommand { get; }

        #endregion
    }
} 