using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TA_WPF.Utils;
using TA_WPF.ViewModels;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Controls;

namespace TA_WPF.Views
{
    /// <summary>
    /// OcrTicketWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OcrTicketWindow : Window
    {
        private readonly OcrTicketViewModel _viewModel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        public OcrTicketWindow(MainViewModel mainViewModel)
        {
            try 
            {
                // 在设计时和运行时都会调用的InitializeComponent
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // 捕获由于设计时代码和运行时代码不一致导致的异常
                Debug.WriteLine($"InitializeComponent错误: {ex.Message}");
                // 不抛出异常，继续执行
            }

            // 创建视图模型
            _viewModel = new OcrTicketViewModel(mainViewModel);

            // 设置数据上下文
            DataContext = _viewModel;

            // 添加图片变更监听
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // 订阅关闭请求事件
            _viewModel.RequestCloseAction += ViewModel_RequestCloseAction;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedImage")
            {
                // 当图片变更时检测图片加载状态
                CheckImageLoaded();
            }
            else if (e.PropertyName == "SelectedImagePath")
            {
                // 当图片路径变更时，尝试直接加载图片
                LoadImageFromPath();
            }
        }

        private void LoadImageFromPath()
        {
            try
            {
                if (string.IsNullOrEmpty(_viewModel.SelectedImagePath))
                {
                    LogHelper.LogWarning("图片路径为空，无法加载图片");
                    return;
                }

                if (!File.Exists(_viewModel.SelectedImagePath))
                {
                    LogHelper.LogError($"图片文件不存在: {_viewModel.SelectedImagePath}", null);
                    MessageBoxHelper.ShowError($"图片文件不存在: {_viewModel.SelectedImagePath}");
                    return;
                }

                LogHelper.LogInfo($"尝试加载图片: {_viewModel.SelectedImagePath}");

                // 重要：创建一个新的BitmapImage并显式设置属性
                var bitmap = new BitmapImage();

                try
                {
                    // 使用FileStream方式加载图片
                    using (var stream = new FileStream(_viewModel.SelectedImagePath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // 关键：加载完后关闭流
                        bitmap.DecodePixelWidth = 600; // 限制宽度
                        bitmap.EndInit();
                        bitmap.Freeze(); // 确保可以跨线程访问
                    }

                    // 设置图像控件和更新UI
                    try
                    {
                        // 将错误处理包装在try-catch块中，防止设计时代码和运行时代码之间的差异导致错误
                        if (this.FindName("DisplayImage") is Image displayImage)
                            displayImage.Source = bitmap;
                    
                        if (this.FindName("ImageContainer") is Panel imageContainer)
                            imageContainer.Visibility = Visibility.Visible;
                    
                        if (this.FindName("NoImageBorder") is UIElement noImageBorder)
                            noImageBorder.Visibility = Visibility.Collapsed;
                        
                        // 更新ViewModel中的图像
                        _viewModel.SelectedImage = bitmap;
                    }
                    catch (Exception ex)
                    {
                        // 即使UI控件访问失败，也要保证图像被加载
                        LogHelper.LogWarning($"访问UI控件时出错，但图像已加载: {ex.Message}");
                        _viewModel.SelectedImage = bitmap;
                    }

                    LogHelper.LogInfo($"成功加载图片: {Path.GetFileName(_viewModel.SelectedImagePath)}");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"加载图片失败，详细错误: {ex.GetType().Name} - {ex.Message}", ex);

                    // 尝试备用方式加载
                    TryAlternativeImageLoading();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"处理图片路径过程中出错: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"加载图片失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试备用方式加载图片（使用本地绝对路径方式）
        /// </summary>
        private void TryAlternativeImageLoading()
        {
            try
            {
                LogHelper.LogInfo("尝试使用备用方式加载图片...");

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_viewModel.SelectedImagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();

                // 设置图像，使用try-catch块保护UI更新
                try 
                {
                    if (this.FindName("DisplayImage") is Image displayImage)
                        displayImage.Source = bitmap;
                        
                    // 更新ViewModel中的图像
                    _viewModel.SelectedImage = bitmap;
                }
                catch (Exception ex)
                {
                    LogHelper.LogWarning($"设置图像时出错，但图像已加载: {ex.Message}");
                    _viewModel.SelectedImage = bitmap;
                }

                LogHelper.LogInfo("备用方式成功加载图片");
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"备用方式加载图片也失败: {ex.Message}", ex);
                MessageBoxHelper.ShowError($"无法加载图片，请尝试其他图片: {ex.Message}");
            }
        }

        private void CheckImageLoaded()
        {
            try
            {
                if (_viewModel.SelectedImage != null)
                {
                    LogHelper.LogInfo("窗口检测到图片已被加载");

                    // 尝试确保图片在UI上显示
                    var image = _viewModel.SelectedImage;
                    if (image.Width > 0 && image.Height > 0)
                    {
                        LogHelper.LogInfo($"图片尺寸正常: {image.Width}x{image.Height}");
                    }
                    else
                    {
                        LogHelper.LogWarning("图片尺寸异常，可能无法正常显示");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("检测图片加载状态时出错", ex);
            }
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.SetWindowClosed();
            // 取消订阅关闭请求事件
            _viewModel.RequestCloseAction -= ViewModel_RequestCloseAction;
        }

        /// <summary>
        /// 窗口关闭前检查是否正在下载OCR模型或Python
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 检查是否正在下载OCR模型
            if (_viewModel.IsDownloadingModel)
            {
                var result = MessageBoxHelper.ShowConfirmation(
                    "您确定要退出本窗口吗？这会中断OCR模型下载进程", 
                    "确认退出", 
                    MessageBoxButton.YesNo);
                
                if (result != MessageBoxResult.Yes)
                {
                    // 取消关闭
                    e.Cancel = true;
                }
                else
                {
                    // 如果用户选择关闭，此处可添加取消模型下载的代码
                }
                return;
            }
            
            // 获取OCR环境服务实例
            var ocrEnvironmentService = _viewModel.GetType().GetField("_ocrEnvironmentService", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_viewModel);
            
            if (ocrEnvironmentService != null)
            {
                // 检查是否正在下载Python
                var isDownloadingPythonProperty = ocrEnvironmentService.GetType().GetProperty("IsDownloadingPython");
                bool isDownloadingPython = false;
                
                if (isDownloadingPythonProperty != null && isDownloadingPythonProperty.GetValue(ocrEnvironmentService) is bool pythonDownloading)
                {
                    isDownloadingPython = pythonDownloading;
                }
                
                // 检查是否正在下载CNOCR
                var isDownloadingCnocrProperty = ocrEnvironmentService.GetType().GetProperty("IsDownloadingCnocr");
                bool isDownloadingCnocr = false;
                
                if (isDownloadingCnocrProperty != null && isDownloadingCnocrProperty.GetValue(ocrEnvironmentService) is bool cnocrDownloading)
                {
                    isDownloadingCnocr = cnocrDownloading;
                }
                
                // 如果正在下载Python或CNOCR，询问用户是否确认退出
                if (isDownloadingPython || isDownloadingCnocr)
                {
                    string message = isDownloadingPython 
                        ? "Python尚未下载完毕，现在退出会导致下载中断，是否继续？" 
                        : "CNOCR正在安装中，现在退出会导致安装中断，是否继续？";
                    
                    var result = MessageBoxHelper.ShowConfirmation(
                        message, 
                        "确认退出", 
                        MessageBoxButton.YesNo);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        // 取消关闭
                        e.Cancel = true;
                    }
                    else
                    {
                        // 显示取消下载提示
                        LogHelper.LogInfo($"用户选择中断{(isDownloadingPython ? "Python" : "CNOCR")}下载并关闭窗口");
                        
                        try
                        {
                            // 取消下载
                            string cancelMethodName = isDownloadingPython ? "CancelPythonDownload" : "CancelCnocrDownload";
                            var cancelMethod = ocrEnvironmentService.GetType().GetMethod(cancelMethodName);
                            if (cancelMethod != null)
                            {
                                cancelMethod.Invoke(ocrEnvironmentService, null);
                                
                                // 延迟关闭窗口，给资源清理一些时间
                                Task.Run(async () =>
                                {
                                    // 等待资源清理（至少3秒）
                                    await Task.Delay(3000);
                                    
                                    // 确保完全清理，重新检查环境状态
                                    var checkEnvironmentMethod = ocrEnvironmentService.GetType().GetMethod("CheckEnvironment");
                                    if (checkEnvironmentMethod != null)
                                    {
                                        try
                                        {
                                            // 异步执行环境检查
                                            await (Task)checkEnvironmentMethod.Invoke(ocrEnvironmentService, null);
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"关闭窗口时重置环境状态出错: {ex.Message}");
                                        }
                                    }
                                });
                                
                                // 强制进行垃圾回收，释放资源
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"取消下载时发生错误: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理ViewModel请求关闭窗口的事件
        /// </summary>
        private void ViewModel_RequestCloseAction()
        {
            this.Close();
        }

        /// <summary>
        /// 出发站失去焦点时处理
        /// </summary>
        private void DepartStation_LostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.OnStationLostFocus(true);
        }

        /// <summary>
        /// 到达站失去焦点时处理
        /// </summary>
        private void ArriveStation_LostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.OnStationLostFocus(false);
        }

        /// <summary>
        /// 金额输入验证
        /// </summary>
        private void MoneyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 允许输入数字、小数点和退格键
            Regex regex = new Regex(@"^[0-9]+(\.[0-9]{0,2})?$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 金额失去焦点时处理格式化
        /// </summary>
        private void MoneyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.Money > 0)
            {
                // 格式化为两位小数
                _viewModel.Money = Math.Round(_viewModel.Money, 2);
            }
        }

        /// <summary>
        /// 车次号输入验证
        /// </summary>
        private void TrainNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 座位号输入验证
        /// </summary>
        private void SeatNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 无论选择什么座位类型，都只允许输入数字
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 车厢号输入验证
        /// </summary>
        private void CoachNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 处理拖放图片预览事件，允许拖放
        /// </summary>
        private void NoImageBorder_PreviewDragOver(object sender, DragEventArgs e)
        {
            // 检查是否有文件被拖放
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 不阻止默认行为，允许拖放
                e.Handled = true;
                // 显示可以放下的效果
                e.Effects = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// 处理拖放图片完成事件
        /// </summary>
        private void NoImageBorder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                // 检查是否有文件被拖放
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // 获取拖放的文件列表
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    
                    // 只取第一个文件
                    if (files.Length > 0)
                    {
                        string filePath = files[0];
                        string fileExtension = Path.GetExtension(filePath).ToLower();
                        
                        // 验证是否为图片文件
                        if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".bmp")
                        {
                            LogHelper.LogInfo($"拖放导入图片: {filePath}");
                            
                            // 尝试重置表单状态 - 通过反射调用私有方法
                            var resetFormMethod = _viewModel.GetType().GetMethod("ResetFormState", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (resetFormMethod != null)
                            {
                                resetFormMethod.Invoke(_viewModel, null);
                                LogHelper.LogInfo("已重置表单状态");
                            }
                            
                            // 清除之前的OCR结果
                            if (_viewModel.OcrResults != null)
                            {
                                _viewModel.OcrResults.Clear();
                                _viewModel.AverageConfidence = 0;
                                _viewModel.JsonResult = string.Empty;
                            }
                            
                            // 设置新选择的图片路径
                            _viewModel.SelectedImagePath = filePath;
                            
                            // 显示图片加载成功消息
                            LogHelper.LogInfo($"成功通过拖放导入图片: {Path.GetFileName(filePath)}");
                            
                            // 手动触发命令状态更新，确保按钮可用性立即更新
                            CommandManager.InvalidateRequerySuggested();
                        }
                        else
                        {
                            MessageBoxHelper.ShowWarning("请选择有效的图片文件(jpg, jpeg, png, bmp)");
                        }
                    }
                }
                
                // 标记事件已处理，防止事件冒泡导致重复处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError("拖放图片时出错", ex);
                MessageBoxHelper.ShowError($"拖放图片时出错: {ex.Message}");
            }
        }
    }
}