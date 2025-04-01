using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TA_WPF.Models;
using TA_WPF.Services;
using TA_WPF.Utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Globalization;
using System.Windows.Data;

namespace TA_WPF.ViewModels
{
    /// <summary>
    /// OCR识别车票视图模型
    /// </summary>
    public class OcrTicketViewModel : BaseViewModel
    {
        private readonly PythonService _pythonService;
        private readonly MainViewModel _mainViewModel;
        private string _selectedImagePath;
        private BitmapImage _selectedImage;
        private bool _isPythonInstalled;
        private bool _isCnocrInstalled;
        private bool _isOcrModelInstalled;
        private bool _isLoading;
        private string _jsonResult;
        private string _statusMessage;
        private string _loadingMessage;
        private ObservableCollection<OcrResult> _ocrResults;
        private double _averageConfidence;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mainViewModel">主视图模型</param>
        public OcrTicketViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _pythonService = new PythonService();
            _ocrResults = new ObservableCollection<OcrResult>();
            _loadingMessage = "正在检查环境，请稍候...";
            _averageConfidence = 0;
            
            // 使用项目中现有的RelayCommand实现
            SelectImageCommand = new RelayCommand(async () => await SelectImage(), CanImportTicket);
            RunOcrCommand = new RelayCommand(async () => await RunOcr(), CanRunOcr);
            CheckEnvironmentCommand = new RelayCommand(async () => await CheckEnvironment());
            OpenCnocrInstallGuideCommand = new RelayCommand(OpenCnocrInstallGuide);
            
            // 启动时自动检查环境（使用ConfigureAwait(false)避免死锁）
            _ = CheckEnvironment();
        }

        /// <summary>
        /// 选择图片命令
        /// </summary>
        public ICommand SelectImageCommand { get; }
        
        /// <summary>
        /// 运行OCR命令
        /// </summary>
        public ICommand RunOcrCommand { get; }
        
        /// <summary>
        /// 检查环境命令
        /// </summary>
        public ICommand CheckEnvironmentCommand { get; }
        
        /// <summary>
        /// 打开cnocr安装指南命令
        /// </summary>
        public ICommand OpenCnocrInstallGuideCommand { get; }

