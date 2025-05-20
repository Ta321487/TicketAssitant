using System.Windows;
using System.Windows.Controls;
using TA_WPF.ViewModels;
using MaterialDesignThemes.Wpf;
using TA_WPF.Services;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace TA_WPF.Views
{
    /// <summary>
    /// AddTicketsToCollectionPanel.xaml 的交互逻辑
    /// </summary>
    public partial class AddTicketsToCollectionPanel : UserControl
    {
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AddTicketsToCollectionPanel()
        {
            InitializeComponent();
            
            // 获取主题服务
            _themeService = Services.ThemeService.Instance;
            
            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            ApplyTheme(isDarkMode);
            
            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;
            
            // 控件卸载时取消订阅事件
            this.Unloaded += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
        }
        
        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme(bool isDarkMode)
        {
            // 设置控件主题
            ThemeAssist.SetTheme(this, isDarkMode ? BaseTheme.Dark : BaseTheme.Light);
            
            // 获取当前资源字典
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            
            // 设置深色/浅色模式
            theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);
            
            // 强制刷新控件
            this.UpdateLayout();
        }
        
        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            // 更新控件主题
            ApplyTheme(isDarkMode);
        }
        
        /// <summary>
        /// 验证车次号输入
        /// </summary>
        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 获取当前文本框
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // 获取当前文本和选中文本
            string currentText = textBox.Text;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;
            
            // 计算如果输入被接受后的新文本
            string newText = currentText.Substring(0, selectionStart) + e.Text +
                            (selectionLength > 0 ? "" : currentText.Substring(selectionStart + selectionLength));
            
            // 仅允许输入数字
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
                return;
            }
            
            // 检查长度是否超过4位
            if (newText.Length > 4)
            {
                e.Handled = true;
                return;
            }
            
            // 检查是否以0开头
            if (selectionStart == 0 && e.Text == "0")
            {
                e.Handled = true;
                return;
            }
        }
        
        /// <summary>
        /// 验证年份输入
        /// </summary>
        private void Year_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 仅允许输入数字
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
} 