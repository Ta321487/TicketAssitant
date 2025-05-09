using System.Windows;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

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
        }
    }
} 