        /// <summary>
        /// 选中的图片路径
        /// </summary>
        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set 
            { 
                if (_selectedImagePath != value)
                {
                    _selectedImagePath = value;
                    OnPropertyChanged(nameof(SelectedImagePath));
                }
            }
        }

        /// <summary>
        /// 选中的图片
        /// </summary>
        public BitmapImage SelectedImage
        {
            get => _selectedImage;
            set 
            { 
                if (_selectedImage != value)
                {
                    _selectedImage = value;
                    OnPropertyChanged(nameof(SelectedImage));
                }
            }
        }

        /// <summary>
        /// Python是否已安装
        /// </summary>
        public bool IsPythonInstalled
        {
            get => _isPythonInstalled;
            set 
            { 
                if (_isPythonInstalled != value)
                {
                    _isPythonInstalled = value;
                    OnPropertyChanged(nameof(IsPythonInstalled));
                }
            }
        }

        /// <summary>
        /// cnocr是否已安装
        /// </summary>
        public bool IsCnocrInstalled
        {
            get => _isCnocrInstalled;
            set 
            { 
                if (_isCnocrInstalled != value)
                {
                    _isCnocrInstalled = value;
                    OnPropertyChanged(nameof(IsCnocrInstalled));
                }
            }
        }

        /// <summary>
        /// OCR模型是否已安装
        /// </summary>
        public bool IsOcrModelInstalled
        {
            get => _isOcrModelInstalled;
            set 
            { 
                if (_isOcrModelInstalled != value)
                {
                    _isOcrModelInstalled = value;
                    OnPropertyChanged(nameof(IsOcrModelInstalled));
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set 
            { 
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        /// <summary>
        /// JSON结果
        /// </summary>
        public string JsonResult
        {
            get => _jsonResult;
            set 
            { 
                if (_jsonResult != value)
                {
                    _jsonResult = value;
                    OnPropertyChanged(nameof(JsonResult));
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set 
            { 
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        /// <summary>
        /// 加载消息
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set 
            { 
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    OnPropertyChanged(nameof(LoadingMessage));
                }
            }
        }

        /// <summary>
        /// OCR结果集合
        /// </summary>
        public ObservableCollection<OcrResult> OcrResults
        {
            get => _ocrResults;
            set 
            { 
                if (_ocrResults != value)
                {
                    _ocrResults = value;
                    OnPropertyChanged(nameof(OcrResults));
                }
            }
        }

        /// <summary>
        /// 平均置信度
        /// </summary>
        public double AverageConfidence
        {
            get => _averageConfidence;
            set 
            { 
                if (_averageConfidence != value)
                {
                    _averageConfidence = value;
                    OnPropertyChanged(nameof(AverageConfidence));
                }
            }
        }

        /// <summary>
        /// MainViewModel引用
        /// </summary>
        public MainViewModel MainViewModel => _mainViewModel;

        /// <summary>
        /// 选择图片
        /// </summary>
        private async Task SelectImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                Title = "选择车票图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 清空之前的结果
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        SelectedImage = null;
                        OcrResults.Clear();
                        JsonResult = string.Empty;
                        AverageConfidence = 0;
                    });
                    
                    string filePath = openFileDialog.FileName;
                    LogHelper.LogInfo($"准备加载图片: {filePath}");
                    
                    if (!File.Exists(filePath))
                    {
                        LogHelper.LogError($"文件不存在: {filePath}", null);
                        MessageBoxHelper.ShowError($"文件不存在: {filePath}");
                        return;
                    }
                    
                    // 仅设置路径，让窗口代码处理图片加载
                    SelectedImagePath = filePath;
                    StatusMessage = $"已选择图片: {Path.GetFileName(filePath)}";
                    
                    // 图片加载由UI层负责完成
                    LogHelper.LogInfo($"已设置图片路径，等待UI层加载: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError($"选择图片过程中出错", ex);
                    MessageBoxHelper.ShowError($"选择图片过程中出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行OCR识别
        /// </summary>
        private async Task RunOcr()
        {
            if (string.IsNullOrEmpty(SelectedImagePath))
            {
                StatusMessage = "请先选择一张车票图片";
                MessageBoxHelper.ShowWarning("请先选择一张车票图片");
                return;
            }

            try
            {
                StatusMessage = "正在执行OCR识别...";
                LoadingMessage = "正在进行OCR图像识别，请稍候...";
                IsLoading = true;
                JsonResult = string.Empty;
                OcrResults.Clear();
                AverageConfidence = 0;
                
                string result = await _pythonService.RunOcrWithExternalProcess(SelectedImagePath);
                
                // 检查是否有错误
                try
                {
                    var error = JsonConvert.DeserializeObject<OcrError>(result);
                    if (!string.IsNullOrEmpty(error?.Error))
                    {
                        StatusMessage = $"OCR处理错误: {error.Error}";
                        MessageBoxHelper.ShowError($"OCR处理错误: {error.Error}");
                        return;
                    }
                }
                catch { /* 不是错误对象，继续处理 */ }
                
                // 将JSON结果格式化后再存储
                JsonResult = GetFormattedJson(result);
                
                // 使用 JsonHelper 解析结果
                try
                {
                    var ocrResults = JsonHelper.TryParseOcrResults(result);
                    
                    if (ocrResults != null && ocrResults.Count > 0)
                    {
                        foreach (var ocrResult in ocrResults)
                        {
                            OcrResults.Add(ocrResult);
                        }
                        
                        // 计算平均置信度
                        if (ocrResults.Count > 0)
                        {
                            double totalScore = 0;
                            foreach (var ocrItem in ocrResults)
                            {
                                totalScore += ocrItem.Score;
                            }
                            AverageConfidence = totalScore / ocrResults.Count;
                        }
                        else
                        {
                            AverageConfidence = 0;
                        }
                        
                        StatusMessage = $"OCR识别完成，识别到 {OcrResults.Count} 个文本块";
                        MessageBoxHelper.ShowInfo($"OCR识别完成，识别到 {OcrResults.Count} 个文本块");
                    }
                    else
                    {
                        StatusMessage = "OCR识别完成，但未解析到有效结果";
                        MessageBoxHelper.ShowWarning("OCR识别完成，但未解析到有效结果");
                    }
                }
                catch (Exception jsonEx)
                {
                    StatusMessage = $"解析OCR结果时出错: {jsonEx.Message}";
                    MessageBoxHelper.ShowError($"解析OCR结果时出错: {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"执行OCR识别时出错", ex);
                MessageBoxHelper.ShowError($"执行OCR时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// 判断是否可以运行OCR
        /// </summary>
        private bool CanRunOcr()
        {
            return !IsLoading && IsPythonInstalled && IsCnocrInstalled && !string.IsNullOrEmpty(SelectedImagePath);
        }

        /// <summary>
        /// 检查环境
        /// </summary>
        private async Task CheckEnvironment()
        {
            // 在UI线程设置状态
            await Application.Current.Dispatcher.InvokeAsync(() => {
                StatusMessage = "正在检查Python环境...";
                LoadingMessage = "正在检查Python和OCR环境，请稍候...";
                IsLoading = true;
            });

            try
            {
                // 检查Python是否安装（在后台线程）
                bool pythonInstalled = await _pythonService.CheckPythonInstalled();
                
                // 在UI线程更新状态
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    IsPythonInstalled = pythonInstalled;
                });
                
                if (!pythonInstalled)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        StatusMessage = "未检测到Python安装，请先安装Python";
                        MessageBoxHelper.ShowWarning("未检测到Python安装，请先安装Python");
                    });
                    return;
                }
                
                // 检查cnocr包是否安装（在后台线程）
                bool cnocrInstalled = await _pythonService.CheckPackageInstalled("cnocr");
                
                // 在UI线程更新状态
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    IsCnocrInstalled = cnocrInstalled;
                });
                
                if (!cnocrInstalled)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        StatusMessage = "检测到Python已安装，但未安装cnocr包，请参考安装指南";
                        MessageBoxHelper.ShowWarning("检测到Python已安装，但未安装cnocr包，请参考安装指南");
                    });
                    return;
                }
                
                // 检查OCR模型是否安装（在后台线程）
                bool ocrModelInstalled = await _pythonService.CheckOcrModelInstalled();
                
                // 在UI线程更新最终状态
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    IsOcrModelInstalled = ocrModelInstalled;
                    
                    if (!ocrModelInstalled)
                    {
                        StatusMessage = "检测到cnocr已安装，但OCR模型可能未下载，首次运行OCR时将自动下载模型";
                        MessageBoxHelper.ShowInfo("检测到cnocr已安装，但OCR模型可能未下载，首次运行OCR时将自动下载模型");
                    }
                    else
                    {
                        StatusMessage = "环境检查完成，Python和cnocr包已正确安装";
                        MessageBoxHelper.ShowInfo("环境检查完成，Python和cnocr包已正确安装");
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    StatusMessage = $"检查环境时出错: {ex.Message}";
                    MessageBoxHelper.ShowError($"检查环境时出错: {ex.Message}");
                });
            }
            finally
            {
                // 确保在UI线程完成最终状态更新和命令状态刷新
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    IsLoading = false;
                    // 显式刷新命令状态，确保按钮状态立即更新
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }
        
        /// <summary>
        /// 打开cnocr安装指南
        /// </summary>
        private void OpenCnocrInstallGuide()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://cnocr.readthedocs.io/zh-cn/stable/install/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"打开安装指南时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取格式化的JSON字符串
        /// </summary>
        private string GetFormattedJson(string inputJson)
        {
            if (string.IsNullOrWhiteSpace(inputJson))
                return string.Empty;
                
            try
            {
                // 尝试解析JSON并重新格式化
                var parsedJson = JsonConvert.DeserializeObject(inputJson);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch
            {
                // 如果解析失败，返回原始JSON
                return inputJson;
            }
        }
        
        /// <summary>
        /// 是否可以导入车票图片
        /// </summary>
        public bool CanImportTicket()
        {
            // 只要不是正在加载状态，就允许导入图片
            // 即使环境未完全准备好，也允许用户点击按钮，会在后续流程中提示安装相关环境
            return !IsLoading;
        }
    }
} 