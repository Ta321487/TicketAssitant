using System.Windows;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// PdfImportWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PdfImportWindow : Window
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        public PdfImportWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = new PdfImportViewModel(mainViewModel);
        }
    }
} 