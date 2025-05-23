using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        // 字体大小的最小值和最大值
        private const double MinFontSize = 12;
        private const double MaxFontSize = 20;

        private Slider fontSizeSlider;
        private TextBlock fontSizeText;

        public SettingsPage()
        {
            // 注意：InitializeComponent 是自动生成的方法，编译器会补全
            InitializeComponent();

            // 注册加载事件
            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try 
            {
                // 查找控件 - 使用逻辑树查找而不是FindName，更加可靠
                fontSizeSlider = GetDescendantByName(this, "FontSizeSlider") as Slider;
                fontSizeText = GetDescendantByName(this, "FontSizeText") as TextBlock;

                // 确保字体大小滑块的值与当前应用程序的字体大小一致
                UpdateFontSizeFromConfig();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载设置页面控件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 在逻辑树中查找指定名称的元素
        /// </summary>
        private static DependencyObject GetDescendantByName(DependencyObject parent, string name)
        {
            // 检查当前元素
            if (parent is FrameworkElement element && element.Name == name)
                return parent;

            // 递归搜索所有子元素
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                DependencyObject result = GetDescendantByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void UpdateFontSizeFromConfig()
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
                        if (fontSize < MinFontSize)
                        {
                            fontSize = MinFontSize;
                        }
                        else if (fontSize > MaxFontSize)
                        {
                            fontSize = MaxFontSize;
                        }

                        // 如果DataContext已设置，更新ViewModel中的FontSize属性
                        if (DataContext is SettingsViewModel viewModel)
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
                Debug.WriteLine($"更新字体大小时出错: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 验证TextBox输入，只允许输入数字和字母
        /// </summary>
        private void ApiKey_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 使用正则表达式验证输入是否为数字或字母
            Regex regex = new Regex("[^a-zA-Z0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 拦截空格键，阻止输入空格
        /// </summary>
        private void ApiKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // 阻止空格键输入
            }
        }

        /// <summary>
        /// 文本改变时删除所有空格
        /// </summary>
        private void ApiKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;
                string text = textBox.Text;
                
                // 检查是否包含空格
                if (text.Contains(" "))
                {
                    // 移除所有空格
                    string newText = text.Replace(" ", "");
                    textBox.Text = newText;
                    
                    // 调整光标位置，避免光标跳到文本末尾
                    if (caretIndex > newText.Length)
                    {
                        textBox.CaretIndex = newText.Length;
                    }
                    else
                    {
                        // 计算新的光标位置，考虑到空格被删除的情况
                        int spacesBeforeCaretInOriginal = text.Substring(0, caretIndex).Count(c => c == ' ');
                        textBox.CaretIndex = caretIndex - spacesBeforeCaretInOriginal;
                    }
                }
            }
        }

        /// <summary>
        /// 粘贴事件处理，过滤粘贴内容中的空格
        /// </summary>
        private void ApiKey_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = e.DataObject.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(pastedText))
                {
                    // 移除所有空格和不允许的字符
                    string filteredText = Regex.Replace(pastedText, @"[^a-zA-Z0-9]", "");
                    
                    if (sender is TextBox textBox)
                    {
                        // 手动处理粘贴操作
                        int caretIndex = textBox.CaretIndex;
                        string currentText = textBox.Text;
                        string newText = currentText.Substring(0, caretIndex) + filteredText;
                        
                        if (caretIndex < currentText.Length)
                        {
                            newText += currentText.Substring(caretIndex);
                        }
                        
                        textBox.Text = newText;
                        textBox.CaretIndex = caretIndex + filteredText.Length;
                    }
                    
                    // 取消默认的粘贴操作
                    e.CancelCommand();
                }
            }
        }

        /// <summary>
        /// 字体大小滑块值变化事件处理
        /// </summary>
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 四舍五入到整数
            int fontSize = (int)Math.Round(e.NewValue);

            // 更新字体大小显示
            if (fontSizeText != null)
            {
                fontSizeText.Text = $"{fontSize}pt";
            }

            // 如果DataContext已设置，更新ViewModel中的FontSize属性
            if (DataContext is SettingsViewModel viewModel)
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