using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Models;
using TA_WPF.ViewModels;
using TA_WPF.Utils;
using System.Linq;

namespace TA_WPF.Views
{
    /// <summary>
    /// QueryAllCollectionsPage.xaml 的交互逻辑
    /// </summary>
    public partial class QueryAllCollectionsPage : UserControl
    {
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        private Point _startPoint;
        private bool _isDragging;
        private TicketCollectionInfo _draggedItem;
        private ListBoxItem _insertionIndicator;
        
        public QueryAllCollectionsPage()
        {
            InitializeComponent();
            
            // 初始化页码提示框
            InitializePageNumberTooltip();
            
            // 初始化拖拽指示器
            InitializeDragInsertionIndicator();
        }

        /// <summary>
        /// 初始化页码提示框
        /// </summary>
        private void InitializePageNumberTooltip()
        {
            _tooltipText = new TextBlock
            {
                Padding = new Thickness(8),
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                Foreground = System.Windows.Media.Brushes.White,
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
        /// 初始化拖拽指示器
        /// </summary>
        private void InitializeDragInsertionIndicator()
        {
            _insertionIndicator = new ListBoxItem
            {
                Width = 4,  // 改为垂直线，设置宽度而不是高度
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#673AB7")), // 紫色指示器
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch, // 垂直拉伸
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                MinHeight = 200 // 确保高度足够
            };
            
            // 确保指示器在最前方显示
            Panel.SetZIndex(_insertionIndicator, 1000);
        }

        /// <summary>
        /// 处理页码信息面板的点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;

            if (pageInfoPanel != null && pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                pageInfoPanel.Visibility = Visibility.Collapsed;
                pageNumberInput.Visibility = Visibility.Visible;

                // 设置当前页码为默认值
                var viewModel = DataContext as QueryAllCollectionsViewModel;
                if (viewModel != null)
                {
                    pageNumberInput.Text = viewModel.PaginationViewModel.CurrentPage.ToString();
                }

                // 聚焦并全选
                pageNumberInput.Focus();
                pageNumberInput.SelectAll();
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
                var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
                
                if (pageInfoPanel != null && pageNumberInput != null)
                {
                    pageInfoPanel.Visibility = Visibility.Visible;
                    pageNumberInput.Visibility = Visibility.Collapsed;
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
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            
            if (pageInfoPanel != null && pageNumberInput != null)
            {
                pageInfoPanel.Visibility = Visibility.Visible;
                pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 排序按钮点击事件处理程序
        /// </summary>
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开上下文菜单
            Button button = sender as Button;
            if (button != null && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 尝试导航到指定页码
        /// </summary>
        private void TryNavigateToPage()
        {
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            
            if (pageInfoPanel == null || pageNumberInput == null)
                return;

            var viewModel = DataContext as QueryAllCollectionsViewModel;
            if (viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber > 0 && pageNumber <= viewModel.PaginationViewModel.TotalPages)
                {
                    // 设置新的页码
                    viewModel.PaginationViewModel.CurrentPage = pageNumber;
                }
                else
                {
                    // 显示错误提示
                    _tooltipText.Text = $"页码必须在 1 到 {viewModel.PaginationViewModel.TotalPages} 之间";
                    _pageNumberTooltip.PlacementTarget = pageNumberInput;
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
                    pageNumberInput.Text = viewModel.PaginationViewModel.CurrentPage.ToString();
                    pageNumberInput.SelectAll();
                    return;
                }
            }

            // 恢复显示页码信息
            pageInfoPanel.Visibility = Visibility.Visible;
            pageNumberInput.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 处理网格视图中的收藏夹项点击事件，实现选择功能
        /// </summary>
        private void Collection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 如果是双击，不在这里处理，让事件冒泡给ListBox的MouseDoubleClick处理
            if (e.ClickCount == 2)
            {
                return;
            }

            if (sender is Grid grid && grid.DataContext is TicketCollectionInfo collection)
            {
                var viewModel = DataContext as QueryAllCollectionsViewModel;
                if (viewModel == null) return;

                // 设置选择状态
                collection.IsSelected = !collection.IsSelected;

                // 更新ViewModel的选中项
                if (collection.IsSelected)
                {
                    if (!viewModel.SelectedCollections.Contains(collection))
                    {
                        viewModel.SelectedCollections.Add(collection);
                    }
                }
                else
                {
                    if (viewModel.SelectedCollections.Contains(collection))
                    {
                        viewModel.SelectedCollections.Remove(collection);
                    }
                }
            }
        }

        /// <summary>
        /// DataGrid双击事件处理
        /// </summary>
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as QueryAllCollectionsViewModel;
            if (viewModel == null) return;

            // 从DataGrid获取选中的行
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is TicketCollectionInfo collection)
            {
                // 设置批量操作标志(如有该字段)
                var field = viewModel.GetType().GetField("_isBatchSelectionOperation", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(viewModel, true);
                
                // 确保只有当前项被选中
                foreach (var item in viewModel.Collections)
                {
                    item.IsSelected = (item == collection);
                }
                
                // 清空并重新设置选中集合
                viewModel.SelectedCollections.Clear();
                viewModel.SelectedCollections.Add(collection);
                
                // 重置批量操作标志
                if (field != null)
                    field.SetValue(viewModel, false);
                
                // 设置当前选择项
                viewModel.SelectedCollection = collection;
                
                // 执行打开命令
                if (viewModel.OpenCollectionTicketsCommand.CanExecute(null))
                {
                    viewModel.OpenCollectionTicketsCommand.Execute(null);
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// ListBox双击事件处理
        /// </summary>
        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
                {
            var viewModel = DataContext as QueryAllCollectionsViewModel;
            if (viewModel == null) return;

            // 从原始事件源获取数据上下文
            if (e.OriginalSource is FrameworkElement element && element.DataContext is TicketCollectionInfo collection)
            {
                // 设置批量操作标志(如有该字段)
                var field = viewModel.GetType().GetField("_isBatchSelectionOperation", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(viewModel, true);
                    
                // 确保只有当前项被选中
                foreach (var item in viewModel.Collections)
                {
                    item.IsSelected = (item == collection);
                }
                
                // 清空并重新设置选中集合
                viewModel.SelectedCollections.Clear();
                viewModel.SelectedCollections.Add(collection);
                
                // 重置批量操作标志
                if (field != null)
                    field.SetValue(viewModel, false);
                
                // 设置当前选择项
                viewModel.SelectedCollection = collection;
                
                // 执行打开命令
                if (viewModel.OpenCollectionTicketsCommand.CanExecute(null))
                    {
                    viewModel.OpenCollectionTicketsCommand.Execute(null);
                    }
                
                e.Handled = true;
            }
        }
        
        #region 拖拽排序相关方法

        /// <summary>
        /// 鼠标按下事件，开始拖拽
        /// </summary>
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查原始源是否为有效的视觉元素
            if (!(e.OriginalSource is Visual))
            {
                return;
            }

            // 如果不是点击在Grid上，则忽略（避免影响普通选择）
            if (!(FindVisualParent<Grid>(e.OriginalSource as DependencyObject) is Grid targetGrid) ||
                targetGrid.DataContext is not TicketCollectionInfo)
            {
                return;
            }

            _startPoint = e.GetPosition(null);
            _isDragging = false;
            
            // 获取被点击的项数据
            if (sender is ListBox listBox && 
                FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject) is ListBoxItem item && 
                item.DataContext is TicketCollectionInfo collection)
            {
                _draggedItem = collection;
            }
        }

        /// <summary>
        /// 鼠标移动事件，执行拖拽
        /// </summary>
        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _draggedItem != null)
            {
                Point position = e.GetPosition(null);
                
                // 检查是否达到拖拽门槛距离
                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    
                    // 开始拖拽前清除所有存在的指示器
                    if (sender is ListBox listBox)
                    {
                        // 清除ListBox中的插入指示器
                        if (listBox.Items.Contains(_insertionIndicator))
                        {
                            listBox.Items.Remove(_insertionIndicator);
                        }
                        
                        // 清除Panel中的边框指示器
                        var itemsPanel = FindVisualChild<Panel>(listBox);
                        if (itemsPanel != null)
                        {
                            var indicators = itemsPanel.Children.OfType<Border>()
                                .Where(b => b.Name == "DragInsertionIndicator").ToList();
                            foreach (var indicator in indicators)
                            {
                                itemsPanel.Children.Remove(indicator);
                            }
                        }
                    }
                    
                    // 启动拖放操作
                    DragDrop.DoDragDrop(sender as ListBox, _draggedItem, DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// 拖拽经过事件，显示插入指示器
        /// </summary>
        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 获取当前鼠标位置相对于ListBox
                Point point = e.GetPosition(listBox);
                
                // 隐藏之前的指示器
                if (listBox.Items.Contains(_insertionIndicator))
                {
                    listBox.Items.Remove(_insertionIndicator);
                }
                
                // 找到鼠标下方的项
                HitTestResult result = VisualTreeHelper.HitTest(listBox, point);
                if (result != null)
                {
                    ListBoxItem targetItem = FindVisualParent<ListBoxItem>(result.VisualHit);
                    if (targetItem != null && targetItem.DataContext is TicketCollectionInfo targetCollection && targetCollection != _draggedItem)
                    {
                        // 找到目标项在集合中的索引
                        int targetIndex = listBox.Items.IndexOf(targetCollection);
                        if (targetIndex >= 0)
                        {
                            // 在目标项旁边添加垂直线指示器
                            var itemsPanel = FindVisualChild<Panel>(listBox);
                            if (itemsPanel != null)
                            {
                                // 清除任何已存在的指示器
                                var existingIndicators = itemsPanel.Children.OfType<Border>().Where(b => b.Name == "DragInsertionIndicator").ToList();
                                foreach (var indicator in existingIndicators)
                                {
                                    itemsPanel.Children.Remove(indicator);
                                }
                                
                                // 获取目标项的位置信息
                                Point targetPos = targetItem.TranslatePoint(new Point(0, 0), itemsPanel);
                                
                                // 计算鼠标与目标项的相对位置
                                Point mousePos = e.GetPosition(itemsPanel);
                                
                                // 创建一个新的边框作为放置指示器
                                var border = new Border 
                                {
                                    Name = "DragInsertionIndicator",
                                    Width = 4,
                                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#673AB7")),
                                    Height = targetItem.ActualHeight,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    Margin = new Thickness(0)
                                };
                                
                                // 添加边框到ItemsPanel
                                itemsPanel.Children.Add(border);
                                
                                // 根据鼠标位置确定放置位置
                                double itemWidth = targetItem.ActualWidth;
                                double itemX = targetPos.X;
                                double mouseX = mousePos.X;
                                
                                // 计算指示器的位置
                                double indicatorX;
                                
                                // 如果鼠标在项的中点之前，则放在项的左侧
                                // 否则放在项的右侧
                                if (mouseX < itemX + itemWidth / 2)
                                {
                                    indicatorX = itemX;
                                }
                                else
                                {
                                    indicatorX = itemX + itemWidth;
                                }
                                
                                // 设置边框位置 - 检查Panel类型并适当设置位置
                                if (itemsPanel is Canvas)
                                {
                                Canvas.SetLeft(border, indicatorX);
                                Canvas.SetTop(border, targetPos.Y);
                                }
                                else
                                {
                                    // 如果不是Canvas，使用Margin来定位
                                    border.Margin = new Thickness(indicatorX, targetPos.Y, 0, 0);
                                    
                                    // 确保使用绝对定位
                                    border.HorizontalAlignment = HorizontalAlignment.Left;
                                    border.VerticalAlignment = VerticalAlignment.Top;
                                    
                                    // 确保边框在正确的Z顺序上显示
                                    Panel.SetZIndex(border, 1000);
                                }
                            }
                            
                            e.Effects = DragDropEffects.Move;
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 拖拽放置事件，更新排序顺序
        /// </summary>
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (_draggedItem == null) return;
            
            if (sender is ListBox listBox)
            {
                // 清理任何显示的指示器
                var itemsPanel = FindVisualChild<Panel>(listBox);
                if (itemsPanel != null)
                {
                    // 查找并移除所有拖拽指示器
                    var indicators = itemsPanel.Children.OfType<Border>()
                        .Where(b => b.Name == "DragInsertionIndicator").ToList();
                    foreach (var indicator in indicators)
                    {
                        itemsPanel.Children.Remove(indicator);
                    }
                }
                
                // 如果还存在插入指示器，也要移除
                if (listBox.Items.Contains(_insertionIndicator))
                {
                    listBox.Items.Remove(_insertionIndicator);
                }
                
                // 获取当前鼠标位置相对于ListBox
                Point point = e.GetPosition(listBox);
                
                // 尝试找到鼠标下方的项
                HitTestResult result = VisualTreeHelper.HitTest(listBox, point);
                TicketCollectionInfo targetCollection = null;
                
                if (result != null)
                {
                    // 直接命中了某个项
                    ListBoxItem targetItem = FindVisualParent<ListBoxItem>(result.VisualHit);
                    if (targetItem != null && targetItem.DataContext is TicketCollectionInfo hitCollection)
                    {
                        targetCollection = hitCollection;
                    }
                }
                
                // 如果没有直接命中项（可能在项目之间），找到最近的项
                if (targetCollection == null && itemsPanel != null)
                {
                    double minDistance = double.MaxValue;
                    Point mousePos = e.GetPosition(itemsPanel);
                    
                    // 获取所有可见的ListBoxItem
                    var visibleItems = listBox.Items.OfType<TicketCollectionInfo>()
                        .Select(item => listBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem)
                        .Where(container => container != null)
                        .ToList();
                    
                    foreach (var container in visibleItems)
                    {
                        if (container.DataContext is TicketCollectionInfo itemCollection)
                        {
                            // 获取项的位置和边界
                            Point containerPos = container.TranslatePoint(new Point(0, 0), itemsPanel);
                            double containerWidth = container.ActualWidth;
                            double containerHeight = container.ActualHeight;
                            
                            // 计算鼠标与项边界的距离
                            double leftDist = Math.Abs(mousePos.X - containerPos.X);
                            double rightDist = Math.Abs(mousePos.X - (containerPos.X + containerWidth));
                            double topDist = Math.Abs(mousePos.Y - containerPos.Y);
                            double bottomDist = Math.Abs(mousePos.Y - (containerPos.Y + containerHeight));
                            
                            // 找出最短距离
                            double minContainerDist = Math.Min(Math.Min(leftDist, rightDist), Math.Min(topDist, bottomDist));
                            
                            if (minContainerDist < minDistance)
                            {
                                minDistance = minContainerDist;
                                targetCollection = itemCollection;
                            }
                        }
                    }
                }
                
                // 如果找到目标项，并且它不是被拖动的项自己
                if (targetCollection != null && targetCollection != _draggedItem)
                {
                    // 获取ViewModel
                    var viewModel = DataContext as QueryAllCollectionsViewModel;
                    if (viewModel == null) return;
                    
                    // 更新排序顺序
                    viewModel.UpdateItemOrder(_draggedItem, targetCollection);
                }
                
                // 清除拖拽状态
                _isDragging = false;
                _draggedItem = null;
            }
        }
        
        /// <summary>
        /// 查找视觉树父级
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;
            
            // 获取父级
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            
            // 检查是否是目标类型
            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                // 递归查找更上层的父级
                return FindVisualParent<T>(parentObject);
            }
        }
        
        /// <summary>
        /// 查找视觉树子级
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // 检查是否是目标类型
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                // 递归查找更下层的子级
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
        
        #endregion
    }
} 