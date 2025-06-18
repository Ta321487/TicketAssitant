using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.Services;
using TA_WPF.Utils;
using TA_WPF.ViewModels;

namespace TA_WPF.Views
{
    /// <summary>
    /// PdfImportWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PdfImportWindow : Window
    {
        private PdfImportViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        /// <param name="pdfImportService">PDF导入服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        public PdfImportWindow(MainViewModel mainViewModel, PdfImportService pdfImportService, StationSearchService stationSearchService)
        {
            InitializeComponent();
            _viewModel = new PdfImportViewModel(mainViewModel, pdfImportService, stationSearchService);
            DataContext = _viewModel;
        }

        /// <summary>
        /// 车厢号输入验证，只允许输入数字
        /// </summary>
        private void CoachNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车厢号输入时出错", ex);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 车次号输入验证，只允许输入数字
        /// </summary>
        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字
                Regex regex = new Regex("[^0-9]+");
                e.Handled = regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理车次号输入时出错", ex);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 金额输入验证，只允许输入数字和小数点
        /// </summary>
        private void MoneyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // 只允许输入数字和小数点
                Regex regex = new Regex("[^0-9.]+");
                e.Handled = regex.IsMatch(e.Text);

                // 如果输入的是小数点，检测是否已经有小数点
                if (e.Text == ".")
                {
                    TextBox textBox = sender as TextBox;
                    if (textBox != null && textBox.Text.Contains("."))
                    {
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额输入时出错", ex);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 金额输入框按键处理，特别处理小数点的删除情况
        /// </summary>
        private void MoneyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    // 判断是否所有文本都被选中
                    bool allTextSelected = textBox.SelectionLength == textBox.Text.Length && textBox.SelectionLength > 0;

                    // 处理全选后按Delete或Backspace的情况
                    if (allTextSelected && (e.Key == Key.Delete || e.Key == Key.Back))
                    {
                        // 替换为"0.00"而不是空字符串
                        textBox.Text = "0.00";
                        textBox.SelectAll();
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine("金额框全选删除: 已替换为0.00");
                        return;
                    }

                    if (e.Key == Key.Back)
                    {
                        int caretIndex = textBox.CaretIndex;
                        string text = textBox.Text;

                        // 光标在小数点后面时
                        if (caretIndex > 0 && caretIndex < text.Length && text[caretIndex - 1] == '.')
                        {
                            // 记录当前光标位置的前后部分
                            string textBeforeCaret = text.Substring(0, caretIndex - 1);
                            string textAfterCaret = text.Substring(caretIndex);

                            // 构建新值，确保小数部分仍然是小数
                            decimal newValue;
                            bool parseSuccess = false;

                            // 尝试解析小数点前的部分
                            if (decimal.TryParse(textBeforeCaret, out decimal beforePart))
                            {
                                // 尝试解析小数点后的部分作为小数
                                if (decimal.TryParse("0." + textAfterCaret, out decimal afterPart))
                                {
                                    // 合并两个部分
                                    newValue = beforePart + afterPart;
                                    parseSuccess = true;

                                    // 转换为字符串，保持格式
                                    string newText = newValue.ToString("F" + textAfterCaret.Length);

                                    // 日志输出
                                    System.Diagnostics.Debug.WriteLine($"金额框移除小数点: 原值={text}, 光标位置={caretIndex}, 修改后={newText}");

                                    // 更新文本内容
                                    textBox.Text = newText;

                                    // 设置光标位置在原来小数点的位置
                                    textBox.CaretIndex = caretIndex - 1;

                                    // 标记事件已处理
                                    e.Handled = true;
                                }
                            }

                            if (!parseSuccess)
                            {
                                // 如果解析失败，使用原始的方式处理
                                System.Diagnostics.Debug.WriteLine($"金额框移除小数点(解析失败): 原值={text}, 光标位置={caretIndex}, 尝试简单拼接");

                                // 更新文本内容，移除小数点
                                textBox.Text = textBeforeCaret + textAfterCaret;

                                // 设置光标位置在原来小数点的位置
                                textBox.CaretIndex = caretIndex - 1;

                                // 标记事件已处理
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额键盘按键事件时出错", ex);
            }
        }

        /// <summary>
        /// 金额失去焦点时格式化显示
        /// </summary>
        private void MoneyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null && _viewModel != null)
                {
                    // 保存当前前景色
                    var foreground = textBox.Foreground;

                    // 尝试解析金额
                    if (decimal.TryParse(textBox.Text, out decimal amount))
                    {
                        // 格式化为两位小数
                        _viewModel.Money = Math.Round(amount, 2);
                    }
                    else if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        // 如果为空，设置为0.00
                        _viewModel.Money = 0;
                    }
                    else
                    {
                        // 如果无法解析，恢复为0.00
                        MessageBoxHelper.ShowWarning("请输入有效的金额数值");
                        _viewModel.Money = 0;
                    }

                    // 确保前景色不变
                    textBox.Foreground = foreground;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("处理金额失去焦点事件时出错", ex);
            }
        }

        /// <summary>
        /// 出发车站列表选择变更事件处理
        /// </summary>
        private void DepartStationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PdfImportViewModel viewModel && e.AddedItems.Count > 0)
            {
                viewModel.HandleDepartStationSelected();
            }
        }

        /// <summary>
        /// 到达车站列表选择变更事件处理
        /// </summary>
        private void ArriveStationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PdfImportViewModel viewModel && e.AddedItems.Count > 0)
            {
                viewModel.HandleArriveStationSelected();
            }
        }
    }
}