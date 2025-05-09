using System.Windows;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// AddCollectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddCollectionWindow : Window
    {
        private readonly AddCollectionViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public AddCollectionWindow(DatabaseService databaseService = null)
        {
            InitializeComponent();
            
            // 创建视图模型
            _viewModel = new AddCollectionViewModel(databaseService);
            
            // 设置DataContext
            DataContext = _viewModel;
        }
    }
} 