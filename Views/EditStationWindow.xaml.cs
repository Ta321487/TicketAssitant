using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.ViewModels;
using System.ComponentModel;
using TA_WPF.Utils; // 添加工具类引用

namespace TA_WPF.Views
{
    /// <summary>
    /// EditStationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditStationWindow : Window
    {
        private readonly EditStationViewModel _viewModel;
        private readonly ThemeService _themeService;
        private bool _isClosing = false; // 窗口关闭标志

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="stationSearchService">车站搜索服务</param>
        /// <param name="stationToEdit">要编辑的车站信息</param>
        /// <param name="refreshCallback">刷新回调</param>
        public EditStationWindow(DatabaseService databaseService, StationSearchService stationSearchService, StationInfo stationToEdit, Action refreshCallback)
        {
            InitializeComponent();

            // 获取主题服务实例
            _themeService = ThemeService.Instance;
            
            // 创建配置服务和地理编码服务实例
            var configurationService = new ConfigurationService();
            var geocodingService = new GeocodingService(configurationService);

            // 初始化ViewModel
            _viewModel = new EditStationViewModel(
                databaseService, 
                stationSearchService, 
                geocodingService, 
                configurationService, 
                stationToEdit, 
                refreshCallback);
            
            // 设置DataContext
            DataContext = _viewModel;

            // 订阅关闭窗口事件
            _viewModel.CloseWindow += (s, e) => 
            {
                this.DialogResult = true;
                Close();
            };

            // 设置所有者窗口
            if (Application.Current?.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            // 应用当前主题
            bool isDarkMode = _themeService.IsDarkThemeActive();
            _themeService.ApplyThemeToWindow(this, isDarkMode);

            // 订阅主题变化事件
            _themeService.ThemeChanged += ThemeService_ThemeChanged;

            // 窗口关闭时取消订阅事件
            this.Closed += (s, e) => {
                _themeService.ThemeChanged -= ThemeService_ThemeChanged;
            };

            // 更新字体大小
            UpdateFontSize();
        }
        
        /// <summary>
        /// 窗口关闭前检查是否有未保存的修改
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            // 标记窗口正在关闭
            _isClosing = true;
            
            base.OnClosing(e);
            
            try
            {
                // 如果DialogResult已设置，说明是通过保存按钮关闭的，不需要提示
                if (this.DialogResult.HasValue)
                    return;
                    
                // 检查是否有未保存的修改
                if (_viewModel.HasUnsavedChanges())
                {
                    // 显示确认对话框
                    bool? result = MessageDialog.Show(
                        "您有未保存的修改，是否保存？",
                        "未保存的修改",
                        MessageType.Question,
                        MessageButtons.YesNoCancel,
                        this);
                        
                    if (result == true) // 是
                    {
                        // 执行保存命令
                        if (_viewModel.SaveCommand.CanExecute(null))
                        {
                            _viewModel.SaveCommand.Execute(null);
                            
                            // 如果保存命令执行后窗口仍然打开，说明保存失败，取消关闭
                            if (this.IsVisible)
                            {
                                e.Cancel = true;
                            }
                        }
                        else
                        {
                            // 如果保存命令无法执行，取消关闭
                            e.Cancel = true;
                        }
                    }
                    else if (result == null) // 取消
                    {
                        // 取消关闭
                        e.Cancel = true;
                    }
                    // 否则 (result == false) 不保存，直接关闭
                }
                // 如果没有修改，直接关闭窗口，不提示
            }
            catch (Exception ex)
            {
                LogHelper.LogError("关闭修改车站窗口时出错", ex);
                MessageBoxHelper.ShowError("关闭窗口时出错: " + ex.Message);
            }
        }

        /// <summary>
        /// 主题变化事件处理
        /// </summary>
        private void ThemeService_ThemeChanged(object sender, bool isDarkMode)
        {
            // 应用主题到当前窗口
            _themeService.ApplyThemeToWindow(this, isDarkMode);
        }

        /// <summary>
        /// 更新字体大小
        /// </summary>
        private void UpdateFontSize()
        {
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.Contains("MaterialDesignFontSize"))
            {
                double fontSize = (double)Application.Current.Resources["MaterialDesignFontSize"];
                if (_viewModel != null)
                {
                    _viewModel.FontSize = fontSize;
                }
            }
        }
        
