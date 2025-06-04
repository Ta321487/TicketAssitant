using System.Windows.Controls;
using TA_WPF.ViewModels;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace TA_WPF.Views
{
    /// <summary>
    /// RouteTicketView.xaml 的交互逻辑
    /// </summary>
    public partial class RouteTicketView : UserControl
    {
        public RouteTicketView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理数据网格选择变更事件
        /// </summary>
        private void TicketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is RouteTicketViewModel viewModel)
            {
                Debug.WriteLine($"SelectionChanged - 添加项: {e.AddedItems.Count}, 移除项: {e.RemovedItems.Count}, DataGrid选中项: {TicketsDataGrid.SelectedItems.Count}");
                
                // 从多选到单选（如全选后点击单条记录）的处理
                if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 1)
                {
                    Debug.WriteLine("检测到从多选到单选的操作，清除所有选择状态");
                    
                    // 清除所有项的选择状态
                    foreach (var ticket in viewModel.Tickets)
                    {
                        ticket.IsSelected = false;
                    }
                    
                    // 清空选中集合
                    viewModel.SelectedTickets.Clear();
                }
                
                // 处理新增选中项
                foreach (var item in e.AddedItems.Cast<object>())
                {
                    if (item is Models.RouteTicketMapping ticket)
                    {
                        ticket.IsSelected = true;
                    }
                }

                // 处理取消选中项
                foreach (var item in e.RemovedItems.Cast<object>())
                {
                    if (item is Models.RouteTicketMapping ticket)
                    {
                        ticket.IsSelected = false;
                    }
                }

                // 为所有行同步选择状态（包括不可见的行）
                int selectedCount = 0;
                var selectedItems = new HashSet<int>(); // 使用HashSet防止重复计数
                
                foreach (var ticket in viewModel.Tickets)
                {
                    bool isSelectedInDataGrid = TicketsDataGrid.SelectedItems.Contains(ticket);
                    
                    // 如果DataGrid选择状态与IsSelected属性不一致，则以DataGrid的选择状态为准
                    if (isSelectedInDataGrid != ticket.IsSelected)
                    {
                        ticket.IsSelected = isSelectedInDataGrid;
                    }
                    
                    // 确保每个项只计数一次
                    if (ticket.IsSelected && !selectedItems.Contains(ticket.Id))
                    {
                        selectedCount++;
                        selectedItems.Add(ticket.Id);
                    }
                }
                
                Debug.WriteLine($"同步后Tickets中选中项数量: {selectedCount}");

                // 同步当前页的选择状态到ViewModel
                viewModel.SynchronizeSelectionStates();
                
                Debug.WriteLine($"SynchronizeSelectionStates后SelectedTickets数量: {viewModel.SelectedTickets.Count}, SelectedItemsCount: {viewModel.SelectedItemsCount}");
            }
        }
    }
} 