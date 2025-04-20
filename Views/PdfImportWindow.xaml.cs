using System.Windows;
using TA_WPF.Services;
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
        /// <param name="pdfImportService">PDF导入服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        public PdfImportWindow(MainViewModel mainViewModel, PdfImportService pdfImportService, StationSearchService stationSearchService)
        {
            InitializeComponent();
            DataContext = new PdfImportViewModel(mainViewModel,pdfImportService,stationSearchService);
        }
    }
} 