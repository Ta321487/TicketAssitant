using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Models;
using TA_WPF.Utils;
using TA_WPF.ViewModels;
namespace TA_WPF.Views
{
    /// <summary>
    /// Interaction logic for QueryAllRoutesPage.xaml
    /// </summary>
    public partial class QueryAllRoutesPage : UserControl
    {
        private bool _isInternalSelectionChange = false;
        private RouteInfo _lastSelectedItem = null;
        private StackPanel _pageInfoPanel;
        private TextBox _pageNumberInput;
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        private bool _isProcessingDoubleClick = false;

        public QueryAllRoutesPage()
        {
            InitializeComponent();

            // 在DataContext变更后，订阅ViewModel的事件
            DataContextChanged += QueryAllRoutesPage_DataContextChanged;

            // 初始化页码相关控件
            InitializePageComponents();

            // 添加加载完成事件，确保DataGrid获得焦点
            Loaded += QueryAllRoutesPage_Loaded;

            // 手动绑定PreviewMouseRightButtonDown事件，确保即使在窗口关闭后也能捕获右键点击
            var dataGrid = this.FindName("RoutesDataGrid") as DataGrid;
            if (dataGrid != null)
            {
                dataGrid.PreviewMouseRightButtonDown += RoutesDataGrid_PreviewMouseRightButtonDown;
                dataGrid.MouseDoubleClick += RoutesDataGrid_MouseDoubleClick;
                Debug.WriteLine("已手动绑定PreviewMouseRightButtonDown和MouseDoubleClick事件到DataGrid");
            }
        }

        /// <summary>
        /// 初始化页码相关组件
        /// </summary>
        private void InitializePageComponents()
        {
            // 获取控件引用
            _pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            _pageNumberInput = this.FindName("PageNumberInput") as TextBox;

            // 初始化页码提示工具提示
            _tooltipText = new TextBlock
            {
                Padding = new Thickness(8),
                Background = Brushes.DarkSlateGray,
                Foreground = Brushes.White,
                FontSize = 14
            };

            _pageNumberTooltip = new Popup
            {
                Child = _tooltipText,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };
        }

        /// <summary>
        /// 页面加载完成后的处理
        /// </summary>
        private void QueryAllRoutesPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保DataGrid获取焦点，以便键盘事件能正常触发
            var dataGrid = GetRoutesDataGrid();
            if (dataGrid != null)
            {
                // 确保绑定了预览右键点击事件，这对解决第二次及后续右键点击问题至关重要
                dataGrid.PreviewMouseRightButtonDown -= RoutesDataGrid_PreviewMouseRightButtonDown; // 移除可能存在的重复订阅
                dataGrid.PreviewMouseRightButtonDown += RoutesDataGrid_PreviewMouseRightButtonDown;
                
                // 确保绑定了双击事件
                dataGrid.MouseDoubleClick -= RoutesDataGrid_MouseDoubleClick; // 移除可能存在的重复订阅
                dataGrid.MouseDoubleClick += RoutesDataGrid_MouseDoubleClick;
                
                Debug.WriteLine("QueryAllRoutesPage_Loaded - 已确保绑定PreviewMouseRightButtonDown和MouseDoubleClick事件");

                dataGrid.Focus();
            }
        }

        /// <summary>
        /// 处理页码信息面板的点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pageInfoPanel == null || _pageNumberInput == null)
            {
                _pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                _pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            }

            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                _pageInfoPanel.Visibility = Visibility.Collapsed;
                _pageNumberInput.Visibility = Visibility.Visible;

                // 设置当前页码为默认值
                var viewModel = DataContext as QueryAllRoutesViewModel;
                if (viewModel != null)
                {
                    _pageNumberInput.Text = viewModel.PaginationViewModel.CurrentPage.ToString();
                }

