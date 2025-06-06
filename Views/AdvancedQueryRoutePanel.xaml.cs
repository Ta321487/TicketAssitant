using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// AdvancedQueryRoutePanel.xaml 的交互逻辑
    /// </summary>
    public partial class AdvancedQueryRoutePanel : UserControl
    {
        private DispatcherTimer _inputDebounceTimer;

        public AdvancedQueryRoutePanel()
        {
            InitializeComponent();

            // 添加Loaded事件处理，确保UI正确反映ViewModel状态
            this.Loaded += AdvancedQueryRoutePanel_Loaded;

            // 初始化输入延迟计时器，用于路线名称搜索
            _inputDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // 设置300毫秒的输入间隔
            };
            _inputDebounceTimer.Tick += InputDebounceTimer_Tick;
        }

        private void AdvancedQueryRoutePanel_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保数据绑定已更新
            if (DataContext is AdvancedQueryRouteViewModel viewModel)
            {
                // 触发属性更新，确保UI反映最新状态
                viewModel.NotifyPropertiesChanged();
            }

            // 为路线名称输入框添加文本变更事件
            var routeNameTextBox = FindRouteNameTextBox();
            if (routeNameTextBox != null)
            {
                routeNameTextBox.TextChanged += RouteNameTextBox_TextChanged;
            }
        }

        private TextBox FindRouteNameTextBox()
        {
            // 查找XAML中的路线名称输入框
            // 这里使用简单的查找方法，也可以通过添加Name属性并使用FindName更直接地查找
            var grids = FindVisualChildren<Grid>(this);
            foreach (var grid in grids)
            {
                var textBoxes = FindVisualChildren<TextBox>(grid);
                foreach (var textBox in textBoxes)
                {
                    // 通过绑定属性识别是否为路线名称输入框
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    if (binding?.ResolvedSourcePropertyName == "RouteNameFilter")
                    {
                        return textBox;
                    }
                }
            }
            return null;
        }

        private void RouteNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is AdvancedQueryRouteViewModel viewModel)
            {
                // 重置定时器，实现输入防抖
                _inputDebounceTimer.Stop();
                _inputDebounceTimer.Start();
            }
        }

        private void InputDebounceTimer_Tick(object sender, EventArgs e)
        {
            // 停止定时器
            _inputDebounceTimer.Stop();

            // 此时已经过了设定的延迟时间，可以执行搜索操作
            // 无需额外代码，因为文本已经通过绑定更新到ViewModel，ViewModel会自动执行搜索
        }

        /// <summary>
        /// 查找指定类型的子控件
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}