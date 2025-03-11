using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using TA_WPF.ViewModels;

namespace TA_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 数据表格引用
        private DataGrid _mainDataGrid;
        // 菜单按钮引用
        private ToggleButton _menuToggleButton;
        // 设置按钮引用
        private Button _settingsButton;
        // 防抖计时器
        private DispatcherTimer _resizeTimer;
        // 是否正在调整列宽
        private bool _isAdjustingColumns = false;
        
        public MainWindow(string connectionString)
        {
            InitializeComponent();
            
            // 设置DataContext
            DataContext = new MainViewModel(connectionString);
            
            // 获取控件引用
            _mainDataGrid = this.FindName("MainDataGrid") as DataGrid;
            _menuToggleButton = this.FindName("MenuToggleButton") as ToggleButton;
            _settingsButton = this.FindName("SettingsButton") as Button;
            
            // 初始化防抖计时器
            _resizeTimer = new DispatcherTimer();
            _resizeTimer.Interval = TimeSpan.FromMilliseconds(300); // 300毫秒的防抖延迟
            _resizeTimer.Tick += (s, e) => 
            {
                _resizeTimer.Stop();
                AdjustDataGridColumns();
            };
            
            // 注册窗口大小变化事件
            this.SizeChanged += Window_SizeChanged;
            
            // 注册窗口状态变化事件
            this.StateChanged += Window_StateChanged;
            
            // 注册侧边栏状态变化事件
            if (_menuToggleButton != null)
            {
                _menuToggleButton.Checked += MenuToggleButton_CheckedChanged;
                _menuToggleButton.Unchecked += MenuToggleButton_CheckedChanged;
            }
            
            // 初始调整列宽
            this.Loaded += (s, e) => 
            {
                // 延迟执行，确保UI已完全加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AdjustDataGridColumns();
                    
                    // 输出调试信息
                    Console.WriteLine("MainWindow加载完成");
                    Console.WriteLine($"MainDataGrid可见性: {_mainDataGrid.Visibility}");
                    Console.WriteLine($"MainDataGrid项目数: {_mainDataGrid.Items.Count}");
                }), DispatcherPriority.Loaded);
            };
        }
        
        /// <summary>
        /// 处理设置按钮点击事件
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowSettings = true;
                viewModel.ShowWelcome = false;
                viewModel.ShowDataGrid = false;
                
                // 关闭侧边栏
                if (_menuToggleButton != null)
                {
                    _menuToggleButton.IsChecked = false;
                }
            }
        }
        
        /// <summary>
        /// 窗口大小变化时调整表格列宽
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 使用防抖计时器延迟调整列宽
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }
        
        /// <summary>
        /// 窗口状态变化时调整表格列宽
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            // 使用防抖计时器延迟调整列宽
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }
        
        /// <summary>
        /// 侧边栏状态变化时调整表格列宽
        /// </summary>
        private void MenuToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // 使用防抖计时器延迟调整列宽
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }
        
        /// <summary>
        /// 数据表格大小变化时调整列宽
        /// </summary>
        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 使用防抖计时器延迟调整列宽
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }
        
        /// <summary>
        /// 调整数据表格列宽
        /// </summary>
        private void AdjustDataGridColumns()
        {
            // 防止重入
            if (_isAdjustingColumns || _mainDataGrid == null || _mainDataGrid.Columns.Count == 0)
                return;
                
            try
            {
                _isAdjustingColumns = true;
                
                // 计算可用宽度
                double totalAvailableWidth = _mainDataGrid.ActualWidth - 30; // 减去滚动条和边距
                
                // 如果可用宽度太小，使用自动宽度
                if (totalAvailableWidth < 800)
                {
                    foreach (var column in _mainDataGrid.Columns)
                    {
                        if (column is DataGridTextColumn textColumn)
                        {
                            textColumn.Width = DataGridLength.Auto;
                        }
                    }
                    return;
                }
                
                // 列数较多时，使用固定宽度策略
                if (_mainDataGrid.Columns.Count > 10)
                {
                    // 为每列分配固定宽度，但确保最小宽度
                    foreach (var column in _mainDataGrid.Columns)
                    {
                        if (column is DataGridTextColumn textColumn)
                        {
                            double minWidth = textColumn.MinWidth;
                            // 使用较小的固定宽度
                            double fixedWidth = Math.Max(minWidth, 100);
                            textColumn.Width = new DataGridLength(fixedWidth);
                        }
                    }
                    return;
                }
                
                // 列数较少时，使用平均宽度策略
                double avgColumnWidth = totalAvailableWidth / _mainDataGrid.Columns.Count;
                
                // 为每列分配平均宽度，但考虑最小宽度
                foreach (var column in _mainDataGrid.Columns)
                {
                    if (column is DataGridTextColumn textColumn)
                    {
                        double minWidth = textColumn.MinWidth;
                        double finalWidth = Math.Max(minWidth, avgColumnWidth);
                        textColumn.Width = new DataGridLength(finalWidth);
                    }
                }
            }
            finally
            {
                _isAdjustingColumns = false;
            }
        }
    }
}