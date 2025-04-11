using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TA_WPF.Utils;
using System.Windows.Threading;

namespace TA_WPF.Services
{
    /// <summary>
    /// OCR环境服务，负责检测和管理OCR相关环境
    /// </summary>
    public class OcrEnvironmentService : INotifyPropertyChanged
    {
        private readonly PythonService _pythonService;
        
        private bool _isPythonInstalled;
        private bool _isCnocrInstalled;
        private bool _isOcrModelInstalled;
        private bool _isEnvironmentReady;
        private string _statusMessage;
        private string _loadingMessage;
        private bool _isLoading;
        private bool _isWindowClosed;

        /// <summary>
        /// 环境检测完成事件
        /// </summary>
        public event EventHandler<bool> EnvironmentCheckCompleted;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pythonService">Python服务</param>
        public OcrEnvironmentService(PythonService pythonService)
        {
            _pythonService = pythonService;
        }

        /// <summary>
        /// Python是否已安装
        /// </summary>
        public bool IsPythonInstalled
        {
            get => _isPythonInstalled;
            private set
            {
                if (_isPythonInstalled != value)
                {
                    _isPythonInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// CNOCR是否已安装
        /// </summary>
        public bool IsCnocrInstalled
        {
            get => _isCnocrInstalled;
            private set
            {
                if (_isCnocrInstalled != value)
                {
                    _isCnocrInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// OCR模型是否已安装
        /// </summary>
        public bool IsOcrModelInstalled
        {
            get => _isOcrModelInstalled;
            private set
            {
                if (_isOcrModelInstalled != value)
                {
                    _isOcrModelInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 环境是否准备就绪
        /// </summary>
        public bool IsEnvironmentReady
        {
            get => _isEnvironmentReady;
            private set
            {
                if (_isEnvironmentReady != value)
                {
                    _isEnvironmentReady = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 加载消息
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            private set
            {
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 检查OCR环境
        /// </summary>
        /// <returns>检查完成的任务</returns>
        public async Task CheckEnvironment()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "正在检查环境...";
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

                // 检查Python是否安装
                LoadingMessage = "正在检查Python...";
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                IsPythonInstalled = await _pythonService.CheckPythonInstalled();

                // 检查cnocr包是否安装
                LoadingMessage = "正在检查CNOCR...";
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                IsCnocrInstalled = await _pythonService.CheckCnocrInstalled();

                // 检查OCR模型是否安装
                LoadingMessage = "正在检查OCR模型...";
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                IsOcrModelInstalled = await _pythonService.CheckOcrModelInstalled();

                // 更新环境就绪状态
                IsEnvironmentReady = IsPythonInstalled && IsCnocrInstalled;

                // 更新状态消息
                if (IsEnvironmentReady)
                {
                    StatusMessage = "OCR环境已准备就绪。";
                    
                    // 如果OCR模型未安装，添加提示
                    if (!IsOcrModelInstalled)
                    {
                        StatusMessage += "\n注意: OCR模型未检测到，首次识别可能会自动下载模型";
                    }
                    
                    // 仅在窗口未关闭时显示消息框
                    if (!_isWindowClosed)
                    {
                        // 在UI线程显示环境检测结果 - 简化消息
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBoxHelper.ShowInfo("环境检测完成，可以开始导入图片了。");
                        });
                    }
                }
                else
                {
                    string errorMessage = "OCR环境未准备好:";
                    if (!IsPythonInstalled) errorMessage += "\n- Python未安装";
                    if (!IsCnocrInstalled) errorMessage += "\n- CNOCR未安装";
                    if (!IsOcrModelInstalled) errorMessage += "\n- OCR模型未下载";
                    
                    StatusMessage = errorMessage;
                    
                    // 仅在窗口未关闭时显示消息框
                    if (!_isWindowClosed)
                    {
                        // 在UI线程显示环境检测结果 - 简化错误消息
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBoxHelper.ShowWarning("环境检测未通过，请先安装所需环境再使用OCR识别功能。");
                        });
                    }
                }
                
                // 触发环境检测完成事件
                EnvironmentCheckCompleted?.Invoke(this, IsEnvironmentReady);
            }
            catch (Exception ex)
            {
                StatusMessage = $"检查环境时出错：{ex.Message}";
                IsEnvironmentReady = false;
                
                // 在UI线程显示错误
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBoxHelper.ShowError($"检查环境时出错: {ex.Message}");
                });
                
                // 触发环境检测完成事件
                EnvironmentCheckCompleted?.Invoke(this, false);
            }
            finally
            {
                IsLoading = false;
                LoadingMessage = string.Empty;
                
                // 刷新命令状态
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        /// <summary>
        /// 打开CNOCR安装指南
        /// </summary>
        public void OpenCnocrInstallGuide()
        {
            try
            {
                // 打开CNOCR安装指南网页
                string cnocrUrl = "https://github.com/breezedeus/cnocr";
                Process.Start(new ProcessStartInfo
                {
                    FileName = cnocrUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"无法打开浏览器：{ex.Message}";
                MessageBoxHelper.ShowError($"无法打开浏览器：{ex.Message}");
            }
        }

        /// <summary>
        /// 设置状态消息
        /// </summary>
        /// <param name="message">状态消息</param>
        public void SetStatusMessage(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// 设置加载消息
        /// </summary>
        /// <param name="message">加载消息</param>
        public void SetLoadingMessage(string message)
        {
            LoadingMessage = message;
        }

        /// <summary>
        /// 设置窗口已关闭状态
        /// </summary>
        public void SetWindowClosed()
        {
            _isWindowClosed = true;
        }

        /// <summary>
        /// 重置窗口关闭状态
        /// </summary>
        public void ResetWindowClosed()
        {
            _isWindowClosed = false;
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 