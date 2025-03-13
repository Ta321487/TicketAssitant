using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.ViewModels;
using System.Windows.Controls.Primitives;

namespace TA_WPF.Views
{
    /// <summary>
    /// QueryAllTicketsPage.xaml 的交互逻辑
    /// </summary>
    public partial class QueryAllTicketsPage : UserControl
    {
        private Popup _pageNumberTooltip;
        private TextBlock _tooltipText;
        private StackPanel _pageInfoPanel;
        private TextBox _pageNumberInput;

        public QueryAllTicketsPage()
        {
            InitializeComponent();
            
            // 初始化页码提示框
            InitializePageNumberTooltip();
            
            // 获取控件引用
            _pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            _pageNumberInput = this.FindName("PageNumberInput") as TextBox;
        }
        
        /// <summary>
        /// 初始化页码提示框
        /// </summary>
        private void InitializePageNumberTooltip()
        {
            _tooltipText = new TextBlock
            {
                Padding = new Thickness(8),
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14
            };

            _pageNumberTooltip = new Popup
            {
                Child = _tooltipText,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };
        }
        
        /// <summary>
        /// 处理页码信息面板的点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pageInfoPanel == null || _pageNumberInput == null)
            {
                _pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                _pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            }

            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                _pageInfoPanel.Visibility = Visibility.Collapsed;
                _pageNumberInput.Visibility = Visibility.Visible;
                
                // 设置当前页码为默认值
                var viewModel = DataContext as QueryAllTicketsViewModel;
                if (viewModel != null)
                {
                    _pageNumberInput.Text = viewModel.CurrentPage.ToString();
                }
                
                // 聚焦并全选
                _pageNumberInput.Focus();
                _pageNumberInput.SelectAll();
            }
        }
        
        /// <summary>
        /// 处理页码输入框按键事件
        /// </summary>
        private void PageNumberInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 按回车键确认输入
                TryNavigateToPage();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // 按ESC键取消输入
                RestorePageInfoDisplay();
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 限制只能输入数字
        /// </summary>
        private void PageNumberInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            // 检查输入的数字是否在有效范围内
            var textBox = sender as TextBox;
            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (textBox != null && viewModel != null)
            {
                // 获取输入后的完整文本
                string newText = textBox.Text.Substring(0, textBox.SelectionStart) + e.Text + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);
                
                // 尝试解析为数字
                if (int.TryParse(newText, out int pageNumber))
                {
                    // 如果输入的数字大于总页数，则不允许输入
                    if (pageNumber > viewModel.TotalPages)
                    {
                        e.Handled = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// 阻止粘贴非数字内容
        /// </summary>
        private void PageNumberInput_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                // 获取剪贴板内容
                string clipboardText = Clipboard.GetText();
                
                // 检查是否为数字
                if (!int.TryParse(clipboardText, out _))
                {
                    e.Handled = true;
                    return;
                }
                
                // 检查粘贴后的数字是否在有效范围内
                var textBox = sender as TextBox;
                var viewModel = DataContext as QueryAllTicketsViewModel;
                if (textBox != null && viewModel != null)
                {
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // 获取粘贴后的完整文本
                        string newText = textBox.Text.Substring(0, textBox.SelectionStart) + clipboardText + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);
                        
                        // 尝试解析为数字
                        if (int.TryParse(newText, out int pageNumber))
                        {
                            // 如果粘贴后的数字大于总页数，则不允许粘贴
                            if (pageNumber > viewModel.TotalPages)
                            {
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理页码输入框失去焦点事件
        /// </summary>
        private void PageNumberInput_LostFocus(object sender, RoutedEventArgs e)
        {
            RestorePageInfoDisplay();
        }
        
        /// <summary>
        /// 尝试导航到输入的页码
        /// </summary>
        private void TryNavigateToPage()
        {
            if (_pageNumberInput == null || _pageInfoPanel == null)
                return;

            var viewModel = DataContext as QueryAllTicketsViewModel;
            if (viewModel == null)
                return;

            // 尝试解析页码
            if (int.TryParse(_pageNumberInput.Text, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber >= 1 && pageNumber <= viewModel.TotalPages)
                {
                    // 导航到指定页码
                    viewModel.CurrentPage = pageNumber;
                }
                else if (pageNumber < 1)
                {
                    // 如果页码小于1，则导航到第一页
                    viewModel.CurrentPage = 1;
                    ShowPageNumberTooltip($"已自动跳转到第1页");
                }
                else if (pageNumber > viewModel.TotalPages)
                {
                    // 如果页码大于总页数，则导航到最后一页
                    viewModel.CurrentPage = viewModel.TotalPages;
                    ShowPageNumberTooltip($"已自动跳转到第{viewModel.TotalPages}页");
                }
            }
            else if (string.IsNullOrWhiteSpace(_pageNumberInput.Text))
            {
                // 如果输入为空，则不做任何操作
            }
            
            // 恢复页码信息显示
            RestorePageInfoDisplay();
        }
        
        /// <summary>
        /// 显示页码提示信息
        /// </summary>
        private void ShowPageNumberTooltip(string message)
        {
            if (_pageNumberTooltip != null && _tooltipText != null && _pageInfoPanel != null)
            {
                _tooltipText.Text = message;
                _pageNumberTooltip.PlacementTarget = _pageInfoPanel;
                _pageNumberTooltip.IsOpen = true;
                
                // 3秒后自动关闭
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, e) =>
                {
                    _pageNumberTooltip.IsOpen = false;
                    timer.Stop();
                };
                timer.Start();
            }
        }
        
        /// <summary>
        /// 恢复页码信息显示
        /// </summary>
        private void RestorePageInfoDisplay()
        {
            if (_pageInfoPanel != null && _pageNumberInput != null)
            {
                _pageInfoPanel.Visibility = Visibility.Visible;
                _pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }
    }
} 