        /// <summary>
        /// 处理铁路局建议列表选择改变事件
        /// </summary>
        private void RailwayBureauListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                try
                {
                    string selectedValue = listBox.SelectedItem.ToString();
                    Debug.WriteLine($"[调试] 选择了铁路局: {selectedValue}");
                    
                    _viewModel.RailwayBureau = selectedValue;
                    Debug.WriteLine($"[调试] 设置了RailwayBureau: {_viewModel.RailwayBureau}");
                    
                    // 关闭下拉框
                    _viewModel.IsRailwayBureauDropdownOpen = false;
                    Debug.WriteLine("[调试] 关闭了下拉框");
                    
                    // 清除选择，避免下次打开时仍然选中
                    listBox.SelectedItem = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[调试] 异常: {ex.Message}");
                    MessageBox.Show($"选择铁路局发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 处理铁路局建议项点击事件 (已废弃，使用SelectionChanged事件)
        /// </summary>
        private void RailwayBureauSuggestion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
            {
                try
                {
                    // 获取选中的值
                    string selectedValue = textBlock.Text;
                    Debug.WriteLine($"[调试] 点击了铁路局选项: {selectedValue}");
                    
                    // 直接设置文本框的值，通过在父控件中查找同名控件
                    TextBox railwayBureauTextBox = this.FindName("RailwayBureauTextBox") as TextBox;
                    if (railwayBureauTextBox != null)
                    {
                        Debug.WriteLine($"[调试] 找到文本框控件，当前值: {railwayBureauTextBox.Text}");
                        railwayBureauTextBox.Text = selectedValue;
                        Debug.WriteLine($"[调试] 设置文本框新值: {selectedValue}");
                        
                        // 通知数据绑定更新
                        var bindingExpression = railwayBureauTextBox.GetBindingExpression(TextBox.TextProperty);
                        if (bindingExpression != null)
                        {
                            Debug.WriteLine("[调试] 找到绑定表达式，正在更新源");
                            bindingExpression.UpdateSource();
                        }
                        else
                        {
                            Debug.WriteLine("[调试] 未找到绑定表达式");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[调试] 未找到文本框控件");
                    }
                    
                    // 通过ViewModel设置属性
                    Debug.WriteLine($"[调试] ViewModel.RailwayBureau之前的值: {_viewModel.RailwayBureau}");
                    Debug.WriteLine($"[调试] ViewModel.RailwayBureauInput之前的值: {_viewModel.RailwayBureauInput}");
                    
                    _viewModel.RailwayBureau = selectedValue;
                    _viewModel.RailwayBureauInput = selectedValue;
                    
                    Debug.WriteLine($"[调试] ViewModel.RailwayBureau设置后的值: {_viewModel.RailwayBureau}");
                    Debug.WriteLine($"[调试] ViewModel.RailwayBureauInput设置后的值: {_viewModel.RailwayBureauInput}");
                    
                    // 关闭下拉框
                    _viewModel.IsRailwayBureauDropdownOpen = false;
                    Debug.WriteLine("[调试] 已关闭下拉框");
                    
                    // 手动触发属性变更通知
                    var propChangedMethod = _viewModel.GetType().GetMethod("OnPropertyChanged", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (propChangedMethod != null)
                    {
                        Debug.WriteLine("[调试] 手动触发属性变更通知");
                        propChangedMethod.Invoke(_viewModel, new object[] { "RailwayBureauInput" });
                    }
                    
                    // 聚焦其他控件，清除当前焦点
                    this.Focus();
                    Debug.WriteLine("[调试] 窗口获得焦点");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[调试] 发生异常: {ex.Message}");
                    Debug.WriteLine($"[调试] 异常详情: {ex}");
                    MessageBox.Show($"选择铁路局时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                e.Handled = true;
            }
        }
    }
} 