using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TA_WPF.Models;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// RouteStatisticalAbstractView.xaml 的交互逻辑
    /// </summary>
    public partial class RouteStatisticalAbstractView : UserControl
    {
        private RouteStatisticalAbstractViewModel _viewModel;

        public RouteStatisticalAbstractView()
        {
            InitializeComponent();
            
            // 数据上下文改变时加载数据
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is RouteStatisticalAbstractViewModel vm)
                {
                    _viewModel = vm;
                    
                    // 加载数据
                    LoadDataAsync();
                }
            };
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private async void LoadDataAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.RefreshDataAsync();
            }
        }
    }
} 