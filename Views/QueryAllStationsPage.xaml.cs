using System.Windows.Controls;
using TA_WPF.Models;
using TA_WPF.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace TA_WPF.Views
{
    /// <summary>
    /// Interaction logic for QueryAllStationsPage.xaml
    /// </summary>
    public partial class QueryAllStationsPage : UserControl
    {
        private bool _isInternalSelectionChange = false;
        private StationInfo _lastSelectedItem = null;

        public QueryAllStationsPage()
        {
            InitializeComponent();
            
            // 在DataContext变更后，订阅ViewModel的事件
            DataContextChanged += QueryAllStationsPage_DataContextChanged;
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