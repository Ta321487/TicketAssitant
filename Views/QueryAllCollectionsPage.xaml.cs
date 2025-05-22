using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Models;
using TA_WPF.ViewModels;
using TA_WPF.Utils;

namespace TA_WPF.Views
{
    /// <summary>
    /// QueryAllCollectionsPage.xaml 的交互逻辑
    /// </summary>
    public partial class QueryAllCollectionsPage : UserControl
    {
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        
        public QueryAllCollectionsPage()
        {
            InitializeComponent();
            
            // 初始化页码提示框
            InitializePageNumberTooltip();
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
    }
} 