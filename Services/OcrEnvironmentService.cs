using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TA_WPF.Utils;
using System.Windows.Threading;
using System.Net.Http;

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
        private bool _isInstallingPython;
        private int _pythonInstallProgress;

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
        /// 是否正在安装Python
        /// </summary>
        public bool IsInstallingPython
        {
            get => _isInstallingPython;
            private set
            {
                if (_isInstallingPython != value)
                {
                    _isInstallingPython = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Python安装进度
        /// </summary>
        public int PythonInstallProgress
        {
            get => _pythonInstallProgress;
            private set
            {
                if (_pythonInstallProgress != value)
                {
                    _pythonInstallProgress = value;
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

                // 将所有检测状态设置为null（表示检测中）
                IsPythonInstalled = null;
                IsCnocrInstalled = null;
                IsOcrModelInstalled = null;
                
                // 确保UI更新以显示检测中状态
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
                IsEnvironmentReady = IsPythonInstalled == true && IsCnocrInstalled == true;

                // 仅在窗口未关闭时显示消息框
                if (!_isWindowClosed)
                {
                    // 在UI线程显示环境检测结果
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (IsEnvironmentReady)
                        {
                            if (IsOcrModelInstalled != true)
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
                            if (IsPythonInstalled != true) errorMessage += "\n- Python未安装";
                            if (IsCnocrInstalled != true) errorMessage += "\n- CNOCR未安装";
                            
                            MessageBoxHelper.ShowWarning($"{errorMessage}\n\n请先安装所需环境再使用OCR识别功能。");
                        }
                    });
                }
                
                // 触发环境检测完成事件
                EnvironmentCheckCompleted?.Invoke(this, IsEnvironmentReady);
            }
            catch (Exception ex)
            {
                // 检测失败时，将所有状态设置为false
                IsPythonInstalled = false;
                IsCnocrInstalled = false;
                IsOcrModelInstalled = false;
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
        /// 安装Python 3.12.9
        /// </summary>
        /// <returns>安装完成的任务</returns>
        public async Task InstallPython()
        {
            if (IsInstallingPython)
                return;

            try
            {
                IsInstallingPython = true;
                PythonInstallProgress = 0;
                LoadingMessage = "准备安装Python 3.12.9...";

                // 创建临时目录
                string tempDir = Path.Combine(Path.GetTempPath(), "PythonInstall");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                // Python安装程序URL
                string pythonInstallerUrl = "https://www.python.org/ftp/python/3.12.9/python-3.12.9-amd64.exe";
                string installerPath = Path.Combine(tempDir, "python-3.12.9-amd64.exe");

                // 下载Python安装程序
                using (var httpClient = new HttpClient())
                {
                    LoadingMessage = "正在下载Python 3.12.9安装程序...";
                    
                    // 创建进度报告处理
                    var progress = new Progress<float>(percent =>
                    {
                        PythonInstallProgress = (int)(percent * 50); // 下载占进度的50%
                        LoadingMessage = $"正在下载Python 3.12.9安装程序... {PythonInstallProgress}%";
                    });

                    // 下载文件
                    await DownloadFileWithProgressAsync(httpClient, pythonInstallerUrl, installerPath, progress);
                }

                // 安装Python
                LoadingMessage = "正在安装Python 3.12.9...";
                PythonInstallProgress = 50;

                // 安装参数 (静默安装，添加到PATH，预编译标准库)
                string arguments = "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0 Include_doc=0 Shortcuts=0";
                
                using (var process = new Process())
                {
                    process.StartInfo.FileName = installerPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    
                    // 启动进程
                    process.Start();
                    
                    // 模拟安装进度
                    for (int i = 50; i <= 95; i += 5)
                    {
                        PythonInstallProgress = i;
                        LoadingMessage = $"正在安装Python 3.12.9... {i}%";
                        await Task.Delay(1000);
                        
                        // 检查进程是否已结束
                        if (process.HasExited)
                            break;
                    }
                    
                    // 等待进程结束
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Python安装失败，退出代码: {process.ExitCode}");
                    }
                }

                // 清理临时文件
                try
                {
                    if (File.Exists(installerPath))
                        File.Delete(installerPath);
                    
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    LogHelper.LogWarning($"清理临时文件时出错: {ex.Message}");
                }

                PythonInstallProgress = 100;
                LoadingMessage = "Python 3.12.9安装完成！正在安装CNOCR包...";

                // 安装CNOCR包
                await InstallCnocr();

                // 重新检查环境
                await CheckEnvironment();

                MessageBoxHelper.ShowInfo("Python 3.12.9已成功安装！");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("安装Python时出错", ex);
                MessageBoxHelper.ShowError($"安装Python时出错: {ex.Message}");
            }
            finally
            {
                IsInstallingPython = false;
                LoadingMessage = string.Empty;
            }
        }
        
        /// <summary>
        /// 安装CNOCR包
        /// </summary>
        private async Task InstallCnocr()
        {
            try
            {
                LoadingMessage = "正在安装CNOCR包...";
                
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "python";
                    process.StartInfo.Arguments = "-m pip install --upgrade pip cnocr";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"CNOCR安装失败: {error}");
                    }
                }
                
                LoadingMessage = "CNOCR包安装完成！";
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("安装CNOCR包时出错", ex);
                MessageBoxHelper.ShowWarning($"安装CNOCR包时出错: {ex.Message}\n\n请手动安装CNOCR包。");
            }
        }

        /// <summary>
        /// 带进度的文件下载
        /// </summary>
        private async Task DownloadFileWithProgressAsync(HttpClient client, string url, string destinationPath, IProgress<float> progress)
        {
            // 获取文件大小
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            
            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var downloadStream = await client.GetStreamAsync(url))
            {
                var buffer = new byte[8192];
                var bytesRead = 0;
                var totalBytesRead = 0L;
                
                while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes > 0 && progress != null)
                    {
                        var progressPercentage = (float)totalBytesRead / totalBytes;
                        progress.Report(progressPercentage);
                    }
                }
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
        /// 更新OCR模型安装状态
        /// </summary>
        /// <returns>检查完成的任务</returns>
        public async Task UpdateOcrModelStatus()
        {
            try
            {
                // 检查OCR模型是否安装
                IsOcrModelInstalled = await _pythonService.CheckOcrModelInstalled();
            }
            catch (Exception ex)
            {
                StatusMessage = $"检查OCR模型时出错：{ex.Message}";
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