using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace TA_WPF.Views
{
    /// <summary>
    /// AdvancedQueryTicketPanel.xaml 的交互逻辑
    /// </summary>
    public partial class AdvancedQueryTicketPanel : UserControl
    {
        public AdvancedQueryTicketPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 验证车次号输入，只允许输入数字且不能以0开头
        /// </summary>
        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;

            // 使用正则表达式验证输入
            Regex regex = new Regex("[^0-9]+");
            bool isNotNumber = regex.IsMatch(e.Text);

            // 检测是否是数字
            if (isNotNumber)
            {
                e.Handled = true;
                return;
            }

            // 如果是第一个字符且为0，则拒绝输入
            if (textBox.Text.Length == 0 && e.Text == "0")
            {
                e.Handled = true;
                return;
            }

            // 确保长度不超过4位
            if (textBox.Text.Length >= 4)
            {
                e.Handled = true;
                return;
            }
        }
    }
}