                // 聚焦并全选
                _pageNumberInput.Focus();
                _pageNumberInput.SelectAll();
            }
        }

        /// <summary>
        /// 处理页码输入框的键盘事件
        /// </summary>
        private void PageNumberInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryNavigateToPage();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // 取消输入，恢复显示页码信息
                if (_pageInfoPanel != null && _pageNumberInput != null)
                {
                    _pageInfoPanel.Visibility = Visibility.Visible;
                    _pageNumberInput.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 处理页码输入框失去焦点事件
        /// </summary>
        private void PageNumberInput_LostFocus(object sender, RoutedEventArgs e)
        {
            // 恢复显示页码信息
            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                _pageInfoPanel.Visibility = Visibility.Visible;
                _pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 限制只能输入数字
        /// </summary>
        private void PageNumberInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            // 检测输入的数字是否在有效范围内
            var textBox = sender as TextBox;
            var viewModel = DataContext as QueryAllRoutesViewModel;
            if (textBox != null && viewModel != null)
            {
                // 获取输入后的完整文本
                string newText = textBox.Text.Substring(0, textBox.SelectionStart) + e.Text + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

                // 尝试解析为数字
                if (int.TryParse(newText, out int pageNumber))
                {
                    // 如果输入的数字大于总页数，则不允许输入
                    if (pageNumber > viewModel.PaginationViewModel.TotalPages)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 尝试导航到指定页码
        /// </summary>
        private void TryNavigateToPage()
        {
            if (_pageInfoPanel == null || _pageNumberInput == null)
                return;

            var viewModel = DataContext as QueryAllRoutesViewModel;
            if (viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(_pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= viewModel.PaginationViewModel.TotalPages)
                {
                    // 设置新的页码
                    viewModel.PaginationViewModel.CurrentPage = pageNumber;

                    // 确保页码变更后触发数据加载
                    viewModel.PaginationViewModel.IsInitialized = true;

                    // 直接调用加载方法确保数据刷新
                    _ = viewModel.LoadRoutesAsync();
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {viewModel.PaginationViewModel.TotalPages} 之间";
                    _pageNumberTooltip.PlacementTarget = _pageNumberInput;
                    _pageNumberTooltip.IsOpen = true;

                    // 3秒后自动关闭提示
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, args) =>
                    {
                        _pageNumberTooltip.IsOpen = false;
                        timer.Stop();
                    };
                    timer.Start();

                    // 恢复原始页码
                    _pageNumberInput.Text = viewModel.PaginationViewModel.CurrentPage.ToString();
                    _pageNumberInput.SelectAll();
                    return;
                }
            }

            // 恢复显示页码信息
            _pageInfoPanel.Visibility = Visibility.Visible;
            _pageNumberInput.Visibility = Visibility.Collapsed;
        }

        private void QueryAllRoutesPage_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is QueryAllRoutesViewModel oldViewModel)
            {
                // 取消订阅旧的ViewModel事件
                oldViewModel.SelectionChanged -= ViewModel_SelectionChanged;
            }

            if (e.NewValue is QueryAllRoutesViewModel newViewModel)
            {
                // 订阅新的ViewModel事件
                newViewModel.SelectionChanged += ViewModel_SelectionChanged;
            }
        }

        private void ViewModel_SelectionChanged(object sender, QueryAllRoutesViewModel.RouteSelectionChangedEventArgs e)
        {
            try
            {
                _isInternalSelectionChange = true;

                var dataGrid = GetRoutesDataGrid();
                if (dataGrid != null)
                {
                    // 清除之前的选择
                    dataGrid.SelectedItems.Clear();

                    // 添加新的选择
                    foreach (var item in e.AddedItems)
                    {
                        dataGrid.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                _isInternalSelectionChange = false;
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 防止循环事件
            if (_isInternalSelectionChange) return;

            Debug.WriteLine($"DataGrid_SelectionChanged: 添加项 {e.AddedItems.Count}, 移除项 {e.RemovedItems.Count}");

            if (DataContext is QueryAllRoutesViewModel viewModel)
            {
                // 获取当前激活的键盘修饰键状态
                bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                bool isModifierKeyPressed = isCtrlPressed || isShiftPressed;

                Debug.WriteLine($"DataGrid_SelectionChanged - Ctrl: {isCtrlPressed}, Shift: {isShiftPressed}");

                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                // 如果是Shift键按下状态，阻止默认的清除选择行为
                if (isShiftPressed && _lastSelectedItem != null && e.AddedItems.Count > 0)
                {
                    try
                    {
                        _isInternalSelectionChange = true;

                        // 防止原有选择项被清除
                        foreach (RouteInfo item in e.RemovedItems)
                        {
                            // 在Shift模式下，不应该移除之前的选择
                            Debug.WriteLine($"保留Shift选择中的项: {item.RouteName}");
                            if (!dataGrid.SelectedItems.Contains(item))
                            {
                                dataGrid.SelectedItems.Add(item);
                            }
                            item.IsSelected = true;

                            // 同步到ViewModel
                            if (!viewModel.SelectedRoutes.Contains(item))
                            {
                                viewModel.SelectedRoutes.Add(item);
                            }
                        }

                        // 记录当前选中项，供Shift选择使用
                        RouteInfo currentItem = e.AddedItems[0] as RouteInfo;

                        // 直接调用HandleShiftSelection处理范围选择
                        Debug.WriteLine("处理Shift连续选择");
                        HandleShiftSelection(viewModel, currentItem, dataGrid);

                        // 更新SelectedRoute属性
                        if (viewModel.SelectedRoutes.Count == 1)
                        {
                            viewModel.SelectedRoute = viewModel.SelectedRoutes[0];
                        }
                        else
                        {
                            viewModel.SelectedRoute = null;
                        }

                        // 手动触发属性更新
                        viewModel.NotifySelectionChanged();

                        Debug.WriteLine($"Shift选择后当前选中项数量: {viewModel.SelectedRoutes.Count}");
                        return;
                    }
                    finally
                    {
                        _isInternalSelectionChange = false;
                    }
                }

                // 如果是Ctrl键按下状态，使用自定义处理
                else if (isCtrlPressed && e.AddedItems.Count > 0)
                {
                    try
                    {
                        _isInternalSelectionChange = true;

                        // 防止在Ctrl键模式下其他选中项被错误清除
                        foreach (RouteInfo item in e.RemovedItems)
                        {
                            if (item.IsSelected)
                            {
                                Debug.WriteLine($"在Ctrl模式下保留选中状态: {item.RouteName}");
                                if (!dataGrid.SelectedItems.Contains(item))
                                {
                                    dataGrid.SelectedItems.Add(item);
                                }
                            }
                        }

                        // 处理新添加的项
                        foreach (RouteInfo item in e.AddedItems)
                        {
                            Debug.WriteLine($"Ctrl模式添加项: {item.RouteName}");
                            item.IsSelected = true;
                            if (!viewModel.SelectedRoutes.Contains(item))
                            {
                                viewModel.SelectedRoutes.Add(item);
                            }
                        }

                        // 记录最后选中的项
                        if (e.AddedItems.Count > 0)
                        {
                            _lastSelectedItem = e.AddedItems[e.AddedItems.Count - 1] as RouteInfo;
                        }

                        // 同步数据状态和UI状态
                        foreach (var item in viewModel.Routes)
                        {
                            bool shouldBeSelected = viewModel.SelectedRoutes.Contains(item);

                            if (shouldBeSelected && !dataGrid.SelectedItems.Contains(item))
                            {
                                dataGrid.SelectedItems.Add(item);
                            }
                            else if (!shouldBeSelected && dataGrid.SelectedItems.Contains(item))
                            {
                                dataGrid.SelectedItems.Remove(item);
                            }

                            item.IsSelected = shouldBeSelected;
                        }

                        // 更新SelectedRoute属性
                        if (viewModel.SelectedRoutes.Count == 1)
                        {
                            viewModel.SelectedRoute = viewModel.SelectedRoutes[0];
                        }
                        else
                        {
                            viewModel.SelectedRoute = null;
                        }

                        // 手动触发属性更新
                        viewModel.NotifySelectionChanged();

                        Debug.WriteLine($"Ctrl处理后当前选中项数量: {viewModel.SelectedRoutes.Count}");
                        return;
                    }
                    finally
                    {
                        _isInternalSelectionChange = false;
                    }
                }

                // 无修饰键的常规处理
                try
                {
                    _isInternalSelectionChange = true;

                    // 更新所有项的选择状态
                    foreach (RouteInfo item in e.RemovedItems)
                    {
                        Debug.WriteLine($"移除项: {item.RouteName}");
                        item.IsSelected = false;
                        if (viewModel.SelectedRoutes.Contains(item))
                        {
                            viewModel.SelectedRoutes.Remove(item);
                        }
                    }

                    // 添加新选择的项
                    foreach (RouteInfo item in e.AddedItems)
                    {
                        Debug.WriteLine($"添加项: {item.RouteName}");
                        item.IsSelected = true;
                        if (!viewModel.SelectedRoutes.Contains(item))
                        {
                            viewModel.SelectedRoutes.Add(item);
                        }
                    }

                    // 记录最后选中的项，用于后续Shift多选操作
                    if (e.AddedItems.Count > 0)
                    {
                        _lastSelectedItem = e.AddedItems[e.AddedItems.Count - 1] as RouteInfo;
                        Debug.WriteLine($"更新最后选中项: {_lastSelectedItem?.RouteName}");
                    }

                    // 更新SelectedRoute属性以便修改按钮能正确获取
                    if (viewModel.SelectedRoutes.Count == 1)
                    {
                        viewModel.SelectedRoute = viewModel.SelectedRoutes[0];
                    }
                    else
                    {
                        viewModel.SelectedRoute = null;
                    }

                    // 手动触发属性更新
                    viewModel.NotifySelectionChanged();

                    // 确保选中项在视觉上也被选中（显示紫色指示器）
                    // 先清除再添加所有选中项，确保UI状态与模型状态完全同步
                    dataGrid.SelectedItems.Clear();
                    foreach (RouteInfo item in viewModel.SelectedRoutes)
                    {
                        dataGrid.SelectedItems.Add(item);
                    }

                    Debug.WriteLine($"当前选中项数量: {viewModel.SelectedRoutes.Count}");
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }
        }

        /// <summary>
        /// 处理Shift键连续选择
        /// </summary>
        private void HandleShiftSelection(QueryAllRoutesViewModel viewModel, RouteInfo currentItem, DataGrid dataGrid)
        {
            // 当没有上一次选择或当前选择为空时，直接返回
            if (_lastSelectedItem == null || currentItem == null)
            {
                Debug.WriteLine("HandleShiftSelection: 缺少必要的参考点，取消处理");
                return;
            }

            Debug.WriteLine($"HandleShiftSelection: 从 {_lastSelectedItem.RouteName} 到 {currentItem.RouteName}");

            // 防止重复选择同一个项
            if (_lastSelectedItem == currentItem)
            {
                Debug.WriteLine("选择了相同的项，无需处理范围选择");
                return;
            }

            // 找到上一个选中项和当前选中项的索引
            int lastIndex = -1;
            int currentIndex = -1;

            // 遍历所有项目获取索引，避免可能的索引不匹配问题
            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                var item = dataGrid.Items[i] as RouteInfo;
                if (item == _lastSelectedItem)
                {
                    lastIndex = i;
                }
                if (item == currentItem)
                {
                    currentIndex = i;
                }

                // 找到两个索引后可以提前退出循环
                if (lastIndex != -1 && currentIndex != -1)
                {
                    break;
                }
            }

            if (lastIndex == -1 || currentIndex == -1)
            {
                Debug.WriteLine("找不到索引，使用Items.IndexOf再次尝试");

                // 再次尝试使用IndexOf获取索引
                lastIndex = dataGrid.Items.IndexOf(_lastSelectedItem);
                currentIndex = dataGrid.Items.IndexOf(currentItem);

                if (lastIndex == -1 || currentIndex == -1)
                {
                    Debug.WriteLine("仍然找不到索引，取消处理");
                    return;
                }
            }

            Debug.WriteLine($"索引范围: 从 {lastIndex} 到 {currentIndex}");

            // 计算开始和结束索引
            int startIndex = System.Math.Min(lastIndex, currentIndex);
            int endIndex = System.Math.Max(lastIndex, currentIndex);

            Debug.WriteLine($"选择范围: {startIndex} 到 {endIndex}");

            // 为避免循环事件，在一批操作中完成所有选择
            try
            {
                _isInternalSelectionChange = true;

                // 确保已经选中的项不会丢失（处理ShiftKey不会清除原有选择）
                var existingSelectedItems = new List<RouteInfo>();
                foreach (RouteInfo item in dataGrid.SelectedItems)
                {
                    existingSelectedItems.Add(item);
                }

                // 确保原始选择不丢失
                if (!existingSelectedItems.Contains(_lastSelectedItem))
                {
                    existingSelectedItems.Add(_lastSelectedItem);
                }

                // 选择范围内的所有项
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (i >= 0 && i < dataGrid.Items.Count)
                    {
                        var item = dataGrid.Items[i] as RouteInfo;
                        if (item != null)
                        {
                            Debug.WriteLine($"处理范围内项: {item.RouteName}");

                            // 首先确保数据模型状态正确
                            item.IsSelected = true;

                            // 然后确保视图模型状态正确
                            if (!viewModel.SelectedRoutes.Contains(item))
                            {
                                viewModel.SelectedRoutes.Add(item);
                            }

                            // 添加到临时选中列表
                            if (!existingSelectedItems.Contains(item))
                            {
                                existingSelectedItems.Add(item);
                            }
                        }
                    }
                }

                // 重要：确保UI状态与模型状态同步
                // 先清除所有DataGrid选择，然后重新添加所有需要选中的项
                dataGrid.SelectedItems.Clear();
                foreach (var item in existingSelectedItems)
                {
                    dataGrid.SelectedItems.Add(item);
                    Debug.WriteLine($"同步Shift选择状态: {item.RouteName}");
                }

                // 手动触发属性更新
                viewModel.NotifySelectionChanged();

                Debug.WriteLine($"Shift选择完成，当前选中项: {viewModel.SelectedRoutes.Count}");
            }
            finally
            {
                _isInternalSelectionChange = false;
            }
        }

        /// <summary>
        /// 处理键盘按键事件，支持Ctrl+A全选和删除键
        /// </summary>
        private void RoutesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is QueryAllRoutesViewModel viewModel)
            {
                // 处理Ctrl+A全选
                if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (viewModel.CanSelectAll())
                    {
                        viewModel.SelectAll();
                        e.Handled = true;
                    }
                }

                // 处理Delete键删除选中项
                if (e.Key == Key.Delete && viewModel.SelectedRoutes.Count > 0)
                {
                    // 直接调用批量删除命令，与红色删除按钮行为一致
                    viewModel.DeleteRoutesCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 备用的键盘事件处理，确保在PreviewKeyDown失效时也能捕获键盘事件
        /// </summary>
        private void RoutesDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果PreviewKeyDown已经处理了事件，则直接返回
            if (e.Handled) return;

            if (DataContext is QueryAllRoutesViewModel viewModel)
            {
                // 备份处理Ctrl+A全选
                if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    Debug.WriteLine("KeyDown: 捕获到Ctrl+A组合键");
                    if (viewModel.CanSelectAll())
                    {
                        viewModel.SelectAll();
                        e.Handled = true;
                    }
                }

                // 备份处理Delete键删除选中项
                if (e.Key == Key.Delete && viewModel.SelectedRoutes.Count > 0)
                {
                    Debug.WriteLine("KeyDown: 捕获到Delete键");
                    // 直接调用批量删除命令
                    viewModel.DeleteRoutesCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 获取RoutesDataGrid控件的引用
        /// </summary>
        private DataGrid GetRoutesDataGrid()
        {
            return this.FindName("RoutesDataGrid") as DataGrid;
        }

        /// <summary>
        /// 处理DataGrid的鼠标点击事件
        /// </summary>
        private void RoutesDataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(DataContext is QueryAllRoutesViewModel viewModel) || !(sender is DataGrid dataGrid))
                return;

            // 如果是左键双击，不拦截事件，让双击事件处理器处理
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                Debug.WriteLine("RoutesDataGrid_PreviewMouseDown - 检测到左键双击，不拦截事件");
                // 重要：不设置e.Handled = true，允许事件继续传播
                return;
            }

            // 获取修饰键状态
            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool isModifierKeyPressed = isCtrlPressed || isShiftPressed;

            Debug.WriteLine($"RoutesDataGrid_PreviewMouseDown - Ctrl: {isCtrlPressed}, Shift: {isShiftPressed}");

            // 获取点击位置下的行
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            DataGridRow row = null;

            // 向上查找DataGridRow
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (!(dep is DataGridRow clickedRow && clickedRow.Item is RouteInfo clickedItem))
                return;

            // 记录当前点击的行数据
            row = clickedRow;

            // 如果是Shift键多选
            if (isShiftPressed && _lastSelectedItem != null && _lastSelectedItem != clickedItem)
            {
                Debug.WriteLine($"Shift键多选: 从 {_lastSelectedItem.RouteName} 到 {clickedItem.RouteName}");

                try
                {
                    _isInternalSelectionChange = true;

                    // 记录当前选中项，供后续处理
                    if (!viewModel.SelectedRoutes.Contains(clickedItem))
                    {
                        viewModel.SelectedRoutes.Add(clickedItem);
                        clickedItem.IsSelected = true;
                    }

                    // 执行范围选择
                    HandleShiftSelection(viewModel, clickedItem, dataGrid);

                    // 阻止默认的选择行为，因为我们已经手动处理了选择逻辑
                    e.Handled = true;
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }
            // 如果是Ctrl键多选
            else if (isCtrlPressed)
            {
                Debug.WriteLine($"Ctrl键多选: 选择或取消选择 {clickedItem.RouteName}");

                try
                {
                    _isInternalSelectionChange = true;

                    // 现有选中项集合（用于确定是否需要重新同步UI）
                    var currentSelectedItems = new HashSet<RouteInfo>(viewModel.SelectedRoutes);

                    // 切换选中状态
                    if (clickedItem.IsSelected)
                    {
                        // 取消选中
                        clickedItem.IsSelected = false;
                        if (viewModel.SelectedRoutes.Contains(clickedItem))
                        {
                            viewModel.SelectedRoutes.Remove(clickedItem);
                        }
                        // 从DataGrid中移除选择
                        if (dataGrid.SelectedItems.Contains(clickedItem))
                        {
                            dataGrid.SelectedItems.Remove(clickedItem);
                        }
                    }
                    else
                    {
                        // 选中
                        clickedItem.IsSelected = true;
                        if (!viewModel.SelectedRoutes.Contains(clickedItem))
                        {
                            viewModel.SelectedRoutes.Add(clickedItem);
                        }
                        // 添加到DataGrid选择中
                        if (!dataGrid.SelectedItems.Contains(clickedItem))
                        {
                            dataGrid.SelectedItems.Add(clickedItem);
                        }
                    }

                    // 更新最后选中项
                    if (clickedItem.IsSelected)
                    {
                        _lastSelectedItem = clickedItem;
                    }

                    // 更新SelectedRoute属性
                    if (viewModel.SelectedRoutes.Count == 1)
                    {
                        viewModel.SelectedRoute = viewModel.SelectedRoutes[0];
                    }
                    else
                    {
                        viewModel.SelectedRoute = null;
                    }

                    // 关键修复：确保UI状态与数据状态同步
                    // 清除并重新设置DataGrid的选中项
                    dataGrid.SelectedItems.Clear();
                    foreach (var item in viewModel.SelectedRoutes)
                    {
                        dataGrid.SelectedItems.Add(item);
                        Debug.WriteLine($"同步选中状态: {item.RouteName}");
                    }

                    // 手动触发属性更新
                    viewModel.NotifySelectionChanged();

                    // 阻止默认的选择行为
                    e.Handled = true;

                    Debug.WriteLine($"Ctrl键多选后当前选中数量: {viewModel.SelectedRoutes.Count}");
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }
            // 如果没有按下修饰键且当前是全选状态，则只选择点击的行
            else if (!isModifierKeyPressed && viewModel.IsAllSelected)
            {
                Debug.WriteLine($"从全选状态切换到单选: {clickedItem.RouteName}");

                try
                {
                    _isInternalSelectionChange = true;

                    dataGrid.SelectedItems.Clear();
                    dataGrid.SelectedItem = clickedItem;

                    // 更新所有项的选择状态
                    foreach (var item in viewModel.Routes)
                    {
                        item.IsSelected = (item == clickedItem);
                    }

                    // 更新ViewModel中的SelectedRoutes集合
                    viewModel.SelectedRoutes.Clear();
                    viewModel.SelectedRoutes.Add(clickedItem);
                    viewModel.SelectedRoute = clickedItem;

                    // 记录最后选中项
                    _lastSelectedItem = clickedItem;

                    // 手动触发属性更新
                    viewModel.NotifySelectionChanged();

                    // 阻止默认的选择行为
                    e.Handled = true;

                    Debug.WriteLine("单项选择完成");
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }
            // 无修饰键的普通单击
            else if (!isModifierKeyPressed)
            {
                // 记录最后选中项，用于后续的Shift多选
                _lastSelectedItem = clickedItem;
                Debug.WriteLine($"记录普通点击的行: {clickedItem.RouteName}");
            }
        }

        /// <summary>
        /// 处理DataGrid预览右键点击事件，确保每次右键都能被正确捕获
        /// </summary>
        private void RoutesDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 预览右键点击事件触发");

            if (!(DataContext is QueryAllRoutesViewModel viewModel))
            {
                Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - DataContext不是QueryAllRoutesViewModel类型");
                return;
            }

            // 获取点击位置下的行
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // 向上查找DataGridRow
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            // 如果找到了行，处理该行
            if (dep is DataGridRow clickedRow && clickedRow.Item is RouteInfo clickedItem)
            {
                Debug.WriteLine($"RoutesDataGrid_PreviewMouseRightButtonDown - 右键点击行: {clickedItem.RouteName} (ID: {clickedItem.Id})");

                // 阻止事件冒泡，防止触发其他处理
                e.Handled = true;

                // 先选中该行
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid != null)
                {
                    // 如果没有按住Ctrl或Shift，则清除其他选择
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) &&
                        !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    {
                        try
                        {
                            _isInternalSelectionChange = true;

                            dataGrid.SelectedItems.Clear();
                            viewModel.SelectedRoutes.Clear();

                            // 选中右键点击的行
                            dataGrid.SelectedItems.Add(clickedItem);
                            viewModel.SelectedRoutes.Add(clickedItem);
                            viewModel.SelectedRoute = clickedItem;
                            clickedItem.IsSelected = true;

                            // 记录最后选中项
                            _lastSelectedItem = clickedItem;

                            // 手动触发属性更新
                            viewModel.NotifySelectionChanged();

                            Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 已选中右键点击的行");
                        }
                        finally
                        {
                            _isInternalSelectionChange = false;
                        }
                    }
                }

                try
                {
                    Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 尝试打开路线详情窗口");

                    // 直接创建新窗口实例打开，这是最可靠的方式
                    var routeDetailWindow = new RouteDetailWindow(clickedItem, viewModel.DatabaseService, viewModel.MainViewModel);
                    routeDetailWindow.Owner = Application.Current.MainWindow;

                    Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 显示路线详情窗口");
                    routeDetailWindow.ShowDialog();
                    Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 路线详情窗口已关闭");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RoutesDataGrid_PreviewMouseRightButtonDown - 打开路线详情窗口失败: {ex.Message}");

                    // 作为备选方案，尝试使用命令方式
                    try
                    {
                        Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 尝试使用命令方式打开");
                        if (viewModel.ShowRouteDetailsCommand != null && viewModel.ShowRouteDetailsCommand.CanExecute(clickedItem))
                        {
                            viewModel.ShowRouteDetailsCommand.Execute(clickedItem);
                        }
                    }
                    catch (Exception cmdEx)
                    {
                        Debug.WriteLine($"RoutesDataGrid_PreviewMouseRightButtonDown - 命令方式也失败: {cmdEx.Message}");
                    }
                }
            }
            else
            {
                Debug.WriteLine("RoutesDataGrid_PreviewMouseRightButtonDown - 没有找到点击的行");
            }
        }

        /// <summary>
        /// 处理DataGrid行选中事件
        /// </summary>
        private void DataGridRow_Selected(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("DataGridRow_Selected 触发");

            if (sender is DataGridRow row && row.Item is RouteInfo item && DataContext is QueryAllRoutesViewModel viewModel)
            {
                // 更新模型的选中状态
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    Debug.WriteLine($"行选中: {item.RouteName}");

                    // 如果不在SelectedRoutes中，则添加
                    if (!viewModel.SelectedRoutes.Contains(item))
                    {
                        viewModel.SelectedRoutes.Add(item);
                    }

                    // 触发UI更新
                    viewModel.NotifySelectionChanged();
                }
            }
        }

        /// <summary>
        /// 处理DataGrid行取消选中事件
        /// </summary>
        private void DataGridRow_Unselected(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("DataGridRow_Unselected 触发");

            if (sender is DataGridRow row && row.Item is RouteInfo item && DataContext is QueryAllRoutesViewModel viewModel)
            {
                // 更新模型的选中状态
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    Debug.WriteLine($"行取消选中: {item.RouteName}");

                    // 从SelectedRoutes中移除
                    if (viewModel.SelectedRoutes.Contains(item))
                    {
                        viewModel.SelectedRoutes.Remove(item);
                    }

                    // 触发UI更新
                    viewModel.NotifySelectionChanged();
                }
            }
        }

        /// <summary>
        /// 处理DataGrid右键点击事件，重定向到PreviewMouseRightButtonDown处理程序
        /// 保留此方法以确保向后兼容，防止在XAML中直接设置此事件处理程序时发生异常
        /// </summary>
        private void RoutesDataGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("RoutesDataGrid_MouseRightButtonDown - 调用被重定向到PreviewMouseRightButtonDown处理程序");
            // 重定向到预览事件处理程序
            RoutesDataGrid_PreviewMouseRightButtonDown(sender, e);
        }

        /// <summary>
        /// 处理DataGrid双击事件
        /// </summary>
        private void RoutesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 如果事件已处理，则不再处理
            if (e.Handled)
                return;

            // 双击的是左键
            if (e.ChangedButton != MouseButton.Left)
                return;
            
            // 获取视图模型
            var viewModel = DataContext as QueryAllRoutesViewModel;
            if (viewModel == null || viewModel.SelectedRoute == null)
                return;
        
            // 标记事件已处理
            e.Handled = true;
        
            // 获取选中的路线
            var selectedRoute = viewModel.SelectedRoute;
        
            // 延迟执行，确保其他UI事件完成
            System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() => {
                try
                {
                    Debug.WriteLine($"双击处理: 选中路线 {selectedRoute.RouteName}");
                    viewModel.DoubleClickEditCommand.Execute(selectedRoute);
                }
                catch(Exception ex)
                {
                    LogHelper.LogError("双击处理异常", ex);
                }
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
