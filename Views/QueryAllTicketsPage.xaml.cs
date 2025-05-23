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
    /// QueryAllTicketsPage.xaml 的交互逻辑
    /// </summary>
    public partial class QueryAllTicketsPage : UserControl
    {
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        private StackPanel _pageInfoPanel;
        private TextBox _pageNumberInput;

        public QueryAllTicketsPage()
        {
            InitializeComponent();

            // 初始化页码提示框
            InitializePageNumberTooltip();

            // 获取控件引用
            _pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            _pageNumberInput = this.FindName("PageNumberInput") as TextBox;

            // 添加DataGrid的鼠标事件处理
            TicketsDataGrid.PreviewMouseDown += TicketsDataGrid_PreviewMouseDown;

            // 添加DataGrid的单元格工具提示事件处理
            TicketsDataGrid.LoadingRow += TicketsDataGrid_LoadingRow;
            
            // 添加DataGrid的键盘事件处理，支持Ctrl+A全选和Delete删除
            TicketsDataGrid.PreviewKeyDown += TicketsDataGrid_PreviewKeyDown;
        }

        /// <summary>
        /// 处理DataGrid的键盘事件，支持Ctrl+A全选和Delete删除
        /// </summary>
        private void TicketsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is TicketBaseViewModel viewModel)
            {
                // 处理Ctrl+A全选
                if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    viewModel.SelectAllCommand.Execute(null);
                    e.Handled = true;
                }
                
                // 处理Delete键删除选中项
                if (e.Key == Key.Delete && viewModel.HasSelectedItems)
                {
                    // 调用删除命令，与红色删除按钮行为一致
                    viewModel.DeleteTicketsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 初始化页码提示框
        /// </summary>
        private void InitializePageNumberTooltip()
        {
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
                var viewModel = DataContext as QueryAllTicketsViewModel;
                if (viewModel != null)
                {
                    _pageNumberInput.Text = viewModel.CurrentPage.ToString();
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
            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (textBox != null && viewModel != null)
            {
                // 获取输入后的完整文本
                string newText = textBox.Text.Substring(0, textBox.SelectionStart) + e.Text + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

                // 尝试解析为数字
                if (int.TryParse(newText, out int pageNumber))
                {
                    // 如果输入的数字大于总页数，则不允许输入
                    if (pageNumber > viewModel.TotalPages)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 阻止粘贴非数字内容
        /// </summary>
        private void PageNumberInput_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                // 获取剪贴板内容
                string clipboardText = Clipboard.GetText();

                // 检测是否为数字
                if (!int.TryParse(clipboardText, out _))
                {
                    e.Handled = true;
                    return;
                }

                // 检测粘贴后的数字是否在有效范围内
                var textBox = sender as TextBox;
                var viewModel = DataContext as QueryAllTicketsViewModel;
                if (textBox != null && viewModel != null)
                {
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // 获取粘贴后的完整文本
                        string newText = textBox.Text.Substring(0, textBox.SelectionStart) + clipboardText + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

                        // 尝试解析为数字
                        if (int.TryParse(newText, out int pageNumber))
                        {
                            // 如果粘贴后的数字大于总页数，则不允许粘贴
                            if (pageNumber > viewModel.TotalPages)
                            {
                                e.Handled = true;
                            }
                        }
                    }
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
        /// 尝试导航到指定页码
        /// </summary>
        private void TryNavigateToPage()
        {
            if (_pageInfoPanel == null || _pageNumberInput == null)
                return;

            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(_pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= viewModel.TotalPages)
                {
                    // 设置新的页码
                    viewModel.CurrentPage = pageNumber;
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {viewModel.TotalPages} 之间";
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
                    _pageNumberInput.Text = viewModel.CurrentPage.ToString();
                    _pageNumberInput.SelectAll();
                    return;
                }
            }

            // 恢复显示页码信息
            _pageInfoPanel.Visibility = Visibility.Visible;
            _pageNumberInput.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 处理数据表格选择变更事件
        /// </summary>
        private void TicketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取DataGrid
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // 获取ViewModel
            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (viewModel == null) return;

            // 获取当前激活的键盘修饰键状态，用于处理多选和连续选择
            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // 处理取消选中的项
            foreach (TrainRideInfo item in e.RemovedItems)
            {
                // 更新模型的选中状态
                item.IsSelected = false;
            }

            // 处理新选中的项
            foreach (TrainRideInfo item in e.AddedItems)
            {
                // 更新模型的选中状态
                item.IsSelected = true;
            }

            // 检测是否需要更新全选状态
            if (viewModel is TicketBaseViewModel ticketViewModel)
            {
                // 获取当前页的所有项
                var items = ticketViewModel.TrainRideInfos;

                if (items != null && items.Count > 0)
                {
                    // 检测是否所有项都被选中
                    bool allSelected = items.All(item => item.IsSelected);
                    int selectedCount = items.Count(item => item.IsSelected);

                    // 强制更新选中项计数，确保EditTicketCommand可用性正确
                    ticketViewModel.UpdateSelectedItemsCountExternal(selectedCount);

                    // 如果全选状态与实际不符，则更新全选状态
                    if (allSelected != ticketViewModel.IsAllSelected)
                    {
                        // 使用反射获取私有字段_isUpdatingAllSelected
                        var field = typeof(TicketBaseViewModel).GetField("_isUpdatingAllSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            // 设置_isUpdatingAllSelected为true，避免循环调用
                            field.SetValue(ticketViewModel, true);

                            // 更新全选状态
                            ticketViewModel.IsAllSelected = allSelected;

                            // 设置_isUpdatingAllSelected为false
                            field.SetValue(ticketViewModel, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理DataGrid的鼠标点击事件
        /// </summary>
        private void TicketsDataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 检查是否按下了Ctrl键或Shift键
            bool isModifierKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                                       Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                               
            // 如果没有按下修饰键且当前是全选状态，则模拟单击行为
            if (!isModifierKeyPressed && DataContext is TicketBaseViewModel viewModel && viewModel.IsAllSelected)
            {
                // 获取DataGrid引用
                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;
                
                // 获取点击位置下的行
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                DataGridRow row = null;
                
                // 向上查找DataGridRow
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                
                if (dep is DataGridRow clickedRow)
                {
                    row = clickedRow;
                }
                
                // 如果点击了有效行，则取消全选并仅选中此行
                if (row != null && row.Item is TrainRideInfo clickedItem)
                {
                    // 清除当前DataGrid的所有选择，然后只选择当前行
                    dataGrid.SelectedItems.Clear();
                    dataGrid.SelectedItem = clickedItem;
                    clickedItem.IsSelected = true;
                    
                    // 更新其他项的选择状态为false
                    foreach (var item in viewModel.TrainRideInfos)
                    {
                        if (item != clickedItem && item.IsSelected)
                        {
                            item.IsSelected = false;
                        }
                    }
                    
                    // 更新ViewModel中的选中状态
                    viewModel.IsAllSelected = false;
                    viewModel.UpdateSelectedItemsCountExternal(1);
                    
                    // 阻止默认的选择行为
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 处理DataGrid行选中事件
        /// </summary>
        private void DataGridRow_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is TrainRideInfo item)
            {
                // 更新模型的选中状态
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    
                    // 获取ViewModel并更新选中状态
                    if (DataContext is TicketBaseViewModel viewModel)
                    {
                        // 通知ViewModel更新选中状态
                        viewModel.UpdateSelectedItemsCountExternal(viewModel.TrainRideInfos.Count(t => t.IsSelected));
                    }
                }
            }
        }

        /// <summary>
        /// 处理DataGrid行取消选中事件
        /// </summary>
        private void DataGridRow_Unselected(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is TrainRideInfo item)
            {
                // 更新模型的选中状态
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    
                    // 获取ViewModel并更新选中状态
                    if (DataContext is TicketBaseViewModel viewModel)
                    {
                        // 通知ViewModel更新选中状态
                        viewModel.UpdateSelectedItemsCountExternal(viewModel.TrainRideInfos.Count(t => t.IsSelected));
                        
                        // 如果ViewModel的IsAllSelected为true但并非所有项都被选中，则更新IsAllSelected
                        if (viewModel.IsAllSelected && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            // 使用反射获取私有字段_isUpdatingAllSelected
                            var field = typeof(TicketBaseViewModel).GetField("_isUpdatingAllSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                // 设置_isUpdatingAllSelected为true，避免循环调用
                                field.SetValue(viewModel, true);
                                
                                // 更新全选状态
                                viewModel.IsAllSelected = false;
                                
                                // 设置_isUpdatingAllSelected为false
                                field.SetValue(viewModel, false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查找可视化树中的父元素
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            // 获取父元素
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // 如果没有父元素，返回null
            if (parentObject == null) return null;

            // 如果父元素是要查找的类型，返回父元素
            if (parentObject is T parent)
            {
                return parent;
            }

            // 递归查找父元素
            return FindVisualParent<T>(parentObject);
        }

        /// <summary>
        /// 处理DataGrid行加载事件，为单元格添加工具提示
        /// </summary>
        private void TicketsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 获取行
            var row = e.Row;

            // 为行中的每个单元格添加工具提示
            row.Loaded += (s, args) =>
            {
                foreach (var cell in FindVisualChildren<DataGridCell>(row))
                {
                    // 为单元格添加鼠标进入事件处理
                    cell.MouseEnter += DataGridCell_MouseEnter;
                }
            };
        }

        /// <summary>
        /// 处理DataGrid单元格鼠标进入事件，显示工具提示
        /// </summary>
        private void DataGridCell_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is DataGridCell cell)
            {
                // 获取单元格内容
                var content = cell.Content;

                // 如果内容是TextBlock，则设置工具提示
                if (content is TextBlock textBlock)
                {
                    // 检测文本是否被截断
                    if (IsTextTrimmed(textBlock))
                    {
                        // 创建自定义ToolTip
                        var toolTip = new ToolTip
                        {
                            Content = textBlock.Text,
                            Style = FindResource("MaterialDesignDataGridCellToolTip") as Style
                        };

                        // 设置工具提示
                        ToolTipService.SetToolTip(cell, toolTip);

                        // 设置工具提示服务属性
                        ToolTipService.SetInitialShowDelay(cell, 500);
                        ToolTipService.SetShowDuration(cell, 10000);
                        ToolTipService.SetBetweenShowDelay(cell, 0);
                    }
                    else
                    {
                        // 如果文本没有被截断，则移除工具提示
                        ToolTipService.SetToolTip(cell, null);
                    }
                }
            }
        }

        /// <summary>
        /// 检测TextBlock的文本是否被截断
        /// </summary>
        private bool IsTextTrimmed(TextBlock textBlock)
        {
            if (textBlock == null || string.IsNullOrEmpty(textBlock.Text))
                return false;

            // 使用更简单的方法检测文本是否被截断
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double actualWidth = textBlock.DesiredSize.Width;

            // 如果实际宽度大于TextBlock的宽度，则文本被截断
            return actualWidth > textBlock.ActualWidth;
        }

        /// <summary>
        /// 查找可视化树中的子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T childOfType)
                    yield return childOfType;

                foreach (var grandChild in FindVisualChildren<T>(child))
                    yield return grandChild;
            }
        }

        /// <summary>
        /// 处理DataGrid的双击事件
        /// </summary>
        private void TicketsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 获取当前选中的行
            var row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;

            // 获取选中的车票数据
            var ticket = row.Item as TrainRideInfo;
            if (ticket == null) return;

            // 获取ViewModel
            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (viewModel == null) return;

            try
            {
                // 创建修改车票窗口
                var editWindow = new EditTicketWindow(viewModel.DatabaseService, viewModel.MainViewModel, ticket);
                editWindow.Owner = Window.GetWindow(this);
                editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // 显示窗口
                bool? result = editWindow.ShowDialog();

                // 如果用户保存了修改，刷新数据
                if (result == true)
                {
                    // 刷新数据
                    _ = viewModel.RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开修改车票窗口时出错: {ex.Message}");
                LogHelper.LogError("打开修改车票窗口时出错", ex);
            }
        }

        /// <summary>
        /// 处理导入车票按钮点击事件，显示上下文菜单
        /// </summary>
        private void ImportTicketButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
    }
}