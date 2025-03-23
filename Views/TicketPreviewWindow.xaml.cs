using System;
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
        public TicketPreviewWindow(TrainRideInfo selectedTicket)
        {
            InitializeComponent();
            
            var viewModel = new TicketPreviewViewModel(selectedTicket);
            viewModel.RequestClose += (s, e) => Close();
            DataContext = viewModel;
        }
    }
} 