using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Reflection;

namespace TA_WPF.Views
{
    /// <summary>
    /// AddTicketsToCollectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddTicketsToCollectionWindow : Window
    {
        private readonly AddTicketsToCollectionViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collection">收藏夹信息</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="mainViewModel">主视图模型</param>
        public AddTicketsToCollectionWindow(TicketCollectionInfo collection, DatabaseService databaseService, MainViewModel mainViewModel)
        {
            InitializeComponent();
            
            // 获取主题服务
            _themeService = ThemeService.Instance;
            
            // 创建视图模型
            _viewModel = new AddTicketsToCollectionViewModel(collection, databaseService, mainViewModel);
            DataContext = _viewModel;
            
            // 应用当前主题
            ApplyTheme(_viewModel.MainViewModel.IsDarkMode);
            
            // 订阅主题变更事件
            _themeService.ThemeChanged += OnThemeChanged;
            
            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) =>
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            };
        }
        
        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme(bool isDarkMode)
        {
            // 设置窗口主题
            ThemeAssist.SetTheme(this, isDarkMode ? BaseTheme.Dark : BaseTheme.Light);

            // 获取当前资源字典
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            // 设置深色/浅色模式
            theme.SetBaseTheme(isDarkMode ? Theme.Dark : Theme.Light);

            // 应用主题到窗口
            paletteHelper.SetTheme(theme);

            // 强制刷新窗口
            this.UpdateLayout();
        }

        /// <summary>
        /// 主题变更事件处理
        /// </summary>
        private void OnThemeChanged(object sender, bool isDarkMode)
        {
            // 更新窗口主题
            ApplyTheme(isDarkMode);
        }
        
        /// <summary>
        /// 数据表格选择变更事件
        /// </summary>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // 判断是否由SelectAll或UnselectAll触发的事件
                // 如果SelectedTickets数量与滚动条位置无关，则说明是由VM全选/取消选择触发的
                if ((_viewModel.SelectedTickets.Count == _viewModel.Tickets.Count && _viewModel.SelectedTickets.Count > 0 && 
                    dataGrid.SelectedItems.Count == _viewModel.Tickets.Count) ||
                    (_viewModel.SelectedTickets.Count == 0 && dataGrid.SelectedItems.Count == 0))
                {
                    // 由SelectAll或UnselectAll触发的，不需要更新ViewModel
                    return;
                }
                
                // 清空已选中项集合
                _viewModel.SelectedTickets.Clear();
                
                // 添加所有当前选中的项
                foreach (TrainRideInfo item in dataGrid.SelectedItems)
                {
                    item.IsSelected = true;
                    _viewModel.SelectedTickets.Add(item);
                }

                // 更新未选中项状态
                foreach (TrainRideInfo item in _viewModel.Tickets)
                {
                    if (!dataGrid.SelectedItems.Contains(item))
                    {
                        item.IsSelected = false;
                    }
                }
                
                // 直接更新ViewModel属性
                _viewModel.SelectedItemsCount = dataGrid.SelectedItems.Count;
                _viewModel.HasSelectedItems = dataGrid.SelectedItems.Count > 0;
            }
        }

        /// <summary>
        /// 窗口关闭前触发的事件
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果有数据变更，设置DialogResult为true
            if (_viewModel.HasDataChanged)
            {
                DialogResult = true;
            }
        }

        /// <summary>
        /// 页码输入框获得焦点事件
        /// </summary>
        private void PageNumber_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 获取焦点时选中所有文本，方便用户直接输入新的页码
                textBox.SelectAll();
            }
        }

        /// <summary>
        /// 页码输入框失去焦点事件
        /// </summary>
        private void PageNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ValidateAndNavigateToPage(textBox.Text);
            }
            
            // 恢复显示页码信息
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
            
            if (pageInfoPanel != null && pageNumberInput != null)
            {
                pageInfoPanel.Visibility = Visibility.Visible;
                pageNumberInput.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 页码信息面板点击事件，切换到输入模式
        /// </summary>
        private void PageInfoPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
            var pageNumberInput = this.FindName("PageNumberInput") as TextBox;

            if (pageInfoPanel != null && pageNumberInput != null)
            {
                // 显示输入框，隐藏页码信息
                pageInfoPanel.Visibility = Visibility.Collapsed;
                pageNumberInput.Visibility = Visibility.Visible;

                // 设置当前页码为默认值
                pageNumberInput.Text = _viewModel.PaginationViewModel.CurrentPage.ToString();

                // 聚焦并全选
                pageNumberInput.Focus();
                pageNumberInput.SelectAll();
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 页码输入框按键事件
        /// </summary>
        private void PageNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox)
                {
                    ValidateAndNavigateToPage(textBox.Text);
                    
                    // 恢复显示页码信息
                    var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                    var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
                    
                    if (pageInfoPanel != null && pageNumberInput != null)
                    {
                        pageInfoPanel.Visibility = Visibility.Visible;
                        pageNumberInput.Visibility = Visibility.Collapsed;
                    }
                    
                    // 回车后让输入框失去焦点
                    FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
                    Keyboard.ClearFocus();
                    
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                // 取消输入，恢复显示页码信息
                var pageInfoPanel = this.FindName("PageInfoPanel") as StackPanel;
                var pageNumberInput = this.FindName("PageNumberInput") as TextBox;
                
                if (pageInfoPanel != null && pageNumberInput != null)
                {
                    pageInfoPanel.Visibility = Visibility.Visible;
                    pageNumberInput.Visibility = Visibility.Collapsed;
                    
                    // ESC后让输入框失去焦点
                    FocusManager.SetFocusedElement(FocusManager.GetFocusScope(pageNumberInput), null);
                    Keyboard.ClearFocus();
                    
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 验证页码输入，仅允许数字
        /// </summary>
        private void PageNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 验证输入是否为数字
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            // 获取当前文本框
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // 获取当前文本和选中文本
            string currentText = textBox.Text;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;
            
            // 计算输入后的新文本
            string newText;
            if (selectionLength > 0)
            {
                // 如果有选中的文本，则替换
                newText = currentText.Substring(0, selectionStart) + e.Text + 
                          currentText.Substring(selectionStart + selectionLength);
            }
            else
            {
                // 如果没有选中的文本，则插入
                newText = currentText.Substring(0, selectionStart) + e.Text + 
                          currentText.Substring(selectionStart);
            }
            
            // 验证输入后的文本是否为有效页码
            // 在这里，我们只验证格式而不是范围，范围验证在失去焦点或按回车时进行
            if (!int.TryParse(newText, out _))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 验证并导航到指定页码
        /// </summary>
        private void ValidateAndNavigateToPage(string pageText)
        {
            if (int.TryParse(pageText, out int pageNumber))
            {
                int totalPages = _viewModel.PaginationViewModel.TotalPages;
                
                // 验证页码范围
                if (pageNumber >= 1 && pageNumber <= totalPages)
                {
                    // 记录修改前的页码
                    int oldPage = _viewModel.PaginationViewModel.CurrentPage;
                    
                    // 导航到指定页码
                    _viewModel.PaginationViewModel.CurrentPage = pageNumber;
                    
                    // 如果页码真的发生变化，确保手动触发数据加载
                    if (oldPage != pageNumber)
                    {
                        // 通过反射调用LoadTickets方法
                        var loadTicketsMethod = _viewModel.GetType().GetMethod("LoadTickets", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (loadTicketsMethod != null)
                        {
                            loadTicketsMethod.Invoke(_viewModel, null);
                        }
                    }
                }
                else
                {
                    // 如果页码超出范围，恢复为当前页码
                    _viewModel.PaginationViewModel.NotifyCurrentPageChanged();
                }
            }
            else
            {
                // 如果输入的不是有效数字，恢复为当前页码
                _viewModel.PaginationViewModel.NotifyCurrentPageChanged();
            }
        }
    }
} 