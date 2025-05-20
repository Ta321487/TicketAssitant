using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
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
        /// 车厢号输入验证，只允许输入数字
        /// </summary>
        private void CoachNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车厢号输入时出错", ex);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 车次号输入验证，只允许输入数字
        /// </summary>
        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车次号输入时出错", ex);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 出发车站列表选择变更事件处理
        /// </summary>
        private void DepartStationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PdfImportViewModel viewModel && e.AddedItems.Count > 0)
            {
                viewModel.HandleDepartStationSelected();
            }
        }

        /// <summary>
        /// 到达车站列表选择变更事件处理
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