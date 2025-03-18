using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TA_WPF.ViewModels;

namespace TA_WPF.Converters
{
    /// <summary>
    /// 将布尔值转换为WindowState的转换器
    /// </summary>
    public class FullScreenWindowStateConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为WindowState
        /// </summary>
        /// <param name="value">布尔值，表示是否全屏</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数，可以是WindowState类型，表示之前的窗口状态</param>
        /// <param name="culture">区域信息</param>
        /// <returns>全屏时返回Maximized，否则返回之前的状态或Normal</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFullScreen && isFullScreen)
            {
                // 全屏模式下使用最大化状态
                return WindowState.Maximized;
            }
            
            // 尝试从绑定源获取DashboardViewModel
            if (value is bool && Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel && 
                mainViewModel.DashboardViewModel != null)
            {
                WindowState previousState = mainViewModel.DashboardViewModel.PreviousWindowState;
                if (previousState != WindowState.Minimized)
                {
                    return previousState;
                }
            }
            
            return WindowState.Normal; // 默认返回普通状态
        }

        /// <summary>
        /// 将WindowState转换为布尔值
        /// </summary>
        /// <param name="value">WindowState值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>如果是Maximized则返回true，否则返回false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WindowState state)
            {
                return state == WindowState.Maximized;
            }
            
            return false;
        }
    }
} 