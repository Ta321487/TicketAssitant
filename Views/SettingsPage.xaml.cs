using System;
using System.Windows;
using System.Windows.Controls;
using TA_WPF.ViewModels;
using System.Configuration;
using System.Globalization;

namespace TA_WPF.Views
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            
            // 注册加载事件
            this.Loaded += SettingsPage_Loaded;
        }
        
        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保字体大小滑块的值与当前应用程序的字体大小一致
            UpdateFontSizeSlider();
        }
        
        private void UpdateFontSizeSlider()
        {
            try
            {
                // 从配置文件中读取当前字体大小
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["FontSize"] != null)
                {
                    if (double.TryParse(config.AppSettings.Settings["FontSize"].Value, out double fontSize))
                    {
                        // 确保字体大小在有效范围内
                        if (fontSize < FontSizeSlider.Minimum)
                        {
                            fontSize = FontSizeSlider.Minimum;
                        }
                        else if (fontSize > FontSizeSlider.Maximum)
                        {
                            fontSize = FontSizeSlider.Maximum;
                        }
                        
                        // 如果DataContext已设置，更新ViewModel中的FontSize属性
                        if (DataContext is MainViewModel viewModel)
                        {
                            // 暂时取消绑定更新，避免循环更新
                            viewModel.SuspendFontSizeUpdate = true;
                            viewModel.FontSize = fontSize;
                            viewModel.SuspendFontSizeUpdate = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新字体大小滑块时出错: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
} 