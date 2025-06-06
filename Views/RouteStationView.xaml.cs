using System.Diagnostics;
using System.Windows.Controls;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// RouteStationView.xaml 的交互逻辑
    /// </summary>
    public partial class RouteStationView : UserControl
    {
        public RouteStationView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理数据网格选择变更事件
        /// </summary>
        private void StationsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is RouteStationViewModel viewModel)
            {
                Debug.WriteLine($"SelectionChanged - 添加项: {e.AddedItems.Count}, 移除项: {e.RemovedItems.Count}, DataGrid选中项: {StationsDataGrid.SelectedItems.Count}");

                // 从多选到单选（如全选后点击单条记录）的处理
                if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 1)
                {
                    Debug.WriteLine("检测到从多选到单选的操作，清除所有选择状态");

                    // 清除所有项的选择状态
                    foreach (var station in viewModel.Stations)
                    {
                        station.IsSelected = false;
                    }

                    // 清空选中集合
                    viewModel.SelectedStations.Clear();
                }

                // 处理新增选中项
                foreach (var item in e.AddedItems.Cast<object>())
                {
                    if (item is Models.RouteStationMapping station)
                    {
                        station.IsSelected = true;
                    }
                }

                // 处理取消选中项
                foreach (var item in e.RemovedItems.Cast<object>())
                {
                    if (item is Models.RouteStationMapping station)
                    {
                        station.IsSelected = false;
                    }
                }

                // 为所有行同步选择状态（包括不可见的行）
                int selectedCount = 0;
                var selectedItems = new HashSet<int>(); // 使用HashSet防止重复计数

                foreach (var station in viewModel.Stations)
                {
                    bool isSelectedInDataGrid = StationsDataGrid.SelectedItems.Contains(station);

                    // 如果DataGrid选择状态与IsSelected属性不一致，则以DataGrid的选择状态为准
                    if (isSelectedInDataGrid != station.IsSelected)
                    {
                        station.IsSelected = isSelectedInDataGrid;
                    }

                    // 确保每个项只计数一次
                    if (station.IsSelected && !selectedItems.Contains(station.Id))
                    {
                        selectedCount++;
                        selectedItems.Add(station.Id);
                    }
                }

                Debug.WriteLine($"同步后Stations中选中项数量: {selectedCount}");

                // 同步当前页的选择状态到ViewModel
                viewModel.SynchronizeSelectionStates();

                Debug.WriteLine($"SynchronizeSelectionStates后SelectedStations数量: {viewModel.SelectedStations.Count}, SelectedItemsCount: {viewModel.SelectedItemsCount}");
            }
        }
    }
}