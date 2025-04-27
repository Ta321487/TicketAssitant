using System.Windows.Controls;
using TA_WPF.Models;
using TA_WPF.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace TA_WPF.Views
{
    /// <summary>
    /// Interaction logic for QueryAllStationsPage.xaml
    /// </summary>
    public partial class QueryAllStationsPage : UserControl
    {
        private bool _isInternalSelectionChange = false;
        private StationInfo _lastSelectedItem = null;
        private StackPanel _pageInfoPanel;
        private TextBox _pageNumberInput;
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;

        public QueryAllStationsPage()
        {
            InitializeComponent();
            
            // 在DataContext变更后，订阅ViewModel的事件
            DataContextChanged += QueryAllStationsPage_DataContextChanged;
            
            // 初始化页码相关控件
            InitializePageComponents();
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
                var viewModel = DataContext as QueryAllStationsViewModel;
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
            var viewModel = DataContext as QueryAllStationsViewModel;
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

            var viewModel = DataContext as QueryAllStationsViewModel;
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
                    _ = viewModel.LoadStationsAsync();
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
        
        private void QueryAllStationsPage_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is QueryAllStationsViewModel oldViewModel)
            {
                // 取消订阅旧的ViewModel事件
                oldViewModel.SelectionChanged -= ViewModel_SelectionChanged;
            }
            
            if (e.NewValue is QueryAllStationsViewModel newViewModel)
            {
                // 订阅新的ViewModel事件
                newViewModel.SelectionChanged += ViewModel_SelectionChanged;
            }
        }
        
        private void ViewModel_SelectionChanged(object sender, QueryAllStationsViewModel.StationSelectionChangedEventArgs e)
        {
            try
            {
                _isInternalSelectionChange = true;
                
                var dataGrid = GetStationsDataGrid();
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
            
            if (DataContext is QueryAllStationsViewModel viewModel)
            {
                // 获取当前激活的键盘修饰键状态
                bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                
                // 更新所有项的选择状态
                foreach (StationInfo item in e.RemovedItems)
                {
                    item.IsSelected = false;
                    if (viewModel.SelectedStations.Contains(item))
                    {
                        viewModel.SelectedStations.Remove(item);
                    }
                }

                // 添加新选择的项
                foreach (StationInfo item in e.AddedItems)
                {
                    item.IsSelected = true;
                    if (!viewModel.SelectedStations.Contains(item))
                    {
                        viewModel.SelectedStations.Add(item);
                    }
                    
                    // 记录最后一个选中的项，用于Shift选择
                    _lastSelectedItem = item;
                }
                
                // 处理Shift键连续选择
                if (isShiftPressed && e.AddedItems.Count > 0 && sender is DataGrid dataGrid && dataGrid.Items.Count > 0)
                {
                    HandleShiftSelection(viewModel, e.AddedItems[0] as StationInfo, dataGrid);
                }
                
                // 更新SelectedStation属性以便修改按钮能正确获取
                if (viewModel.SelectedStations.Count == 1)
                {
                    viewModel.SelectedStation = viewModel.SelectedStations[0];
                }
                else
                {
                    viewModel.SelectedStation = null;
                }
                
                // 手动触发属性更新
                viewModel.NotifySelectionChanged();
            }
        }
        
        /// <summary>
        /// 处理Shift键连续选择
        /// </summary>
        private void HandleShiftSelection(QueryAllStationsViewModel viewModel, StationInfo currentItem, DataGrid dataGrid)
        {
            if (_lastSelectedItem == null || currentItem == null) return;
            
            // 找到上一个选中项和当前选中项的索引
            int lastIndex = dataGrid.Items.IndexOf(_lastSelectedItem);
            int currentIndex = dataGrid.Items.IndexOf(currentItem);
            
            if (lastIndex == -1 || currentIndex == -1) return;
            
            try
            {
                _isInternalSelectionChange = true;
                
                // 计算开始和结束索引
                int startIndex = System.Math.Min(lastIndex, currentIndex);
                int endIndex = System.Math.Max(lastIndex, currentIndex);
                
                // 选择开始和结束索引之间的所有项
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (i >= 0 && i < dataGrid.Items.Count)
                    {
                        var item = dataGrid.Items[i] as StationInfo;
                        if (item != null)
                        {
                            if (!dataGrid.SelectedItems.Contains(item))
                            {
                                dataGrid.SelectedItems.Add(item);
                            }
                            
                            if (!viewModel.SelectedStations.Contains(item))
                            {
                                viewModel.SelectedStations.Add(item);
                                item.IsSelected = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                _isInternalSelectionChange = false;
            }
        }
        
        /// <summary>
        /// 处理键盘按键事件，支持Ctrl+A全选和删除键
        /// </summary>
        private void StationsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is QueryAllStationsViewModel viewModel)
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
                if (e.Key == Key.Delete && viewModel.SelectedStations.Count > 0)
                {
                    // 直接调用批量删除命令，与红色删除按钮行为一致
                    viewModel.DeleteStationsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
        
        /// <summary>
        /// 获取StationsDataGrid控件的引用
        /// </summary>
        private DataGrid GetStationsDataGrid()
        {
            return this.FindName("StationsDataGrid") as DataGrid;
        }
    }
} 