using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TA_WPF.Utils;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Input;
using System.Linq;
using System.Timers;
using System.Text;

namespace TA_WPF.Services
{
    /// <summary>
    /// OCR环境服务，负责检测和管理OCR相关环境
    /// </summary>
    public class OcrEnvironmentService : INotifyPropertyChanged
    {
        private readonly PythonService _pythonService;
        
        private bool? _isPythonInstalled;
        private bool? _isCnocrInstalled;
        private bool? _isOcrModelInstalled;
        private bool _isEnvironmentReady;
        private string _statusMessage;
        private string _loadingMessage;
        private bool _isLoading;
        private bool _isWindowClosed;
        private bool _isDownloadingPython;
        private bool _isDownloadingCnocr;
        private string _pythonInstallerPath;
        private Process _pythonDownloadProcess;
        private Process _cnocrInstallProcess;
        private CancellationTokenSource _downloadCancellationTokenSource;

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
        public bool? IsPythonInstalled
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
        public bool? IsCnocrInstalled
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
        public bool? IsOcrModelInstalled
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
        /// 是否正在下载Python
        /// </summary>
        public bool IsDownloadingPython
        {
            get => _isDownloadingPython;
            private set
            {
                if (_isDownloadingPython != value)
                {
                    _isDownloadingPython = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// 是否正在下载CNOCR
        /// </summary>
        public bool IsDownloadingCnocr
        {
            get => _isDownloadingCnocr;
            private set
            {
                if (_isDownloadingCnocr != value)
                {
                    _isDownloadingCnocr = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Python安装程序路径
        /// </summary>
        public string PythonInstallerPath
        {
            get => _pythonInstallerPath;
            private set
            {
                if (_pythonInstallerPath != value)
                {
                    _pythonInstallerPath = value;
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

                // 重置所有状态为null（检测中）
                IsPythonInstalled = null;
                IsCnocrInstalled = null;
                IsOcrModelInstalled = null;
                
                // 确保UI立即更新状态
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                // 检查Python是否安装
                LoadingMessage = "正在检查Python...";
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                // 进行多次尝试检测Python
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    IsPythonInstalled = await _pythonService.CheckPythonInstalled();
                    if (IsPythonInstalled == true)
                        break;
                        
                    // 如果第一次检测失败，等待短暂时间后再试一次
                    // 这是因为有时环境变量或系统路径可能需要一些时间刷新
                    if (attempt == 0 && IsPythonInstalled != true)
                    {
                        await Task.Delay(500);
                        // 强制进行垃圾回收，释放资源
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                // 如果Python已安装，才检查cnocr包
                if (IsPythonInstalled == true)
                {
                    // 检查cnocr包是否安装
                    LoadingMessage = "正在检查CNOCR...";
                    // 确保UI更新
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    IsCnocrInstalled = await _pythonService.CheckCnocrInstalled();

                    // 检查cnstd包是否安装（文本检测用）
                    LoadingMessage = "正在检查文本检测模块...";
                    // 确保UI更新
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    bool isCnstdInstalled = await _pythonService.CheckCnstdInstalled();
                    
                    // 在调试输出中记录检测结果
                    Debug.WriteLine($"CNSTD模块检测结果: {(isCnstdInstalled ? "已安装" : "未安装")}");

                    // 检查OCR模型是否安装
                    LoadingMessage = "正在检查OCR模型...";
                    // 确保UI更新
                    await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                    IsOcrModelInstalled = await _pythonService.CheckOcrModelInstalled();
                }

                // 更新环境就绪状态
                IsEnvironmentReady = IsPythonInstalled == true && IsCnocrInstalled == true;

                // 仅在窗口未关闭时显示消息框
                if (!_isWindowClosed)
                {
                    // 在UI线程显示环境检测结果
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (IsEnvironmentReady)
                        {
                            if (!IsOcrModelInstalled.HasValue || !IsOcrModelInstalled.Value)
                            {
                                // 环境就绪但模型未安装，提示用户首次识别会自动下载模型
                                MessageBoxHelper.ShowInfo("环境检测完成，可以开始导入图片了。\n\n注意：OCR模型未检测到，首次识别时会自动下载模型文件，请耐心等待。");
                            }
                            else
                            {
                                // 环境完全就绪
                                MessageBoxHelper.ShowInfo("环境检测完成，可以开始导入图片了。");
                            }
                        }
                        else
                        {
                            string errorMessage = "OCR环境未准备好:";
                            if (!IsPythonInstalled.HasValue || !IsPythonInstalled.Value) errorMessage += "\n- Python未安装";
                            if (!IsCnocrInstalled.HasValue || !IsCnocrInstalled.Value) errorMessage += "\n- CNOCR未安装";
                            
                            MessageBoxHelper.ShowWarning($"{errorMessage}\n\n请先安装所需环境再使用OCR识别功能。");
                            LogHelper.LogWarning($"{errorMessage}\n\n请先安装所需环境再使用OCR识别功能。");
                        }
                    });
                }
                
                // 确保UI更新完成
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                // 触发环境检测完成事件
                EnvironmentCheckCompleted?.Invoke(this, IsEnvironmentReady);
            }
            catch (Exception ex)
            {
                IsEnvironmentReady = false;
                
                // 在UI线程显示错误
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBoxHelper.ShowError($"检查环境时出错: {ex.Message}");
                    LogHelper.LogError(StatusMessage, ex);
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
                    CommandManager.InvalidateRequerySuggested();
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
                LogHelper.LogError($"无法打开浏览器：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 下载安装CNOCR包
        /// </summary>
        /// <param name="mirror">镜像源地址，默认为阿里云镜像</param>
        public async Task DownloadInstallCnocr(string mirror = "https://mirrors.aliyun.com/pypi/simple")
        {
            try
            {
                // 如果Python未安装，不能安装CNOCR
                if (IsPythonInstalled != true)
                {
                    MessageBoxHelper.ShowWarning("请先安装Python再安装CNOCR");
                    LogHelper.LogWarning("CNOCR安装失败：Python未安装");
                    return;
                }
                
                // 确保之前的下载已完全取消和清理
                if (IsDownloadingCnocr)
                {
                    LoadingMessage = "正在清理之前的下载资源...";
                    StatusMessage = "正在清理之前的下载资源...";
                    CancelCnocrDownload();
                    
                    // 等待资源完全清理
                    await Task.Delay(1000);
                    
                    // 强制GC回收资源
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                
                // 设置状态
                IsLoading = true;
                IsDownloadingCnocr = true;
                LoadingMessage = "正在下载安装CNOCR...";
                StatusMessage = "正在下载安装CNOCR...";
                
                // 更新UI进度条到0
                UpdateProgress(0);
                
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                // 清理之前可能存在的CNOCR包
                await CleanPreviousCnocrInstallation();
                
                // 启动进度更新计时器
                using (var progressTimer = new System.Timers.Timer(500)) // 每500毫秒更新一次
                {
                    int currentProgress = 0;
                    DateTime startTime = DateTime.Now;
                    
                    progressTimer.Elapsed += (s, e) => 
                    {
                        // 计算估计的下载安装进度
                        // CNOCR下载安装通常需要30-60秒，我们基于时间估算进度
                        TimeSpan elapsed = DateTime.Now - startTime;
                        
                        // 估计总安装时间为60秒，将进度限制在0-95%范围内
                        // 保留最后5%用于最终确认安装成功
                        int estimatedProgress = Math.Min(95, (int)(elapsed.TotalSeconds / 60.0 * 100));
                        
                        // 如果进度变化了，才更新UI
                        if (estimatedProgress > currentProgress)
                        {
                            currentProgress = estimatedProgress;
                            UpdateProgress(currentProgress);
                        }
                    };
                    
                    // 启动计时器
                    progressTimer.Start();
                    
                    try
                    {
                        // 获取Python路径
                        string pythonPath = await GetPythonPath();
                        
                        // 构建pip安装命令，使用python -m pip确保能找到pip模块
                        string pipCommand = $"\"{pythonPath}\" -m pip install cnocr[ort-cpu] -i {mirror}";
                        
                        // 创建进程启动信息
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {pipCommand}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        
                        // 创建并启动进程
                        using (var process = new Process { StartInfo = startInfo })
                        {
                            StringBuilder output = new StringBuilder();
                            StringBuilder error = new StringBuilder();
                            
                            // 捕获输出
                            process.OutputDataReceived += (sender, e) => 
                            {
                                if (!string.IsNullOrEmpty(e.Data))
                                {
                                    Debug.WriteLine($"CNOCR安装输出: {e.Data}");
                                    LogHelper.LogInfo($"CNOCR安装输出：{ e.Data}");
                                    output.AppendLine(e.Data);
                                    
                                    // 更新加载消息显示下载进度信息
                                    if (e.Data.Contains("Downloading") || e.Data.Contains("Installing"))
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            LoadingMessage = $"正在下载安装CNOCR: {e.Data}";
                                        });
                                    }
                                }
                            };
                            
                            // 捕获错误
                            process.ErrorDataReceived += (sender, e) => 
                            {
                                if (!string.IsNullOrEmpty(e.Data))
                                {
                                    Debug.WriteLine($"CNOCR安装错误: {e.Data}");
                                    LogHelper.LogError($"CNOCR安装错误: {e.Data}");
                                    error.AppendLine(e.Data);
                                }
                            };
                            
                            // 保存进程引用以便取消
                            _cnocrInstallProcess = process;
                            
                            // 启动进程
                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            
                            // 等待进程完成
                            await process.WaitForExitAsync();
                            
                            // 停止计时器
                            progressTimer.Stop();
                            
                            // 检查退出代码
                            if (process.ExitCode == 0)
                            {
                                // 安装成功
                                LoadingMessage = "CNOCR安装成功，正在检查环境...";
                                StatusMessage = "CNOCR安装成功";
                                
                                // 更新进度为100%
                                UpdateProgress(100);
                                
                                // 重新检查环境
                                await CheckEnvironment();
                            }
                            else
                            {
                                // 安装失败
                                LoadingMessage = $"CNOCR安装失败，退出代码: {process.ExitCode}";
                                StatusMessage = "CNOCR安装失败";
                                LogHelper.LogError($"CNOCR安装失败，退出代码: {process.ExitCode}");
                                
                                string errorMessage = error.ToString();
                                if (string.IsNullOrEmpty(errorMessage))
                                {
                                    errorMessage = "未知错误，请检查网络连接或Python环境";
                                }
                                
                                // 在UI线程显示错误
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    MessageBoxHelper.ShowError($"安装CNOCR失败: {errorMessage}");
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        progressTimer.Stop();
                        throw ex; // 重新抛出异常以便在外层处理
                    }
                }
            }
            catch (Exception ex)
            {
                LoadingMessage = $"安装CNOCR时出错: {ex.Message}";
                StatusMessage = $"安装CNOCR时出错: {ex.Message}";
                
                // 在UI线程显示错误
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBoxHelper.ShowError($"安装CNOCR时出错: {ex.Message}");
                    LogHelper.LogError($"安装CNOCR时出错: {ex.Message}");
                });
            }
            finally
            {
                // 清理资源
                _cnocrInstallProcess = null;
                
                IsLoading = false;
                IsDownloadingCnocr = false;
                LoadingMessage = string.Empty;
                
                // 刷新命令状态
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
                
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        /// <summary>
        /// 取消CNOCR下载
        /// </summary>
        public void CancelCnocrDownload()
        {
            try
            {
                // 标记取消状态
                IsDownloadingCnocr = false;
                LoadingMessage = "正在取消CNOCR安装...";
                StatusMessage = "正在取消CNOCR安装...";
                
                // 终止安装进程
                if (_cnocrInstallProcess != null)
                {
                    try
                    {
                        if (!_cnocrInstallProcess.HasExited)
                        {
                            // 尝试终止进程
                            _cnocrInstallProcess.Kill(true); // 递归终止所有子进程
                            
                            // 等待进程终止
                            if (!_cnocrInstallProcess.WaitForExit(2000))
                            {
                                Debug.WriteLine("无法终止CNOCR安装进程");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"终止CNOCR安装进程时出错: {ex.Message}");
                        LogHelper.LogError($"终止CNOCR安装进程时出错: {ex.Message}", ex);
                    }
                    finally
                    {
                        _cnocrInstallProcess = null;
                    }
                }
                
                // 清理CNOCR包
                Task.Run(async () => 
                {
                    try
                    {
                        await CleanPreviousCnocrInstallation();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"清理CNOCR包时出错: {ex.Message}");
                    }
                });
                
                // 释放所有网络连接资源
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 重置所有状态
                IsLoading = false;
                IsDownloadingCnocr = false;
                LoadingMessage = string.Empty;
                StatusMessage = "CNOCR安装已取消";
                
                // 在UI线程刷新命令状态
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
                
                // 最终清理
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"取消CNOCR安装时出错: {ex.Message}");
                LogHelper.LogError($"取消CNOCR安装时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理之前的CNOCR安装
        /// </summary>
        private async Task CleanPreviousCnocrInstallation()
        {
            try
            {
                // 如果Python未安装，无需清理
                if (IsPythonInstalled != true)
                    return;
                
                LoadingMessage = "正在清理旧的CNOCR包...";
                
                // 检查是否安装了cnocr
                bool isCnocrInstalled = await _pythonService.CheckCnocrInstalled();
                
                if (isCnocrInstalled)
                {
                    // 获取Python路径
                    string pythonPath = await GetPythonPath();
                    
                    // 执行pip卸载命令
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{pythonPath}\" -m pip uninstall -y cnocr",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    using (var process = new Process { StartInfo = startInfo })
                    {
                        process.Start();
                        await process.WaitForExitAsync();
                    }
                    
                    // 清理.cnocr目录和AppData中的cnocr目录（如果存在）
                    string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    
                    string[] cnocrDirs = new[]
                    {
                        Path.Combine(userDir, ".cnocr"),
                        Path.Combine(appDataDir, "cnocr")
                    };
                    
                    foreach (string dir in cnocrDirs)
                    {
                        if (Directory.Exists(dir))
                        {
                            try
                            {
                                // 确保没有打开的文件句柄
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                
                                // 递归删除目录
                                Directory.Delete(dir, true);
                                
                                // 等待删除完成
                                await Task.Delay(100);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"删除CNOCR目录失败: {dir}, 错误: {ex.Message}");
                                LogHelper.LogError($"删除CNOCR目录失败, 错误: {ex.Message}");
                                // 继续处理，不要中断流程
                            }
                        }
                    }
                    
                    // 强制进行一次垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理CNOCR安装时出错: {ex.Message}");
                LogHelper.LogError($"清理CNOCR安装时出错: {ex.Message}");
                // 继续处理，不要中断流程
            }
        }
        
        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgress(int progress)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 通过反射获取当前打开的OCR窗口并更新进度
                var windows = Application.Current.Windows.OfType<Window>();
                var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                if (ocrWindow != null && ocrWindow.DataContext != null)
                {
                    var progressProperty = ocrWindow.DataContext.GetType().GetProperty("Progress");
                    if (progressProperty != null)
                    {
                        progressProperty.SetValue(ocrWindow.DataContext, progress);
                    }
                }
            });
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
            
            // 如果正在下载Python，取消下载
            if (IsDownloadingPython)
            {
                CancelPythonDownload();
            }
            
            // 如果正在下载CNOCR，取消下载
            if (IsDownloadingCnocr)
            {
                CancelCnocrDownload();
            }
            
            // 释放资源
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// 重置窗口关闭状态
        /// </summary>
        public void ResetWindowClosed()
        {
            _isWindowClosed = false;
            
            // 重新检查环境状态
            Task.Run(async () => 
            {
                try
                {
                    // 轻量级检查，只更新Python安装状态
                    IsPythonInstalled = await _pythonService.CheckPythonInstalled();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"重置窗口状态时检查环境出错: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 更新OCR模型安装状态
        /// </summary>
        /// <param name="logMessages">是否记录状态变更消息</param>
        /// <returns>检查完成的任务</returns>
        public async Task UpdateOcrModelStatus(bool logMessages = false)
        {
            try
            {
                // 记录当前状态
                bool? previousStatus = IsOcrModelInstalled;
                
                // 检查OCR模型是否安装
                if (logMessages) SetStatusMessage("正在检查OCR模型...");
                var isModelInstalled = await _pythonService.CheckOcrModelInstalled();
                
                // 如果模型不存在，尝试从内置Assets复制
                if (!isModelInstalled)
                {
                    if (logMessages) SetStatusMessage("OCR模型未安装，尝试从内置资源复制...");
                    bool copySuccess = await _pythonService.CopyModelFilesFromAssets();
                    
                    if (copySuccess)
                    {
                        if (logMessages) SetStatusMessage("成功从内置资源复制OCR模型");
                        // 重新检查模型状态
                        isModelInstalled = await _pythonService.CheckOcrModelInstalled();
                    }
                    else if (logMessages)
                    {
                        SetStatusMessage("无法从内置资源复制OCR模型");
                    }
                }
                
                // 更新模型状态
                IsOcrModelInstalled = isModelInstalled;
                
                // 如果状态变化并且需要记录消息
                if (logMessages && previousStatus != IsOcrModelInstalled)
                {
                    if (IsOcrModelInstalled == true && previousStatus != true)
                    {
                        // 模型已下载完成或成功复制
                        string message = previousStatus == null ? "OCR模型已成功安装" : "OCR模型已成功复制";
                        StatusMessage = message;
                        
                        // 显示提示
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var windows = Application.Current.Windows.OfType<Window>();
                            var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                            if (ocrWindow != null && !_isWindowClosed)
                            {
                                MessageBoxHelper.ShowInfo($"{message}，现在可以进行使用了。");
                            }
                        });
                    }
                    else if (IsOcrModelInstalled == false && previousStatus != false)
                    {
                        // 模型未安装
                        StatusMessage = "OCR模型未安装";
                    }
                }
                else if (logMessages)
                {
                    SetStatusMessage($"OCR模型{(IsOcrModelInstalled == true ? "已安装" : "未安装")}");
                }
            }
            catch (Exception ex)
            {
                IsOcrModelInstalled = false;
                StatusMessage = $"检查OCR模型时出错：{ex.Message}";
            }
        }

        /// <summary>
        /// 下载Python 3.12.9
        /// </summary>
        /// <returns>下载任务</returns>
        public async Task DownloadPython()
        {
            try
            {
                // 确保之前的下载已完全取消和清理
                if (IsDownloadingPython)
                {
                    LoadingMessage = "正在清理之前的下载资源...";
                    StatusMessage = "正在清理之前的下载资源...";
                    CancelPythonDownload();
                    
                    // 等待资源完全清理
                    await Task.Delay(1000);
                    
                    // 强制GC回收资源
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                
                // 设置状态
                IsLoading = true;
                IsDownloadingPython = true;
                LoadingMessage = "正在下载Python 3.12.9...";
                
                // 确保UI更新
                await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                
                // 创建临时目录
                string tempDir = Path.Combine(Path.GetTempPath(), "TicketAssist");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                // Python安装包的URL和目标路径
                string pythonUrl = "https://www.python.org/ftp/python/3.12.9/python-3.12.9-amd64.exe";
                string fileName = Path.GetFileName(pythonUrl);
                string targetPath = Path.Combine(tempDir, fileName);
                
                // 如果目标文件存在但可能损坏或被锁定，尝试删除
                if (File.Exists(targetPath))
                {
                    try
                    {
                        // 尝试打开文件，验证是否可访问
                        using (var fileStream = new FileStream(targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            // 文件可以访问，关闭流
                            fileStream.Close();
                        }
                        
                        // 文件完好且可访问，继续使用
                        LoadingMessage = "已找到Python安装程序，准备安装...";
                        PythonInstallerPath = targetPath;
                        await InstallPython(targetPath);
                        return;
                    }
                    catch (IOException)
                    {
                        // 文件被锁定或损坏，尝试删除
                        LoadingMessage = "清理旧的下载文件...";
                        try
                        {
                            // 尝试强制删除
                            File.Delete(targetPath);
                            await Task.Delay(500); // 等待删除完成
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"删除已有Python安装文件失败: {ex.Message}");
                            // 使用备用文件名
                            targetPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now.Ticks}.exe");
                        }
                    }
                }
                
                // 设置安装程序路径
                PythonInstallerPath = targetPath;
                
                // 初始化进度
                int progress = 0;
                
                try
                {
                    // 创建取消令牌
                    _downloadCancellationTokenSource = new CancellationTokenSource();
                    var token = _downloadCancellationTokenSource.Token;
                    
                    using (var client = new System.Net.WebClient())
                    {
                        // 注册进度变更事件
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            // 更新进度
                            progress = e.ProgressPercentage;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LoadingMessage = "正在下载Python 3.12.9...";
                                StatusMessage = "正在下载Python...";
                                
                                // 更新进度条
                                var windows = Application.Current.Windows.OfType<Window>();
                                var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                if (ocrWindow != null && ocrWindow.DataContext != null)
                                {
                                    var progressProperty = ocrWindow.DataContext.GetType().GetProperty("Progress");
                                    if (progressProperty != null)
                                    {
                                        progressProperty.SetValue(ocrWindow.DataContext, progress);
                                    }
                                }
                            });
                        };
                        
                        client.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Cancelled)
                            {
                                // 如果取消，删除临时文件
                                try
                                {
                                    if (File.Exists(targetPath))
                                    {
                                        File.Delete(targetPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"删除临时文件时出错: {ex.Message}");
                                }
                            }
                        };
                        
                        // 开始下载
                        await client.DownloadFileTaskAsync(new Uri(pythonUrl), targetPath);
                    }
                    
                    // 检查是否被取消
                    token.ThrowIfCancellationRequested();
                    
                    // 下载完成，准备安装
                    LoadingMessage = "Python下载完成，准备安装...";
                    await InstallPython(targetPath);
                }
                catch (OperationCanceledException)
                {
                    LoadingMessage = "Python下载已取消";
                    StatusMessage = "Python下载已取消";
                    
                    // 清理临时文件
                    try
                    {
                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                            PythonInstallerPath = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"取消后清理临时文件时出错: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    LoadingMessage = $"下载Python时出错: {ex.Message}";
                    StatusMessage = $"下载Python时出错: {ex.Message}";
                    
                    // 在UI线程显示错误
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBoxHelper.ShowError($"下载Python时出错: {ex.Message}");
                        LogHelper.LogError($"下载Python时出错: {ex.Message}", ex);
                    });
                }
            }
            finally
            {
                // 清理资源
                if (_downloadCancellationTokenSource != null)
                {
                    _downloadCancellationTokenSource.Dispose();
                    _downloadCancellationTokenSource = null;
                }
                
                IsLoading = false;
                IsDownloadingPython = false;
                LoadingMessage = string.Empty;
                
                // 刷新命令状态
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        /// <summary>
        /// 取消下载Python
        /// </summary>
        public void CancelPythonDownload()
        {
            try
            {
                // 标记取消状态
                IsDownloadingPython = false;
                LoadingMessage = "正在取消下载...";
                StatusMessage = "正在取消下载...";
                
                // 1. 取消下载任务
                if (_downloadCancellationTokenSource != null)
                {
                    _downloadCancellationTokenSource.Cancel();
                    
                    // 添加短暂延迟，确保取消令牌生效
                    Thread.Sleep(500);
                }
                
                // 2. 终止安装进程
                if (_pythonDownloadProcess != null)
                {
                    try
                    {
                        if (!_pythonDownloadProcess.HasExited)
                        {
                            // 尝试优雅关闭
                            _pythonDownloadProcess.CloseMainWindow();
                            
                            // 等待进程响应关闭请求
                            if (!_pythonDownloadProcess.WaitForExit(1000))
                            {
                                // 如果进程没有及时响应，强制终止
                                _pythonDownloadProcess.Kill();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"终止安装进程时出错: {ex.Message}");
                    }
                    finally
                    {
                        _pythonDownloadProcess = null;
                    }
                }
                
                // 3. 释放所有网络连接资源
                // 通过GC强制回收可能被占用的网络资源
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 4. 清理临时文件
                if (!string.IsNullOrEmpty(PythonInstallerPath))
                {
                    try
                    {
                        // 确保文件不被占用
                        for (int i = 0; i < 3; i++) // 尝试最多3次
                        {
                            if (File.Exists(PythonInstallerPath))
                            {
                                try
                                {
                                    File.Delete(PythonInstallerPath);
                                    break; // 删除成功，跳出循环
                                }
                                catch (IOException)
                                {
                                    // 文件可能被占用，等待短暂时间后重试
                                    Thread.Sleep(500);
                                }
                            }
                            else
                            {
                                break; // 文件不存在，跳出循环
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"删除临时安装文件时出错: {ex.Message}");
                        LogHelper.LogError($"删除临时安装文件时出错: {ex.Message}");
                    }
                    finally
                    {
                        PythonInstallerPath = null;
                    }
                }
                
                // 5. 重置所有状态
                IsLoading = false;
                IsDownloadingPython = false;
                LoadingMessage = string.Empty;
                StatusMessage = "Python下载已取消";
                
                // 6. 释放下载取消令牌
                if (_downloadCancellationTokenSource != null)
                {
                    _downloadCancellationTokenSource.Dispose();
                    _downloadCancellationTokenSource = null;
                }
                
                // 7. 在UI线程刷新命令状态
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
                
                // 8. 清理系统资源
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"取消下载时出错: {ex.Message}");
                LogHelper.LogError("取消下载时出错", ex);
            }
        }

        /// <summary>
        /// 安装Python
        /// </summary>
        /// <param name="installerPath">安装程序路径</param>
        private async Task InstallPython(string installerPath)
        {
            try
            {
                if (!File.Exists(installerPath))
                {
                    LoadingMessage = "找不到Python安装程序";
                    StatusMessage = "找不到Python安装程序";
                    return;
                }
                
                LoadingMessage = "正在安装Python 3.12.9...";
                StatusMessage = "正在安装Python 3.12.9...";
                
                // 创建进度更新的计时器
                System.Timers.Timer progressTimer = null;
                int currentProgress = 0;
                DateTime startTime = DateTime.Now;
                
                try 
                {
                    // 使用计时器定期更新进度
                    progressTimer = new System.Timers.Timer(500); // 每500毫秒更新一次
                    progressTimer.Elapsed += (s, e) => 
                    {
                        // 计算估计的安装进度
                        // Python安装通常需要60-120秒，我们基于时间估算进度
                        TimeSpan elapsed = DateTime.Now - startTime;
                        
                        // 估计总安装时间为90秒，将进度限制在0-100%范围内
                        int estimatedProgress = Math.Min(100, (int)(elapsed.TotalSeconds / 90.0 * 100));
                        
                        // 如果进度变化了，才更新UI
                        if (estimatedProgress > currentProgress)
                        {
                            currentProgress = estimatedProgress;
                            
                            // 在UI线程更新进度和消息
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                LoadingMessage = "正在安装Python 3.12.9...";
                                StatusMessage = "正在安装Python...";
                                
                                // 通过反射获取当前打开的OCR窗口并更新进度
                                var windows = Application.Current.Windows.OfType<Window>();
                                var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                if (ocrWindow != null && ocrWindow.DataContext != null)
                                {
                                    var progressProperty = ocrWindow.DataContext.GetType().GetProperty("Progress");
                                    if (progressProperty != null)
                                    {
                                        progressProperty.SetValue(ocrWindow.DataContext, currentProgress);
                                    }
                                }
                            });
                        }
                    };
                    
                    // 启动计时器
                    progressTimer.Start();
                
                    // 安装参数：静默安装、添加到PATH
                    string arguments = "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0";
                    
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = installerPath;
                        process.StartInfo.Arguments = arguments;
                        process.StartInfo.UseShellExecute = true; // 使用系统Shell执行，会触发UAC提示
                        process.StartInfo.Verb = "runas"; // 请求管理员权限
                        
                        try
                        {
                            _pythonDownloadProcess = process;
                            process.Start();
                            await process.WaitForExitAsync();
                            
                            // 安装完成，确保进度显示100%
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                LoadingMessage = "Python安装完成";
                                StatusMessage = "Python安装完成";
                                
                                // 通过反射获取当前打开的OCR窗口并更新进度为100%
                                var windows = Application.Current.Windows.OfType<Window>();
                                var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                                if (ocrWindow != null && ocrWindow.DataContext != null)
                                {
                                    var progressProperty = ocrWindow.DataContext.GetType().GetProperty("Progress");
                                    if (progressProperty != null)
                                    {
                                        progressProperty.SetValue(ocrWindow.DataContext, 100);
                                    }
                                }
                            });
                            
                            if (process.ExitCode == 0)
                            {
                                // 安装成功
                                LoadingMessage = "Python安装成功，正在检查环境...";
                                StatusMessage = "Python安装成功";
                                
                                // 重新检查环境
                                await CheckEnvironment();
                            }
                            else
                            {
                                // 安装失败
                                LoadingMessage = $"Python安装失败，退出代码: {process.ExitCode}";
                                StatusMessage = "Python安装失败";
                                
                                // 在UI线程显示错误
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    MessageBoxHelper.ShowError("无法安装Python，请检查您的用户权限。");
                                    LogHelper.LogError("无法安装Python，可能是用户权限不足。");
                                });
                            }
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            // 用户可能取消了UAC提示
                            LoadingMessage = "Python安装被取消";
                            StatusMessage = "Python安装被取消";
                            
                            // 在UI线程显示错误
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                MessageBoxHelper.ShowError("无法安装Python，请检查您的用户权限。");
                            });
                        }
                        finally
                        {
                            _pythonDownloadProcess = null;
                        }
                    }
                }
                finally
                {
                    // 停止并释放进度计时器
                    if (progressTimer != null)
                    {
                        progressTimer.Stop();
                        progressTimer.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LoadingMessage = $"安装Python时出错: {ex.Message}";
                StatusMessage = $"安装Python时出错: {ex.Message}";
                
                // 在UI线程显示错误
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBoxHelper.ShowError($"安装Python时出错: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// 获取Python可执行文件的路径
        /// </summary>
        private async Task<string> GetPythonPath()
        {
            // 首先检查_pythonService中是否有Python路径
            var pythonPathField = _pythonService.GetType().GetField("_pythonExePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pythonPathField != null)
            {
                string pythonPath = (string)pythonPathField.GetValue(_pythonService);
                if (!string.IsNullOrEmpty(pythonPath))
                {
                    return pythonPath;
                }
            }
            
            // 如果无法获取路径，返回默认的python命令
            return "python";
        }

        /// <summary>
        /// 下载安装CNSTD文本检测模块
        /// </summary>
        /// <param name="mirror">镜像源地址，默认为阿里云镜像</param>
        public async Task DownloadInstallCnstd(string mirror = "https://mirrors.aliyun.com/pypi/simple")
        {
            try
            {
                IsDownloadingCnocr = true;
                
                // 清理之前可能的过程
                await CleanPreviousCnocrInstallation();
                
                // 创建取消令牌
                _downloadCancellationTokenSource = new CancellationTokenSource();

                // 更新状态
                StatusMessage = "正在安装文本检测模块CNSTD...";
                LoadingMessage = "正在准备下载...";
                
                // 通过反射获取当前打开的OCR窗口并更新进度为0
                Application.Current.Dispatcher.Invoke(() => 
                {
                    var windows = Application.Current.Windows.OfType<Window>();
                    var ocrWindow = windows.FirstOrDefault(w => w.GetType().Name == "OcrTicketWindow");
                    if (ocrWindow != null && ocrWindow.DataContext != null)
                    {
                        var progressProperty = ocrWindow.DataContext.GetType().GetProperty("Progress");
                        if (progressProperty != null)
                        {
                            progressProperty.SetValue(ocrWindow.DataContext, 0);
                        }
                    }
                });
                
                // 启动cnstd安装进程
                _cnocrInstallProcess = new Process();
                _cnocrInstallProcess.StartInfo.FileName = (await GetPythonPath()) ?? "python";
                _cnocrInstallProcess.StartInfo.Arguments = $"-m pip install cnstd -i {mirror} --upgrade";
                _cnocrInstallProcess.StartInfo.UseShellExecute = false;
                _cnocrInstallProcess.StartInfo.RedirectStandardOutput = true;
                _cnocrInstallProcess.StartInfo.RedirectStandardError = true;
                _cnocrInstallProcess.StartInfo.CreateNoWindow = true;
                
                // 显式设置编码，避免乱码
                _cnocrInstallProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _cnocrInstallProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                
                // 创建缓冲区
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();
                
                // 添加输出处理
                _cnocrInstallProcess.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        
                        // 更新状态
                        string simplifiedStatus = "正在安装CNSTD...";
                        
                        // 尝试从输出解析进度
                        if (e.Data.Contains("Downloading"))
                        {
                            simplifiedStatus = "正在下载CNSTD...";
                            UpdateProgress(30);
                        }
                        else if (e.Data.Contains("Processing"))
                        {
                            simplifiedStatus = "正在处理依赖...";
                            UpdateProgress(60);
                        }
                        else if (e.Data.Contains("Installing") || e.Data.Contains("installed"))
                        {
                            simplifiedStatus = "正在安装CNSTD...";
                            UpdateProgress(80);
                        }
                        else if (e.Data.Contains("Successfully"))
                        {
                            simplifiedStatus = "CNSTD安装成功!";
                            UpdateProgress(100);
                        }
                        
                        // 更新界面
                        if (!_isWindowClosed)
                        {
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                LoadingMessage = simplifiedStatus;
                            });
                        }
                    }
                };
                
                _cnocrInstallProcess.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        Debug.WriteLine($"CNSTD安装错误: {e.Data}");
                    }
                };
                
                // 启动进程
                UpdateProgress(10);
                _cnocrInstallProcess.Start();
                _cnocrInstallProcess.BeginOutputReadLine();
                _cnocrInstallProcess.BeginErrorReadLine();
                
                // 等待安装完成，同时允许取消
                await Task.Run(async () =>
                {
                    while (!_cnocrInstallProcess.HasExited)
                    {
                        if (_downloadCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            try
                            {
                                if (!_cnocrInstallProcess.HasExited)
                                {
                                    _cnocrInstallProcess.Kill();
                                }
                            }
                            catch { }
                            break;
                        }
                        
                        await Task.Delay(100);
                    }
                });
                
                // 处理安装结果
                if (!_downloadCancellationTokenSource.Token.IsCancellationRequested && _cnocrInstallProcess.ExitCode == 0)
                {
                    // 安装成功
                    StatusMessage = "CNSTD安装成功";
                    LoadingMessage = "CNSTD安装成功";
                    
                    // 等待片刻，让用户看到成功消息
                    await Task.Delay(1000);
                    
                    // 如果窗口仍然打开，更新环境检测
                    if (!_isWindowClosed)
                    {
                        // 重新检查环境
                        await CheckEnvironment();
                        
                        // 显示提示
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxHelper.ShowInfo("CNSTD文本检测模块安装完成！");
                            LogHelper.LogInfo("CNSTD文本检测模块安装完成！");
                        });
                    }
                }
                else if (_downloadCancellationTokenSource.Token.IsCancellationRequested)
                {
                    // 用户取消
                    StatusMessage = "CNSTD安装已取消";
                    LoadingMessage = "安装已取消";
                }
                else
                {
                    // 安装失败
                    string errorMsg = errorBuilder.ToString();
                    
                    // 记录详细错误
                    Debug.WriteLine($"CNSTD安装失败: {errorMsg}");
                    
                    StatusMessage = "CNSTD安装失败";
                    LoadingMessage = "安装失败";
                    
                    // 显示错误消息
                    if (!_isWindowClosed)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxHelper.ShowError($"CNSTD安装失败，请检查网络连接或尝试手动安装。\n\n错误信息: {errorMsg}");
                            LogHelper.LogError($"CNSTD安装失败，请检查网络连接或尝试手动安装。\n\n错误信息: {errorMsg}");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                StatusMessage = "CNSTD安装失败";
                LoadingMessage = "安装出错";
                
                Debug.WriteLine($"CNSTD安装过程中发生异常: {ex.Message}");
                LogHelper.LogError($"CNSTD安装过程中发生错误: {ex.Message}");
                // 显示错误消息
                if (!_isWindowClosed)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxHelper.ShowError($"CNSTD安装过程中发生错误: {ex.Message}");

                    });
                }
            }
            finally
            {
                // 清理资源
                IsDownloadingCnocr = false;
                
                try
                {
                    if (_cnocrInstallProcess != null && !_cnocrInstallProcess.HasExited)
                    {
                        _cnocrInstallProcess.Kill();
                    }
                }
                catch { }
                
                _cnocrInstallProcess = null;
                
                if (_downloadCancellationTokenSource != null)
                {
                    _downloadCancellationTokenSource.Dispose();
                    _downloadCancellationTokenSource = null;
                }
            }
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