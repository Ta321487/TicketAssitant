using System.Windows;
using System.Windows.Controls;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// PdfImportWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PdfImportWindow : Window
    {
        private PdfImportViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        /// <param name="pdfImportService">PDF导入服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        public PdfImportWindow(MainViewModel mainViewModel, PdfImportService pdfImportService, StationSearchService stationSearchService)
        {
            InitializeComponent();
            _viewModel = new PdfImportViewModel(mainViewModel, pdfImportService, stationSearchService);
            DataContext = _viewModel;
        }

        /// <summary>
        /// 出发站列表选择变更事件处理
        /// </summary>
        private void DepartStationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PdfImportViewModel viewModel && e.AddedItems.Count > 0)
            {
                viewModel.HandleDepartStationSelected();
            }
        }

        /// <summary>
        /// 到达站列表选择变更事件处理
        /// </summary>
        private void ArriveStationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PdfImportViewModel viewModel && e.AddedItems.Count > 0)
            {
                viewModel.HandleArriveStationSelected();
            }
        }
    }
} 