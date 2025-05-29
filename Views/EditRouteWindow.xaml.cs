using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.Diagnostics;
using System.Windows.Media;

namespace TA_WPF.Views
{
    /// <summary>
    /// EditRouteWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditRouteWindow : Window
    {
        private readonly EditRouteViewModel _viewModel;

        /// <summary>
        /// 使用RouteInfo对象初始化窗口的构造函数
        /// </summary>
        /// <param name="route">要编辑的路线</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public EditRouteWindow(RouteInfo route, DatabaseService databaseService = null, MainViewModel mainViewModel = null)
        {
            Debug.WriteLine("正在初始化EditRouteWindow");
            
            // 记录路线数据
            if (route != null)
            {
                Debug.WriteLine($"路线数据: ID={route.Id}, 名称={route.RouteName}");
                Debug.WriteLine($"路线图片数据: {(route.CoverImage != null ? $"{route.CoverImage.Length}字节" : "无图片数据")}");
            }
            
            InitializeComponent();
            
            // 创建视图模型并传入路线对象
            _viewModel = new EditRouteViewModel(route, databaseService, mainViewModel);
            
            // 设置DataContext
            DataContext = _viewModel;
            Debug.WriteLine("EditRouteWindow DataContext已设置");
            
            // 注册Loaded事件，设置初始焦点
            this.Loaded += EditRouteWindow_Loaded;
        }
        
        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void EditRouteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 输出视图模型中的图片状态
            Debug.WriteLine($"EditRouteWindow_Loaded: HasCoverImage={_viewModel.HasCoverImage}");
            if (_viewModel.CoverImage != null)
            {
                Debug.WriteLine($"EditRouteWindow_Loaded: 图片数据长度={_viewModel.CoverImage.Length}字节");
            }
            
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