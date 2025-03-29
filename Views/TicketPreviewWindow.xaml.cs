using System.Windows;
using TA_WPF.Models;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// TicketPreviewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TicketPreviewWindow : Window
    {
        private TicketPreviewViewModel _viewModel;
        
        public TicketPreviewWindow(TrainRideInfo selectedTicket)
        {
            InitializeComponent();
            
            _viewModel = new TicketPreviewViewModel(selectedTicket);
            _viewModel.RequestClose += (s, e) => Close();
            DataContext = _viewModel;
            
            // 订阅主题服务的主题变更事件，以便在主题变化时更新窗口样式
            Services.ThemeService.Instance.ThemeChanged += ThemeService_ThemeChanged;
            
            // 在窗口关闭时取消事件订阅
            Closed += (s, e) => 
            {
                Services.ThemeService.Instance.ThemeChanged -= ThemeService_ThemeChanged;
            };
        }
        
        private void ThemeService_ThemeChanged(object sender, bool isDarkMode)
        {
            // 更新ViewModel中的主题设置
            if (_viewModel != null)
            {
                _viewModel.IsDarkMode = isDarkMode;
            }
        }
    }
} 