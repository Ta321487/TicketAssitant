using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.Windows.Controls;

namespace TA_WPF.Views
{
    /// <summary>
    /// EditCollectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditCollectionWindow : Window
    {
        private readonly EditCollectionViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">要编辑的收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public EditCollectionWindow(TicketCollectionInfo collection, DatabaseService databaseService = null, MainViewModel mainViewModel = null)
        {
            InitializeComponent();
            
            // 创建视图模型并传入收藏夹信息
            _viewModel = new EditCollectionViewModel(collection, databaseService, mainViewModel);
            
            // 设置DataContext
            DataContext = _viewModel;
            
            // 注册Loaded事件，设置初始焦点
            this.Loaded += EditCollectionWindow_Loaded;
        }
        
        /// <summary>
        /// 窗口加载完成事件处理
        /// </summary>
        private void EditCollectionWindow_Loaded(object sender, RoutedEventArgs e)
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
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
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
    }
} 