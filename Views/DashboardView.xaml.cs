using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.ViewModels;
using System.Diagnostics;

namespace TA_WPF.Views
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private bool _isProgrammaticallyChangingBudgetTextBox = false; // 防止 TextChanged 递归的标志

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
        private void DashboardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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
            try
            {
                // 先检查是否为数字
                if (!int.TryParse(e.Text, out _))
                {
                    e.Handled = true;
                    return;
                }

                // 获取文本框
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 获取当前选择范围前后的文本和新输入的文本组合后的完整字符串
                    string newText = textBox.Text.Substring(0, textBox.SelectionStart) + 
                                     e.Text + 
                                     textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

                    // 移除逗号（千位分隔符）再解析
                    newText = newText.Replace(",", "");

                    // 如果文本为空，允许输入
                    if (string.IsNullOrWhiteSpace(newText))
                    {
                        return;
                    }

                    // 尝试解析为数值
                    if (double.TryParse(newText, out double value))
                    {
                        // 如果值超过10000，拒绝输入
                        if (value > 10000)
                        {
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        // 无法解析为数字，拒绝输入
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"验证数字输入时出错: {ex.Message}");
                // 发生异常时，拒绝输入
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理预算文本框文本变更事件
        /// </summary>
        private void BudgetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isProgrammaticallyChangingBudgetTextBox)
                return;

            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    _isProgrammaticallyChangingBudgetTextBox = true;
                    textBox.Text = "0"; // 如果文本框为空，设置为0
                    textBox.SelectAll();  // 选择文本框，以便用户可以轻松覆盖
                    _isProgrammaticallyChangingBudgetTextBox = false;

                    // 确保ViewModel也被更新，尽管绑定"0"应该处理这个问题。
                    if (DataContext is DashboardViewModel viewModel && viewModel.BudgetAmount != 0)
                    {
                        viewModel.BudgetAmount = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 处理预算文本框按键事件，允许删除和退格键
        /// </summary>
        private void BudgetTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            // If we are programmatically changing the text, don't interfere.
            if (_isProgrammaticallyChangingBudgetTextBox)
            {
                return;
            }

            bool willBecomeEmpty = false;

            if (e.Key == Key.Back)
            {
                if (textBox.Text.Length == 1 && textBox.SelectionLength == 0 && textBox.CaretIndex == 1)
                {
                    // 从末尾删除单个字符
                    willBecomeEmpty = true;
                }
                else if (textBox.SelectionLength == textBox.Text.Length)
                {
                    // 所有文本都被选中并将被删除
                    willBecomeEmpty = true;
                }
            }
            else if (e.Key == Key.Delete)
            {
                if (textBox.Text.Length == 1 && textBox.SelectionLength == 0 && textBox.CaretIndex == 0)
                {
                    // 从开始删除单个字符
                    willBecomeEmpty = true;
                }
                else if (textBox.SelectionLength == textBox.Text.Length)
                {
                    // 所有文本都被选中并将被删除
                    willBecomeEmpty = true;
                }
            }

            if (willBecomeEmpty)
            {
                _isProgrammaticallyChangingBudgetTextBox = true;
                textBox.Text = "0";
                // 在"0"的末尾放置光标以获得更好的用户体验
                textBox.CaretIndex = textBox.Text.Length;
                _isProgrammaticallyChangingBudgetTextBox = false;
                e.Handled = true; // 这至关重要，以防止原始按键操作
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                // 允许其他非空删除/退格操作
                return;
            }
            // 对于其他按键，PreviewTextInput (NumberValidationTextBox) 将处理它们。
        }

        /// <summary>
        /// 处理预算文本框失去焦点事件，确保值在有效范围内
        /// </summary>
        private void BudgetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                try
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
                    else
                    {
                        // 无法解析为数字，重置为0
                        if (DataContext is DashboardViewModel viewModel)
                        {
                            viewModel.BudgetAmount = 0;
                            textBox.Text = "0";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"处理预算值时出错: {ex.Message}");
                    // 发生异常时设置为默认值
                    textBox.Text = "0";
                    if (DataContext is DashboardViewModel viewModel)
                    {
                        viewModel.BudgetAmount = 0;
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