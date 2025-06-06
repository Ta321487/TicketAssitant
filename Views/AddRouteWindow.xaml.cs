using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// AddRouteWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddRouteWindow : Window
    {
        private readonly AddRouteViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public AddRouteWindow(DatabaseService databaseService = null, MainViewModel mainViewModel = null)
        {
            InitializeComponent();

            // 创建视图模型
            _viewModel = new AddRouteViewModel(databaseService, mainViewModel);

            // 设置DataContext
            DataContext = _viewModel;

            // 注册Loaded事件，设置初始焦点
            this.Loaded += AddRouteWindow_Loaded;
        }

        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void AddRouteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 查找第一个TextBox控件并设置焦点
            TextBox firstTextBox = FindFirstTextBox();
            if (firstTextBox != null)
            {
                firstTextBox.Focus();
            }
        }

        /// <summary>
        /// 查找第一个TextBox控件
        /// </summary>
        private TextBox FindFirstTextBox()
        {
            // 寻找视觉树中的第一个TextBox
            return FindVisualChild<TextBox>(this);
        }

        /// <summary>
        /// 在视觉树中查找指定类型的第一个子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child != null && child is T)
                {
                    return (T)child;
                }

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        /// <summary>
        /// 总里程输入限制，只允许输入数字和小数点
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 判断输入是否为数字或小数点
            var regex = new Regex(@"^[0-9]+(\.[0-9]*)?$");

            // 获取当前文本框
            TextBox textBox = sender as TextBox;
            string updatedText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            // 如果输入的是中文句号"。"，自动转换为英文句号"."
            if (e.Text == "。")
            {
                e.Handled = true;
                int caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, ".");
                textBox.CaretIndex = caretIndex + 1;
                return;
            }

            // 检查是否为有效数字格式（只允许一个小数点）
            if (!regex.IsMatch(updatedText))
            {
                e.Handled = true;
            }
        }
    }
}