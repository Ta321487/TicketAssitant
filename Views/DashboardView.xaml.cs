using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            // 注册数据上下文变更事件
            DataContextChanged += DashboardView_DataContextChanged;

            // 注册加载事件
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                // 初始化图表数据
                InitializeCharts(viewModel);

                // 加载数据
                viewModel.RefreshDataAsync();
            }
        }

        /// <summary>
        /// 数据上下文变更事件处理
        /// </summary>
        private void DashboardView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DashboardViewModel viewModel)
            {
                // 初始化图表数据
                InitializeCharts(viewModel);
            }
        }

        /// <summary>
        /// 初始化图表数据
        /// </summary>
        private void InitializeCharts(DashboardViewModel viewModel)
        {
            // 月度车票数据图表
            viewModel.MonthlyTicketSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "车票数量",
                    Values = new ChartValues<int>(),
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 10
                },
                new LineSeries
                {
                    Title = "去年同期",
                    Values = new ChartValues<int>(),
                    PointGeometry = DefaultGeometries.Diamond,
                    PointGeometrySize = 10,
                    Stroke = Brushes.Orange,
                    Fill = Brushes.Transparent
                }
            };

            // 月度车票标签
            viewModel.MonthlyTicketLabels = new string[6];

            // 更新月度车票数据
            UpdateMonthlyTicketChart(viewModel);

            // 车票类型饼图
            viewModel.TicketTypeSeries = new SeriesCollection();

            // 更新车票类型数据
            UpdateTicketTypeChart(viewModel);

            // 月度支出图表
            viewModel.ExpenseSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "实际支出",
                    Values = new ChartValues<decimal>()
                },
                new LineSeries
                {
                    Title = "预算线",
                    Values = new ChartValues<double>(),
                    PointGeometry = null,
                    LineSmoothness = 0,
                    Stroke = Brushes.Red,
                    Fill = Brushes.Transparent
                }
            };

            // 月度支出标签
            viewModel.ExpenseLabels = new string[12];

            // 更新月度支出数据
            UpdateExpenseChart(viewModel);

            // 设置Y轴格式化器
            viewModel.MonthlyTicketYFormatter = value => value.ToString("N0");
            viewModel.ExpenseYFormatter = value => $"¥{value:N0}";

            // 订阅数据变更事件
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DashboardViewModel.MonthlyTicketData))
                {
                    UpdateMonthlyTicketChart(viewModel);
                }
                else if (e.PropertyName == nameof(DashboardViewModel.TicketTypeData))
                {
                    UpdateTicketTypeChart(viewModel);
                }
                else if (e.PropertyName == nameof(DashboardViewModel.MonthlyExpenseData))
                {
                    UpdateExpenseChart(viewModel);
                }
            };
        }

        /// <summary>
        /// 更新月度车票图表
        /// </summary>
        private void UpdateMonthlyTicketChart(DashboardViewModel viewModel)
        {
            if (viewModel.MonthlyTicketData == null || !viewModel.MonthlyTicketData.Any()) return;

            var currentYearData = viewModel.MonthlyTicketData
                .Where(d => d.Month.StartsWith(DateTime.Now.Year.ToString()))
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Count)
                .ToList();

            var lastYearData = viewModel.MonthlyTicketData
                .Where(d => d.Month.StartsWith((DateTime.Now.Year - 1).ToString()))
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Count)
                .ToList();

            viewModel.MonthlyTicketSeries[0].Values = new ChartValues<int>(currentYearData);
            viewModel.MonthlyTicketSeries[1].Values = new ChartValues<int>(lastYearData);

            viewModel.MonthlyTicketLabels = viewModel.MonthlyTicketData
                .Where(d => d.Month.StartsWith(DateTime.Now.Year.ToString()))
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Month.Split('/')[1] + "月")
                .ToArray();
        }

        /// <summary>
        /// 更新车票类型图表
        /// </summary>
        private void UpdateTicketTypeChart(DashboardViewModel viewModel)
        {
            if (viewModel.TicketTypeData == null || !viewModel.TicketTypeData.Any()) return;

            // 根据当前主题选择适当的文本颜色
            var textColor = viewModel.IsDarkMode ? Colors.White : Colors.Black;

            viewModel.TicketTypeSeries.Clear();
            foreach (var data in viewModel.TicketTypeData)
            {
                var series = new PieSeries
                {
                    Title = data.TypeName,
                    Values = new ChartValues<int> { data.Count },
                    DataLabels = true,
                    Foreground = new SolidColorBrush(textColor),
                    LabelPoint = point => $"{data.TypeName}: {point.Y}张 ({point.Participation:P1})"
                };
                viewModel.TicketTypeSeries.Add(series);
            }
        }

        /// <summary>
        /// 更新月度支出图表
        /// </summary>
        private void UpdateExpenseChart(DashboardViewModel viewModel)
        {
            if (viewModel.MonthlyExpenseData == null || !viewModel.MonthlyExpenseData.Any()) return;

            var expenseData = viewModel.MonthlyExpenseData
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Expense)
                .ToList();

            var budgetLine = Enumerable.Repeat(viewModel.BudgetAmount, expenseData.Count()).ToList();

            viewModel.ExpenseSeries[0].Values = new ChartValues<decimal>(expenseData);
            viewModel.ExpenseSeries[1].Values = new ChartValues<double>(budgetLine.Select(b => (double)b));

            viewModel.ExpenseLabels = viewModel.MonthlyExpenseData
                .OrderBy(d => int.Parse(d.Month.Split('/')[1]))
                .Select(d => d.Month.Split('/')[1] + "月")
                .ToArray();
        }

        /// <summary>
        /// 饼图点击事件处理
        /// </summary>
        private void PieChart_DataClick(object sender, LiveCharts.ChartPoint chartPoint)
        {
            var viewModel = DataContext as DashboardViewModel;
            if (viewModel != null)
            {
                var ticketType = chartPoint.Instance as LiveCharts.Wpf.PieSeries;
                if (ticketType != null)
                {
                    viewModel.ShowTicketTypeDetailsCommand.Execute(ticketType.Title);
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        /// <summary>
        /// 处理预算文本框文本变更事件
        /// </summary>
        private void BudgetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                // 如果文本框为空，将滑块值设为0
                if (DataContext is DashboardViewModel viewModel)
                {
                    viewModel.BudgetAmount = 0;
                }
            }
        }

        /// <summary>
        /// 处理预算文本框按键事件，允许删除和退格键
        /// </summary>
        private void BudgetTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 允许删除和退格键
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                // 不做任何处理，允许事件继续传递
                return;
            }
        }

        /// <summary>
        /// 处理预算文本框失去焦点事件，确保值在有效范围内
        /// </summary>
        private void BudgetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // 如果文本框为空，设置为0
                    if (DataContext is DashboardViewModel viewModel)
                    {
                        viewModel.BudgetAmount = 0;
                        textBox.Text = "0";
                    }
                }
                else if (double.TryParse(textBox.Text.Replace(",", ""), out double value))
                {
                    // 确保值在0-10000范围内
                    if (value < 0)
                    {
                        value = 0;
                    }
                    else if (value > 10000)
                    {
                        value = 10000;
                    }

                    if (DataContext is DashboardViewModel viewModel)
                    {
                        viewModel.BudgetAmount = value;
                        textBox.Text = value.ToString("N0");
                    }
                }
            }
        }

        /// <summary>
        /// 处理ListView的鼠标滚轮事件，将事件传递给父级ScrollViewer
        /// </summary>
        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                // 获取父级ScrollViewer
                ScrollViewer scrollViewer = FindParentScrollViewer(sender as DependencyObject);
                if (scrollViewer != null)
                {
                    // 将滚动事件传递给父级ScrollViewer
                    e.Handled = true;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                    eventArg.Source = sender;
                    scrollViewer.RaiseEvent(eventArg);
                }
            }
        }

        /// <summary>
        /// 查找父级ScrollViewer控件
        /// </summary>
        private ScrollViewer FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is ScrollViewer))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as ScrollViewer;
        }
    }
}