using System.Windows;
using System.Windows.Controls;
using TA_WPF.ViewModels;
using System.Configuration;

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
        
        /// <summary>
        /// 字体大小滑块值变化事件处理
        /// </summary>
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FontSizeText != null)
            {
                // 四舍五入到整数
                int fontSize = (int)Math.Round(e.NewValue);
                
                // 直接更新显示值，确保实时反馈
                FontSizeText.Text = $"{fontSize}pt";
                
                // 如果DataContext已设置，更新ViewModel中的FontSize属性
                if (DataContext is MainViewModel viewModel)
                {
                    // 确保值已经四舍五入为整数
                    if (Math.Abs(viewModel.FontSize - fontSize) > 0.01)
                    {
                        viewModel.FontSize = fontSize;
                    }
                }
            }
        }
    }
} 